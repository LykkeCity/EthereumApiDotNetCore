using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Core.Utils
{
    public static class ExceptionExtensions
    {
		/// <summary>
		/// Returns true if ethereum node is down
		/// </summary>		
	    public static bool IsNodeDown(this Exception ex)
	    {
		    return ex.Message.Contains("when trying to send rpc");
	    }

    }
}
