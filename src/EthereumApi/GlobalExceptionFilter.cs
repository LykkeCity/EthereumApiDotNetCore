using System;
using System.Net;
using Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Common.Log;

namespace EthereumApi
{
    public class GlobalExceptionFilter : IExceptionFilter, IDisposable
    {
        private readonly ILog _logger;

        public GlobalExceptionFilter(ILog logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            var controller = context.RouteData.Values["controller"];
            var action = context.RouteData.Values["action"];

            int httpCode = 500;
            ApiException ex;

            ExceptionType type = ExceptionType.None;
            string message = "Internal server error. Try again.";
            ClientSideException clientSideException = context.Exception as ClientSideException;
            if (clientSideException != null)
            {
                type = clientSideException.ExceptionType;
                httpCode = 400;
                message = clientSideException.Message;
            }

            _logger.WriteErrorAsync("ApiException", "EthereumApi", $"Controller: {controller}, action: {action}", context.Exception);

            ex = new ApiException
            {
                Error = new ApiError
                {
                    Code = type,
                    Message = message
                }
            };

            context.Result = new ObjectResult(ex)
            {
                StatusCode = httpCode,
                DeclaredType = typeof(ApiException)
            };
        }

        public void Dispose()
        {

        }
    }

    public class ApiException
    {
        public ApiError Error { get; set; }
    }

    public class ApiError
    {
        public ExceptionType Code { get; set; }
        public string Message { get; set; }
    }
}
