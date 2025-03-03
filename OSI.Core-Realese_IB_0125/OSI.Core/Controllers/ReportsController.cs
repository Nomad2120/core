using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using OSI.Core.Auth;
using OSI.Core.Models;
using OSI.Core.Models.Db;
using OSI.Core.Models.Enums;
using OSI.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace OSI.Core.Controllers
{
    /// <summary>
    /// Отчеты
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly IReportsSvc reportsSvc;
        private readonly IPrintInvoiceSvc printInvoiceSvc;
        private readonly ILogger<ReportsController> logger;

        public ReportsController(IReportsSvc reportsSvc, IPrintInvoiceSvc printInvoiceSvc, ILogger<ReportsController> logger)
        {
            this.reportsSvc = reportsSvc;
            this.printInvoiceSvc = printInvoiceSvc;
            this.logger = logger;
        }

        /// <summary>
        /// Файл с долгами за текущий месяц
        /// </summary>
        /// <param name="osiId">Id ОСИ</param>
        /// <param name="debtFileType">Тип файла</param>
        /// <returns></returns>
        [HttpGet("debt-file-current-month/{osiId:int}")]
        [Produces("application/octet-stream", "application/json")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Osi), nameof(osiId))]
        public async Task<IActionResult> DebtFileCurrentMonth(int osiId, [FromQuery][Required] DebtFileTypeCodes debtFileType)
        {
            try
            {
                DateTime today = DateTime.Today;
                DateTime dateBegin = new DateTime(today.Year, today.Month, 1);
                DateTime dateEnd = dateBegin.AddMonths(1);
                var filename = await reportsSvc.GetDebtFile(dateBegin, dateEnd, osiId, debtFileType);
                return PhysicalFile(filename, "application/octet-stream", Path.GetFileName(filename));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                ApiResponse apiResponse = new ApiResponse();
                apiResponse.FromEx(ex);
                return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
            }
        }

        /// <summary>
        /// Файл с долгами за период
        /// </summary>
        /// <param name="osiId">Id ОСИ</param>
        /// <param name="onDate1">период с</param>
        /// <param name="onDate2">по</param>
        /// <param name="debtFileType">Тип файла</param>
        /// <returns></returns>
        [HttpGet("debt-file-on-period/{osiId:int}")]
        [Produces("application/octet-stream", "application/json")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Osi), nameof(osiId))]
        public async Task<IActionResult> DebtFileOnPeriod(int osiId,
            [FromQuery][Required] DateTime onDate1,
            [FromQuery][Required] DateTime onDate2,
            [FromQuery][Required] DebtFileTypeCodes debtFileType)
        {
            try
            {
                var filename = await reportsSvc.GetDebtFile(onDate1, onDate2, osiId, debtFileType);
                return PhysicalFile(filename, "application/octet-stream", Path.GetFileName(filename));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                ApiResponse apiResponse = new ApiResponse();
                apiResponse.FromEx(ex);
                return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
            }
        }

        /// <summary>
        /// Файл с долгами за период по всем ОСИ
        /// </summary>
        /// <param name="onDate1">период с</param>
        /// <param name="onDate2">по</param>
        /// <param name="debtFileType">Тип файла</param>
        /// <returns></returns>
        [HttpGet("all-osi-debt-file-on-period")]
        [Produces("application/octet-stream", "application/json")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        [Authorize(Roles.Support)]
        public async Task<IActionResult> DebtAllInOneFileOnPeriod([FromQuery][Required] DateTime onDate1,
            [FromQuery][Required] DateTime onDate2,
            [FromQuery][Required] DebtFileTypeCodes debtFileType)
        {
            try
            {
                var filename = await reportsSvc.GetAllInOneDebtFile(onDate1, onDate2, debtFileType);
                return PhysicalFile(filename, "application/octet-stream", Path.GetFileName(filename));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                ApiResponse apiResponse = new ApiResponse();
                apiResponse.FromEx(ex);
                return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
            }
        }

        /// <summary>
        /// Файл с долгами за текущий месяц по всем ОСИ
        /// </summary>
        /// <param name="debtFileType">Тип файла</param>
        /// <returns></returns>
        [HttpGet("all-osi-debt-file-current-month")]
        [Produces("application/octet-stream", "application/json")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        [Authorize(Roles.Support)]
        public async Task<IActionResult> DebtAllInOneFileOnCurrentMonth([FromQuery][Required] DebtFileTypeCodes debtFileType)
        {
            try
            {
                DateTime today = DateTime.Today;
                DateTime dateBegin = new DateTime(today.Year, today.Month, 1);
                DateTime dateEnd = dateBegin.AddMonths(1);
                var filename = await reportsSvc.GetAllInOneDebtFile(dateBegin, dateEnd, debtFileType);
                return PhysicalFile(filename, "application/octet-stream", Path.GetFileName(filename));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                ApiResponse apiResponse = new ApiResponse();
                apiResponse.FromEx(ex);
                return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
            }
        }

        /// <summary>
        /// Файл для проверки актов
        /// </summary>
        /// <param name="onDate">Дата планов начислений - первое число предыдущего месяца</param>
        /// <returns></returns>
        [HttpGet("file-for-check-acts")]
        [Produces("application/octet-stream", "application/json")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        [Authorize(Roles.Support)]
        public async Task<IActionResult> FileForCheckActs([FromQuery][Required] DateTime onDate)
        {
            try
            {
                var filename = await reportsSvc.FileForCheckActs(onDate);
                return PhysicalFile(filename, "application/octet-stream", Path.GetFileName(filename));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                ApiResponse apiResponse = new ApiResponse();
                apiResponse.FromEx(ex);
                return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
            }
        }

        /// <summary>
        /// Сведения об ОСИ
        /// </summary>
        /// <returns></returns>
        [HttpGet("osi-information")]
        [Produces("application/octet-stream", "application/json")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        [Authorize(Roles.Support)]
        public async Task<IActionResult> OsiesInformation()
        {
            try
            {
                var filename = await reportsSvc.OsiesInformation();
                return PhysicalFile(filename, "application/octet-stream", Path.GetFileName(filename));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                ApiResponse apiResponse = new ApiResponse();
                apiResponse.FromEx(ex);
                return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
            }
        }

        /// <summary>
        /// Сформировать файл PDF с квитанциями по ОСИ за текущий месяц
        /// </summary>
        /// <param name="osiId">Id ОСИ</param>
        /// <param name="isStraightOrder">Прямой (true) или сквозной (false) порядок абонентов</param>
        /// <param name="isOnlyResidents">Только резиденты и подвалы (true) или все абоненты (false)</param>
        /// <returns></returns>
        [HttpGet("invoices-pdf-on-current-date/{osiId:int}")]
        [Produces("application/octet-stream", "application/json")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Osi), nameof(osiId))]
        public async Task<IActionResult> GetInvoicesByOsiIdOnCurrentDate(int osiId,
                                                                         [FromQuery][Required] bool isStraightOrder,
                                                                         [FromQuery][Required] bool isOnlyResidents)
        {
            try
            {
                var apiResponse = await printInvoiceSvc.GetInvoicesByOsiIdOnCurrentDate(isStraightOrder, isOnlyResidents, osiId);
                if (apiResponse.Code != 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
                }
                return PhysicalFile(apiResponse.Result.Data, "application/octet-stream", Path.GetFileName(apiResponse.Result.Data));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                ApiResponse apiResponse = new ApiResponse();
                apiResponse.FromEx(ex);
                return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
            }
        }

        /// <summary>
        /// Сформировать файл PDF с квитанциями по ОСИ за указанный год/месяц
        /// </summary>
        /// <param name="osiId">Id ОСИ</param>
        /// <param name="year">Год</param>
        /// <param name="month">Месяц</param>
        /// <param name="isStraightOrder">Прямой (true) или сквозной (false) порядок абонентов</param>
        /// <param name="isOnlyResidents">Только резиденты и подвалы (true) или все абоненты (false)</param>
        /// <returns></returns>
        [HttpGet("invoices-pdf-on-period/{osiId:int}")]
        [Produces("application/octet-stream", "application/json")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Osi), nameof(osiId))]
        public async Task<IActionResult> GetInvoicesByOsiIdOnDate(int osiId,
                                                                  [FromQuery][Required] int year,
                                                                  [FromQuery][Required] int month,
                                                                  [FromQuery][Required] bool isStraightOrder,
                                                                  [FromQuery][Required] bool isOnlyResidents)
        {
            try
            {
                var apiResponse = await printInvoiceSvc.GetInvoicesByOsiIdOnDate(isStraightOrder, isOnlyResidents, osiId, year, month);
                if (apiResponse.Code != 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
                }
                return PhysicalFile(apiResponse.Result.Data, "application/octet-stream", Path.GetFileName(apiResponse.Result.Data));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                ApiResponse apiResponse = new ApiResponse();
                apiResponse.FromEx(ex);
                return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
            }
        }

        /// <summary>
        /// Сформировать файл PDF с квитанциями по всем ОСИ на указанный год/месяц
        /// </summary>
        /// <param name="year">Год</param>
        /// <param name="month">Месяц, передаем текущий, т.к. берутся долги вплоть до первого числа указанного месяца</param>
        /// <param name="isStraightOrder">Прямой (true) или сквозной (false) порядок абонентов</param>
        /// <param name="isOnlyResidents">Только резиденты и подвалы (true) или все абоненты (false)</param>
        /// <returns></returns>
        [HttpGet("all-invoices-pdf-on-period")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        [Authorize(Roles.Support)]
        public async Task<IActionResult> GetInvoicesByAllOsinDate([FromQuery][Required] int year,
                                                                  [FromQuery][Required] int month,
                                                                  [FromQuery][Required] bool isStraightOrder,
                                                                  [FromQuery][Required] bool isOnlyResidents)
        {
            try
            {
                var apiResponse = await printInvoiceSvc.GetInvoicesByAllOsiOnDate(isStraightOrder, isOnlyResidents, year, month);
                if (apiResponse.Code != 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
                }
                return StatusCode(StatusCodes.Status200OK, apiResponse);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                ApiResponse apiResponse = new ApiResponse();
                apiResponse.FromEx(ex);
                return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
            }
        }

        /// <summary>
        /// ВРЕМЕННЫЙ МЕТОД: Подсчет кол-ва распечатанных квитанций
        /// </summary>
        /// <param name="year">Год</param>
        /// <param name="month">Месяц, передаем текущий, т.к. берутся долги вплоть до первого числа указанного месяца</param>
        /// <returns></returns>
        //[HttpGet("count-all-invoices-pdf-on-period")]
        //[ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        //[Authorize(Roles.Support)]
        //public async Task<IActionResult> GetCountInvoicesByAllOsiOnDate([FromQuery][Required] int year, [FromQuery][Required] int month)
        //{
        //    try
        //    {
        //        var apiResponse = await printInvoiceSvc.GetCountInvoicesByAllOsiOnDate(year, month);
        //        if (apiResponse.Code != 0)
        //        {
        //            return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
        //        }
        //        return StatusCode(StatusCodes.Status200OK, apiResponse);
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.LogError(ex, "Error");
        //        ApiResponse apiResponse = new ApiResponse();
        //        apiResponse.FromEx(ex);
        //        return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
        //    }
        //}

        /// <summary>
        /// Сформировать файл PDF с квитанцией по абоненту за текущий месяц
        /// </summary>
        /// <param name="abonentId">Id абонента</param>
        /// <returns></returns>
        [HttpGet("abonents-invoice-pdf-on-current-date/{abonentId}")]
        [Produces("application/octet-stream", "application/json")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        [Authorize(Roles.Default | Roles.PAYMENTSERVICE)]
        [UserHasAccessFilter(typeof(Abonent), nameof(abonentId), Roles = Roles.Support | Roles.PAYMENTSERVICE)]
        public async Task<IActionResult> GetInvoiceByAbonentIdOnCurrentDate(int abonentId)
        {
            try
            {
                var apiResponse = await printInvoiceSvc.GetInvoiceByAbonentIdOnCurrentDate(abonentId);
                if (apiResponse.Code != 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
                }
                return File(apiResponse.Result, "application/octet-stream", $"{DateTime.Today:yyyy-MM-dd}_{abonentId}.pdf");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                ApiResponse apiResponse = new ApiResponse();
                apiResponse.FromEx(ex);
                return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
            }
        }

        /// <summary>
        /// Сформировать файл PDF с квитанцией по абоненту за указанный год/месяц
        /// </summary>
        /// <param name="abonentId">Id абонента</param>
        /// <param name="year">Год</param>
        /// <param name="month">Месяц</param>
        /// <returns></returns>
        [HttpGet("abonents-invoice-pdf-on-period/{abonentId}")]
        [Produces("application/octet-stream", "application/json")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        [Authorize]
        [UserHasAccessFilter(typeof(Abonent), nameof(abonentId))]
        public async Task<IActionResult> GetInvoiceByAbonentIdOnDate(int abonentId,
            [FromQuery][Required] int year, [FromQuery][Required] int month)
        {
            try
            {
                var apiResponse = await printInvoiceSvc.GetInvoiceByAbonentIdOnDate(abonentId, year, month);
                if (apiResponse.Code != 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
                }
                return File(apiResponse.Result, "application/octet-stream", $"{new DateTime(year, month, 1):yyyy-MM-dd}_{abonentId}.pdf");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                ApiResponse apiResponse = new ApiResponse();
                apiResponse.FromEx(ex);
                return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
            }
        }

        /// <summary>
        /// Сформировать файл PDF с квитанцией по выбранным абонентам за текущий месяц
        /// </summary>
        /// <param name="abonentIds">Id абонентов</param>
        /// <param name="isStraightOrder">Прямой (true) или сквозной (false) порядок абонентов</param>
        /// <param name="isOnlyResidents">Только резиденты и подвалы (true) или все абоненты (false)</param>
        /// <returns></returns>
        [HttpPost("abonents-invoices-pdf-on-current-date")]
        [Produces("application/octet-stream", "application/json")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        [Authorize]
        [UserHasAccessFilter(typeof(Abonent), nameof(abonentIds))]
        public async Task<IActionResult> GetInvoiceByAbonentIdsOnCurrentDate([FromBody] IEnumerable<int> abonentIds,
                                                                             [FromQuery][Required] bool isStraightOrder,
                                                                             [FromQuery][Required] bool isOnlyResidents)
        {
            try
            {
                var apiResponse = await printInvoiceSvc.GetInvoiceByAbonentIdsOnCurrentDate(isStraightOrder, isOnlyResidents, abonentIds);
                if (apiResponse.Code != 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
                }
                return File(apiResponse.Result, "application/octet-stream", $"{DateTime.Now.Ticks}.pdf");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                ApiResponse apiResponse = new ApiResponse();
                apiResponse.FromEx(ex);
                return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
            }
        }

        /// <summary>
        /// Сформировать файл PDF с квитанциями по выбранным абонентам за указанный год/месяц
        /// </summary>
        /// <param name="abonentIds">Id абонентов</param>
        /// <param name="year">Год</param>
        /// <param name="month">Месяц</param>
        /// <param name="isStraightOrder">Прямой (true) или сквозной (false) порядок абонентов</param>
        /// <param name="isOnlyResidents">Только резиденты и подвалы (true) или все абоненты (false)</param>
        /// <returns></returns>
        [HttpPost("abonents-invoices-pdf-on-period")]
        [Produces("application/octet-stream", "application/json")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        [Authorize]
        [UserHasAccessFilter(typeof(Abonent), nameof(abonentIds))]
        public async Task<IActionResult> GetInvoicesByAbonentIdsOnDate([FromBody] IEnumerable<int> abonentIds,
                                                                       [FromQuery][Required] int year,
                                                                       [FromQuery][Required] int month,
                                                                       [FromQuery][Required] bool isStraightOrder,
                                                                       [FromQuery][Required] bool isOnlyResidents)
        {
            try
            {
                var apiResponse = await printInvoiceSvc.GetInvoiceByAbonentIdsOnDate(isStraightOrder, isOnlyResidents, abonentIds, year, month);
                if (apiResponse.Code != 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
                }
                return File(apiResponse.Result, "application/octet-stream", $"{DateTime.Now.Ticks}.pdf");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                ApiResponse apiResponse = new ApiResponse();
                apiResponse.FromEx(ex);
                return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
            }
        }

        /// <summary>
        /// Файл для проверки актов
        /// </summary>
        /// <param name="onDate">Дата платежей</param>
        /// <returns></returns>
        [HttpGet("dbf-for-kazpost")]
        [Produces("application/octet-stream", "application/json")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        [Authorize(Roles.Support)]
        public async Task<IActionResult> GetPaymentOrdersDBFKazPost([FromQuery][Required] DateTime onDate)
        {
            try
            {
                var apiResponse = await reportsSvc.GetPaymentOrdersDBFKazPost(onDate);
                if (apiResponse.Code != 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
                }
                return PhysicalFile(apiResponse.Result, "application/octet-stream", Path.GetFileName(apiResponse.Result));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                ApiResponse apiResponse = new ApiResponse();
                apiResponse.FromEx(ex);
                return StatusCode(StatusCodes.Status500InternalServerError, apiResponse);
            }
        }
    }
}
