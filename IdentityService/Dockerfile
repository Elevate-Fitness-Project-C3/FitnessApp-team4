
# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
RUN mkdir -p /var/lib/identity/keys && chown $APP_UID /var/lib/identity/keys && chmod 700 /var/lib/identity/keys
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["IdentityService/IdentityService.csproj", "IdentityService/"]
COPY ["IdentityService/Libraries/", "IdentityService/Libraries/"]
RUN dotnet restore "./IdentityService/IdentityService.csproj"
COPY . .
WORKDIR "/src/IdentityService"
RUN dotnet build "./IdentityService.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./IdentityService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final runtime stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IdentityService.dll"]
