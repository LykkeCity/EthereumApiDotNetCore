using Lykke.Service.EthereumCore.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.Service.EthereumCore.Models
{
    public class CashoutModel : BaseCoinRequestModel
    { 
        public string Sign { get; set; }
    }
}
