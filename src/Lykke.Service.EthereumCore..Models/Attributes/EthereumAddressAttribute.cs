using Lykke.Service.EthereumCore.Services.Coins;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Lykke.Service.EthereumCore.Models.Attributes
{
    [AttributeUsage(AttributeTargets.Property |
  AttributeTargets.Field, AllowMultiple = false)]
    sealed public class EthereumAddressAttribute : ValidationAttribute
    {
        private readonly bool _allowsEmpty;

        public EthereumAddressAttribute(bool allowsEmpty = false)
        {
            _allowsEmpty = allowsEmpty;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            string address = value as string;
            if (address == null)
            {
                if (!_allowsEmpty)
                {
                    return new ValidationResult($"Address should not be null ({validationContext.DisplayName})");
                }
                else
                {
                    return null;
                }
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
