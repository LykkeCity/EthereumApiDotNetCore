using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Core.Repositories
{
    public interface IAddressNonce
    {
        string Address { get; set; }
        BigInteger Nonce { get; set; }
    }

    public class AddressNonce : IAddressNonce
    {
        public string Address { get; set; }
        public BigInteger Nonce { get; set; }
    }

    public interface INonceRepository
    {
        Task SaveAsync(IAddressNonce nonce);
        Task CleanAsync();
        Task<IAddressNonce> GetAsync(string address);
    }
}
