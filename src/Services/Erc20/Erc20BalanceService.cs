using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using BusinessModels.Erc20;
using EthereumSamuraiApiCaller;
using EthereumSamuraiApiCaller.Models;

namespace Services.Erc20
{
    public interface IErc20BalanceService
    {
        Task<IEnumerable<AddressTokenBalance>> GetBalancesForAddress(
            string address,
            IEnumerable<string> erc20TokenAddresses);
    }

    public class Erc20BalanceService : IErc20BalanceService
    {
        private readonly IEthereumSamuraiApi _ethereumSamuraiApi;

        public Erc20BalanceService(IEthereumSamuraiApi ethereumSamuraiApi)
        {
            _ethereumSamuraiApi = ethereumSamuraiApi;
        }

        public async Task<IEnumerable<AddressTokenBalance>> GetBalancesForAddress(
            string address,
            IEnumerable<string> erc20TokenAddresses)
        {
            var response = await _ethereumSamuraiApi.ApiErc20BalanceGetErc20BalanceByAddressPostAsync
            (
                address,
                erc20TokenAddresses?.ToList()
            );


            return (response as IEnumerable<Erc20BalanceResponse>)?.Select(x => new AddressTokenBalance
            {
                Balance           = BigInteger.Parse(x.Amount),
                Erc20TokenAddress = x.Contract,
                UserAddress       = x.Address
            });
        }
    }
}