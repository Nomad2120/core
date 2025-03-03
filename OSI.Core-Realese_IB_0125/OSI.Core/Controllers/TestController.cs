using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OSI.Core.Models.Db;
using OSI.Core.Services;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Controllers
{
#if DEBUG
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IQRCodeSvc qrCodeSvc;

        public TestController(IQRCodeSvc qrCodeSvc)
        {
            this.qrCodeSvc = qrCodeSvc;
        }

        /// <summary>
        /// qr
        /// </summary>
        /// <returns></returns>
        [HttpGet("/qr")]
        public ActionResult<string> GetQR([Required][FromQuery]int pixels = 3)
        {
            string qr = qrCodeSvc.GetQRCodeBase64(pixels, "https://kaspi.kz/pay/NurTau?5742=345345");
            return Ok(qr);
        }
    }
#endif
}
