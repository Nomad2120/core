using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OSI.Core.Models;
using OSI.Core.Models.Banks;
using OSI.Core.Models.Requests;
using OSI.Core.Models.Responses;
using OSI.Core.Swagger;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace OSI.PaymentService.Controllers
{
    [Route("api")]
    [ApiController]
    public class PaymentServiceController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly JsonSerializerOptions jsonSerializerOptions;

        public PaymentServiceController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            this.configuration = configuration;
            this.httpClientFactory = httpClientFactory;
            jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        private static string token = null;
        private static readonly object tokenLock = new();

        private string GetToken()
        {
            if (token == null)
            {
                lock (tokenLock)
                {
                    if (token == null)
                    {
                        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(configuration["Token:Secret"]));
                        SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

                        var claims = new List<Claim>
                        {
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                            new Claim(ClaimTypes.UserData, "PaymentService"),
                            new Claim(ClaimTypes.NameIdentifier, "PaymentService"),
                            new Claim(ClaimTypes.Name, "PaymentService"),
                            new Claim(ClaimTypes.Role, "PAYMENTSERVICE"),
                        };

                        JwtSecurityToken jwtToken = new(
                            issuer: configuration["Token:Issuer"],
                            audience: configuration["Token:Issuer"],
                            claims: claims,
                            expires: DateTime.Now.AddYears(10),
                            signingCredentials: creds);

                        token = new JwtSecurityTokenHandler().WriteToken(jwtToken);
                    }
                }
            }
            return token;
        }

        private async Task<ApiResponse> CheckBankCode(string token, string bankCode, string ip)
        {
            ApiResponse apiResponse = new();
            if (string.IsNullOrWhiteSpace(ip))
            {
                ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            }
            using HttpClient client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var httpResponseMessage = await client.GetAsync(configuration["CoreApiUrl"] + $"contracts/check?bankCode={HttpUtility.UrlEncode(bankCode)}&ip={HttpUtility.UrlEncode(ip)}");
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                apiResponse = await httpResponseMessage.Content.ReadFromJsonAsync<ApiResponse>();
            }
            else
            {
                apiResponse.Code = 2;
                apiResponse.Message = "Ошибка при соединении с Core API";
            }
            return apiResponse;
        }

        /// <summary>
        /// Получение информации об абоненте
        /// </summary>
        /// <param name="bankCode">Код банка</param>
        /// <param name="abonentNum">Номер абонента</param>
        /// <returns>Информация об абоненте</returns>
        [HttpGet("{bankCode}/abonent-info/{abonentNum}")]
        public async Task<ApiResponse<AbonentInfoResponse>> GetAbonentInfo(string bankCode, string abonentNum, [SwaggerIgnore][FromQuery] string ip = null)
        {
            ApiResponse<AbonentInfoResponse> apiResponse = new();
            try
            {
                string token = GetToken();
                apiResponse = (await CheckBankCode(token, bankCode, ip)).ToApiResponse<AbonentInfoResponse>();
                if (apiResponse.Code == 0)
                {
                    using HttpClient client = httpClientFactory.CreateClient();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var abonentHttpResponseMessage = await client.GetAsync(configuration["CoreApiUrl"] + $"abonents/payment-service/{abonentNum}");
                    if (abonentHttpResponseMessage.IsSuccessStatusCode)
                    {
                        var abonentResponse = await abonentHttpResponseMessage.Content.ReadFromJsonAsync<ApiResponse<Core.Models.Db.Abonent>>(jsonSerializerOptions);
                        apiResponse.From(abonentResponse);
                        if (apiResponse.Code == 0)
                        {
                            var abonentId = abonentResponse.Result.Id;
                            var httpResponseMessage = await client.GetAsync(configuration["CoreApiUrl"] + $"transactions/saldo/{abonentId}");
                            if (httpResponseMessage.IsSuccessStatusCode)
                            {
                                var endSaldoResponse = await httpResponseMessage.Content.ReadFromJsonAsync<ApiResponse<EndSaldoResponse>>();
                                apiResponse.From(endSaldoResponse);
                                apiResponse.Result = new AbonentInfoResponse(abonentNum, abonentResponse.Result, endSaldoResponse.Result);
                            }
                            else
                            {
                                apiResponse.Code = 2;
                                apiResponse.Message = "Ошибка при соединении с Core API";
                            }
                        }
                    }
                    else
                    {
                        apiResponse.Code = 2;
                        apiResponse.Message = "Ошибка при соединении с Core API";
                    }
                }
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex, 1);
            }
            return apiResponse;
        }

        /// <summary>
        /// Получение PDF квитанции абонента
        /// </summary>
        /// <param name="bankCode">Код банка</param>
        /// <param name="abonentNum">Номер абонента</param>
        /// <returns>PDF квитанция в base64</returns>
        [HttpGet("{bankCode}/abonent-invoice-pdf/{abonentNum}")]
        public async Task<ApiResponse<string>> GetAbonentPdf(string bankCode, string abonentNum, [SwaggerIgnore][FromQuery] string ip = null)
        {
            ApiResponse<string> apiResponse = new();
            try
            {
                string token = GetToken();
                apiResponse.From(await CheckBankCode(token, bankCode, ip));
                if (apiResponse.Code == 0)
                {
                    using HttpClient client = httpClientFactory.CreateClient();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var abonentHttpResponseMessage = await client.GetAsync(configuration["CoreApiUrl"] + $"abonents/payment-service/{abonentNum}");
                    if (abonentHttpResponseMessage.IsSuccessStatusCode)
                    {
                        var abonentResponse = await abonentHttpResponseMessage.Content.ReadFromJsonAsync<ApiResponse<Core.Models.Db.Abonent>>(jsonSerializerOptions);
                        apiResponse.From(abonentResponse);
                        if (apiResponse.Code == 0)
                        {
                            var abonentId = abonentResponse.Result.Id;
                            var httpResponseMessage = await client.GetAsync(configuration["CoreApiUrl"] + $"reports/abonents-invoice-pdf-on-current-date/{abonentId}");
                            if (httpResponseMessage.IsSuccessStatusCode)
                            {
                                apiResponse.Result = Convert.ToBase64String(await httpResponseMessage.Content.ReadAsByteArrayAsync());
                            }
                            else
                            {
                                try
                                {
                                    apiResponse.From(await httpResponseMessage.Content.ReadFromJsonAsync<ApiResponse>());
                                }
                                catch
                                {
                                    apiResponse.Code = 2;
                                    apiResponse.Message = "Ошибка при соединении с Core API";
                                }
                            }
                        }
                    }
                    else
                    {
                        apiResponse.Code = 2;
                        apiResponse.Message = "Ошибка при соединении с Core API";
                    }
                }
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex, 1);
            }
            return apiResponse;
        }

        //[ApiExplorerSettings(IgnoreApi = true)]
        //[HttpGet("{bankCode}/abonent-infos/{abonentNumOrPhone}")]
        //public async Task<ApiResponse<IEnumerable<AbonentInfoResponse>>> GetAbonentInfos(string bankCode, string abonentNumOrPhone)
        //{
        //    ApiResponse<IEnumerable<AbonentInfoResponse>> apiResponse = new();
        //    try
        //    {
        //        apiResponse.From(await CheckBankCode(bankCode));
        //        if (apiResponse.Code == 0)
        //        {
        //            bool isPhone = abonentNumOrPhone.Length == 10 && abonentNumOrPhone.StartsWith("7");
        //            using HttpClient client = httpClientFactory.CreateClient();
        //            string token = GetToken();
        //            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        //            List<int> abonentNums = new();
        //            if (!isPhone)
        //            {
        //                abonentNums.Add(int.TryParse(abonentNumOrPhone, out int abonentNum) ? abonentNum : 0);
        //            }
        //            else
        //            {
        //                var abonentsByPhoneHttpResponseMessage = await client.GetAsync(configuration["CoreApiUrl"] + $"abonents/phone/{abonentNumOrPhone}");
        //                if (abonentsByPhoneHttpResponseMessage.IsSuccessStatusCode)
        //                {
        //                    var abonentsByPhoneResponse = await abonentsByPhoneHttpResponseMessage.Content.ReadFromJsonAsync<ApiResponse<List<int>>>();
        //                    apiResponse.From(abonentsByPhoneResponse);
        //                    if (apiResponse.Code == 0)
        //                    {
        //                        abonentNums = abonentsByPhoneResponse.Result;
        //                    }
        //                }
        //                else
        //                {
        //                    apiResponse.Code = 2;
        //                    apiResponse.Message = "Ошибка при соединении с Core API";
        //                }
        //            }
        //            List<AbonentInfoResponse> result = new();
        //            foreach (var abonentNum in abonentNums)
        //            {
        //                var httpResponseMessage = await client.GetAsync(configuration["CoreApiUrl"] + $"transactions/saldo/{abonentNum}");
        //                if (httpResponseMessage.IsSuccessStatusCode)
        //                {
        //                    var endSaldoResponse = await httpResponseMessage.Content.ReadFromJsonAsync<ApiResponse<EndSaldoResponse>>();
        //                    apiResponse.From(endSaldoResponse);
        //                    result.Add(new AbonentInfoResponse(abonentNum, "", endSaldoResponse: endSaldoResponse.Result));
        //                }
        //                else
        //                {
        //                    apiResponse.Code = 2;
        //                    apiResponse.Message = "Ошибка при соединении с Core API";
        //                    apiResponse.Result = null;
        //                    break;
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        apiResponse.FromEx(ex, 1);
        //    }
        //    return apiResponse;
        //}

        /// <summary>
        /// Создание платежа
        /// </summary>
        /// <param name="bankCode">Код банка</param>
        /// <param name="request">Запрос на создание платежа</param>
        /// <returns>Ответ на создание платежа</returns>
        [HttpPost("{bankCode}/payment")]
        public async Task<ApiResponse<CreatePaymentResponse>> CreatePayment(string bankCode, CreatePaymentRequest request, [SwaggerIgnore][FromQuery] string ip = null)
        {
            ApiResponse<CreatePaymentResponse> apiResponse = new();
            try
            {
                string token = GetToken();
                apiResponse = (await CheckBankCode(token, bankCode, ip)).ToApiResponse<CreatePaymentResponse>();
                if (apiResponse.Code == 0)
                {
                    using HttpClient client = httpClientFactory.CreateClient();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    var httpResponseMessage = await client.PostAsJsonAsync(configuration["CoreApiUrl"] + $"transactions/payment?bankCode={bankCode}", request);
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        apiResponse = await httpResponseMessage.Content.ReadFromJsonAsync<ApiResponse<CreatePaymentResponse>>();
                    }
                    else
                    {
                        apiResponse.Code = 2;
                        apiResponse.Message = "Ошибка при соединении с Core API";
                    }
                }
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex, 1);
            }
            return apiResponse;
        }

        /// <summary>
        /// Получение списка необработанных платежей за указанную дату
        /// </summary>
        /// <param name="bankCode">Код банка</param>
        /// <param name="date">Дата в формате yyyy-MM-dd</param>
        /// <returns>Список необработанных платежей</returns>
        [HttpGet("{bankCode}/not-processed-payments/{date}")]
        public async Task<ApiResponse<List<NotProcessedPayment>>> GetNotProcessedPayments(string bankCode, DateTime date, [SwaggerIgnore][FromQuery] string ip = null)
        {
            ApiResponse<List<NotProcessedPayment>> apiResponse = new();
            try
            {
                string token = GetToken();
                apiResponse = (await CheckBankCode(token, bankCode, ip)).ToApiResponse<List<NotProcessedPayment>>();
                if (apiResponse.Code == 0)
                {
                    using HttpClient client = httpClientFactory.CreateClient();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    var httpResponseMessage = await client.GetAsync(configuration["CoreApiUrl"] + $"paymentOrders/notProcessedPayments?bankCode={bankCode}&date={date:O}");
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        apiResponse = await httpResponseMessage.Content.ReadFromJsonAsync<ApiResponse<List<NotProcessedPayment>>>();
                    }
                    else
                    {
                        apiResponse.Code = 2;
                        apiResponse.Message = "Ошибка при соединении с Core API";
                    }
                }
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex, 1);
            }
            return apiResponse;
        }

        /// <summary>
        /// Передача банком статуса успешной сверки платежей и их обработка за указанную дату
        /// </summary>
        /// <param name="bankCode">Код банка</param>
        /// <param name="date">Дата в формате yyyy-MM-dd</param>
        /// <returns></returns>
        [HttpPost("{bankCode}/process-payments/{date}")]
        public async Task<ApiResponse> ProcessPayments(string bankCode, DateTime date, [SwaggerIgnore][FromQuery] string ip = null)
        {
            ApiResponse apiResponse = new();
            try
            {
                string token = GetToken();
                apiResponse = (await CheckBankCode(token, bankCode, ip)).ToApiResponse<List<NotProcessedPayment>>();
                if (apiResponse.Code == 0)
                {
                    using HttpClient client = httpClientFactory.CreateClient();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    var httpResponseMessage = await client.PostAsync(configuration["CoreApiUrl"] + $"paymentOrders/processPayments?bankCode={bankCode}&date={date:O}", new StringContent(string.Empty, Encoding.UTF8, "application/json"));
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        apiResponse = await httpResponseMessage.Content.ReadFromJsonAsync<ApiResponse>();
                    }
                    else
                    {
                        apiResponse.Code = 2;
                        apiResponse.Message = "Ошибка при соединении с Core API";
                    }
                }
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex, 1);
            }
            return apiResponse;
        }

        /// <summary>
        /// Получение электронных платежных поручений
        /// </summary>
        /// <param name="bankCode">Код банка</param>
        /// <param name="date">Дата в формате yyyy-MM-dd</param>
        /// <returns>Список платежных поручений</returns>
        [HttpGet("{bankCode}/payment-orders/{date}")]
        public async Task<ApiResponse<List<PaymentOrder>>> GetPaymentOrders(string bankCode, DateTime date, [SwaggerIgnore][FromQuery] string ip = null)
        {
            ApiResponse<List<PaymentOrder>> apiResponse = new();
            try
            {
                string token = GetToken();
                apiResponse = (await CheckBankCode(token, bankCode, ip)).ToApiResponse<List<PaymentOrder>>();
                if (apiResponse.Code == 0)
                {
                    using HttpClient client = httpClientFactory.CreateClient();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    var httpResponseMessage = await client.GetAsync(configuration["CoreApiUrl"] + $"paymentOrders/paymentOrders?bankCode={bankCode}&date={date:O}");
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        apiResponse = await httpResponseMessage.Content.ReadFromJsonAsync<ApiResponse<List<PaymentOrder>>>();
                    }
                    else
                    {
                        apiResponse.Code = 2;
                        apiResponse.Message = "Ошибка при соединении с Core API";
                    }
                }
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex, 1);
            }
            return apiResponse;
        }
    }
}
