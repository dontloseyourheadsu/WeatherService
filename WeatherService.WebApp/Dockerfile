FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["WeatherService.WebApp/WeatherService.WebApp.csproj", "WeatherService.WebApp/"]
COPY ["WeatherService.Contracts/WeatherService.Contracts.csproj", "WeatherService.Contracts/"]
COPY ["WeatherService.ServiceDefaults/WeatherService.ServiceDefaults.csproj", "WeatherService.ServiceDefaults/"]
RUN dotnet restore "WeatherService.WebApp/WeatherService.WebApp.csproj"
COPY . .
WORKDIR "/src/WeatherService.WebApp"
RUN dotnet build "WeatherService.WebApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WeatherService.WebApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WeatherService.WebApp.dll"]

