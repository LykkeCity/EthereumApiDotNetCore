FROM microsoft/dotnet:1.1.2
ENTRYPOINT ["dotnet", "JobRunner.dll"]
ARG source=.
WORKDIR /app
COPY $source .
