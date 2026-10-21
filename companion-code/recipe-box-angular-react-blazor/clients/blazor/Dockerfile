# syntax=docker/dockerfile:1
# Build context: repo root (needs shared/ and the root Directory.*.props).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
ARG TAILWIND_VERSION=4.3.3
RUN arch=$([ "$TARGETARCH" = "arm64" ] && echo arm64 || echo x64) \
    && curl -fsSL -o /usr/local/bin/tailwindcss \
         "https://github.com/tailwindlabs/tailwindcss/releases/download/v${TAILWIND_VERSION}/tailwindcss-linux-${arch}" \
    && chmod +x /usr/local/bin/tailwindcss

WORKDIR /src
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY shared/RecipeBox.Shared/RecipeBox.Shared.csproj shared/RecipeBox.Shared/
COPY clients/blazor/RecipeBox.Blazor.csproj clients/blazor/
RUN dotnet restore clients/blazor/RecipeBox.Blazor.csproj

COPY shared/RecipeBox.Shared/ shared/RecipeBox.Shared/
COPY clients/blazor/ clients/blazor/
RUN tailwindcss -i clients/blazor/Styles/styles.css -o clients/blazor/wwwroot/css/app.css --minify
RUN dotnet publish clients/blazor/RecipeBox.Blazor.csproj -c Release -o /app --no-restore

FROM nginxinc/nginx-unprivileged:1.29-alpine
COPY clients/blazor/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/wwwroot /usr/share/nginx/html
EXPOSE 8080
