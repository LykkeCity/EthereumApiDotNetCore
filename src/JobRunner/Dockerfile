FROM microsoft/dotnet:2.0-runtime
ENTRYPOINT ["dotnet", "JobRunner.dll"]
ARG source=.
WORKDIR /app
COPY $source .
