using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AzureRepositories.Azure;
using Core.Repositories;
using Microsoft.WindowsAzure.Storage;
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

		public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
		{
			base.ReadEntity(properties, operationContext);

			foreach (var p in GetType().GetProperties().Where(x => x.PropertyType == typeof(decimal) && properties.ContainsKey(x.Name)))
				p.SetValue(this, Convert.ToDecimal(properties[p.Name].StringValue));
		}

		public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
		{
			var properties = base.WriteEntity(operationContext);

			foreach (var p in GetType().GetProperties().Where(x => x.PropertyType == typeof(decimal)))
				properties.Add(p.Name, new EntityProperty(p.GetValue(this).ToString()));

			return properties;
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

		public async Task ProcessContractsAsync(Action<IEnumerable<IUserContract>> chunks)
		{
			await _table.GetDataByChunksAsync(chunks);
		}

		public async Task ReplaceAsync(IUserContract contract)
		{
			var entity = UserContractEntity.Create(contract);

			await _table.ReplaceAsync(entity, contractEntity =>
			 {
				 contractEntity.BalanceNotChangedCount = entity.BalanceNotChangedCount;
				 contractEntity.LastBalance = entity.LastBalance;
				 return contractEntity;
			 });
		}

		
	}
}
