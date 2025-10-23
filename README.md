# WeatherService

A modern weather forecasting REST API built with .NET and MongoDB, providing real-time and cached weather data via endpoints for both geographic coordinates and location names. This project demonstrates robust practices in API design, caching with MongoDB, and integration with external weather and geocoding providers.

---

## Table of Contents

- [Endpoints](#endpoints)
- [Tech Stack](#tech-stack)
- [How It Works](#how-it-works)
- [Project Structure](#project-structure)
- [Deployment & Usage](#deployment--usage)
- [Other Notes](#other-notes)

---

## Endpoints

All endpoints are prefixed with `/api/forecast`.

### 1. **Echo**

- **Route:** `GET /api/forecast/echo`
- **Description:** Health check endpoint. Echoes back the provided message to verify the API is running.
- **Query Parameter:** `message` (string)
- **Response:** The echoed string.

### 2. **Get Forecast by Coordinates**

- **Route:** `GET /api/forecast/coordinates`
- **Description:** Retrieves weather forecast using latitude and longitude.
- **Query Parameters:**
  - `latitude` (double, required)
  - `longitude` (double, required)
- **Response:**
  - 200: Weather forecast details for the specified coordinates.
  - 400/422/500: Error responses in case of invalid input or server error.

### 3. **Get Forecast by Location**

- **Route:** `GET /api/forecast/location`
- **Description:** Retrieves weather forecast by resolving a named location (e.g., "London").
- **Query Parameter:** `location` (string, required)
- **Response:**
  - 200: Weather forecast details for the resolved location.
  - 400/422/500: Error responses for input or server errors.

---

## Tech Stack

- **.NET (ASP.NET Core):** Main application and API framework.
- **MongoDB:** Acts as a caching layer for weather forecasts, reducing external API calls and improving response times.
- **Serilog:** Structured logging for diagnostics and monitoring.
- **Swagger/OpenAPI:** Integrated for API documentation and interactive exploration.
- **External Providers:**
  - **Open-Meteo:** Source for weather forecast data.
  - **Geocoding API:** Converts location names to coordinates.

---

## How It Works

### **Flow Overview**

1. **Request Handling:**
   - User calls a forecast endpoint with either coordinates or a location name.
2. **Caching with MongoDB:**
   - The service first checks MongoDB for a cached forecast for the requested time/location.
   - If present, returns cached data.
   - If absent, fetches fresh data from Open-Meteo, stores it in MongoDB, and returns the result.
3. **Geocoding:**
   - If the request uses a location name, the Geocoding API resolves it to latitude/longitude.
4. **Forecast Data Model:**
   - Each forecast includes latitude, longitude, temperature, wind speed/direction (with units), and sunrise time.

### **MongoDB as Caching**

- Forecasts are stored in MongoDB with timestamp, location, and weather metrics.
- On every request, forecast retrieval checks for an existing document with matching time and coordinates (hourly granularity).
- If missing, the service fetches from Open-Meteo and inserts a new document for future cache hits.

---

## Project Structure

- **WeatherService.Api/**
  - `Controllers/ForecastController.cs` � API endpoints logic.
  - `ApiEndpoints.cs` � Centralized endpoint routes.
  - `Program.cs` � App configuration and startup.
  - `Mapping/` � Maps internal models to API response models.
- **WeatherService.Application/**
  - `Services/ForecastService.cs` � Core logic: orchestrates caching, external API calls, and data transformation.
  - `Repositories/MongoDbForecastRepository.cs` � Reads/writes forecast documents to MongoDB.
  - `Models/` � Data models for MongoDB, Open-Meteo, request/response contracts, and configuration options.
  - `Mapping/` � Converts between external/internal models and MongoDB schemas.
- **Configuration:**
  - MongoDB, Open-Meteo, and Geocoding API settings are managed via dependency injection and `IOptions<T>` pattern.

---

## Deployment & Usage

1. **Configure MongoDB, Open-Meteo, and Geocoding API settings** in `appsettings.json` or via environment variables.
2. **Run the API:**
   - Use `dotnet run` from the solution root.
   - Swagger UI is available in development mode for testing endpoints.

---

## MongoDB with Docker

You can run MongoDB locally via Docker. Below are three common setups and how to point the API at each.

### Option A: No-auth, published port (recommended for local dev)

This exposes MongoDB on your host at `localhost:27017`.

- Create a data volume (optional but recommended):

  ```fish
  docker volume create mongo_data
  ```

- Start MongoDB (no auth), publishing the port:

  ```fish
  docker run -d --name mongodb -p 27017:27017 -v mongo_data:/data/db mongo:7
  ```

- App configuration:
  - `WeatherService.Api/appsettings.json` already points to `mongodb://localhost:27017` (no auth), which works with this setup.

### Option B: With authentication, published port

This sets up a root user, then creates an application user for the `WeatherDb` database.

1. Start MongoDB with root credentials and a default DB:

   ```fish
   docker run -d --name mongodb \
     -p 27017:27017 \
     -e MONGO_INITDB_ROOT_USERNAME=root \
     -e MONGO_INITDB_ROOT_PASSWORD=secret \
     -e MONGO_INITDB_DATABASE=WeatherDb \
     -v mongo_data:/data/db \
     mongo:7
   ```

2. Create an application user (one-time):

   ```fish
   docker exec -it mongodb mongosh -u root -p secret --authenticationDatabase admin \
     --eval 'use WeatherDb; db.createUser({ user: "weatheruser", pwd: "weatherpass", roles: [{ role: "readWrite", db: "WeatherDb" }] });'
   ```

3. App configuration (choose one):

- From host (published port):

  In `WeatherService.Api/appsettings.json`, set:

  ```json
  {
    "MongoDb": {
      "ConnectionUri": "mongodb://weatheruser:weatherpass@localhost:27017/WeatherDb?authSource=WeatherDb",
      "DatabaseName": "WeatherDb",
      "Collections": { "Forecasts": "Forecasts" }
    }
  }
  ```

- Container-to-container (internal DNS, see Option C):

  Use: `mongodb://weatheruser:weatherpass@mongodb:27017/WeatherDb?authSource=WeatherDb`

### Option C: Internal-only networking (no host port published)

If you prefer to avoid exposing MongoDB on your host, run both the API and MongoDB on the same Docker network and connect using the container DNS name (`mongodb`). Note: host processes (like `dotnet run` on your machine) cannot connect to MongoDB unless the port is published.

1. Create a Docker network:

   ```fish
   docker network create weather-net
   ```

2. Start MongoDB on that network (no published port):

   ```fish
   docker run -d --name mongodb --network weather-net -v mongo_data:/data/db mongo:7
   ```

3. Run the API in Docker on the same network and use the `mongodb` hostname in the connection string. If you want to use the provided config files:

   - `WeatherService.Api/appsettings.Docker.json` sets the connection to `mongodb://mongodb:27017` (no auth).
   - `WeatherService.Api/appsettings.DockerAuth.json` sets the connection to `mongodb://weatheruser:weatherpass@mongodb:27017/WeatherDb?authSource=WeatherDb` (with auth).

   When running the API container, ensure it joins `weather-net` so it can resolve `mongodb`:

   ```fish
   # Example (you'll need a Dockerfile for the API if you want to containerize it)
   docker run -d --name weather-api --network weather-net -p 5000:8080 weather-service-image
   ```

   If you are running the API on the host (not in Docker), you must publish MongoDB's port (use Options A or B). Without publishing, the host cannot reach the internal Docker port.

### Selecting an appsettings variant

ASP.NET Core automatically loads `appsettings.json` and overrides with `appsettings.{Environment}.json` if you set the environment.

- Use default (no-auth localhost): just run normally.
- Use Docker variants for container-to-container networking:

  ```fish
  # No auth, internal DNS to MongoDB
  set -x ASPNETCORE_ENVIRONMENT Docker
  dotnet run --project WeatherService.Api

  # With auth, internal DNS to MongoDB
  set -x ASPNETCORE_ENVIRONMENT DockerAuth
  dotnet run --project WeatherService.Api
  ```

Tip: You can also override the connection string via environment variable without editing files:

```fish
set -x MongoDb__ConnectionUri "mongodb://weatheruser:weatherpass@localhost:27017/WeatherDb?authSource=WeatherDb"
dotnet run --project WeatherService.Api
```

---

## Other Notes

- **Logging:** Uses Serilog for structured logging; logs can be configured for various sinks (console, files, etc.).
- **Error Handling:** Custom middleware for uniform error responses.
- **Extensibility:** The project is designed for easy integration of additional weather providers or caching strategies.
- **Testing:** (If present in the repo) Tests likely reside in a separate test project or within the `WeatherService.Tests` namespace.

---

## Example Query

```http
GET /api/forecast/coordinates?latitude=51.5074&longitude=-0.1278
```

**Response:**

```json
{
  "latitude": 51.5074,
  "longitude": -0.1278,
  "temperature": 18.5,
  "temperatureUnit": "C",
  "windDirection": 90,
  "windDirectionUnit": "�",
  "windSpeed": 12.1,
  "windSpeedUnit": "km/h",
  "sunrise": "2025-05-18T04:58:00Z"
}
```

```http
GET /api/forecast/location?location=London
```

**Response:**

```json
{
  "latitude": 51.5074,
  "longitude": -0.1278,
  "temperature": 18.5,
  "temperatureUnit": "C",
  "windDirection": 90,
  "windDirectionUnit": "�",
  "windSpeed": 12.1,
  "windSpeedUnit": "km/h",
  "sunrise": "2025-05-18T04:58:00Z"
}
```

---

## License

MIT or as specified in the repository.

---

## Contributing

Contributions welcome! Please open issues and pull requests for enhancements or bug fixes.

---

**Maintainer:** [dontloseyourheadsu](https://github.com/dontloseyourheadsu)
