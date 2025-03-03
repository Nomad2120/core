using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OSI.Core.Auth;
using OSI.Core.Models;
using OSI.Core.Models.Db;
using OSI.Core.Models.Requests;
using OSI.Core.Models.Responses;
using OSI.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Controllers
{
    /// <summary>
    /// Абоненты ОСИ
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AbonentsController : ControllerBase
    {
        private readonly IAbonentSvc abonentSvc;
        private readonly IOsiServiceSvc osiServiceSvc;

        public AbonentsController(IAbonentSvc abonentSvc, IOsiServiceSvc osiServiceSvc)
        {
            this.abonentSvc = abonentSvc;
            this.osiServiceSvc = osiServiceSvc;
        }

        /// <summary>
        /// Получить данные абонента
        /// </summary>
        /// <param name="id">Id абонента</param>
        /// <response code="200">Абонент</response>
        [HttpGet("{id:int}")]
        [Authorize]
        [UserHasAccessFilter(typeof(Abonent), nameof(id))]
        public async Task<ApiResponse<Abonent>> GetAbonentById(int id)
        {
            var apiResponse = new ApiResponse<Abonent>();
            try
            {
                Abonent abonent = await abonentSvc.GetAbonentById(id);
                apiResponse.Result = abonent;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Получить данные абонента для платежного сервиса
        /// </summary>
        /// <param name="abonentNum">Номер абонента</param>
        /// <response code="200">Абонент</response>
        [HttpGet("payment-service/{abonentNum}")]
        [Authorize(Roles.ADMIN | Roles.PAYMENTSERVICE)]
        //[UserHasAccessFilter("AbonentNum", nameof(abonentNum))]
        public async Task<ApiResponse<Abonent>> GetAbonentForPaymentService(string abonentNum)
        {
            var apiResponse = new ApiResponse<Abonent>();
            try
            {
                Abonent abonent = await abonentSvc.GetAbonentForPaymentService(abonentNum);
                apiResponse.Result = abonent;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
                apiResponse.Code = ex.Message switch
                {
                    "Абонент не найден" => 101,
                    "ОСИ не активно" => 102,
                    _ => 100,
                };
            }
            return apiResponse;
        }

        /// <summary>
        /// Добавление абонента
        /// </summary>
        [HttpPost]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Osi), nameof(request), nameof(AbonentRequest.OsiId))]
        public async Task<ApiResponse<Abonent>> AddAbonent(AbonentRequest request)
        {
            var apiResponse = new ApiResponse<Abonent>();
            try
            {
                Abonent abonent = await abonentSvc.AddAbonent(request);
                apiResponse.Result = abonent;
            }
            catch (Exception ex)
            {
                apiResponse.FromError(abonentSvc.GetExceptionMessage(ex));
            }
            return apiResponse;
        }

        /// <summary>
        /// Изменение абонента
        /// </summary>
        /// <param name="id">Id абонента</param>
        /// <param name="request"></param>
        [HttpPut("{id:int}")]
        [Authorize(Roles.ADMIN | Roles.CHAIRMAN)]
        [UserHasAccessFilter(typeof(Abonent), nameof(id))]
        [UserHasAccessFilter(typeof(Osi), nameof(request), nameof(AbonentRequest.OsiId))]
        public async Task<ApiResponse> UpdateAbonent(int id, AbonentRequest request)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await abonentSvc.UpdateAbonent(id, request);
            }
            catch (Exception ex)
            {
                apiResponse.FromError(abonentSvc.GetExceptionMessage(ex));
            }
            return apiResponse;
        }

        /// <summary>
        /// Удаление абонента
        /// </summary>
        /// <param name="id">Id абонента</param>
        [HttpDelete("{id:int}")]
        [Authorize(Roles.ADMIN)]
        public async Task<ApiResponse> DeleteAbonent(int id)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await abonentSvc.DeleteAbonent(id);
            }
            catch (Exception ex)
            {
                apiResponse.FromError(abonentSvc.GetExceptionMessage(ex));
            }
            return apiResponse;
        }

        /// <summary>
        /// Получить данные ОСИ по абоненту
        /// </summary>
        /// <param name="id">Id абонента</param>
        /// <response code="200">ОСИ</response>
        [HttpGet("{id:int}/get-osi")]
        [Authorize]
        [UserHasAccessFilter(typeof(Abonent), nameof(id))]
        public async Task<ApiResponse<Osi>> GetOsiByAbonentId(int id)
        {
            var apiResponse = new ApiResponse<Osi>();
            try
            {
                Osi osi = await abonentSvc.GetOsiByAbonentId(id);
                apiResponse.Result = osi;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Добавление арендатора
        /// </summary>
        [HttpPost("arendator")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Osi), nameof(request), nameof(ArendatorRequest.OsiId))]
        public async Task<ApiResponse<Arendator>> AddArendator(ArendatorRequest request)
        {
            var apiResponse = new ApiResponse<Arendator>();
            try
            {
                Arendator arendator = await abonentSvc.AddArendator(request);
                apiResponse.Result = arendator;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Получить подключенные услуги по абоненту
        /// </summary>
        /// <param name="osiId">Id ОСИ</param>
        /// <param name="abonentId">Id абонента</param>
        /// <param name="groupId">Id группы</param>
        /// <response code="200">ОСИ</response>
        //[HttpGet("get-services")]
        //[Authorize]
        //[UserHasAccessFilter(typeof(Abonent), nameof(abonentId))]
        //[UserHasAccessFilter(typeof(Osi), nameof(osiId))]
        //public async Task<ApiResponse<List<ServiceByAbonentResponse>>> GetOsiByAbonentId([FromQuery] int osiId, [FromQuery] int abonentId, [FromQuery] int groupId)
        //{
        //    var apiResponse = new ApiResponse<List<ServiceByAbonentResponse>>();
        //    try
        //    {
        //        var list = await osiServiceSvc.GetServicesByAbonent(osiId, abonentId, groupId);
        //        apiResponse.Result = list;
        //    }
        //    catch (Exception ex)
        //    {
        //        apiResponse.FromEx(ex);
        //    }
        //    return apiResponse;
        //}
    }
}
