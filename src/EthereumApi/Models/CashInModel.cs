using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EthereumApi.Models
{
    public class CashInModel : BaseCoinRequestModel
    {
        [Required]
        public string Coin { get; set; }

        [Required]
        public string Receiver { get; set; }

        [Required]
        [RegularExpression("^[1-9][0-9]*$")] //big int
        public string Amount { get; set; }
    }
}
