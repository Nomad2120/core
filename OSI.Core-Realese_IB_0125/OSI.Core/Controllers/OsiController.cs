using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OSI.Core.Services;
using OSI.Core.Models;
using OSI.Core.Models.Db;
using OSI.Core.Models.Requests;
using OSI.Core.Models.Enums;
using OSI.Core.Models.Responses;
using OSI.Core.Logic;
using OSI.Core.Auth;

namespace OSI.Core.Controllers
{
    /// <summary>
    /// ОСИ
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class OsiController : ControllerBase
    {
        private readonly IOsiSvc osiSvc;
        private readonly IOsiServiceSvc osiServiceSvc;
        private readonly IOsiServiceCompanySvc osiServiceCompanySvc;
        private readonly IPlanAccuralSvc planAccuralSvc;
        private readonly IOsiAccountSvc osiAccountSvc;
        private readonly IOsiAccountApplicationSvc osiAccountApplicationSvc;
        private readonly IServiceGroupSaldoSvc serviceGroupSaldoSvc;
        private readonly IPastDebtSvc pastDebtSvc;

        public OsiController(IOsiSvc osiSvc,
            IOsiServiceSvc osiServiceSvc,
            IOsiServiceCompanySvc osiServiceCompanySvc,
            IPlanAccuralSvc planAccuralSvc,
            IOsiAccountSvc osiAccountSvc,
            IOsiAccountApplicationSvc osiAccountApplicationSvc,
            IServiceGroupSaldoSvc serviceGroupSaldoSvc,
            IPastDebtSvc pastDebtSvc)
        {
            this.osiSvc = osiSvc;
            this.osiServiceSvc = osiServiceSvc;
            this.osiServiceCompanySvc = osiServiceCompanySvc;
            this.planAccuralSvc = planAccuralSvc;
            this.osiAccountSvc = osiAccountSvc;
            this.osiAccountApplicationSvc = osiAccountApplicationSvc;
            this.serviceGroupSaldoSvc = serviceGroupSaldoSvc;
            this.pastDebtSvc = pastDebtSvc;
        }

        /// <summary>
        /// Получить список всех ОСИ
        /// </summary>
        /// <response code="200">Список Osi</response>
        [HttpGet("all")]
        [Authorize(Roles.Support)]
        public async Task<ApiResponse<IEnumerable<Osi>>> GetOsies()
        {
            var apiResponse = new ApiResponse<IEnumerable<Osi>>();
            try
            {
                var osies = await osiSvc.GetOsies();
                apiResponse.Result = osies;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Получить список всех активных ОСИ
        /// </summary>
        /// <response code="200">Список Osi</response>
        [HttpGet("all-active")]
        [Authorize(Roles.Support)]
        public async Task<ApiResponse<IEnumerable<Osi>>> GetActiveOsies()
        {
            var apiResponse = new ApiResponse<IEnumerable<Osi>>();
            try
            {
                var osies = await osiSvc.GetActiveOsies();
                apiResponse.Result = osies;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }


        /// <summary>
        /// Получить данные по ОСИ
        /// </summary>
        /// <param name="id">Id объекта ОСИ</param>
        /// <response code="200">Osi</response>
        [HttpGet("{id:int}")]
        [Authorize]
        [UserHasAccessFilter(typeof(Osi), nameof(id))]
        public async Task<ApiResponse<Osi>> GetOsiById(int id)
        {
            var apiResponse = new ApiResponse<Osi>();
            try
            {
                Osi osi = await osiSvc.GetOsiById(id);
                apiResponse.Result = osi;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Изменение ОСИ
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles.ADMIN | Roles.CHAIRMAN)]
        [UserHasAccessFilter(typeof(Osi), nameof(id))]
        public async Task<ApiResponse> UpdateOsi(int id, OsiRequest request)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await osiSvc.UpdateOsi(id, request, HttpContext.User);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Проверка всех данных и запуск ОСИ в работу (для председателя)
        /// </summary>
        /// <param name="id">Id объекта ОСИ</param>
        /// <returns></returns>
        [HttpPut("{id}/start")]
        [Authorize(Roles.Support)]
        public async Task<ApiResponse> StartOsi(int id)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await osiSvc.StartOsi(id);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Остановка деятельности объекта ОСИ (для председателя)
        /// </summary>
        /// <param name="id">Id объекта ОСИ</param>
        /// <returns></returns>
        [HttpPut("{id}/stop")]
        [Authorize(Roles.Support)]
        public async Task<ApiResponse> StopOsi(int id)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await osiSvc.StopOsi(id);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Активация объекта ОСИ, чтобы председатель увидел его у себя
        /// </summary>
        /// <param name="id">Id объекта ОСИ</param>
        /// <returns></returns>
        [HttpPut("{id}/activate")]
        [Authorize(Roles.Support)]
        public async Task<ApiResponse> ActivateOsi(int id)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await osiSvc.ActivateOsi(id);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Деактивация объекта ОСИ, чтобы председатель не увидел его у себя
        /// </summary>
        /// <param name="id">Id объекта ОСИ</param>
        /// <returns></returns>
        [HttpPut("{id}/deactivate")]
        [Authorize(Roles.Support)]
        public async Task<ApiResponse> DeactivateOsi(int id)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await osiSvc.DeactivateOsi(id);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Добавление сканированного документа к ОСИ
        /// </summary>
        /// <param name="id">Id ОСИ</param>
        /// <param name="request">Сканированный документ, где data это byte[]</param>
        /// <response code="200">OsiDoc</response>
        [HttpPost("{id:int}/docs")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Osi), nameof(id))]
        public async Task<ApiResponse<OsiDoc>> AddOsiDoc(int id, [FromBody] AddScanDoc request)
        {
            var apiResponse = new ApiResponse<OsiDoc>();
            try
            {
                var osiDoc = await osiSvc.AddOsiDoc(id, request);
                apiResponse.Result = osiDoc;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }

            return apiResponse;
        }

        /// <summary>
        /// Получить сканированные документы по ОСИ
        /// </summary>
        /// <param name="id">Id ОСИ</param>
        /// <response code="200">Список OsiDoc</response>
        [HttpGet("{id:int}/docs")]
        [Authorize]
        [UserHasAccessFilter(typeof(Osi), nameof(id))]
        public async Task<ApiResponse<IEnumerable<OsiDoc>>> GetOsiDocs(int id)
        {
            var apiResponse = new ApiResponse<IEnumerable<OsiDoc>>();
            try
            {
                var osiDocs = await osiSvc.GetOsiDocs(id);
                apiResponse.Result = osiDocs;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }

            return apiResponse;
        }

        /// <summary>
        /// Удалить сканированный документ у ОСИ
        /// </summary>
        /// <param name="id">Id ОСИ</param>
        /// <param name="docId">Id скан.документа</param>
        /// <returns></returns>
        [HttpDelete("{id:int}/docs/{docId}")]
        [Authorize(Roles.ADMIN | Roles.CHAIRMAN)]
        [UserHasAccessFilter(typeof(Osi), nameof(id))]
        [UserHasAccessFilter(typeof(OsiDoc), nameof(docId))]
        public async Task<ApiResponse> DeleteOsiDoc(int id, int docId)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await osiSvc.DeleteOsiDoc(id, docId);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }

            return apiResponse;
        }

        /// <summary>
        /// Абоненты данного ОСИ
        /// </summary>
        /// <param name="id">Id ОСИ</param>
        /// <param name="onlyExternals">Только внешние абоненты (арендаторы)</param>
        /// <response code="200">Список Abonent</response>
        [HttpGet("{id:int}/abonents")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Osi), nameof(id))]
        public async Task<ApiResponse<IEnumerable<Abonent>>> GetAbonentsByOsiId(int id, [FromQuery] bool onlyExternals = false)
        {
            var apiResponse = new ApiResponse<IEnumerable<Abonent>>();
            try
            {
                IEnumerable<Abonent> models = await osiSvc.GetAbonentsByOsiId(id, onlyExternals);
                apiResponse.Result = models;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        // OSI-169
        /// <summary>
        /// Получить абонента данного ОСИ по номеру помещения
        /// </summary>
        /// <param name="id">Id ОСИ</param>
        /// <param name="flat">Номер помещения</param>
        /// <response code="200">Список Abonent</response>
        [HttpGet("{id:int}/abonents/{flat}")]
        [Authorize(Roles.Support)]
        //[UserHasAccessFilter(typeof(Osi), nameof(id))]
        //[UserHasAccessFilter("AbonentFlat", nameof(id), null, nameof(flat))]
        public async Task<ApiResponse<Abonent>> GetAbonentByOsiIdAndFlat(int id, string flat) =>
            await ApiResponse.CreateEx(async () => await osiSvc.GetAbonentByOsiIdAndFlat(id, flat));

        /// <summary>
        /// Счета данного ОСИ
        /// </summary>
        /// <param name="id">Id ОСИ</param>
        /// <response code="200">Список OsiAccount</response>
        [HttpGet("{id:int}/accounts")]
        [Authorize]
        [UserHasAccessFilter(typeof(Osi), nameof(id))]
        public async Task<ApiResponse<IEnumerable<OsiAccount>>> GetOsiAccountsByOsiId(int id)
        {
            var apiResponse = new ApiResponse<IEnumerable<OsiAccount>>();
            try
            {
                IEnumerable<OsiAccount> models = await osiAccountSvc.GetOsiAccountsByOsiId(id);
                apiResponse.Result = models;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Счета данного ОСИ
        /// </summary>
        /// <param name="id">Id ОСИ</param>
        /// <response code="200">Список OsiAccount</response>
        [HttpGet("{id:int}/account-applications")]
        [Authorize]
        [UserHasAccessFilter(typeof(Osi), nameof(id))]
        public Task<ApiResponse<IEnumerable<OsiAccountApplication>>> GetOsiAccountApplicationsByOsiId(int id) 
            => ApiResponse.CreateEx(() => osiAccountApplicationSvc.GetOsiAccountApplicationsByOsiId(id));

        /// <summary>
        /// Планы начислений данного ОСИ
        /// </summary>
        /// <param name="id">Id ОСИ</param>
        /// <response code="200">Список PlanAccural</response>
        [HttpGet("{id:int}/plan-accurals")]
        [Authorize]
        [UserHasAccessFilter(typeof(Osi), nameof(id))]
        public async Task<ApiResponse<IEnumerable<PlanAccural>>> GetPlanAccuralsByOsiId(int id)
        {
            var apiResponse = new ApiResponse<IEnumerable<PlanAccural>>();
            try
            {
                IEnumerable<PlanAccural> models = await planAccuralSvc.GetPlanAccuralsByOsiId(id);
                apiResponse.Result = models;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Получить последний план начислений или создать новый
        /// </summary>
        /// <param name="id">Id ОСИ</param>
        /// <response code="200">PlanAccural</response>
        [HttpGet("{id:int}/get-last-plan-or-create-new")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Osi), nameof(id))]
        public async Task<ApiResponse<PlanAccural>> GetLastPlanAccuralByOsiIdOrCreateNew(int id)
        {
            var apiResponse = new ApiResponse<PlanAccural>();
            try
            {
                PlanAccural model = await PlanAccuralLogic.GetLastPlanAccuralByOsiIdOrCreateNew(id);
                apiResponse.Result = model;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Список услуг данного ОСИ
        /// </summary>
        /// <param name="id">Id ОСИ</param>
        /// <response code="200">Список OsiService</response>
        [HttpGet("{id:int}/services")]
        [Authorize]
        [UserHasAccessFilter(typeof(Osi), nameof(id))]
        public async Task<ApiResponse<IEnumerable<OsiService>>> GetServicesByOsiId(int id)
        {
            var apiResponse = new ApiResponse<IEnumerable<OsiService>>();
            try
            {
                IEnumerable<OsiService> models = await osiServiceSvc.GetOsiServicesByOsiId(id);
                apiResponse.Result = models;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Список услуг данного ОСИ с абонентами
        /// </summary>
        /// <param name="id">Id ОСИ</param>
        /// <response code="200">Список OsiService</response>
        [HttpGet("{id:int}/v2/services")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Osi), nameof(id))]
        public async Task<ApiResponse<IEnumerable<ServiceGroupResponse>>> GetServiceGroupsInfo(int id)
        {
            var apiResponse = new ApiResponse<IEnumerable<ServiceGroupResponse>>();
            try
            {
                IEnumerable<ServiceGroupResponse> models = await osiServiceSvc.GetServiceGroupsInfo(id);
                apiResponse.Result = models;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Список сервисных компаний данного ОСИ
        /// </summary>
        /// <param name="id">Id ОСИ</param>
        /// <response code="200">Список OsiServiceCompany</response>
        [HttpGet("{id:int}/service-companies")]
        [Authorize]
        [UserHasAccessFilter(typeof(Osi), nameof(id))]
        public async Task<ApiResponse<IEnumerable<OsiServiceCompany>>> GetServiceCompaniesByOsiId(int id)
        {
            var apiResponse = new ApiResponse<IEnumerable<OsiServiceCompany>>();
            try
            {
                IEnumerable<OsiServiceCompany> models = await osiServiceCompanySvc.GetOsiServiceCompanysByOsiId(id);
                apiResponse.Result = models;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Установить шаг визарда
        /// </summary>
        /// <param name="id">Id ОСИ</param>
        /// <param name="wizardStep">Шаг визарда</param>
        [HttpPut("{id:int}/wizard-step")]
        [Consumes("text/plain")]
        [Authorize(Roles.ADMIN | Roles.CHAIRMAN)]
        [UserHasAccessFilter(typeof(Osi), nameof(id))]
        public async Task<ApiResponse> SaveWizardStep(int id, [FromBody] string wizardStep)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await osiSvc.SaveWizardStep(id, wizardStep);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Список неподписанных актов по ОСИ
        /// </summary>
        /// <param name="id">Id ОСИ</param>
        /// <response code="200">Список актов</response>
        [HttpGet("{id:int}/not-signed-acts")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Osi), nameof(id))]
        public async Task<ApiResponse<IEnumerable<Act>>> GetNotSignedActsByOsiId(int id)
        {
            var apiResponse = new ApiResponse<IEnumerable<Act>>();
            try
            {
                IEnumerable<Act> models = await osiSvc.GetActsByOsiId(id, ActStateCodes.CREATED);
                apiResponse.Result = models;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Список подписанных актов по ОСИ
        /// </summary>
        /// <param name="id">Id ОСИ</param>
        /// <response code="200">Список актов</response>
        [HttpGet("{id:int}/signed-acts")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Osi), nameof(id))]
        public async Task<ApiResponse<IEnumerable<Act>>> GetSignedAndProvActsByOsiId(int id)
        {
            var apiResponse = new ApiResponse<IEnumerable<Act>>();
            try
            {
                IEnumerable<Act> models = await osiSvc.GetActsByOsiId(id, ActStateCodes.SIGNED);
                apiResponse.Result = models;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Получить начальное сальдо по всем группам услуг для данного ОСИ
        /// </summary>
        /// <param name="id">Id ОСИ</param>
        /// <response code="200">Список актов</response>
        [HttpGet("{id:int}/saldo-by-groups")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Osi), nameof(id))]
        public async Task<ApiResponse<IEnumerable<ServiceGroupSaldoResponse>>> GetServiceGroupSaldoByOsiId(int id)
        {
            var apiResponse = new ApiResponse<IEnumerable<ServiceGroupSaldoResponse>>();
            try
            {
                var models = await serviceGroupSaldoSvc.GetServiceGroupSaldoByOsiId(id);
                apiResponse.Result = models;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        //OSI-133
        /// <summary>
        /// Получить долги прошлых периодов по данному ОСИ
        /// </summary>
        /// <param name="id">Id ОСИ</param>
        [HttpGet("{id:int}/past-debts")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Osi), nameof(id))]
        public async Task<ApiResponse<IEnumerable<PastDebtsByOsiResponse>>> GetPastDebts(int id) =>
            await ApiResponse.CreateEx(async () => await pastDebtSvc.GetPastDebtsByOsiId(id));

        //OSI-214
        /// <summary>
        /// Выдает всевозможные группы и сервисы, которые когда-либо были на данном ОСИ, для создания корректировок
        /// </summary>
        /// <param name="id">Id ОСИ</param>
        [HttpGet("{id:int}/group-and-services-for-fixes")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Osi), nameof(id))]
        public async Task<ApiResponse<dynamic>> GetGroupAndServicesForFixes(int id) =>
            await ApiResponse.CreateEx(async () => await osiServiceSvc.GetGroupAndServicesForFixes(id));

        /// <summary>
        /// Проверка на необходимость подписания новой оферты
        /// </summary>
        /// <param name="id">id ОСИ</param>
        /// <returns></returns>
        [HttpGet("{id:int}/is-need-sign-new-offer")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Osi), nameof(id))]
        public async Task<ApiResponse<bool>> IsNeedSignNewOffer(int id) =>
            await ApiResponse.CreateEx(async () => await osiSvc.IsNeedSignNewOffer(id));

        /// <summary>
        /// Проверка на необходимость подписания новой оферты
        /// </summary>
        /// <param name="id">id ОСИ</param>
        /// <returns></returns>
        [HttpGet("{id:int}/add-in-promo")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Osi), nameof(id))]
        public async Task<ApiResponse> AddInPromo(int id) => await ApiResponse.CreateEx(async () => await osiSvc.AddInPromo(id));

        /// <summary>
        /// Загрузить абонентов из эксель-файла
        /// </summary>
        /// <param name="id">id ОСИ</param>
        /// <param name="fileContents">Файл</param>
        /// <response code="200">Список абонентов</response>
        [HttpPost("{id:int}/load-abonents-from-excel")]
        [Consumes("application/octet-stream")]
        [Authorize(Exclude = Roles.ABONENT)]
        [UserHasAccessFilter(typeof(Osi), nameof(id))]
        public async Task<ApiResponse<List<Abonent>>> LoadFromExcel(int id, [FromBody] byte[] fileContents)
            => await ApiResponse.CreateEx(async () => await osiSvc.LoadAbonentsFromExcel(id, fileContents));

        /// <summary>
        /// Переделать последние начисления ОСИ
        /// </summary>
        /// <param name="id">Id ОСИ</param>
        /// <returns></returns>
        [HttpPut("{id:int}/remake-last-accurals")]
        [Authorize(Exclude = Roles.ABONENT)]
        public async Task<ApiResponse> RemakeAccuralsAtLastPlan(int id)
            => await ApiResponse.CreateEx(async () => await osiSvc.RemakeAccuralsAtLastPlan(id));
    }
}
