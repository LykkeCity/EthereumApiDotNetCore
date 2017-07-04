using Core.Exceptions;
using Nethereum.RPC.Eth.Transactions;
using Services.Signature;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Services.Transactions
{
    public interface IRawTransactionSubmitter
    {
        Task<string> SubmitSignedTransaction(string from, string signedTrHex);
    }

    public class RawTransactionSubmitter : IRawTransactionSubmitter
    {
        private readonly IWeb3 _web3;
        private readonly ISignatureChecker _signatureChecker;

        public RawTransactionSubmitter(IWeb3 web3, ISignatureChecker signatureChecker)
        {
            _signatureChecker = signatureChecker;
            _web3 = web3;
        }

        public async Task<string> SubmitSignedTransaction(string from, string signedTrHex)
        {
            bool isSignedRight = await _signatureChecker.CheckTransactionSign(from, signedTrHex);
            if (!isSignedRight)
            {
                throw new ClientSideException(ExceptionType.WrongSign, "WrongSign");
            }

            var ethSendTransaction = new EthSendRawTransaction(_web3.Client);
            string transactionHex = await ethSendTransaction.SendRequestAsync(signedTrHex);

            return transactionHex;
        }
    }
}
