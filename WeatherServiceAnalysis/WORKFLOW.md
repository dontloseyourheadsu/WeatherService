# ERA5-Land Data ETL Workflow

This guide walks you through the complete process of downloading, extracting, and loading ERA5-Land historical weather data into MongoDB.

## Prerequisites

1. **Python Environment**: Activate the repository's virtual environment

   ```fish
   source .venv/bin/activate  # From repo root
   ```

2. **Install Dependencies**:

   ```fish
   pip install cdsapi xarray netCDF4 h5netcdf numpy pandas pymongo python-dotenv dask
   ```

3. **CDS API Setup**: Create `~/.cdsapirc` with your Copernicus credentials

   ```
   url: https://cds.climate.copernicus.eu/api/v2
   key: {YOUR_UID}:{YOUR_API_KEY}
   ```

4. **MongoDB**: Ensure MongoDB is running (local or remote)

5. **Environment Configuration**: Copy and configure `.env`
   ```fish
   cd WeatherServiceAnalysis
   cp .env.example .env
   # Edit .env with your MongoDB connection details
   ```

## Step-by-Step Workflow

### Step 1: Download ERA5-Land Data

Download historical weather data from Copernicus CDS:

```fish
cd WeatherServiceAnalysis
python mongodb-download-datasets.py
```

**What this does:**

- Downloads monthly ERA5-Land data for the configured region and year
- Saves files to `era5_land_data/` directory
- Files are downloaded as ZIP archives with `.nc` extension
- Example: `era5-land-mexico-2022-01.nc` (actually a ZIP file)

**Time estimate:** 10-30 minutes per month depending on network speed and CDS queue

### Step 2: Extract NetCDF Files

Extract the actual NetCDF files from downloaded ZIP archives:

```fish
./extract-netcdf-files.sh
```

**What this does:**

- Scans `era5_land_data/` for `.nc` files
- Extracts `data_0.nc` from each ZIP archive
- Renames extracted files with `-extracted.nc` suffix
- Example: `era5-land-mexico-2022-01-extracted.nc`
- Skips already-extracted files

**Output:**

```
Found 12 .nc archive(s) to extract
========================================
Extracting: era5-land-mexico-2022-01
  ✓ Extracted to: era5-land-mexico-2022-01-extracted.nc
...
========================================
Extraction complete!
  Extracted: 12 file(s)
  Skipped: 0 file(s) (already extracted)
```

**Time estimate:** 1-2 minutes for 12 monthly files

### Step 3: Load Data into MongoDB

Process and upload the extracted NetCDF files to MongoDB:

```fish
# From WeatherServiceAnalysis directory
cd ..  # Return to repo root
./.venv/bin/python WeatherServiceAnalysis/store-weather-data.py
```

**What this does:**

- Reads extracted NetCDF files matching pattern in `.env`
- Processes data in weekly chunks to manage memory
- Derives temperature (°C), wind speed (m/s), and wind direction (°)
- Uploads to MongoDB in batches (default: 10,000 operations per batch)
- Upserts records (inserts new, updates existing)
- Creates unique index on (timestamp, latitude, longitude)

**Progress output:**

```
--- Processing file 1/12: era5-land-mexico-2022-01-extracted.nc ---
Total time steps: 744, processing in 5 chunk(s)...
  Processing chunk 1/5 (time steps 0 to 167)...
    DataFrame created with 45,000 valid records...
    Uploading 45,000 operations in 5 batch(es)...
      Processing batch 1/5 (10,000 operations)...
        Batch 1 complete: Upserted: 10,000, Matched: 0, Modified: 0
      Processing batch 2/5 (10,000 operations)...
        ...
    MongoDB Upload Complete - Total: Upserted: 45,000, Matched: 0, Modified: 0
```

**Time estimate:**

- Varies greatly based on:
  - Dataset size (grid resolution, time range)
  - Network latency to MongoDB
  - System RAM and CPU
- Typical: 5-15 minutes per monthly file for Mexico region

**Memory management:**

- Default batch size: 10,000 operations
- Adjust via `MONGO_BATCH_SIZE` in `.env` if needed:
  - `5000` for systems with 4-8GB RAM
  - `10000` for systems with 8-16GB RAM
  - `20000` for systems with 16GB+ RAM

## Troubleshooting

### "No NetCDF files found"

- Ensure you ran `extract-netcdf-files.sh` first
- Check `.env` has correct `NETCDF_PATH_PATTERN=era5_land_data/*-extracted.nc`

### "Unknown file format" errors

- Files may be corrupted downloads
- Re-download using `mongodb-download-datasets.py`
- Ensure you accepted the dataset license on CDS website

### Memory errors during upload

- Reduce `MONGO_BATCH_SIZE` in `.env`
- Close other memory-intensive applications
- Consider processing files one at a time

### MongoDB connection errors

- Verify MongoDB is running: `docker ps` or `systemctl status mongod`
- Check `MONGODB_URI` in `.env` is correct
- Test connection: `mongosh $MONGODB_URI`

## Data Validation

After loading, verify data in MongoDB:

```fish
mongosh mongodb://localhost:27017/WeatherDb

# In mongosh:
db.Forecasts.countDocuments()  # Should show total records
db.Forecasts.findOne()  # View sample record
db.getCollectionInfos({name: "Forecasts"})  # Check indexes
```

## Summary

The complete workflow is:

1. **Download** (`mongodb-download-datasets.py`) → ZIP archives in `era5_land_data/`
2. **Extract** (`extract-netcdf-files.sh`) → NetCDF files with `-extracted.nc` suffix
3. **Load** (`store-weather-data.py`) → MongoDB `Forecasts` collection

Each step can be run independently and is idempotent (safe to re-run).
