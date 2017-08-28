FROM microsoft/aspnetcore:1.1.2
ENTRYPOINT ["dotnet", "ApiRunner.dll"]
ARG source=.
WORKDIR /app
COPY $source .

EXPOSE 5000/tcp

ENV ASPNETCORE_URLS http://*:5000
