using System;
using System.Net.Http;

// ReSharper disable once CheckNamespace
namespace Lykke.Service.EthereumCore.Client
{
    public partial class EthereumCoreAPI
    {
        /// <inheritdoc />
        /// <summary>
        /// Should be used to prevent memory leak in RetryPolicy
        /// </summary>
        public EthereumCoreAPI(Uri baseUri, HttpClient client) : base(client)
        {
            Initialize();

            BaseUri = baseUri ?? throw new ArgumentNullException("baseUri");
        }
    }
}