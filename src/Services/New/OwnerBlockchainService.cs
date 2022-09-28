using Lykke.Service.EthereumCore.Core;
using Lykke.Service.EthereumCore.Core.Repositories;
using Lykke.Service.EthereumCore.Core.Settings;
using Nethereum.Hex.HexTypes;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Nethereum.Web3;
using Common;

namespace Lykke.Service.EthereumCore.Services.New
{
    /*
        function addOwners(address[] owners) returns (bool isOk);
        function removeOwners(address[] owners) returns (bool isOk);
        function isOwner(address ownerAddress) returns (bool isOwner);
    */
    public interface IOwnerBlockchainService
    {
        Task<string> AddOwnersToMainExchangeAsync(IEnumerable<IOwner> owners);
        Task<string> RemoveOwnersFromMainExchangeAsync(IEnumerable<IOwner> owners);
    }

    public class OwnerBlockchainService : IOwnerBlockchainService
    {
        private readonly IWeb3 _web3;
        private IBaseSettings _baseSettings;
        private readonly ILog _log;

        public OwnerBlockchainService(IWeb3 web3, IBaseSettings baseSettings, ILog log)
        {
            _baseSettings = baseSettings;
            _log = log;
            _web3 = web3;
        }

        public async Task<string> AddOwnersToMainExchangeAsync(IEnumerable<IOwner> owners)
        {
            var contract = _web3.Eth.GetContract(_baseSettings.MainExchangeContract.Abi, _baseSettings.MainExchangeContract.Address);
            var addOwners = contract.GetFunction("addOwners");
            var ownerAddresses = owners.Select(x => x.Address).ToArray();

            _log.WriteInfoAsync(nameof(OwnerBlockchainService), nameof(AddOwnersToMainExchangeAsync),
                new
                {
                    _baseSettings.GasForCoinTransaction,
                    OwnerAddress = ownerAddresses
                }.ToJson(),
                "Adding Owners to main exchange ");
            
            var transactionHash = await addOwners.SendTransactionAsync(
                _baseSettings.EthereumMainAccount, 
                new HexBigInteger(_baseSettings.GasForCoinTransaction),
                new HexBigInteger(0), new object[] { ownerAddresses });

            return transactionHash;
        }

        public async Task<string> RemoveOwnersFromMainExchangeAsync(IEnumerable<IOwner> owners)
        {
            var contract = _web3.Eth.GetContract(_baseSettings.MainExchangeContract.Abi, _baseSettings.MainExchangeContract.Address);
            var removeOwners = contract.GetFunction("removeOwners");
            var ownerAddresses = owners.Select(x => x.Address).ToArray();
            
            _log.WriteInfoAsync(nameof(OwnerBlockchainService), nameof(RemoveOwnersFromMainExchangeAsync),
                new
                {
                    _baseSettings.GasForCoinTransaction,
                    OwnerAddresses = ownerAddresses
                }.ToJson(),
                "Removing owners from Main Exchange");
            
            var transactionHash = await removeOwners.SendTransactionAsync(_baseSettings.EthereumMainAccount,
                        new HexBigInteger(_baseSettings.GasForCoinTransaction), new HexBigInteger(0), ownerAddresses);

            return transactionHash;
        }
    }
}