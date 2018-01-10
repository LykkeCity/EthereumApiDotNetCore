FROM microsoft/aspnetcore:2.0
ENTRYPOINT ["dotnet", "JobRunner.dll"]
ARG source=.
WORKDIR /app
COPY $source .
COPY /runtimes/debian-x64/native/libuv.so /app

EXPOSE 5000/tcp

ENV ASPNETCORE_URLS http://*:5000
