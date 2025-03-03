using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OSI.Core.Auth;
using OSI.Core.Models;
using OSI.Core.Models.Db;
using OSI.Core.Models.Requests;
using OSI.Core.Models.Responses;
using OSI.Core.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OSI.Core.Controllers
{
    /// <summary>
    /// Отчеты председателей перед жильцами
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AccountReportsController : ControllerBase
    {
        private readonly IAccountReportSvc accountReportSvc;
        private readonly ILogger<AccountReportsController> logger;

        public AccountReportsController(IAccountReportSvc accountReportSvc, ILogger<AccountReportsController> logger)
        {
            this.accountReportSvc = accountReportSvc;
            this.logger = logger;
        }

        /// <summary>
        /// Получить отчеты
        /// </summary>
        /// <param name="osiId">Id ОСИ</param>
        /// <returns></returns>
        [HttpGet("osi/{osiId}")]
        [Authorize]
        [UserHasAccessFilter(typeof(Osi), nameof(osiId))]
        public Task<ApiResponse<IEnumerable<AccountReport>>> GetAccountReports(int osiId)
            => ApiResponse.CreateEx(() => accountReportSvc.GetAccountReports(osiId), logger);

        /// <summary>
        /// Получить статус по отчету прошлого месяца
        /// </summary>
        /// <param name="osiId">Id ОСИ</param>
        /// <returns></returns>
        [HttpGet("osi/{osiId}/prev-month-status")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Osi), nameof(osiId))]
        public Task<ApiResponse<AccountReportStatusResponse>> GetPrevMonthAccountReportStatus(int osiId)
            => ApiResponse.CreateEx(() => accountReportSvc.GetPrevMonthAccountReportStatus(osiId), logger);

        /// <summary>
        /// Создать отчет
        /// </summary>
        /// <param name="request">Запрос на создание отчета</param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles.ADMIN)]
        [UserHasAccessFilter(typeof(Osi), nameof(request), nameof(request.OsiId))]
        public Task<ApiResponse<AccountReport>> CreateAccountReport(AccountReportRequest request)
            => ApiResponse.CreateEx(() => accountReportSvc.CreateAccountReport(request), logger);

        /// <summary>
        /// Прикрепить выписку по счету
        /// </summary>
        /// <param name="listId">Id списка по счету</param>
        /// <param name="fileContents">Файл</param>
        /// <returns></returns>
        [HttpPut("list/{listId}")]
        [Consumes("application/octet-stream")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(AccountReportList), nameof(listId))]
        public Task<ApiResponse> FillList(int listId, [FromBody] byte[] fileContents)
            => ApiResponse.CreateEx(() => accountReportSvc.FillList(listId, fileContents), logger);

        /// <summary>
        /// Получить список по счету
        /// </summary>
        /// <param name="listId">Id списка по счету</param>
        /// <returns></returns>
        [HttpGet("list/{listId}")]
        [Authorize]
        [UserHasAccessFilter(typeof(AccountReportList), nameof(listId))]
        public Task<ApiResponse<AccountReportList>> GetList(int listId)
            => ApiResponse.CreateEx(() => accountReportSvc.GetList(listId), logger);

        /// <summary>
        /// Обновить данные по комментариям
        /// </summary>
        /// <param name="listId">Id списка по счету</param>
        /// <param name="items">Запрос на добавление комментария</param>
        /// <returns></returns>
        [HttpPost("list/{listId}/details")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(AccountReportList), nameof(listId))]
        public Task<ApiResponse<IEnumerable<AccountReportUpdateListDetailsItem>>> UpdateListDetails(int listId, [FromBody] IEnumerable<AccountReportUpdateListDetailsItem> items)
            => ApiResponse.CreateEx(() => accountReportSvc.UpdateListDetails(listId, items), logger);

        /// <summary>
        /// Опубликовать отчет
        /// </summary>
        /// <param name="id">Id отчета</param>
        /// <returns></returns>
        [HttpPut("{id}")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(AccountReport), nameof(id))]
        public Task<ApiResponse> PublishAccountReport(int id, [FromBody] AccountReportPublishRequest request)
            => ApiResponse.CreateEx(() => accountReportSvc.PublishAccountReport(id, request), logger);

        /// <summary>
        /// Получить список категорий для выбора
        /// </summary>
        /// <returns></returns>
        [HttpGet("categories")]
        [Authorize]
        public Task<ApiResponse<IEnumerable<AccountReportCategoryResponse>>> GetCategories()
            => ApiResponse.CreateEx(() => accountReportSvc.GetCategories());

        /// <summary>
        /// Получить данные для формирования ежемесячного отчета
        /// </summary>
        /// <param name="id">Id отчета</param>
        /// <returns></returns>
        [HttpPost("{id}/monthly-form-data")]
        [Authorize(Roles.ADMIN)]
        [UserHasAccessFilter(typeof(AccountReport), nameof(id))]
        public Task<ApiResponse<AccountReportFormData>> GetMonthlyReportFormData(int id, [FromBody] AccountReportPublishRequest request)
            => ApiResponse.CreateEx(() => accountReportSvc.GetMonthlyReportFormData(id, request), logger);
    }
}
