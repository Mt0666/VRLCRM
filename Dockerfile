# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["VRLCRM.Domain/VRLCRM.Domain.csproj", "VRLCRM.Domain/"]
COPY ["VRLCRM.Application/VRLCRM.Application.csproj", "VRLCRM.Application/"]
COPY ["VRLCRM.Infrastructure/VRLCRM.Infrastructure.csproj", "VRLCRM.Infrastructure/"]
COPY ["VRLCRM.csproj", "./"]
RUN dotnet restore "VRLCRM.csproj"

COPY . .
RUN dotnet publish "VRLCRM.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_EnableDiagnostics=0

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=90s --retries=5 \
  CMD curl -fsS http://127.0.0.1:8080/ || exit 1

ENTRYPOINT ["dotnet", "VRLCRM.dll"]
