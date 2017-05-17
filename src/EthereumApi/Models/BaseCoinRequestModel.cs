using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EthereumApi.Models
{
    public class BaseCoinRequestModel
    {
        [Required]
        public Guid Id { get; set; }
    }
}
