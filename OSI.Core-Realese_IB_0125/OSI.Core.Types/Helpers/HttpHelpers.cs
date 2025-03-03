using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OSI.Core.Helpers
{
    public static class HttpHelpers
    {
        public static bool AlwaysLogBytes { get; set; } = false;

        public static int LogBytesThreshold { get; set; } = 10 * 1024; //10 kB

        public static async Task<string> ToStringAsync(this HttpRequestMessage httpRequestMessage, bool addContent)
        {
            return
                $"Method: {httpRequestMessage.Method.Method}" +
                $", RequestUri: '{httpRequestMessage.RequestUri}'" +
                $", Version: {httpRequestMessage.Version}" +
                Environment.NewLine + "Headers:" + Environment.NewLine + httpRequestMessage.Headers.ToString() +
                (httpRequestMessage.Content != null ? httpRequestMessage.Content.Headers.ToString() : "") +
                ((addContent && httpRequestMessage.Content != null) ?
                Environment.NewLine + "Content:" +
                Environment.NewLine +
                ((httpRequestMessage.Content.Headers.ContentType?.MediaType != "application/octet-stream" || AlwaysLogBytes) ?
                await httpRequestMessage.Content.ReadAsStringAsync() :
                $"[{httpRequestMessage.Content.Headers.ContentLength} bytes]") :
                "");
        }

        public static async Task<string> ToStringAsync(this HttpResponseMessage httpResponseMessage, bool addContent)
        {
            return
                $"StatusCode: {(int)httpResponseMessage.StatusCode}" +
                $", ReasonPhrase: '{httpResponseMessage.ReasonPhrase}'" +
                $", Version: {httpResponseMessage.Version}" +
                Environment.NewLine + "Headers:" + Environment.NewLine + httpResponseMessage.Headers.ToString() +
                (httpResponseMessage.Content != null ? httpResponseMessage.Content.Headers.ToString() : "") +
                ((addContent && httpResponseMessage.Content != null) ?
                Environment.NewLine + "Content:" +
                Environment.NewLine +
                ((httpResponseMessage.Content.Headers.ContentType?.MediaType != "application/octet-stream" || AlwaysLogBytes) ?
                await httpResponseMessage.Content.ReadAsStringAsync() :
                $"[{httpResponseMessage.Content.Headers.ContentLength} bytes]") :
                "");
        }

        public static async Task<string> ToStringAsync(this HttpRequest httpRequest)
        {
            string? content = null;
            if (httpRequest.ContentLength > 0)
            {
                if (httpRequest.ContentType != "application/octet-stream" || AlwaysLogBytes || httpRequest.ContentLength <= LogBytesThreshold)
                {
                    httpRequest.EnableBuffering();

                    var buffer = new byte[Convert.ToInt32(httpRequest.ContentLength)];

                    await httpRequest.Body.ReadAsync(buffer.AsMemory(0, buffer.Length));

                    content = Encoding.UTF8.GetString(buffer);

                    httpRequest.Body.Position = 0;
                }
                else
                {
                    content = $"[{httpRequest.ContentLength} bytes]";
                }
            }

            return
                $"Protocol: {httpRequest.Protocol}" +
                $", Method: {httpRequest.Method}" +
                $", RequestUri: '{httpRequest.GetDisplayUrl()}'" +
                Environment.NewLine + "Headers:" +
                Environment.NewLine + httpRequest.Headers.Aggregate("", (headers, header) => $"{headers}{header.Key}: {header.Value}{Environment.NewLine}") +
                (content != null ? Environment.NewLine + "Content:" + Environment.NewLine + content : "");
        }

        public static async Task<string> ToStringAsync(this HttpResponse httpResponse)
        {
            string? content = null;
            var contentLength = httpResponse.ContentLength ?? httpResponse.Body.Length;
            if (contentLength > 0)
            {
                if (httpResponse.ContentType != "application/octet-stream" || AlwaysLogBytes || contentLength <= LogBytesThreshold)
                {
                    httpResponse.Body.Seek(0, SeekOrigin.Begin);

                    content = await new StreamReader(httpResponse.Body).ReadToEndAsync();
                }
                else
                {
                    content = $"[{contentLength} bytes]";
                }
            }

            return
                $"StatusCode: {httpResponse.StatusCode}" +
                Environment.NewLine + "Headers:" +
                Environment.NewLine + httpResponse.Headers.Aggregate("", (headers, header) => $"{headers}{header.Key}: {header.Value}{Environment.NewLine}") +
                (content != null ? Environment.NewLine + "Content:" + Environment.NewLine + content : "");
        }
    }
}
