using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lykke.Service.EthereumCore.Attributes;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Lykke.Service.EthereumCore.Filters.Swagger
{
    public class ApiKeyHeaderAccessTokenOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            var filterPipeline = context.ApiDescription.ActionDescriptor.FilterDescriptors;
            var isTokenAccess = filterPipeline.Select(filterInfo => filterInfo.Filter).Any(filter => filter is ApiKeyAuthorizeAttribute);
            if (isTokenAccess)
            {
                if (operation.Parameters == null)
                    operation.Parameters = new List<IParameter>();

                operation.Parameters.Add(new NonBodyParameter
                {
                    Name = "ApiKey",
                    In = "header",
                    Description = "Api key",
                    Required = true,
                    Type = "string"
                });
            }
        }
    }
}
