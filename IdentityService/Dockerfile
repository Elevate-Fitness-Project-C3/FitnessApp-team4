FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
RUN mkdir -p /var/lib/identity/keys && chown $APP_UID /var/lib/identity/keys && chmod 700 /var/lib/identity/keys
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS development
WORKDIR /src/IdentityService
RUN mkdir -p bin obj /var/lib/identity/keys && chown -R $APP_UID /src /var/lib/identity
USER $APP_UID
COPY ["IdentityService/IdentityService.csproj", "./"]
COPY --chown=$APP_UID Audit.Contracts/ /src/Audit.Contracts/
RUN dotnet restore
ENTRYPOINT ["dotnet", "watch", "--project", "IdentityService.csproj", "--non-interactive", "--no-hot-reload", "run", "--no-launch-profile"]

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["IdentityService/IdentityService.csproj", "IdentityService/"]
COPY ["Audit.Contracts/Audit.Contracts.csproj", "Audit.Contracts/"]
RUN dotnet restore "./IdentityService/IdentityService.csproj"
# Copy only the service sources: never the whole build context (it may contain .env or other secrets).
COPY IdentityService/ IdentityService/
COPY Audit.Contracts/ Audit.Contracts/
WORKDIR "/src/IdentityService"
RUN dotnet build "./IdentityService.csproj" -c $BUILD_CONFIGURATION -o /app/build
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./IdentityService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IdentityService.dll"]
