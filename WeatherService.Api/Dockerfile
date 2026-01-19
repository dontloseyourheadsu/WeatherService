FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["WeatherService.Api/WeatherService.Api.csproj", "WeatherService.Api/"]
COPY ["WeatherService.Application/WeatherService.Application.csproj", "WeatherService.Application/"]
COPY ["WeatherService.Contracts/WeatherService.Contracts.csproj", "WeatherService.Contracts/"]
RUN dotnet restore "WeatherService.Api/WeatherService.Api.csproj"
COPY . .
WORKDIR "/src/WeatherService.Api"
RUN dotnet build "WeatherService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WeatherService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WeatherService.Api.dll"]
