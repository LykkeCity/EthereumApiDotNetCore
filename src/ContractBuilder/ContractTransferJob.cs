//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Lykke.Service.EthereumCore.AzureRepositories.Repositories;
//using Microsoft.WindowsAzure.Storage.Table;
//using AzureStorage.Tables;

//namespace ContractBuilder
//{
//    public class ContractTransferJob
//    {
//        public async Task Start(string fromConnString, string toConnString)
//        {
//            try
//            {
//                if (string.IsNullOrWhiteSpace(fromConnString) || string.IsNullOrWhiteSpace(toConnString))
//                {
//                    Console.WriteLine("App settings is empty");
//                    return;
//                }

//                var storageFrom = new AzureTableStorage<WalletUserEntity>(fromConnString, "WalletCredentials", null);
//                var storageTo = new AzureTableStorage<UserContractEntity>(toConnString, "UserContracts", null);

//                var data =
//                    (await
//                            storageFrom.GetDataAsync("EthConversionWallet", x => !string.IsNullOrWhiteSpace(x.EthConversionWalletAddress)))
//                        .ToList();

//                Console.WriteLine($"Found {data.Count} records with non-empty contract address");

//                for (int i = 0; i < data.Count; i++)
//                {
//                    await storageTo.InsertOrMergeAsync(new UserContractEntity
//                    {
//                        PartitionKey = UserContractEntity.GenerateParitionKey(),
//                        RowKey = data[i].EthConversionWalletAddress,
//                        CreateDt = DateTime.UtcNow
//                    });

//                    if (i > 0 && i % 10 == 0)
//                        Console.WriteLine($"Processed {i} of {data.Count}");
//                }

//                Console.WriteLine("Job done!");
//            }
//            catch (Exception e)
//            {
//                Console.WriteLine(e.Message);
//                Console.WriteLine("Job failed!");
//            }
//        }
//    }

//    public class WalletUserEntity : TableEntity
//    {
//        public string EthConversionWalletAddress { get; set; }
//    }
//}
