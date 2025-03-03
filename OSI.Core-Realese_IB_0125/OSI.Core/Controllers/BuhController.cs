using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OSI.Core.Auth;
using OSI.Core.Models;
using OSI.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Controllers
{
    /// <summary>
    /// Ежемесячные бухгалтерские операции: начисления, акты, планы
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class BuhController : ControllerBase
    {
        private readonly IBuhSvc buhSvc;

        public BuhController(IBuhSvc buhSvc)
        {
            this.buhSvc = buhSvc;
        }

        /// <summary>
        /// Сделать начисления по всем планам начислений на текущий день (только по рабочим ОСИ)
        /// </summary>
        /// <param name="recreate">Переделать в том числе уже начисленные планы</param>
        /// <returns></returns>
        [HttpPut("create-accurals")]
        [Authorize(Roles.Support)]
        public async Task<ApiResponse<string>> CreateAccurals(bool recreate)
        {
            var apiResponse = new ApiResponse<string>();
            try
            {
                string result = await buhSvc.CreateAccurals(recreate);
                apiResponse.Result = result;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;

        }

        /// <summary>
        /// Сделать начисления по всем планам начислений 
        /// </summary>
        /// <param name="year">Год</param>
        /// <param name="month">Месяц, передается текущий, т.е. наступил 12-ый месяц, пора делать начисления, передаем 12</param>
        /// <param name="recreate">Переделать в том числе уже начисленные планы</param>
        /// <param name="allOsi">ВНИМАТЕЛЬНО!!! Если да, то учитываются даже нерабочие ОСИ</param>
        /// <returns></returns>
        [HttpPut("create-accurals-in-custom-period")]
        [Authorize(Roles.Support)]
        public async Task<ApiResponse<string>> CreateAccurals(int year, int month, bool recreate = false, bool allOsi = false)
        {
            var apiResponse = new ApiResponse<string>();
            try
            {
                string result = await buhSvc.CreateAccurals(year, month, recreate, allOsi);
                apiResponse.Result = result;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Сделать акты за год и месяц
        /// </summary>
        /// <param name="year">Год</param>
        /// <param name="month">Месяц. Обычно передаем предыдущий месяц</param>
        /// <param name="onlyIfNotExists">Создаст акт, только если его не было, иначе будет пересоздавать с проверкой если он уже был подписан</param>
        /// <returns></returns>
        [HttpPut("create-acts")]
        [Authorize(Roles.Support)]
        public async Task<ApiResponse<string>> CreateActs(int year, int month, bool onlyIfNotExists = false)
        {
            var apiResponse = new ApiResponse<string>();
            try
            {
                string result = await buhSvc.CreateActs(year, month, onlyIfNotExists);
                apiResponse.Result = result;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Создать массово ЭСФ по актам за указанным месяц
        /// </summary>
        /// <param name="year">Год</param>
        /// <param name="month">Месяц, обычно передаем предыдущий</param>
        /// <returns></returns>
        [HttpPut("create-esfs")]
        [Authorize(Roles.Support)]
        public async Task<ApiResponse<string>> CreateEsfs(int year, int month)
        {
            var apiResponse = new ApiResponse<string>();
            try
            {
                string result = await buhSvc.CreateEsfs(year, month);
                apiResponse.Result = result;
            }
            catch (Exception ex)
            {
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Создаем новые планы начислений по всем работающим ОСИ
        /// </summary>
        /// <returns></returns>
        //[HttpPut("create-new-plans")]
        //public async Task<ApiResponse<string>> CreateNewPlans()
        //{
        //    var apiResponse = new ApiResponse<string>();
        //    try
        //    {
        //        string result = await buhSvc.CreateNewPlans();
        //        apiResponse.Result = result;
        //    }
        //    catch (Exception ex)
        //    {
        //        apiResponse.FromEx(ex);
        //    }
        //    return apiResponse;
        //}
    }
}
