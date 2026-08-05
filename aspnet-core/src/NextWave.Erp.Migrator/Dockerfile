#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/NextWave.Erp.Migrator/NextWave.Erp.Migrator.csproj", "src/NextWave.Erp.Migrator/"]
COPY ["src/NextWave.Erp.EntityFrameworkCore/NextWave.Erp.EntityFrameworkCore.csproj", "src/NextWave.Erp.EntityFrameworkCore/"]
COPY ["src/NextWave.Erp.Core/NextWave.Erp.Core.csproj", "src/NextWave.Erp.Core/"]
COPY ["src/NextWave.Erp.Core.Shared/NextWave.Erp.Core.Shared.csproj", "src/NextWave.Erp.Core.Shared/"]
RUN dotnet restore "src/NextWave.Erp.Migrator/NextWave.Erp.Migrator.csproj"
COPY . .
WORKDIR "/src/src/NextWave.Erp.Migrator"
RUN dotnet build "NextWave.Erp.Migrator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NextWave.Erp.Migrator.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NextWave.Erp.Migrator.dll", "-s"]
