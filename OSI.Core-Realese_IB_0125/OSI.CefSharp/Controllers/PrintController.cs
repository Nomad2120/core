using CefSharp.OffScreen;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OSI.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;

namespace CefSharp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrintController : ControllerBase
    {
        private readonly ILogger<PrintController> logger;

        public PrintController(ILogger<PrintController> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Сгенерировать PDF из HTML
        /// </summary>
        /// <param name="htmlPath">Полный путь к файлу HTML</param>
        /// <returns>Полный путь к файлу PDF</returns>
        [HttpGet("pdf")]
        public async Task<ApiResponse<string>> GetPdfFromFile([FromQuery][Required] string htmlPath)
        {
            ApiResponse<string> apiResponse = new ApiResponse<string>();
            try
            {
                // преобразуем в pdf
                logger.LogDebug("chromium initialized - start");
                using var browser = new ChromiumWebBrowser();
                logger.LogDebug("chromium initialized - end");
                while (!browser.IsBrowserInitialized) { }
                bool isLoaded = false;
                browser.LoadingStateChanged += (sender, ea) =>
                {
                    if (!ea.IsLoading) isLoaded = true;
                };
                logger.LogDebug("chromium load file name - start");
                string htmlContent = System.IO.File.ReadAllText(htmlPath);
                browser.LoadHtml(htmlContent, Path.ChangeExtension(htmlPath, ".html"));
                logger.LogDebug("chromium load file name - end");
                logger.LogDebug("chromium while (!isLoaded) - start");
                while (!isLoaded) { await Task.Delay(500); }
                logger.LogDebug("chromium while (!isLoaded) - end");
                PdfPrintSettings pdfSettings = new PdfPrintSettings()
                {
                    HeaderFooterEnabled = false,
                    Landscape = false,
                    MarginType = CefPdfPrintMarginType.None,
                    PageWidth = 210000,
                    PageHeight = 297000,
                    SelectionOnly = false,
                    BackgroundsEnabled = true
                };
                string pdfPath = Path.ChangeExtension(htmlPath, "pdf");
                logger.LogDebug("chromium pdf - start");
                await browser.PrintToPdfAsync(pdfPath, pdfSettings);
                logger.LogDebug("chromium pdf - end");
                apiResponse.Result = pdfPath;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        /// <summary>
        /// Сгенерировать PDF из HTML
        /// </summary>
        /// <param name="htmlContent">HTML</param>
        /// <returns>PDF в Base64</returns>
        [HttpPost("pdf")]
        public async Task<ApiResponse<string>> GetPdfFromHtml([FromBody] string htmlContent)
        {
            ApiResponse<string> apiResponse = new ApiResponse<string>();
            try
            {
                // преобразуем в pdf
                logger.LogDebug("chromium initialized - start");
                using var browser = new ChromiumWebBrowser();
                logger.LogDebug("chromium initialized - end");
                while (!browser.IsBrowserInitialized) { }
                bool isLoaded = false;
                browser.LoadingStateChanged += (sender, ea) =>
                {
                    if (!ea.IsLoading) isLoaded = true;
                };
                logger.LogDebug("chromium load file name - start");
                browser.LoadHtml(htmlContent);
                logger.LogDebug("chromium load file name - end");
                logger.LogDebug("chromium while (!isLoaded) - start");
                while (!isLoaded) { await Task.Delay(500); }
                logger.LogDebug("chromium while (!isLoaded) - end");
                PdfPrintSettings pdfSettings = new PdfPrintSettings()
                {
                    HeaderFooterEnabled = false,
                    Landscape = false,
                    MarginType = CefPdfPrintMarginType.None,
                    PageWidth = 210000,
                    PageHeight = 297000,
                    SelectionOnly = false,
                    BackgroundsEnabled = true
                };
                string pdfPath = Path.ChangeExtension(Path.GetTempFileName(), "pdf");
                logger.LogDebug("chromium pdf - start");
                await browser.PrintToPdfAsync(pdfPath, pdfSettings);
                logger.LogDebug("chromium pdf - end");
                apiResponse.Result = Convert.ToBase64String(System.IO.File.ReadAllBytes(pdfPath));
                System.IO.File.Delete(pdfPath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error");
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }
    }
}
