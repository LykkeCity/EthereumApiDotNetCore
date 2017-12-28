using Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EthereumApi.Models
{
    public class CashoutModel : BaseCoinRequestModel
    { 
        public string Sign { get; set; }
    }
}
