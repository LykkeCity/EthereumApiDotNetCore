using Core.Log;
using Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Services.Coins;

namespace Services
{
	public static class RegisterDependency
	{
		public static void RegisterServices(this IServiceCollection services)
		{
			services.AddTransient<IContractService, ContractService>();
			services.AddTransient<IPaymentService, PaymentService>();
			services.AddTransient<IEthereumQueueOutService, EthereumQueueOutService>();
			services.AddTransient<IContractQueueService, ContractQueueService>();
			services.AddTransient<IEmailNotifierService, EmailNotifierService>();
			services.AddTransient<IContractTransferTransactionService, ContractTransferTransactionService>();
			services.AddTransient<IEthereumTransactionService, EthereumTransactionService>();
			services.AddTransient<ICoinContractService, CoinContractService>();
			services.AddTransient<ICoinTransactionService, CoinTransactionService>();
			services.AddTransient<IManualEventsService, ManualEventsService>();
		}
	}
}
