FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/StubService/StubService.csproj src/StubService/
RUN dotnet restore src/StubService/StubService.csproj
COPY src/StubService/ src/StubService/
RUN dotnet publish src/StubService/StubService.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "StubService.dll"]
