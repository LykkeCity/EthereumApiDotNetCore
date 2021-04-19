using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.PassTokenIntegration.DelegatingHandlers
{
    internal class ApiKeyHeaderHandler : DelegatingHandler
    {
        private readonly string _apiKey;

        public ApiKeyHeaderHandler(string apiKey)
        {
            _apiKey = apiKey;
        }

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.TryAddWithoutValidation("Authorization", _apiKey);

            return base.SendAsync(request, cancellationToken);
        }
    }
}
