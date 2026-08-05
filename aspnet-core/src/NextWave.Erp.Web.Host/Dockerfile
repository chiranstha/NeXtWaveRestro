#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/NextWave.Erp.Web.Host/NextWave.Erp.Web.Host.csproj", "src/NextWave.Erp.Web.Host/"]
COPY ["src/NextWave.Erp.Web.Core/NextWave.Erp.Web.Core.csproj", "src/NextWave.Erp.Web.Core/"]
COPY ["src/NextWave.Erp.Application/NextWave.Erp.Application.csproj", "src/NextWave.Erp.Application/"]
COPY ["src/NextWave.Erp.Application.Shared/NextWave.Erp.Application.Shared.csproj", "src/NextWave.Erp.Application.Shared/"]
COPY ["src/NextWave.Erp.Core.Shared/NextWave.Erp.Core.Shared.csproj", "src/NextWave.Erp.Core.Shared/"]
COPY ["src/NextWave.Erp.Core/NextWave.Erp.Core.csproj", "src/NextWave.Erp.Core/"]
COPY ["src/NextWave.Erp.EntityFrameworkCore/NextWave.Erp.EntityFrameworkCore.csproj", "src/NextWave.Erp.EntityFrameworkCore/"]
COPY ["src/NextWave.Erp.GraphQL/NextWave.Erp.GraphQL.csproj", "src/NextWave.Erp.GraphQL/"]
RUN dotnet restore "src/NextWave.Erp.Web.Host/NextWave.Erp.Web.Host.csproj"
COPY . .
WORKDIR "/src/src/NextWave.Erp.Web.Host"
RUN dotnet build "NextWave.Erp.Web.Host.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NextWave.Erp.Web.Host.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NextWave.Erp.Web.Host.dll"]
