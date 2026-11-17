FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Audit.Contracts/Audit.Contracts.csproj Audit.Contracts/
COPY AuditService/AuditService.csproj AuditService/
RUN dotnet restore AuditService/AuditService.csproj
COPY Audit.Contracts/ Audit.Contracts/
COPY AuditService/ AuditService/
RUN dotnet publish AuditService/AuditService.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
USER $APP_UID
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AuditService.dll"]
