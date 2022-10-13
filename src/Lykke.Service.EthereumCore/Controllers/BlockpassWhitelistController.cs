using Lykke.Service.EthereumCore.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Lykke.Service.EthereumCore.Models.Attributes;
using Lykke.Service.EthereumCore.Core.PassToken;

namespace Lykke.Service.EthereumCore.Controllers
{
    [Route("api/blockpass/whitelist")]
    [Produces("application/json")]
    public class BlockpassWhitelistController : Controller
    {
        private readonly IBlockPassService _blockPassService;

        public BlockpassWhitelistController(IBlockPassService blockPassService)
        {
            _blockPassService = blockPassService;
        }

        [HttpPost("{address}")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(void), 404)]
        [ProducesResponseType(typeof(ApiException), 400)]
        [ProducesResponseType(typeof(ApiException), 500)]
        public async Task<IActionResult> AddAddress([FromRoute][EthereumAddress] string address)
        {
            try
            {
                var ticketId = await _blockPassService.AddToWhiteListAsync(address);

                return Ok(ticketId);
            }
            catch (ClientSideException ex)
            {
                if (ex.ExceptionType == ExceptionType.EntityAlreadyExists ||
                    ex.ExceptionType == ExceptionType.OperationWithIdAlreadyExists)
                {

                    return Ok($"Address already passed to BlockPass");
                }

                throw;
            }            
        }
    }
}
