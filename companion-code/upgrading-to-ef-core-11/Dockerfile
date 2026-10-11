# Builds and runs the EF Core 11 upgrade-guide verification program, which
# prints the actual SQL EF Core 11 generates for each example quoted in the
# post. Nothing here executes against a real database - ToQueryString()
# translates each query without opening a connection, so this only needs
# the SDK, not SQL Server.
#
# Build:
#   docker build -t ef11-upgrade-demo .
#
# Run (mount a host folder to collect the output):
#   docker run --rm -v "$PWD/results:/out" ef11-upgrade-demo
#
# Requires a preview .NET 11 SDK image, since EF Core 11 is still
# prerelease as of this writing.
FROM mcr.microsoft.com/dotnet/nightly/sdk:11.0-preview AS build
WORKDIR /src

COPY EfCore11Demo/ EfCore11Demo/

RUN dotnet build EfCore11Demo/EfCore11Demo.csproj -c Release

FROM build AS runtime
WORKDIR /src
RUN mkdir -p /out

CMD ["sh", "-c", "dotnet run --project EfCore11Demo/EfCore11Demo.csproj -c Release --no-build | tee /out/generated-sql.txt"]
