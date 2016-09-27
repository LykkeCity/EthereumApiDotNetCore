using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureRepositories.Azure;
using Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Repositories
{
	public class UserContractEntity : TableEntity, IUserContract
	{
		public static string GenerateParitionKey()
		{
			return "UserContract";
		}

		public string Address => RowKey;
		public DateTime CreateDt { get; set; }

		public int BalanceNotChangedCount { get; set; }

		public decimal LastBalance { get; set; }

		public static UserContractEntity Create(IUserContract userContract)
		{
			return new UserContractEntity
			{
				PartitionKey = GenerateParitionKey(),
				RowKey = userContract.Address,
				CreateDt = userContract.CreateDt,
				LastBalance = userContract.LastBalance,
				BalanceNotChangedCount = userContract.BalanceNotChangedCount
			};
		}


	}

	public class UserContractRepository : IUserContractRepository
	{
		private readonly INoSQLTableStorage<UserContractEntity> _table;

		public UserContractRepository(INoSQLTableStorage<UserContractEntity> table)
		{
			_table = table;
		}

		public async Task AddAsync(IUserContract contract)
		{
			var entity = UserContractEntity.Create(contract);

			await _table.InsertAsync(entity);
		}

		public async Task ReplaceAsync(IUserContract contract)
		{
			var entity = UserContractEntity.Create(contract);

			await _table.ReplaceAsync(entity, contractEntity =>
			 {
				 contractEntity.BalanceNotChangedCount = contractEntity.BalanceNotChangedCount;
				 contractEntity.LastBalance = contractEntity.LastBalance;
				 return contractEntity;
			 });
		}


		public async Task<IEnumerable<IUserContract>> GetContractsAsync()
		{
			return await _table.GetDataAsync(UserContractEntity.GenerateParitionKey());
		}
	}
}
