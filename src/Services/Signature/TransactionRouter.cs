using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Services.New;

namespace Services.Signature
{
    public class TransactionRouter : ITransactionRouter, IDisposable
    {
        private readonly TimeSpan      _cacheDuration;
        private readonly IOwnerService _ownerService;
        private readonly SemaphoreSlim _semaphore;

        private int            _addressIndex;
        private string[]       _addresses;
        private DateTimeOffset _lastOwnerCheck;


        public TransactionRouter(
            IOwnerService ownerService)
        {
            _addresses      = new string[0];
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
                    _addresses      = (await _ownerService.GetAll()).Select(x => x.Address).ToArray();
                    _addressIndex   = 0;
                    _lastOwnerCheck = DateTimeOffset.UtcNow;
                }

                if (_addresses.Length > 0)
                {
                    var nextAddress = _addresses[_addressIndex];

                    _addressIndex = (_addressIndex + 1) % _addresses.Length;

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
    }
}