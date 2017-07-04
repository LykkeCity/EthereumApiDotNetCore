using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Util;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Services.Signature
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
            Nethereum.Signer.Transaction transaction = new Nethereum.Signer.Transaction(signedTrHex.HexToByteArray());
            string signedBy = transaction.Key.GetPublicAddress();

            return _addressUtil.ConvertToChecksumAddress(from) == _addressUtil.ConvertToChecksumAddress(signedBy);
        }
    }
}
