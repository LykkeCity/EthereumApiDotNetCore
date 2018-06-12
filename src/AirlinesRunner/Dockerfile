FROM microsoft/aspnetcore:2.0
ENTRYPOINT ["dotnet", "Lykke.Service.AirlinesJobRunner.dll"]
ARG source=.
WORKDIR /app
COPY $source .

EXPOSE 5000/tcp

ENV ASPNETCORE_URLS http://*:5000
