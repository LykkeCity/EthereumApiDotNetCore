using System.Collections.Generic;
using Lykke.Service.EthereumCore.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Job.EthereumCore.Controllers
{
    [Route("api/[controller]")]
    public class OperationsController : Controller
    {
        private readonly IOperationsService _operationsService;

        public OperationsController(IOperationsService operationsService)
        {
            _operationsService = operationsService;
        }

        [HttpPost("abort/{operationId}")]
        [ProducesResponseType(typeof(List<string>), 200)]
        public IActionResult AbortOperation([FromRoute] string operationId)
        {
            _operationsService.AddOperationToAbort(operationId);

            return Ok(_operationsService.GetAllOperationsToAbort());
        }
    }
}
