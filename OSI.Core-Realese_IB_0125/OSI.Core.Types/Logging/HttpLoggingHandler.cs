using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OSI.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OSI.Core.Logging
{
    public class HttpLoggingHandler : DelegatingHandler
    {
        private readonly ILogger<HttpLoggingHandler> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpLoggingHandler(ILogger<HttpLoggingHandler> logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            string traceIdentifier = _httpContextAccessor.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();
            await Log.RequestAsync(_logger, traceIdentifier, request);
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
            await Log.ResponseAsync(_logger, traceIdentifier, response);

            return response;
        }

        private static class Log
        {
            public static class EventIds
            {
                public static readonly EventId Request = new EventId(100, "Request");
                public static readonly EventId Response = new EventId(101, "Response");
            }

            private static readonly Action<ILogger, string, string, Exception?> _request = LoggerMessage.Define<string, string>(
                LogLevel.Information,
                EventIds.Request,
                $"HTTP request ({{TraceIdentifier}}):{Environment.NewLine}{{Request}}");

            private static readonly Action<ILogger, string, string, Exception?> _response = LoggerMessage.Define<string, string>(
                LogLevel.Information,
                EventIds.Response,
                $"HTTP response ({{TraceIdentifier}}):{Environment.NewLine}{{Response}}");

            public static async Task RequestAsync(ILogger logger, string traceIdentifier, HttpRequestMessage request)
            {
                _request(logger, traceIdentifier, await request.ToStringAsync(true), null);
            }

            public static async Task ResponseAsync(ILogger logger, string traceIdentifier, HttpResponseMessage response)
            {
                _response(logger, traceIdentifier, await response.ToStringAsync(true), null);
            }
        }
    }
}
