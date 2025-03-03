using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OSI.Core.Auth;
using OSI.Core.Models;
using OSI.Core.Models.Jobs;
using OSI.Core.Services;
using System;
using System.Threading.Tasks;

namespace OSI.Core.Controllers
{
    /// <summary>
    /// Джобы
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class JobsController : ControllerBase
    {
        private readonly ILogger<ReportsController> logger;
        private readonly IJobLogic jobLogic;

        public JobsController(ILogger<ReportsController> logger, IJobLogic jobLogic)
        {
            this.logger = logger;
            this.jobLogic = jobLogic;
        }

        /// <summary>
        /// Отправка Dbf Казпочте
        /// </summary>
        /// <param name="onDate">За дату</param>
        /// <param name="email">Почтовые ящики</param>
        /// <returns></returns>
        [HttpPut("job-send-dbf-to-kazpost")]
        [Authorize(Roles.Support)]
        public async Task<IActionResult> RunSendDBFToKazPostJob([FromQuery] DateTime onDate, [FromQuery] string email)
        {
            try
            {
                JobParameters parameters = new JobParameters
                {
                    StartDate = onDate,
                    Email = email
                };

                var apiResponse = await jobLogic.SendDBFToKazPost(parameters);
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
    }
}
