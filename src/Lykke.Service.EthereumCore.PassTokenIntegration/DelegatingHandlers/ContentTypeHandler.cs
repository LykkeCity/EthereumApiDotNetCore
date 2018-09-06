using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.PassTokenIntegration.DelegatingHandlers
{
    internal class ContentTypeHandler : DelegatingHandler
    {
        public ContentTypeHandler()
        { 
        }

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string type = "application/json";
            request.Headers.TryAddWithoutValidation("Accept", type);
            request.Headers.TryAddWithoutValidation("Content-Type", type);

            return base.SendAsync(request, cancellationToken);
        }
    }
}
