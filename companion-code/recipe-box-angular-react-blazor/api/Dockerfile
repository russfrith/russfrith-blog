# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY shared/RecipeBox.Shared/RecipeBox.Shared.csproj shared/RecipeBox.Shared/
COPY api/RecipeBox.Api/RecipeBox.Api.csproj api/RecipeBox.Api/
RUN dotnet restore api/RecipeBox.Api/RecipeBox.Api.csproj
COPY shared/RecipeBox.Shared/ shared/RecipeBox.Shared/
COPY api/RecipeBox.Api/ api/RecipeBox.Api/
RUN dotnet publish api/RecipeBox.Api/RecipeBox.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
RUN mkdir -p /data/images && chown -R $APP_UID /data
COPY --from=build /app .
COPY seed/ /app/seed/
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "RecipeBox.Api.dll"]
