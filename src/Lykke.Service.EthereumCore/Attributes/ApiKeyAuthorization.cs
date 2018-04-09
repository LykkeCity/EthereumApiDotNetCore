using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace Lykke.Service.EthereumCore.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiKeyAuthorizeAttribute : ActionFilterAttribute
    {
        public ApiKeys ApiKeys { get; set; }

        public const string ApiKeyHeaderName = "ApiKey";

        public ApiKeyAuthorizeAttribute()
        {
        }

        public override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            //var Keys = context.HttpContext.RequestServices.GetService(typeof(ApiKeys));

            if (context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var values))
            {
                var match = values.FirstOrDefault(x => ApiKeys.Keys.Contains(x));

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
