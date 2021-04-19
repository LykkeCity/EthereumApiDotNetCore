﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lykke.Service.EthereumCore.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiKeyAuthorizeAttribute : ActionFilterAttribute
    {
        public const string ApiKeyHeaderName = "ApiKey";

        public ApiKeyAuthorizeAttribute()
        {
        }

        public override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var keys = (ApiKeys)context.HttpContext.RequestServices.GetService(typeof(ApiKeys));

            if (context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var values))
            {
                var match = values.FirstOrDefault(x => keys.Keys.Contains(x));

                if (match != null)
                {
                    return base.OnActionExecutionAsync(context, next);
                }
            }

            context.Result = new UnauthorizedResult();

            return Task.FromResult(0);
        }
    }
}
