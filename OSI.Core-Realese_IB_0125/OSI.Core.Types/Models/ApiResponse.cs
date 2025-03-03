using Microsoft.Extensions.Logging;
using OSI.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace OSI.Core.Models
{
    public class ApiResponse
    {
        protected const int defaultCode = 0;
        protected const string defaultMessage = "Success";

        public ApiResponse(int code = defaultCode, string message = defaultMessage)
        {
            Code = code;
            Message = message;
        }

        /// <summary>
        /// Код
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Сообщение
        /// </summary>
        public string Message { get; set; }

        public ApiResponse<T> ToApiResponse<T>(T? result = default) => new ApiResponse<T>(Code, Message, result);

        public static ApiResponse Create(int code = defaultCode, string message = defaultMessage) => new ApiResponse(code, message);

        public static ApiResponse<T> Create<T>(int code = defaultCode, string message = defaultMessage, T? result = default) => new ApiResponse<T>(code, message, result);

        public static ApiResponse CreateEx(Action action, ILogger logger = null, [CallerMemberName] string method = null)
        {
            var apiResponse = new ApiResponse();
            try
            {
                action();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "{Method} Error", method);
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        public static async Task<ApiResponse> CreateEx(Func<Task> action, ILogger logger = null, [CallerMemberName] string method = null)
        {
            var apiResponse = new ApiResponse();
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "{Method} Error", method);
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        public static ApiResponse<T> CreateEx<T>(Func<T> func, ILogger logger = null, [CallerMemberName] string method = null)
        {
            var apiResponse = new ApiResponse<T>();
            try
            {
                apiResponse.Result = func();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "{Method} Error", method);
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        public static async Task<ApiResponse<T>> CreateEx<T>(Func<Task<T>> func, ILogger logger = null, [CallerMemberName] string method = null)
        {
            var apiResponse = new ApiResponse<T>();
            try
            {
                apiResponse.Result = await func();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "{Method} Error", method);
                apiResponse.FromEx(ex);
            }
            return apiResponse;
        }

        public void FromEx(Exception ex, int code = -1)
        {
            Message = ex.Message;
            Code = ex is not ApiException apiEx ? code : apiEx.Code;
        }

        public void FromError(string error, int code = -1)
        {
            Code = code;
            Message = error;
        }

        public void From(ApiResponse apiResponse)
        {
            Code = apiResponse.Code;
            Message = apiResponse.Message;
        }
    }

    public class ApiResponse<T> : ApiResponse
    {
        public ApiResponse(int code = defaultCode, string message = defaultMessage, T? result = default) : base(code, message)
        {
            Result = result;
        }

        /// <summary>
        /// Результат
        /// </summary>
        public T? Result { get; set; }

        public ApiResponse ToApiResponse() => new ApiResponse(Code, Message);

        public static ApiResponse<T> Create(int code = defaultCode, string message = defaultMessage, T? result = default) => new ApiResponse<T>(code, message, result);

        public void From<U>(ApiResponse<U> apiResponse, T result)
        {
            Code = apiResponse.Code;
            Message = apiResponse.Message;
            Result = result;
        }
    }
}
