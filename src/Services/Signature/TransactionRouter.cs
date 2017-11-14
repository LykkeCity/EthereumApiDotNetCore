using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Settings;
using Services.New;

namespace Services.Signature
{
    public class TransactionRouter : ITransactionRouter, IDisposable
    {
        private readonly IBaseSettings _baseSettings;
        private readonly TimeSpan      _cacheDuration;
        private readonly IOwnerService _ownerService;
        private readonly SemaphoreSlim _semaphore;

        private int            _addressIndex;
        private List<string>   _addresses;
        private DateTimeOffset _lastOwnerCheck;


        public TransactionRouter(
            IBaseSettings baseSettings,
            IOwnerService ownerService)
        {
            _addresses      = new List<string>();
            _baseSettings   = baseSettings;
            _cacheDuration  = TimeSpan.FromMinutes(5);
            _ownerService   = ownerService;
            _lastOwnerCheck = DateTimeOffset.MinValue;
            _semaphore      = new SemaphoreSlim(1,1);
        }


        public void Dispose()
        {
            _semaphore.Dispose();
        }

        public async Task<string> GetNextSenderAddressAsync()
        {
            await _semaphore.WaitAsync();

            try
            {
                if (DateTimeOffset.UtcNow - _lastOwnerCheck > _cacheDuration)
                {
                    _addresses      = await GetAddressesAsync();
                    _addressIndex   = 0;
                    _lastOwnerCheck = DateTimeOffset.UtcNow;
                }

                if (_addresses.Count > 0)
                {
                    var nextAddress = _addresses[_addressIndex];

                    _addressIndex = (_addressIndex + 1) % _addresses.Count;

                    return nextAddress;
                }
                else
                {
                    throw new InvalidOperationException("Addresses for transaction router are not specified"); 
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<List<string>> GetAddressesAsync()
        {
            var addresses = new List<string>
            {
                _baseSettings.EthereumMainAccount
            };

            addresses.AddRange((await _ownerService.GetAll()).Select(x => x.Address));

            return addresses;
        }
    }
}