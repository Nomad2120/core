using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OSI.Core.Auth;
using OSI.Core.Models;
using OSI.Core.Models.Db;
using OSI.Core.Models.Responses;
using OSI.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Controllers
{
    /// <summary>
    /// Долги прошлых периодов
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PastDebtsController : ControllerBase
    {
        private readonly IPastDebtSvc pastDebtSvc;

        public PastDebtsController(IPastDebtSvc pastDebtSvc)
        {
            this.pastDebtSvc = pastDebtSvc;
        }

        /// <summary>
        /// Получить долги прошлых периодов
        /// </summary>
        /// <param name="abonentId">Id абонента</param>
        /// <param name="serviceGroupId">Id группы услуг</param>
        [HttpGet]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Abonent), nameof(abonentId))]
        public async Task<ApiResponse<PastDebtsResponse>> GetPastDebts([FromQuery][Required] int abonentId, [FromQuery][Required] int serviceGroupId) =>
            await ApiResponse.CreateEx(async () => await pastDebtSvc.GetPastDebts(abonentId, serviceGroupId));

        /// <summary>
        /// Сохранить долги прошлых периодов
        /// </summary>
        [HttpPost]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Abonent), nameof(abonentId))]
        public async Task<ApiResponse> SavePastDebts([FromQuery][Required] int abonentId, [FromQuery][Required] int serviceGroupId, [FromBody][Required] IEnumerable<PastDebtInfo> pastDebts) =>
            await ApiResponse.CreateEx(async () => await pastDebtSvc.SavePastDebts(abonentId, serviceGroupId, pastDebts));

        //OSI-131
        /// <summary>
        /// Получить данные для отчета "Уведомление должнику"
        /// </summary>
        /// <param name="abonentId">Id абонента</param>
        [HttpGet("debtor-notification")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Abonent), nameof(abonentId))]
        public async Task<ApiResponse<DebtorNotificationResponse>> GetDebtorNotification([FromQuery][Required] int abonentId) =>
            await ApiResponse.CreateEx(async () => await pastDebtSvc.GetDebtorNotification(abonentId));

        // OSI-132
        /// <summary>
        /// Получить данные для заполнения заявления ОСИ нотариусу
        /// </summary>
        /// <param name="abonentId">Id абонента</param>
        [HttpGet("notary-application")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Abonent), nameof(abonentId))]
        public async Task<ApiResponse<NotaryApplicationResponse>> GetNotaryApplicationWithRegistry([FromQuery][Required] int abonentId) =>
            await ApiResponse.CreateEx(async () => await pastDebtSvc.GetNotaryApplicationWithRegistry(abonentId));
    }
}
