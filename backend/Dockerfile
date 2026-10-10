# syntax=docker/dockerfile:1

# ---- build ----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first so this layer is cached until a .csproj changes.
COPY src/TakeoutPlatform.Api/TakeoutPlatform.Api.csproj src/TakeoutPlatform.Api/
RUN dotnet restore src/TakeoutPlatform.Api/TakeoutPlatform.Api.csproj

COPY src/ src/
RUN dotnet publish src/TakeoutPlatform.Api/TakeoutPlatform.Api.csproj \
      -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---- runtime --------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

COPY --from=build /app/publish .

# The base image ships a non-root user; never run the API as root.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "TakeoutPlatform.Api.dll"]
