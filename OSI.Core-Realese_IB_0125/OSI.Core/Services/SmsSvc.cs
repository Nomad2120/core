using System.Threading.Tasks;
using System;
using System.ServiceModel.Channels;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Encodings.Web;
using System.Web;
using System.Collections.Concurrent;

namespace OSI.Core.Services
{
    public interface ISmsSvc
    {
        Task GenerateAndSendOtp(string phone);
    }

    public class SmsSvc : ISmsSvc
    {
        private readonly IConfiguration configuration;
        private readonly IOTPSvc otpSvc;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<SmsSvc> logger;

        private readonly ConcurrentDictionary<string, DateTime> generationTimes = new();

        public SmsSvc(IConfiguration configuration, IOTPSvc otpSvc, IHttpClientFactory httpClientFactory, ILogger<SmsSvc> logger)
        {
            this.configuration = configuration;
            this.otpSvc = otpSvc;
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
        }

        public async Task GenerateAndSendOtp(string phone)
        {
            if (generationTimes.TryGetValue(phone, out var lastGenerated) && DateTime.Now < lastGenerated.AddMinutes(3))
                throw new Exception("Повторная отправка кода возможна только через 3 минуты после предыдущей, попробуйте позже");
            string otp = otpSvc.GenerateOTP(phone);
            await SendMessage(phone: $"8{phone}", message: $"Код подтверждения/Растау коды eOsi.kz {otp}");
            generationTimes.AddOrUpdate(phone, _ => DateTime.Now, (_, _) => DateTime.Now);
        }

        private async Task SendMessage(string phone, string message)
        {
            using var client = httpClientFactory.CreateClient();
            var response = await client.GetStringAsync(configuration["Urls:SmsSend"] + $"&n={phone}&m={HttpUtility.UrlEncode(message)}");
            if (!response.Trim().StartsWith("Sending"))
            {
                logger.LogError("Sms send unexpected response: {response}", response);
                throw new Exception("Ошибка отправки SMS");
            }
        }
    }
}
