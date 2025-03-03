using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.PaymentService
{
    public class BankCodeIpLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<BankCodeIpLoggingMiddleware> _logger;

        public BankCodeIpLoggingMiddleware(RequestDelegate next, ILogger<BankCodeIpLoggingMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            using (NLog.MappedDiagnosticsLogicalContext.SetScoped(new KeyValuePair<string, object>[] { new("BankCode", context.Request.RouteValues["bankCode"]), new("IP", context.Connection.RemoteIpAddress.ToString()) }))
            {
                _logger.LogInformation("");
                await _next(context);
            }
        }
    }
}
