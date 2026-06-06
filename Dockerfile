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

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "VRLCRM.dll"]
