FROM microsoft/aspnetcore:2.0.0
ENTRYPOINT ["dotnet", "ApiRunner.dll"]
ARG source=.
WORKDIR /app
COPY $source .

EXPOSE 5000/tcp

ENV ASPNETCORE_URLS http://*:5000
