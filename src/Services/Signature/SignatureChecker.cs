using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Util;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Services.Signature
{
    public interface ISignatureChecker
    {
        Task<bool> CheckTransactionSign(string from, string signedTrHex);
    }

    public class SignatureChecker : ISignatureChecker
    {
        private readonly AddressUtil _addressUtil;

        public SignatureChecker()
        {
            _addressUtil = new AddressUtil();
        }

        public async Task<bool> CheckTransactionSign(string from, string signedTrHex)
        {
            var transaction = new Nethereum.Signer.TransactionChainId(signedTrHex.HexToByteArray());
            string signedBy = transaction.Key.GetPublicAddress();

            return _addressUtil.ConvertToChecksumAddress(from) == _addressUtil.ConvertToChecksumAddress(signedBy);
        }
    }
}
