using System;
using System.Net;
using Core.Exceptions;
using Core.Log;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

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

			ApiException ex;

			var exception = context.Exception as BackendException;
			if (exception != null)
			{
				ex = new ApiException
				{
					Error = new ApiError
					{
						Code = exception.Type,
						Message = exception.Message
					}
				};
			}
			else
			{
				_logger.WriteError("ApiException", "EthereumApi", $"Controller: {controller}, action: {action}", context.Exception);
				ex = new ApiException
				{
					Error = new ApiError
					{
						Code = BackendExceptionType.None,
						Message = "Internal server error. Try again."
					}
				};
			}

			context.Result = new ObjectResult(ex)
			{
				StatusCode = 500,
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
		public BackendExceptionType Code { get; set; }
		public string Message { get; set; }
	}
}
