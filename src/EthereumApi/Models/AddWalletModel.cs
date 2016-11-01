using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EthereumApi.Models
{
    public class AddWalletModel
    {
        [Required]
        public string UserContract { get; set; }

        [Required]
        public string UserWallet { get; set; }
    }
}
