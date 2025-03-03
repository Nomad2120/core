using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OSI.Core.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Logging
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
        private readonly RequestDelegate _next;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Invoke(HttpContext context)
        {
            await Log.Request(_logger, context.TraceIdentifier, context.Request, context.Request.Method != HttpMethods.Get);

            var originalBodyStream = context.Response.Body;

            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            await Log.Response(_logger, context.TraceIdentifier, context.Response, context.Request.Method != HttpMethods.Get);

            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);

            context.Response.Body = originalBodyStream;
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
                $"Received HTTP request ({{TraceIdentifier}}):{Environment.NewLine}{{Request}}");

            private static readonly Action<ILogger, string, string, Exception?> _response = LoggerMessage.Define<string, string>(
                LogLevel.Information,
                EventIds.Response,
                $"Sent HTTP response ({{TraceIdentifier}}):{Environment.NewLine}{{Response}}");

            private static readonly Action<ILogger, string, string, Exception?> _requestDebug = LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                EventIds.Request,
                $"Received HTTP request ({{TraceIdentifier}}):{Environment.NewLine}{{Request}}");

            private static readonly Action<ILogger, string, string, Exception?> _responseDebug = LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                EventIds.Response,
                $"Sent HTTP response ({{TraceIdentifier}}):{Environment.NewLine}{{Response}}");

            public static async Task Request(ILogger logger, string traceIdentifier, HttpRequest request, bool info = true)
            {
                (info ? _request : _requestDebug)(logger, traceIdentifier, await request.ToStringAsync(), null);
            }

            public static async Task Response(ILogger logger, string traceIdentifier, HttpResponse response, bool info = true)
            {
                (info ? _response : _responseDebug)(logger, traceIdentifier, await response.ToStringAsync(), null);
            }
        }
    }
}
