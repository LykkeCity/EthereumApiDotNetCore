using Core;
using Core.Repositories;
using Core.Settings;
using Nethereum.Hex.HexTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.New
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

        public OwnerBlockchainService(IWeb3 web3, IBaseSettings baseSettings)
        {
            _baseSettings = baseSettings;
            _web3 = web3;
        }

        public async Task<string> AddOwnersToMainExchangeAsync(IEnumerable<IOwner> owners)
        {
            var contract = _web3.Eth.GetContract(_baseSettings.MainExchangeContract.Abi, _baseSettings.MainExchangeContract.Address);
            var addOwners = contract.GetFunction("addOwners");
            var ownerAddresses = owners.Select(x => x.Address).ToArray();
            var transactionHash = await addOwners.SendTransactionAsync(_baseSettings.EthereumMainAccount,
                        new HexBigInteger(Constants.GasForCoinTransaction), new HexBigInteger(0), new object[] { ownerAddresses });

            return transactionHash;
        }

        public async Task<string> RemoveOwnersFromMainExchangeAsync(IEnumerable<IOwner> owners)
        {
            var contract = _web3.Eth.GetContract(_baseSettings.MainExchangeContract.Abi, _baseSettings.MainExchangeContract.Address);
            var removeOwners = contract.GetFunction("removeOwners");
            var ownerAddresses = owners.Select(x => x.Address).ToArray();
            var transactionHash = await removeOwners.SendTransactionAsync(_baseSettings.EthereumMainAccount,
                        new HexBigInteger(Constants.GasForCoinTransaction), new HexBigInteger(0), ownerAddresses);

            return transactionHash;
        }
    }
}
