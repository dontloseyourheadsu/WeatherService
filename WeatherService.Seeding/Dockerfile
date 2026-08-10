FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["WeatherService.Seeding/WeatherService.Seeding.fsproj", "WeatherService.Seeding/"]
RUN dotnet restore "WeatherService.Seeding/WeatherService.Seeding.fsproj"
COPY WeatherService.Seeding/ WeatherService.Seeding/
WORKDIR "/src/WeatherService.Seeding"
RUN dotnet publish "WeatherService.Seeding.fsproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0-preview AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WeatherService.Seeding.dll"]