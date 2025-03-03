using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OSI.Core.Services;
using OSI.Core.Models;
using OSI.Core.Models.Db;
using OSI.Core.Models.Requests;
using OSI.Core.Logic;
using OSI.Core.Auth;

namespace OSI.Core.Controllers
{
    /// <summary>
    /// Планы начислений
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PlanAccuralsController : ControllerBase
    {
        private readonly IPlanAccuralSvc planAccuralSvc;
        private readonly ITransactionSvc transactionSvc;
        private readonly IActSvc actSvc;

        public PlanAccuralsController(IPlanAccuralSvc planAccuralSvc, ITransactionSvc transactionSvc, IActSvc actSvc)
        {
            this.planAccuralSvc = planAccuralSvc;
            this.transactionSvc = transactionSvc;
            this.actSvc = actSvc;
        }

        /// <summary>
        /// Получить данные плана начислений
        /// </summary>
        /// <param name="id">Id плана</param>
        /// <response code="200">PlanAccural</response>
        [HttpGet("{id:int}")]
        [Authorize]
        [UserHasAccessFilter(typeof(PlanAccural), nameof(id))]
        public async Task<ApiResponse<PlanAccural>> GetPlanAccuralById(int id)
        {
            var apiResponse = new ApiResponse<PlanAccural>();
            try
            {
                PlanAccural planAccural = await PlanAccuralLogic.GetPlanAccuralById(id);
                apiResponse.Result = planAccural;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Сделать начисления по плану начислений
        /// </summary>
        /// <param name="id">Id плана начислений</param>
        /// <param name="deleteOldAccurals">Удалить предыдущие начисления</param>
        [HttpPut("{id:int}/create-accurals")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(PlanAccural), nameof(id))]
        public async Task<ApiResponse> CreateAccurals(int id, bool deleteOldAccurals)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await transactionSvc.CreateAccuralsByPlanId(id, deleteOldAccurals);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Включить услугу OSI Billing (USSIKING) в тариф
        /// </summary>
        /// <param name="id">Id плана начислений</param>
        /// <param name="value">да/нет</param>
        [HttpPut("{id:int}/set-ussiking-included")]
        [Authorize(Roles.ADMIN | Roles.CHAIRMAN)]
        [UserHasAccessFilter(typeof(PlanAccural), nameof(id))]
        public async Task<ApiResponse> SetUssikingIncluded(int id, bool value)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await PlanAccuralLogic.SetUssikingIncluded(id, value);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Создать акт выполненных работ по плану начислений
        /// </summary>
        /// <param name="id">Id плана начислений</param>
        /// <response code="200">Акт выполненных работ</response>
        [HttpPut("{id:int}/create-act")]
        [Authorize(Roles.Support)]
        public async Task<ApiResponse<Act>> CreateAct(int id)
        {
            var apiResponse = new ApiResponse<Act>();
            try
            {
                var model = await actSvc.CreateActByPlanAccuralId(id);
                apiResponse.Result = model;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Установить день автоматического начисления 
        /// </summary>
        /// <param name="id">Id плана начислений</param>
        /// <param name="accuralDay">День начисления (от 1 до 10)</param>
        [HttpPut("{id:int}/set-accural-job-at-day")]
        [Authorize(Roles.Support | Roles.ADMIN | Roles.CHAIRMAN)]
        public async Task<ApiResponse> SetAccuralJobAtDay(int id, int accuralDay)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await planAccuralSvc.SetAccuralJobAtDay(id, accuralDay);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }   
    }
}
