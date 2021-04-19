using System.ComponentModel.DataAnnotations;

namespace Lykke.Service.EthereumCore.Models
{
    public class CreateExternalTokenModel
    {
        [Required]
        public string TokenName{ get; set; }

        [Required]
        public bool AllowEmission { get; set; }

        [Required]
        public string TokenSymbol { get; set; }

        [Required]
        public string Version { get; set; }

        [RegularExpression("^([1-9][0-9]*)|([0])$")]
        public string InitialSupply { get; set; }

        [Required]
        public byte Divisibility { get; set; }
    }
}
