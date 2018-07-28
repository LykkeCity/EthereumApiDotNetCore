using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SigningServiceApiCaller.DelegatingHandlers
{
    public class ApiKeyHandler : DelegatingHandler
    {
        private readonly string _apiKeyValue;

        public ApiKeyHandler(string apiKeyValue)
        {
            _apiKeyValue = apiKeyValue;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.Add("apiKey", _apiKeyValue);

            var response = await base.SendAsync(request, cancellationToken);

            return response;
        }
    }
}
