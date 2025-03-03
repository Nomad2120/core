using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OSI.Core.Auth;
using OSI.Core.Models;
using OSI.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Controllers
{
    /// <summary>
    /// Банковские контракты
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ContractsController : ControllerBase
    {
        private readonly IContractSvc contractSvc;
        private readonly ILogger<ContractsController> logger;

        public ContractsController(IContractSvc contractSvc, ILogger<ContractsController> logger)
        {
            this.contractSvc = contractSvc;
            this.logger = logger;
        }

        [HttpGet("check")]
        [Authorize(Roles.Support | Roles.PAYMENTSERVICE)]
        public async Task<ApiResponse> CheckBankCode([Required][FromQuery] string bankCode, [Required][FromQuery] string ip)
        {
            var apiResponse = new ApiResponse();
            try
            {
                apiResponse = await contractSvc.CheckBankCode(bankCode, ip);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                apiResponse.FromEx(ex, 300);
            }
            return apiResponse;
        }
    }
}
