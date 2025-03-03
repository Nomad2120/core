using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OSI.Core.Auth;
using OSI.Core.Models;
using OSI.Core.Models.Db;
using OSI.Core.Models.Requests;
using OSI.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Controllers
{
    /// <summary>
    /// Начальное сальдо по услугам
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceGroupSaldoController : ControllerBase
    {
        private readonly IServiceGroupSaldoSvc serviceGroupSaldoSvc;

        public ServiceGroupSaldoController(IServiceGroupSaldoSvc serviceGroupSaldoSvc)
        {
            this.serviceGroupSaldoSvc = serviceGroupSaldoSvc;
        }

        /// <summary>
        /// Получить данные по сальдо
        /// </summary>
        /// <param name="id">Id сальдо</param>
        /// <response code="200">Сальдо</response>
        [HttpGet("{id:int}")]
        [Authorize]
        [UserHasAccessFilter(typeof(ServiceGroupSaldo), nameof(id))]
        public async Task<ApiResponse<ServiceGroupSaldo>> GetServiceGroupSaldoById(int id)
        {
            var apiResponse = new ApiResponse<ServiceGroupSaldo>();
            try
            {
                ServiceGroupSaldo model = await serviceGroupSaldoSvc.GetServiceGroupSaldoById(id);
                apiResponse.Result = model;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Добавить сальдо
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles.ADMIN | Roles.CHAIRMAN)]
        [UserHasAccessFilter(typeof(Osi), nameof(request), nameof(request.OsiId))]
        [UserHasAccessFilter(typeof(Abonent), nameof(request), nameof(request.AbonentId))]
        public async Task<ApiResponse<ServiceGroupSaldo>> AddServiceGroupSaldo(ServiceGroupSaldoRequest request)
        {
            var apiResponse = new ApiResponse<ServiceGroupSaldo>();
            try
            {
                ServiceGroupSaldo model = await serviceGroupSaldoSvc.AddServiceGroupSaldoByRequest(request);
                apiResponse.Result = model;
            }
            catch (Exception ex)
            {
                apiResponse.FromError(serviceGroupSaldoSvc.GetExceptionMessage(ex));
            }
            return apiResponse;
        }

        /// <summary>
        /// Изменить сальдо 
        /// </summary>
        /// <param name="id">Id сальдо</param>
        /// <param name="request"></param>
        /// <returns></returns>
        //[HttpPut("{id:int}")]
        //public async Task<ApiResponse> UpdateServiceGroupSaldoByRequest(int id, ServiceGroupSaldoRequest request)
        //{
        //    var apiResponse = new ApiResponse();
        //    try
        //    {
        //        await serviceGroupSaldoSvc.UpdateServiceGroupSaldoByRequest(id, request);
        //    }
        //    catch (Exception ex)
        //    {
        //        apiResponse.FromError(serviceGroupSaldoSvc.GetExceptionMessage(ex));
        //    }
        //    return apiResponse;
        //}

        /// <summary>
        /// Изменить сумму сальдо
        /// </summary>
        /// <param name="id">Id сальдо</param>
        /// <param name="saldo"></param>
        /// <returns></returns>
        [HttpPut("{id:int}")]
        [Authorize(Roles.ADMIN | Roles.CHAIRMAN)]
        [UserHasAccessFilter(typeof(ServiceGroupSaldo), nameof(id))]
        public async Task<ApiResponse> UpdateServiceGroupSaldoAmountById(int id, [FromBody]decimal saldo)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await serviceGroupSaldoSvc.UpdateServiceGroupSaldoAmountById(id, saldo);
            }
            catch (Exception ex)
            {
                apiResponse.FromError(serviceGroupSaldoSvc.GetExceptionMessage(ex));
            }
            return apiResponse;
        }

        /// <summary>
        /// Удаление сальдо
        /// </summary>
        /// <param name="id">Id сальдо</param>
        [HttpDelete("{id:int}")]
        [Authorize(Roles.ADMIN)]
        [UserHasAccessFilter(typeof(ServiceGroupSaldo), nameof(id))]
        public async Task<ApiResponse> DeleteServiceGroupSaldo(int id)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await serviceGroupSaldoSvc.DeleteServiceGroupSaldo(id);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }        
    }
}
