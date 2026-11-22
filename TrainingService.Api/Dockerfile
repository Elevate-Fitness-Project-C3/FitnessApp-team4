# 1. Base Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

USER $APP_UID

WORKDIR /app

EXPOSE 8080

ENV ASPNETCORE_HTTP_PORTS=8080


# 2. Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src


COPY ["TrainingService.Domain/TrainingService.Domain.csproj", "TrainingService.Domain/"]
COPY ["TrainingService.Application/TrainingService.Application.csproj", "TrainingService.Application/"]
COPY ["TrainingService.Infrastructure/TrainingService.Infrastructure.csproj", "TrainingService.Infrastructure/"]
COPY ["TrainingService.Api/TrainingService.Api.csproj", "TrainingService.Api/"]

RUN dotnet restore "TrainingService.Api/TrainingService.Api.csproj"

COPY . .


WORKDIR /src/TrainingService.Api

RUN dotnet publish "TrainingService.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false


# 3. Final Runtime Stage
FROM base AS final

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TrainingService.Api.dll"]
