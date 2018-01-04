using Lykke.Service.EthereumCore.Core.Utils;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Util;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Lykke.Service.EthereumCore.Services
{
    public interface IHashCalculator
    {
        byte[] GetHash(Guid id, string coinAddress, string clientAddr, string toAddr, BigInteger amount);
    }

    public class HashCalculator : IHashCalculator
    {
        public HashCalculator()
        {}

        public byte[] GetHash(Guid id, string coinAddress, string clientAddr, string toAddr, BigInteger amount)
        {
            var strForHash = EthUtils.GuidToByteArray(id).ToHex() +
                                        coinAddress.HexToByteArray().ToHex() +
                                        clientAddr.HexToByteArray().ToHex() +
                                        toAddr.HexToByteArray().ToHex() +
                                        EthUtils.BigIntToArrayWithPadding(amount).ToHex();

            var hash = new Sha3Keccack().CalculateHash(strForHash.HexToByteArray());

            return hash;
        }
    }
}
