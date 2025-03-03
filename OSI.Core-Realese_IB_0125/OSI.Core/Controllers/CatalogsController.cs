using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OSI.Core.Auth;
using OSI.Core.Models;
using OSI.Core.Models.Db;
using OSI.Core.Models.Responses;
using OSI.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Controllers
{
    /// <summary>
    /// Справочная информация
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CatalogsController : ControllerBase
    {
        private readonly ICatalogSvc catalogSvc;

        public CatalogsController(ICatalogSvc catalogSvc)
        {
            this.catalogSvc = catalogSvc;
        }

        /// <summary>
        /// Методы начислений
        /// </summary>
        /// <returns></returns>
        [HttpGet("accural-methods")]
        public async Task<ApiResponse<IEnumerable<AccuralMethod>>> GetAccuralMethods()
        {
            var apiResponse = new ApiResponse<IEnumerable<AccuralMethod>>();
            try
            {
                var models = await catalogSvc.GetAccuralMethods();
                apiResponse.Result = models;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Виды помещений
        /// </summary>
        /// <returns></returns>
        [HttpGet("area-types")]
        public async Task<ApiResponse<IEnumerable<AreaType>>> GetAreaTypes()
        {
            var apiResponse = new ApiResponse<IEnumerable<AreaType>>();
            try
            {
                var models = await catalogSvc.GetAreaTypes();
                apiResponse.Result = models;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Типы счетов
        /// </summary>
        /// <returns></returns>
        [HttpGet("account-types")]
        public async Task<ApiResponse<IEnumerable<AccountType>>> GetAccountTypes()
        {
            var apiResponse = new ApiResponse<IEnumerable<AccountType>>();
            try
            {
                var models = await catalogSvc.GetAccountTypes();
                apiResponse.Result = models;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Банки
        /// </summary>
        /// <returns></returns>
        [HttpGet("banks")]
        public async Task<ApiResponse<IEnumerable<Bank>>> GetBanks()
        {
            var apiResponse = new ApiResponse<IEnumerable<Bank>>();
            try
            {
                var models = await catalogSvc.GetBanks();
                apiResponse.Result = models;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Виды документов
        /// </summary>
        /// <returns></returns>
        [HttpGet("doc-types")]
        public async Task<ApiResponse<IEnumerable<DocType>>> GetDocTypes()
        {
            var apiResponse = new ApiResponse<IEnumerable<DocType>>();
            try
            {
                var models = await catalogSvc.GetDocTypes();
                apiResponse.Result = models;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Состояния домов
        /// </summary>
        /// <returns></returns>
        [HttpGet("house-states")]
        public async Task<ApiResponse<IEnumerable<HouseState>>> GetHouseStates()
        {
            var apiResponse = new ApiResponse<IEnumerable<HouseState>>();
            try
            {
                var models = await catalogSvc.GetHouseStates();
                apiResponse.Result = models;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Список КНП
        /// </summary>
        /// <returns></returns>
        [HttpGet("knp")]
        public async Task<ApiResponse<IEnumerable<Knp>>> GetKnps()
        {
            var apiResponse = new ApiResponse<IEnumerable<Knp>>();
            try
            {
                var models = await catalogSvc.GetKnps();
                apiResponse.Result = models;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Виды сервисных компаний
        /// </summary>
        /// <returns></returns>
        [HttpGet("service-companies")]
        public async Task<ApiResponse<IEnumerable<ServiceCompany>>> GetServiceCompanies()
        {
            var apiResponse = new ApiResponse<IEnumerable<ServiceCompany>>();
            try
            {
                var models = await catalogSvc.GetServiceCompanies();
                apiResponse.Result = models;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Группы услуг
        /// </summary>
        /// <returns></returns>
        [HttpGet("service-groups")]
        public async Task<ApiResponse<IEnumerable<ServiceGroup>>> GetServiceGroups()
        {
            var apiResponse = new ApiResponse<IEnumerable<ServiceGroup>>();
            try
            {
                var models = await catalogSvc.GetServiceGroups();
                apiResponse.Result = models;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Типы объединений
        /// </summary>
        /// <returns></returns>
        [HttpGet("union-types")]
        public async Task<ApiResponse<IEnumerable<UnionType>>> GetUnionTypes()
        {
            var apiResponse = new ApiResponse<IEnumerable<UnionType>>();
            try
            {
                var models = await catalogSvc.GetUnionTypes();
                apiResponse.Result = models;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }
    }
}
