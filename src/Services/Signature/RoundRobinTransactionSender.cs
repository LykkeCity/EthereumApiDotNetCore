using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Transactions;
using Services.New;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Core.Repositories;
using System.Threading;
using System.Linq;
using System.Numerics;
using BusinessModels;
using Core.Settings;

namespace Services.Signature
{
    public interface IRoundRobinTransactionSender
    {
        Task<AddressNonceModel> GetSenderAndHisNonce();
    }

    public class RoundRobinTransactionSender : IRoundRobinTransactionSender
    {
        private readonly IWeb3 _web3;
        private readonly IOwnerService _ownerService;
        private DateTime _lastOwnersCheck;
        private List<IOwner> _owners;
        private int _currentOwnerIndex;
        private Dictionary<string, BigInteger> _ownerNonceDictionary;
        private Dictionary<string, SemaphoreSlim> _ownerSemaphoreDictionary;
        private readonly SemaphoreSlim _roundRobinSemaphore;
        private readonly IBaseSettings _baseSettings;

        public RoundRobinTransactionSender(IWeb3 web3, IOwnerService ownerService, IBaseSettings baseSettings)
        {
            _baseSettings = baseSettings;
            _ownerService = ownerService;
            _web3 = web3;
            _lastOwnersCheck = DateTime.UtcNow.AddMinutes(-5);
            _roundRobinSemaphore = new SemaphoreSlim(1, 1);
            _ownerNonceDictionary = new Dictionary<string, BigInteger>();
            _ownerSemaphoreDictionary = new Dictionary<string, SemaphoreSlim>();
            _currentOwnerIndex = 0;
        }

        public async Task<AddressNonceModel> GetSenderAndHisNonce()
        {
            IOwner currentOwner;
            try
            {
                await _roundRobinSemaphore.WaitAsync();
                if (DateTime.UtcNow - _lastOwnersCheck > TimeSpan.FromMinutes(5))
                {
                     var owners = new List<IOwner>() { new Owner() { Address = _baseSettings.EthereumMainAccount } };
                    owners.AddRange(await _ownerService.GetAll());
                    _owners = owners;

                    var checkDictionary = owners.ToDictionary(x => x.Address);
                    foreach (var ownerAddress in _ownerSemaphoreDictionary.Keys)
                    {
                        if (!checkDictionary.ContainsKey(ownerAddress))
                        {
                            _ownerSemaphoreDictionary.Remove(ownerAddress);
                            _ownerNonceDictionary.Remove(ownerAddress);
                        }
                    }
                    _lastOwnersCheck = DateTime.UtcNow;
                }

                currentOwner = _owners[_currentOwnerIndex];
                _currentOwnerIndex = (_currentOwnerIndex + 1) % _owners.Count;
                if (!_ownerSemaphoreDictionary.ContainsKey(currentOwner.Address))
                {
                    _ownerSemaphoreDictionary[currentOwner.Address] = new SemaphoreSlim(1, 1);
                    _ownerNonceDictionary[currentOwner.Address] = -1;
                }
            }
            finally
            {
                _roundRobinSemaphore.Release();
            }

            SemaphoreSlim semaphore = null;
            try
            {
                semaphore = _ownerSemaphoreDictionary[currentOwner.Address];
                await semaphore.WaitAsync();
                var nonce = await GetNonceAsync(currentOwner.Address);

                return new AddressNonceModel()
                {
                    Address = currentOwner.Address,
                    Nonce = nonce
                };
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task<HexBigInteger> GetNonceAsync(string fromAddress)
        {
            var ethGetTransactionCount = new EthGetTransactionCount(_web3.Client);

            HexBigInteger nonce = await ethGetTransactionCount.SendRequestAsync(fromAddress).ConfigureAwait(false);
            BigInteger nonceCount = -1;
            _ownerNonceDictionary.TryGetValue(fromAddress, out nonceCount);

            if (nonce.Value <= nonceCount)
            {
                nonceCount = nonceCount + 1;
                nonce = new HexBigInteger(nonceCount);
            }
            else
            {
                nonceCount = nonce.Value;
            }

            _ownerNonceDictionary[fromAddress] = nonceCount;

            return nonce;
        }
    }
}
