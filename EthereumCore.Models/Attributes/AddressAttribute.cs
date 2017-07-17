using Services.Coins;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EthereumApi.Models.Attributes
{
    [AttributeUsage(AttributeTargets.Property |
  AttributeTargets.Field, AllowMultiple = false)]
    sealed public class EthereumAddressAttribute : ValidationAttribute
    {
        public EthereumAddressAttribute()
        {
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            string address = value as string;
            if (address == null)
            {
                return new ValidationResult($"Address should not be null ({validationContext.DisplayName})");
            }

            IExchangeContractService exchangeContractService = (IExchangeContractService)validationContext.GetService(typeof(IExchangeContractService));
            if (!exchangeContractService.IsValidAddress(address))
            {
                return new ValidationResult($"Given value for ({validationContext.DisplayName}) is not a valid ethereum address");
            }

            return null;
        }
    }
}
