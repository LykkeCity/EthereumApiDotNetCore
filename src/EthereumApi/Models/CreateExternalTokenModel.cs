using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EthereumApi.Models
{
    public class CreateExternalTokenModel
    {
        [Required]
        public string TokenName{ get; set; }
    }
}
