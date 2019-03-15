# Lykke.BilService.ErcWithdraw

## How to withdraw erc 20 from eth deposit address

* build console app with dotnet build
* use dotnet Lykke.BilService.ErcWithdraw [args] to use tool
* needed arguments are:
            fromAddress
            toAddress
            hotWalletAddress
            amountToTransfer
            erc20ContractAddress
            ethereumCoreApiUrl
            ethBilApiUrl
            signFacadeApiUrl
            signFacadeApiKey
            parityUrl
            gasLimit

dotnet Lykke.BilService.ErcWithdraw fromAddress toAddress hotWalletAddress amountToTransfer erc20ContractAddress ethereumCoreApiUrl ethBilApiUrl signFacadeApiUrl signFacadeApiKey parityUrl gasLimit
Console output example:

```
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
    Stopping address observation
    Getting gas price
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Transferring ETH for erc20 transfer
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Building
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Signing
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Broadcasting
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Waiting for tr to complete
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Waiting for 03bb9738-246c-4b18-913a-1116368d6b44 to complete
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Waiting for 03bb9738-246c-4b18-913a-1116368d6b44 to complete
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Waiting for 03bb9738-246c-4b18-913a-1116368d6b44 to complete
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Waiting for 03bb9738-246c-4b18-913a-1116368d6b44 to complete
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Waiting for 03bb9738-246c-4b18-913a-1116368d6b44 to complete
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Waiting for 03bb9738-246c-4b18-913a-1116368d6b44 to complete
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Waiting for 03bb9738-246c-4b18-913a-1116368d6b44 to complete
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Waiting for 03bb9738-246c-4b18-913a-1116368d6b44 to complete
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Waiting for 03bb9738-246c-4b18-913a-1116368d6b44 to complete
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Waiting for 03bb9738-246c-4b18-913a-1116368d6b44 to complete
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Waiting for 03bb9738-246c-4b18-913a-1116368d6b44 to complete
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Waiting for 03bb9738-246c-4b18-913a-1116368d6b44 to complete
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Waiting for 03bb9738-246c-4b18-913a-1116368d6b44 to complete
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Waiting for 03bb9738-246c-4b18-913a-1116368d6b44 to complete
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Waiting for 03bb9738-246c-4b18-913a-1116368d6b44 to complete
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Waiting for 03bb9738-246c-4b18-913a-1116368d6b44 to complete
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Waiting for 03bb9738-246c-4b18-913a-1116368d6b44 to complete
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Waiting for 03bb9738-246c-4b18-913a-1116368d6b44 to complete
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Waiting for 03bb9738-246c-4b18-913a-1116368d6b44 to complete
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Estimate Transaction
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Getting nonce
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Signing erc20 transfer tr
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Sending tr
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Transaction has been sent 0xa8b3c5d66f868c09494e84b24a983d7caf6669e320cd3a03a88c09cc0c2400e7
info: Lykke.BilService.EthereumApi.ErcWithdraw[0]
      Start balance observation
```
