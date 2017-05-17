using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Common.Log;
using Core.Exceptions;
using Microsoft.AspNetCore.Builder;

namespace EthereumApi.Middleware
{

    public sealed class ApiExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILog _logger;

        public ApiExceptionHandler(
            RequestDelegate next,
            ILog logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                //Handle 400 (BadRequest)
                if (ex is ClientSideException)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync(ex.Message);

                    return;
                }

                throw;
            }
        }
    }

    public static class ApiExceptionHandlerEx
    {
        public static void RegisterExceptionHandler(this IApplicationBuilder builder, ILog logger)
        {
            builder.UseMiddleware<ApiExceptionHandler>(logger);
        }
    }
}
