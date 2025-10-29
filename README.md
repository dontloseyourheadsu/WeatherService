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
- [Requirements](#requirements)

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

### Setting Up MongoDB with Docker

To set up MongoDB using Docker, follow these simplified steps:

1. Pull the MongoDB Docker image:

   ```fish
   docker pull mongo
   ```

2. Run the MongoDB container and expose the port:

   ```fish
   docker run --name mongodb -d -p 27017:27017 mongo
   ```

   - `-p 27017:27017`: Maps the container's MongoDB port to your local machine.
   - `--name mongodb`: Names the container `mongodb`.

3. Verify the container is running:
   ```fish
   docker ps
   ```

### Seeding the Database

To seed the MongoDB database with the required collections, run the provided script:

1. Ensure you have Node.js installed.
2. Navigate to the `scripts` directory:
   ```fish
   cd scripts
   ```
3. Install dependencies:
   ```fish
   npm install
   ```
4. Run the seeding script:
   ```fish
   node seedDatabase.js
   ```

---

## Other Notes

- **Logging:** Uses Serilog for structured logging; logs can be configured for various sinks (console, files, etc.).
- **Error Handling:** Custom middleware for uniform error responses.
- **Extensibility:** The project is designed for easy integration of additional weather providers or caching strategies.
- **Testing:** (If present in the repo) Tests likely reside in a separate test project or within the `WeatherService.Tests` namespace.

---

## Requirements

### Node.js

- Node.js (v16 or later)
- MongoDB (v4.4 or later)

### .NET

- .NET 9 SDK
- ASP.NET Core 9

### Environment Variables

- `GEOCODING_API_KEY`: API key for the Geocoding API. This is required for the application to function correctly. Setup an account at [Geocoding API](https://geocode.maps.co/).

### Additional Notes

- Ensure MongoDB is running locally or update the connection string in `appsettings.json` if using a remote instance.

### For Running WeatherServiceAnalysis/mongodb-download-datasets.py

This script downloads data from the Copernicus Climate Data Store (CDS).

- **Python:** The script requires a Python environment to run.
- **cdsapi Library:** You need to install the CDS API client:
  ```fish
  pip install cdsapi
  ```
- **Copernicus CDS Account:** A free account is required on the [Copernicus Climate Data Store](https://cds.climate.copernicus.eu/).
- **CDS API Key Configuration:** You need to set up your API key in a file named `.cdsapirc` in your home directory. The file should contain:
  ```
  url: https://cds.climate.copernicus.eu/api/v2
  key: {YOUR_UID}:{YOUR_API_KEY}
  ```
  Replace `{YOUR_UID}` and `{YOUR_API_KEY}` with the values from your CDS profile page.
- **Accepted Dataset Licenses:** Before downloading data for a specific dataset (like `reanalysis-era5-land` used in the script), you must first log in to the CDS website and accept the terms and conditions for that dataset. You typically do this once per dataset on the dataset's download page.

### For Running WeatherServiceAnalysis/store-weather-data.py

This ETL script processes downloaded ERA5-Land data and loads it into MongoDB.

- **Python:** Requires Python 3.8 or later
- **Required Python packages:** Install all dependencies with:
  ```fish
  pip install xarray netCDF4 h5netcdf numpy pandas pymongo python-dotenv dask
  ```
- **unzip utility:** Required for extracting NetCDF files from downloaded archives (usually pre-installed on Linux/macOS)
- **MongoDB:** Must be running and accessible (local or remote instance)

---

## WeatherServiceAnalysis: Notebook and ETL (.venv + .env)

Both the Jupyter notebook (`WeatherServiceAnalysis/analytics.ipynb`) and the ETL script (`WeatherServiceAnalysis/store-weather-data.py`) are intended to run using the repository's local Python virtual environment `.venv` and a per-folder `.env` file.

### Python environment (.venv)

- Ensure the workspace is using the local interpreter at `.venv/bin/python`.
- In VS Code, select the kernel associated with `.venv` when opening the notebook.
- If extra packages are needed for the ETL (xarray, netCDF4, numpy, pandas, pymongo, python-dotenv, dask), install them into `.venv`.

### Environment variables (.env)

- Create `WeatherServiceAnalysis/.env` (do not commit it; it is git-ignored). A starter file is provided at `WeatherServiceAnalysis/.env.example`.
- Required variables:
  - `MONGODB_URI` – MongoDB connection string (defaults to `mongodb://localhost:27017`).
  - `MONGODB_DB` – Database name (defaults to `WeatherDb`).
  - `MONGODB_COLLECTION` – Collection name (defaults to `Forecasts`).
  - `NETCDF_PATH_PATTERN` – Path pattern to your NetCDF files relative to `WeatherServiceAnalysis/` (defaults to `era5_land_data/*-extracted.nc`).
  - `MONGO_BATCH_SIZE` – (Optional) Number of operations per MongoDB batch (defaults to `10000`). Adjust based on available RAM.
- The ETL script also supports legacy names as fallback: `MONGO_URI`, `MONGO_DB_NAME`, `MONGO_COLLECTION_NAME`.

### Downloading ERA5-Land Data

Use the provided download script to fetch historical weather data from the Copernicus Climate Data Store:

```fish
# From the repo root, activate your virtual environment
source .venv/bin/activate

# Navigate to the analysis directory
cd WeatherServiceAnalysis

# Run the download script (requires CDS API setup - see Requirements section)
python mongodb-download-datasets.py
```

This will download monthly ERA5-Land data files to `WeatherServiceAnalysis/era5_land_data/` as ZIP archives (with `.nc` extension).

### Extracting NetCDF Files

**Important:** The downloaded `.nc` files are actually ZIP archives that need to be extracted before processing.

Run the extraction script to extract the actual NetCDF files:

```fish
# From the WeatherServiceAnalysis directory
./extract-netcdf-files.sh
```

Or if running from the repo root:

```fish
./WeatherServiceAnalysis/extract-netcdf-files.sh
```

This script will:

- Check each `.nc` file in `era5_land_data/`
- Extract `data_0.nc` from each ZIP archive
- Rename extracted files with `-extracted.nc` suffix (e.g., `era5-land-mexico-2022-01-extracted.nc`)
- Skip files that have already been extracted
- Provide a summary of extracted and skipped files

**Note:** If you prefer to extract manually, you can use:

```fish
cd WeatherServiceAnalysis/era5_land_data
for file in *.nc; do
    base="${file%.nc}"
    unzip -j "$file" "data_0.nc" -d .
    mv data_0.nc "${base}-extracted.nc"
done
```

### Running the ETL

After extracting the NetCDF files, load them into MongoDB:

```fish
# From the repo root (ensure .venv is activated)
./.venv/bin/python WeatherServiceAnalysis/store-weather-data.py
```

The script will:

- Read MongoDB settings from environment variables (preferred) or fall back to `WeatherService.Api/appsettings.json`.
- Assert a unique compound index on `(timestamp, latitude, longitude)`.
- Read NetCDF files in weekly time chunks, derive temperature (°C), wind speed (m/s), and wind direction (degrees).
- Upload data to MongoDB in batches (default: 10,000 operations per batch) to prevent RAM overflow.
- Display progress for each batch showing how many batches have been processed.
- Upsert records into the `Forecasts` collection (new records are inserted, existing ones are updated).

**Memory Management:**

The script processes data in two levels of batching:

1. **Time chunks:** Reads NetCDF data in weekly chunks (configurable via `TIME_CHUNK_SIZE`)
2. **MongoDB batches:** Uploads operations in batches of 10,000 (configurable via `MONGO_BATCH_SIZE` environment variable)

If you experience memory issues, you can adjust the batch size:

```fish
# In your .env file
MONGO_BATCH_SIZE=5000  # Smaller batches for systems with less RAM
# or
MONGO_BATCH_SIZE=20000  # Larger batches for systems with more RAM
```

**Progress Tracking:**

The script provides detailed logging:

- File processing progress (e.g., "Processing file 3/12")
- Chunk processing progress (e.g., "Processing chunk 5/52")
- Batch upload progress (e.g., "Processing batch 2/15")
- Per-batch statistics (upserted, matched, modified records)
- Final cumulative statistics for each file

### Troubleshooting NetCDF "Unknown file format"

If the ETL logs `NetCDF: Unknown file format` when opening `.nc` files:

- Ensure the files are not zero or tiny in size (should be larger than a few KB).
- Verify the file header: NetCDF classic starts with `CDF`, NetCDF4 (HDF5) starts with `\x89HDF...`.
- Common causes:
  - Download was interrupted or produced an HTML/JSON error page saved as `.nc`.
  - Dataset license was not accepted on the CDS website before requesting downloads.
  - Using the wrong format (ensure `'format': 'netcdf'` in your CDS request).
- Fix by re-downloading the affected files after accepting licenses. The ETL will skip invalid files and continue.

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
