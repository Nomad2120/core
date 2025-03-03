using Microsoft.AspNetCore.Mvc;
using OSI.Core.Auth;
using OSI.Core.Models;
using OSI.Core.Models.Db;
using OSI.Core.Models.Requests;
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
    /// Заявки на регистрацию ОСИ
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationsController : ControllerBase
    {
        private readonly IRegistrationSvc registrationSvc;
        private readonly IRegistrationAccountSvc registrationAccountSvc;

        public RegistrationsController(IRegistrationSvc registrationSvc, IRegistrationAccountSvc registrationAccountSvc)
        {
            this.registrationSvc = registrationSvc;
            this.registrationAccountSvc = registrationAccountSvc;
        }

        /// <summary>
        /// Получить список всех заявок
        /// </summary>
        [HttpGet("all")]
        [Authorize(Roles.Support)]
        public async Task<ApiResponse<IEnumerable<Registration>>> GetRegistrations()
        {
            var apiResponse = new ApiResponse<IEnumerable<Registration>>();
            try
            {
                var registrations = await registrationSvc.GetRegistrations();
                apiResponse.Result = registrations;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Получить данные заявки
        /// </summary>
        /// <param name="id">Id заявки</param>
        [HttpGet("{id:int}")]
        [Authorize]
        [UserHasAccessFilter(typeof(Registration), nameof(id))]
        public async Task<ApiResponse<Registration>> GetRegistrationsById(int id)
        {
            var apiResponse = new ApiResponse<Registration>();
            try
            {
                Registration registration = await registrationSvc.GetRegistrationById(id);
                apiResponse.Result = registration;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Добавление заявки
        /// </summary>
        /// <response code="200">Номер заявки</response>
        [HttpPost]
        [Authorize(Exclude = Roles.OPERATOR)]
        [UserHasAccessFilter(typeof(User), nameof(request), nameof(RegistrationRequest.UserId))]
        public async Task<ApiResponse<int>> AddRegistration(RegistrationRequest request)
        {
            var apiResponse = new ApiResponse<int>();
            try
            {
                int registrationId = await registrationSvc.AddRegistration(request);
                apiResponse.Result = registrationId;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Изменение заявки
        /// </summary>
        /// <param name="id">Id заявки</param>
        /// <param name="request">Модель для изменения</param>
        [HttpPut("{id:int}")]
        [Authorize]
        [UserHasAccessFilter(typeof(Registration), nameof(id))]
        [UserHasAccessFilter(typeof(User), nameof(request), nameof(RegistrationRequest.UserId))]
        public async Task<ApiResponse> UpdateRegistration(int id, RegistrationRequest request)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await registrationSvc.UpdateRegistration(id, request);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Удаление заявки
        /// </summary>
        /// <param name="id">Id заявки</param>
        [HttpDelete("{id:int}")]
        [Authorize(Roles.ADMIN)]
        public async Task<ApiResponse> DeleteRegistration(int id)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await registrationSvc.DeleteRegistration(id);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Подтверждение создания заявки
        /// </summary>
        /// <param name="id">Id заявки</param>
        /// <returns></returns>
        [HttpPut("{id:int}/confirm-creation")]
        [Authorize]
        [UserHasAccessFilter(typeof(Registration), nameof(id))]
        public async Task<ApiResponse> ConfirmCreation(int id) 
            => await ApiResponse.CreateEx(() => registrationSvc.ConfirmCreation(id));

        /// <summary>
        /// Подписание заявки
        /// </summary>
        /// <param name="id">Id заявки</param>
        /// <param name="extension">Расширение файла</param>
        /// <param name="data">Подписанный документ</param>
        /// <returns></returns>
        [HttpPut("{id:int}/sign")]
        //[Consumes("application/octet-stream")]  для приема именно в byte[], а не в base64
        [Authorize]
        [UserHasAccessFilter(typeof(Registration), nameof(id))]
        public async Task<ApiResponse> SignRegistration(int id, [FromQuery][Required] string extension,[FromBody]byte[] data)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await registrationSvc.SignRegistration(id, extension, data);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Подписание заявки без документа
        /// </summary>
        /// <param name="id">Id заявки</param>
        /// <returns></returns>
        [HttpPut("{id:int}/sign-wo-doc")]
        [Authorize]
        [UserHasAccessFilter(typeof(Registration), nameof(id))]
        public async Task<ApiResponse> SignRegistrationWithoutDoc(int id)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await registrationSvc.SignRegistrationWithoutDoc(id);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Убрать подписание заявки
        /// </summary>
        [HttpPut("{id:int}/unsign")]
        [Authorize(Roles.ADMIN)]
        public async Task<ApiResponse> UnsignRegistration(int id)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await registrationSvc.UnsignRegistration(id);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Подтверждение заявки
        /// </summary>
        /// <param name="id">Id заявки</param>
        /// <returns></returns>
        [HttpPut("{id:int}/confirm")]
        [Authorize(Roles.Support)]
        public async Task<ApiResponse> ConfirmRegistration(int id)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await registrationSvc.ConfirmRegistrationById(id);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Создать ОСИ на основе подтвержденной заявки
        /// </summary>
        /// <param name="id">Id подтвержденной заявки</param>
        /// <response code="200">Созданный объект ОСИ</response>
        [HttpPost("{id}/create-osi")]
        [Authorize(Roles.Support)]
        public async Task<ApiResponse<Osi>> AddOsiByRegistrationId(int id)
        {
            var apiResponse = new ApiResponse<Osi>();
            try
            {
                var osi = await registrationSvc.CreateOsiByRegistrationId(id);
                apiResponse.Result = osi;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Добавление сканированного документа к заявке
        /// </summary>
        /// <param name="id">Id заявки</param>
        /// <param name="request">Сканированный документ, где data это byte[]</param>
        /// <response code="200">Данные добавленного документа</response>
        [HttpPost("{id:int}/docs")]
        [Authorize]
        [UserHasAccessFilter(typeof(Registration), nameof(id))]
        public async Task<ApiResponse<RegistrationDoc>> AddRegistrationDoc(int id, [FromBody]AddScanDoc request)
        {
            var apiResponse = new ApiResponse<RegistrationDoc>();
            try
            {
                var registrationDoc = await registrationSvc.AddRegistrationDoc(id, request);
                apiResponse.Result = registrationDoc;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }

            return apiResponse;
        }

        /// <summary>
        /// Получить сканированные документы по заявке
        /// </summary>
        /// <param name="id">Id заявки</param>
        /// <returns></returns>
        [HttpGet("{id:int}/docs")]
        [Authorize]
        [UserHasAccessFilter(typeof(Registration), nameof(id))]
        public async Task<ApiResponse<IEnumerable<RegistrationDoc>>> GetRegistrationDocs(int id)
        {
            var apiResponse = new ApiResponse<IEnumerable<RegistrationDoc>>();
            try
            {
                var registrationDocs = await registrationSvc.GetRegistrationDocs(id);
                apiResponse.Result = registrationDocs;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }

            return apiResponse;
        }

        /// <summary>
        /// Получить список требуемых документов для подачи заявки
        /// </summary>
        /// <returns></returns>
        [HttpGet("reqdocs")]
        [Authorize]
        public async Task<ApiResponse<IEnumerable<RequiredDocsResponse>>> GetReqDocs([FromQuery]int registrationId)
        {
            var apiResponse = new ApiResponse<IEnumerable<RequiredDocsResponse>>();
            try
            {
                var reqDocs = await registrationSvc.GetRequirmentsDocs(registrationId);
                apiResponse.Result = reqDocs;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }

            return apiResponse;
        }

        /// <summary>
        /// Удалить сканированный документ у заявки
        /// </summary>
        /// <param name="id">Id заявки</param>
        /// <param name="docId">Id скан.документа</param>
        /// <returns></returns>
        [HttpDelete("{id:int}/docs/{docId}")]
        [Authorize]
        public async Task<ApiResponse> DeleteRegistrationDoc(int id, int docId)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await registrationSvc.DeleteRegistrationDoc(id, docId);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }

            return apiResponse;
        }

        /// <summary>
        /// Счета данной заявки
        /// </summary>
        /// <param name="id">Id заявки</param>
        /// <response code="200">Список счетов</response>
        [HttpGet("{id:int}/accounts")]
        [Authorize]
        [UserHasAccessFilter(typeof(Registration), nameof(id))]
        public async Task<ApiResponse<IEnumerable<RegistrationAccount>>> GetRegistrationAccountsByOsiId(int id)
        {
            var apiResponse = new ApiResponse<IEnumerable<RegistrationAccount>>();
            try
            {
                IEnumerable<RegistrationAccount> models = await registrationAccountSvc.GetRegistrationAccountsByRegistrationId(id);
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
        /// <param name="id">Id заявки</param>
        /// <param name="wizardStep">Шаг визарда</param>
        [HttpPut("{id:int}/wizard-step")]
        [Consumes("text/plain")]
        [Authorize(Exclude = Roles.OPERATOR)]
        [UserHasAccessFilter(typeof(Registration), nameof(id))]
        public async Task<ApiResponse> SaveWizardStep(int id, [FromBody] string wizardStep)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await registrationSvc.SaveWizardStep(id, wizardStep);
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }
    }
}
