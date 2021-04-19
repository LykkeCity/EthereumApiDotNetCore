using System;
using System.Net;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Core.Exceptions;
using Lykke.Service.EthereumCore.Core.PassToken;
using Lykke.Service.EthereumCore.PassTokenIntegration;
using Lykke.Service.EthereumCore.PassTokenIntegration.Exceptions;
using Lykke.Service.EthereumCore.PassTokenIntegration.Models.Requests;

namespace Lykke.Service.EthereumCore.Services.PassToken
{
    public class BlockPassService : IBlockPassService
    {
        private const string _addressType = "eth";
        private readonly IBlockPassClient _blockPassClient;

        public BlockPassService(IBlockPassClient blockPassClient)
        {
            _blockPassClient = blockPassClient;
        }

        public async Task<string> AddToWhiteListAsync(string address)
        {
            var addressRequest = new EthAddressRequest()
            {
                AddressType = _addressType,
                Address = address
            };

            EthAddressResponse response = null;

            try
            {
                response = await _blockPassClient.WhitelistAddressAsync(addressRequest);
            }
            catch (NotOkException e)
            {
                if (e.HttpCode == (int)HttpStatusCode.Conflict)
                    throw  new ClientSideException(ExceptionType.EntityAlreadyExists,
                        "Address was passed to BlockPass already.");

                if (e.HttpCode == (int)HttpStatusCode.Forbidden)
                    throw new ClientSideException(ExceptionType.MissingRequiredParams, 
                        "Api key in settings is wrong: " + e.Message);

                if (e.HttpCode == (int) HttpStatusCode.InternalServerError)
                    throw new ClientSideException(ExceptionType.None, 
                        "Unknown BlockPass error: " +e.Message);

                throw new ClientSideException(ExceptionType.None, "Unknown exception: " + e.Message);
            }
            catch (Exception e)
            {
                throw;
            }

            return response.Data.TicketId;
        }
    }
}

//- Response: (details of error response please see Appendix)
//- Code 201: new ticket id created
//- Code 404: API not found
//- Code 403: API_Key is invalid
//- Code 400: bad request(invalid argument or missing argument)
//- Code 409: Ethereum address has already been whitelisted
//- Code 500: internal error
