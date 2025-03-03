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
    /// Услуги ОСИ
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class OsiServicesController : ControllerBase
    {
        private readonly IOsiServiceSvc osiServiceSvc;

        public OsiServicesController(IOsiServiceSvc osiServiceSvc)
        {
            this.osiServiceSvc = osiServiceSvc;
        }

        /// <summary>
        /// Получить данные услуги ОСИ
        /// </summary>
        /// <param name="id">Id услуги ОСИ</param>
        /// <response code="200">OsiService</response>
        [HttpGet("{id:int}")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(OsiService), nameof(id))]
        public async Task<ApiResponse<OsiService>> GetOsiServiceById(int id)
        {
            var apiResponse = new ApiResponse<OsiService>();
            try
            {
                OsiService model = await osiServiceSvc.GetOsiServiceById(id);
                apiResponse.Result = model;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Получить данные услуги ОСИ с абонентами
        /// </summary>
        /// <param name="id">Id услуги ОСИ</param>
        /// <response code="200">OsiService</response>
        //[HttpGet("v2/{id:int}")]
        //public async Task<ApiResponse<OsiServiceResponse>> GetOsiServiceByIdExtended(int id)
        //{
        //    var apiResponse = new ApiResponse<OsiServiceResponse>();
        //    try
        //    {
        //        OsiServiceResponse model = await osiServiceSvc.GetOsiServiceByIdExtended(id);
        //        apiResponse.Result = model;
        //    }
        //    catch (Exception ex)
        //    {
        //        apiResponse.FromEx(ex);
        //    }
        //    return apiResponse;
        //}

        /// <summary>
        /// Добавить услугу ОСИ
        /// </summary>
        /// <param name="request"></param>
        /// <response code="200">OsiService</response>
        [HttpPost]
        [Authorize(Roles.ADMIN | Roles.CHAIRMAN)]
        [UserHasAccessFilter(typeof(Osi), nameof(request), nameof(OsiServiceRequest.OsiId))]
        public async Task<ApiResponse<OsiService>> AddOsiService(OsiServiceRequest request)
        {
            var apiResponse = new ApiResponse<OsiService>();
            try
            {
                OsiService model = await osiServiceSvc.AddOrUpdateOsiService(0, request);
                apiResponse.Result = model;
            }
            catch (Exception ex)
            {
                apiResponse.FromError(osiServiceSvc.GetExceptionMessage(ex));
            }
            return apiResponse;
        }

        /// <summary>
        /// Изменить услугу ОСИ
        /// </summary>
        /// <param name="id">Id услуги ОСИ</param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("{id:int}")]
        [Authorize(Roles.ADMIN | Roles.CHAIRMAN)]
        [UserHasAccessFilter(typeof(Osi), nameof(request), nameof(OsiServiceRequest.OsiId))]
        public async Task<ApiResponse> UpdateOsiService(int id, OsiServiceRequest request)
        {
            var apiResponse = new ApiResponse();
            try
            {
                _ = await osiServiceSvc.AddOrUpdateOsiService(id, request);
            }
            catch (Exception ex)
            {
                apiResponse.FromError(osiServiceSvc.GetExceptionMessage(ex));
            }
            return apiResponse;
        }

        /// <summary>
        /// Получение списка абонентов на услуге
        /// </summary>
        /// <param name="id">Id услуги </param>
        [HttpGet("{id:int}/abonents")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(OsiService), nameof(id))]
        public async Task<ApiResponse<List<AbonentOnServiceResponse>>> GetOsiServiceAbonents(int id)
        {
            var apiResponse = new ApiResponse<List<AbonentOnServiceResponse>>();
            try
            {
                var abonents = await osiServiceSvc.GetOsiServiceAbonents(id);
                apiResponse.Result = abonents;
            }
            catch (Exception ex)
            {
                apiResponse.FromError(osiServiceSvc.GetExceptionMessage(ex));
            }
            return apiResponse;
        }

        /// <summary>
        /// Добавление и/или удаление списка абонентов на услуге
        /// </summary>
        /// <param name="id">Id услуги </param>
        /// <param name="abonents">Список Id абонентов</param>
        /// <response code="200">Кол-во добавленных и/или удаленных абонентов</response>
        [HttpPut("{id:int}/abonents")]
        [Authorize(Roles.ADMIN | Roles.CHAIRMAN)]
        [UserHasAccessFilter(typeof(OsiService), nameof(id))]
        [UserHasAccessFilter(typeof(Abonent), nameof(abonents), nameof(AbonentOnServiceRequest.AbonentId))]
        public async Task<ApiResponse> SetOsiServiceAbonents(int id, List<AbonentOnServiceRequest> abonents)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await osiServiceSvc.SetOsiServiceAbonents(id, abonents);
            }
            catch (Exception ex)
            {
                apiResponse.FromError(osiServiceSvc.GetExceptionMessage(ex));
            }
            return apiResponse;
        }

        /// <summary>
        /// Отключение/включение услуги всем абонентам
        /// </summary>
        /// <param name="id">Id услуги</param>
        /// <param name="isActive">Активна/Неактивна</param>
        [HttpPut("{id:int}/set-state")]
        [Authorize(Roles.ADMIN | Roles.CHAIRMAN)]
        [UserHasAccessFilter(typeof(OsiService), nameof(id))]
        public async Task<ApiResponse> SetStateForService(int id, bool isActive)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await osiServiceSvc.SetStateForService(id, isActive);
            }
            catch (Exception ex)
            {
                apiResponse.FromError(osiServiceSvc.GetExceptionMessage(ex));
            }
            return apiResponse;
        }

        /// <summary>
        /// Удаление услуги
        /// </summary>
        /// <param name="id">Id услуги ОСИ</param>
        //[HttpDelete("{id:int}")]
        //public async Task<ApiResponse> DeleteOsiService(int id)
        //{
        //    var apiResponse = new ApiResponse();
        //    try
        //    {
        //        await osiServiceSvc.DeleteOsiService(id);
        //    }
        //    catch (Exception ex)
        //    {
        //        apiResponse.FromError(osiServiceSvc.GetExceptionMessage(ex));
        //    }
        //    return apiResponse;
        //}
    }
}
