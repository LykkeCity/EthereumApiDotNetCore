using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Util;
using SigningServiceApiCaller;
using SigningServiceApiCaller.Models;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Services.Signature
{
    public interface ISignatureService
    {
        Task<string> SignRawTransactionAsync(string fromAddress, string rawTransactionHex);
    }

    public class SignatureService : ISignatureService
    {
        private readonly ILykkeSigningAPI _signatureApi;
        private readonly AddressUtil _addressUtil;

        public SignatureService(ILykkeSigningAPI signatureApi)
        {
            _signatureApi = signatureApi;
            _addressUtil = new AddressUtil();
        }

        public async Task<string> SignRawTransactionAsync(string fromAddress, string rawTransactionHex)
        {
            var requestBody = new EthereumTransactionSignRequest()
            {
                FromProperty = _addressUtil.ConvertToChecksumAddress(fromAddress),
                Transaction = rawTransactionHex
            };

            var response = await _signatureApi.ApiEthereumSignPostAsync(requestBody);

            return response?.SignedTransaction.EnsureHexPrefix();
        }
    }
}
