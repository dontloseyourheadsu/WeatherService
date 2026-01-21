# Weather Service Analysis (ETL)

## Overview

This application serves as the **ETL (Extract, Transform, Load)** pipeline for the Weather Service solution. It is responsible for populating the MongoDB database with historical weather data.

## Role in Solution

The Analysis service is the data ingestion engine. It connects to the [Copernicus Climate Data Store (CDS)](https://cds.climate.copernicus.eu/), downloads historical ERA5-Land weather data (Temperature, Wind Speed, Wind Direction) for the region of Mexico, processes the raw NetCDF files, and loads the structured data into the shared MongoDB instance used by the API.

## Features

- **Automated Download**: Fetches monthly data from CDS API.
- **Format Handling**: Automatically handles ZIP or NetCDF formats.
- **Transformation**:
  - Converts Temperature from Kelvin to Celsius.
  - Calculates Wind Speed and Direction from U/V vectors.
  - Deduplicates data based on unique compound index: `(timestamp, latitude, longitude)`.
- **Resilient Loading**: Uses MongoDB Bulk Writes for performance and handles duplicate key errors gracefully.
- **Expanded Scope**: Now configured to download data for North and Central America, covering multiple years (2020-2023).

## Configuration

This service is configured primarily through `docker-compose.yaml` and environment variables.

### Environment Variables

| Variable     | Description                   | Default                                 |
| ------------ | ----------------------------- | --------------------------------------- |
| `MONGO_URI`  | Connection string for MongoDB | `mongodb://host.docker.internal:27017`  |
| `MONGO_DB`   | Target Database Name          | `WeatherDb`                             |
| `CDSAPI_URL` | Copernicus API Endpoint       | `https://cds.climate.copernicus.eu/api` |

### Secrets

Sensitive keys are managed via Docker Secrets (mounted at `/run/secrets/`).

- `cds_api_key`: Your UID and API Key for Copernicus CDS (Format: `UID:KEY`).

## Getting Started

### Prerequisites

1.  Ensure you have a Copernicus CDS account and API Key.
2.  Store your key in `secrets/cds_api_key` in the root of the solution.

### Running with Docker Compose

The easiest way to run the ETL process is via Docker Compose from the solution root:

```bash
# Run the analysis container once and remove it after completion
docker compose run --rm analysis
```

Prior to running, ensure the MongoDB container is up:

```bash
docker compose up -d mongo
```

### Data Volume

Data is temporarily stored in the `WeatherServiceAnalysis/data` folder (mapped to `/app/data` in the container) during processing. You can inspect downloaded NetCDF files there if configuration `KEEP_DOWNLOADED_FILES` is enabled in the script.
