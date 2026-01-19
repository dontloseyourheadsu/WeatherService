# Weather MongoDB Explorer

## Overview

The MongoDB Explorer is a lightweight, dual-mode (GUI & CLI) Python application designed to help developers inspect, visualize, and manage the data inside the `WeatherDb`. Since the database runs inside a Docker volume, this tool provides a convenient window into the data without needing to expose ports to external GUIs or install local tools like Compass.

## Role in Solution

This application acts as a **Database Management & Inspection Tool**. It provides visibility into the data ingested by the Analysis service and consumed by the Weather API. It allows for quick debugging, manual data patching, and schema validation.

## Features

- **Dual Interface**:
  - **GUI (Web)**: Built with Streamlit for easy visual navigation and data tables.
  - **CLI**: Interactive command-line interface for terminal-based management.
- **Data Browsing**: View data with pagination (10 items per page).
- **CRL Operations**:
  - **Create**: Insert raw JSON documents.
  - **Read**: Browse collections and documents.
  - **Update**: Modify documents via ID and JSON patches.
  - **Delete**: Remove documents by ID or batch delete using JSON filters.

## Configuration

Configured via `docker-compose.yaml`.

### Environment Variables

| Variable    | Description                 | Default                 | Options      |
| ----------- | --------------------------- | ----------------------- | ------------ |
| `MONGO_URI` | Connection string           | `mongodb://mongo:27017` |              |
| `MONGO_DB`  | Default Database to connect | `WeatherDb`             |              |
| `MODE`      | Interface Mode              | `gui`                   | `gui`, `cli` |

## Getting Started

### Running in GUI Mode (Default)

The service is set to GUI mode by default in the docker-compose file.

1.  Start the service:
    ```bash
    docker compose up -d explorer
    ```
2.  Open your browser and navigate to:
    [http://localhost:8501](http://localhost:8501)

### Running in CLI Mode

To use the command-line interface, you must configure the service to run in CLI mode and attach to the container.

1.  **Edit `docker-compose.yaml`**:
    Change the `MODE` environment variable for the `explorer` service:
    ```yaml
    explorer:
      environment:
        - MODE=cli
    ```
2.  **Recreate the container**:
    ```bash
    docker compose up -d explorer
    ```
3.  **Attach to the container**:
    ```bash
    docker attach weather-mongo-explorer
    ```
    _Note: Press `Enter` once if the prompt doesn't appear immediately._

### CLI Commands

Once inside the CLI, use the numbered menu:

- `1`: List all Databases.
- `2`: List Collections in a Db.
- `3`: View Data (Pagination enabled).
- `4`: Insert a Document (requires valid JSON).
- `5`: Update a Document (requires ID and JSON patch).
- `6`: Delete Document (by ID or Filter).
- `0`: Exit the tool.
