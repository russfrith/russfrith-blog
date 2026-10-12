# Pinned deliberately, not floated to `11.0`: the post's claims (e.g. Identity lockout being
# TimeProvider-aware) are specific to this exact RC1 build. A floating tag would silently start
# pointing at GA bits later and no longer match what's described. Confirmed present on
# mcr.microsoft.com as of 2026-10-11: `docker manifest inspect mcr.microsoft.com/dotnet/sdk:11.0.100-rc.1`.
FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.1

WORKDIR /src

# Restore first, with only the project/solution files, so the NuGet restore layer only
# invalidates when a .csproj or .sln actually changes, not on every source edit.
COPY TimeProviderDemo.slnx ./
COPY src/TimeProviderDemo/TimeProviderDemo.csproj src/TimeProviderDemo/
COPY tests/TimeProviderDemo.Tests/TimeProviderDemo.Tests.csproj tests/TimeProviderDemo.Tests/
RUN dotnet restore

COPY . .

ENTRYPOINT ["dotnet", "test", "--logger", "console;verbosity=normal"]
