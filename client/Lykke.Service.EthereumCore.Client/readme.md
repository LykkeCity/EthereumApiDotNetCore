# API Client Generation

1) Launch Api on port 5000
2) Run ```autorest readme.md``` or execute ```update.ps1```

``` yaml 
input-file: http://localhost:5000/swagger/v1/swagger.json

csharp:
  namespace: Lykke.Service.EthereumCore.Client
  output-folder: ./
```