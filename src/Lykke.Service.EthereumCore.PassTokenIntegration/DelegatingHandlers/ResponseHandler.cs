using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.PassTokenIntegration.Exceptions;
using Lykke.Service.EthereumCore.PassTokenIntegration.Models.Responses;

namespace Lykke.Service.EthereumCore.PassTokenIntegration.DelegatingHandlers
{
    internal class ResponseHandler : DelegatingHandler
    {
        public ResponseHandler()
        {
        }

        /// <inheritdoc />
        /// <exception cref="NotOkException"></exception>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            bool isError = false;
            var result = await base.SendAsync(request, cancellationToken);

            if (result.StatusCode != HttpStatusCode.Created 
                && result.StatusCode != HttpStatusCode.OK)
            {
                var serializedResponse = await result.Content.ReadAsStringAsync();
                string errMsg = null;

                try
                {
                    var errorResponse =
                        Newtonsoft.Json.JsonConvert.DeserializeObject<BlockPassErrorResponse>(serializedResponse);
                    errMsg = errorResponse.Message;
                }
                catch (Exception e)
                {
                    errMsg = $"Could not deserialize: {serializedResponse}";
                }

                throw new NotOkException((int)result.StatusCode, errMsg);
            }

            return result;
        }
    }
}
