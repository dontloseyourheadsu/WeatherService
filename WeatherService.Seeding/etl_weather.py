import os
import sys
import time
import json
import zipfile
import shutil
import logging
import calendar
import gc
from pathlib import Path

import cdsapi
import numpy as np
import pandas as pd
import xarray as xr
from pymongo import MongoClient, UpdateOne, ASCENDING
from pymongo.errors import BulkWriteError

# ==========================================
#      USER CONFIGURATION SECTION
# ==========================================

# 1. MongoDB Configuration
MONGO_URI = os.getenv("MONGO_URI", "mongodb://host.docker.internal:27017")
MONGO_DB = os.getenv("MONGO_DB", "WeatherDb")
MONGO_COLLECTION = os.getenv("MONGO_COLLECTION", "Forecasts")
BATCH_SIZE = 10000  # Operations per batch to send to Mongo

# 2. Copernicus (CDS) API Configuration
# Get key from Docker Secret or Env Var
def get_secret(name, default=None):
    try:
        with open(f"/run/secrets/{name}", "r") as f:
            return f.read().strip()
    except IOError:
        return os.getenv(name.upper(), default)

CDS_URL = os.getenv("CDSAPI_URL", "https://cds.climate.copernicus.eu/api")
CDS_KEY = get_secret("cds_api_key", "8cea242d-8c32-4afa-98ca-ab36c9639277")  # Format: "UID:API-KEY"

# 3. Data Scope Configuration (What to download)
TARGET_YEAR = 2022
# Months to download (1-12). Example: range(1, 13) for all months
TARGET_MONTHS = range(1, 13) 
# Area: [North, West, South, East]
MEXICO_BBOX = [33, -119, 14, -86]

# 4. File Management
DATA_DIR = Path("/app/data")  # Matches Docker volume
KEEP_DOWNLOADED_FILES = False # Set True if you want to inspect files after loading

# ==========================================
#           SETUP & UTILITIES
# ==========================================

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
    handlers=[logging.StreamHandler()]
)

def init_mongo():
    """Ensures MongoDB indexes exist."""
    logging.info(f"Connecting to Mongo for initialization: {MONGO_URI}")
    with MongoClient(MONGO_URI) as client:
        db = client[MONGO_DB]
        coll = db[MONGO_COLLECTION]
        index_name = "timestamp_lat_lon_unique"
        logging.info(f"Ensuring index: {index_name}")
        coll.create_index(
            [("timestamp", ASCENDING), ("latitude", ASCENDING), ("longitude", ASCENDING)],
            unique=True,
            name=index_name
        )

def setup_cds_client():
    """Configures the CDS API client using provided secrets."""
    if not CDS_KEY:
        logging.error("CRITICAL: CDSAPI_KEY is missing. You cannot download data without it.")
        logging.error("Please set the CDSAPI_KEY environment variable.")
        sys.exit(1)
        
    # Write the .cdsapirc file for the library to use
    rc_path = Path.home() / '.cdsapirc'
    with open(rc_path, 'w') as f:
        f.write(f"url: {CDS_URL}\n")
        f.write(f"key: {CDS_KEY}\n")
    
    return cdsapi.Client()

def get_days_for_month(year, month):
    _, last_day = calendar.monthrange(year, month)
    return [f"{d:02d}" for d in range(1, last_day + 1)]

# ==========================================
#           STEP 1: DOWNLOAD
# ==========================================

def download_data(client, year, month):
    """Downloads one month of data from ERA5-Land."""
    month_str = f"{month:02d}"
    output_file = DATA_DIR / f"era5-mexico-{year}-{month_str}.nc"
    
    if output_file.exists():
        logging.info(f"File {output_file.name} already exists. Skipping download.")
        return output_file

    logging.info(f"Downloading data for {year}-{month_str}...")
    
    try:
        client.retrieve(
            'reanalysis-era5-land',
            {
                'product_type': 'reanalysis',
                'variable': [
                    '2m_temperature',
                    '10m_u_component_of_wind',
                    '10m_v_component_of_wind',
                ],
                'year': str(year),
                'month': month_str,
                'day': get_days_for_month(year, month),
                'time': [f"{h:02d}:00" for h in range(24)],
                'area': MEXICO_BBOX,
                'format': 'netcdf',
            },
            str(output_file)
        )
        logging.info(f"Download complete: {output_file.name}")
        return output_file
    except Exception as e:
        logging.error(f"Failed to download {year}-{month_str}: {e}")
        return None

# ==========================================
#           STEP 2: EXTRACT
# ==========================================

def extract_if_zip(file_path: Path):
    """
    Checks if the downloaded .nc file is actually a ZIP archive (common with CDS).
    If so, extracts it and returns the path to the real NetCDF file.
    """
    if not zipfile.is_zipfile(file_path):
        logging.info(f"File {file_path.name} is a valid NetCDF (not a zip).")
        return file_path

    logging.info(f"File {file_path.name} is a ZIP archive. Extracting...")
    extract_dir = file_path.parent / f"{file_path.stem}_extracted"
    extract_dir.mkdir(exist_ok=True)

    with zipfile.ZipFile(file_path, 'r') as zip_ref:
        # ERA5-Land zips usually contain a file named 'data_0.nc'
        zip_ref.extractall(extract_dir)
        
        # Find the .nc file inside
        extracted_files = list(extract_dir.glob("*.nc"))
        if not extracted_files:
            logging.error(f"No .nc file found inside zip {file_path.name}")
            return None
        
        real_nc_file = extracted_files[0]
        final_path = file_path.parent / f"{file_path.stem}_clean.nc"
        
        # Rename/Move to main data dir
        shutil.move(str(real_nc_file), str(final_path))
        
        # Cleanup extract folder and original zip
        shutil.rmtree(extract_dir)
        if not KEEP_DOWNLOADED_FILES:
            file_path.unlink() # Delete the zip
            
        logging.info(f"Extracted to: {final_path.name}")
        return final_path

# ==========================================
#           STEP 3: TRANSFORM & LOAD
# ==========================================

def process_and_load(nc_file: Path, collection):
    """
    Reads NetCDF, transforms data, and upserts to MongoDB in chunks.
    """
    logging.info(f"Processing NetCDF: {nc_file.name}")
    
    try:
        # Open dataset with chunks (Dask) to handle large files
        ds = xr.open_dataset(nc_file, chunks={'time': 168}) # ~1 week chunks
        
        # Normalize time dimension name
        time_dim = 'valid_time' if 'valid_time' in ds.dims else 'time'
        
        # Iterate over chunks
        total_steps = len(ds[time_dim])
        chunk_size = 168
        
        for i in range(0, total_steps, chunk_size):
            # Load chunk into memory
            chunk = ds.isel({time_dim: slice(i, i + chunk_size)}).load()
            
            # --- TRANSFORMATIONS ---
            # 1. Kelvin to Celsius
            temp_c = chunk['t2m'] - 273.15
            
            # 2. Wind Vectors to Speed/Direction
            u10 = chunk['u10']
            v10 = chunk['v10']
            
            # Magnitude
            wind_speed = np.sqrt(u10**2 + v10**2)
            
            # Direction (Meteorological: 0=N, 90=E)
            # Formula: (180 + degrees(atan2(u, v))) % 360
            wind_dir = (np.degrees(np.arctan2(u10, v10)) + 180) % 360

            # Create clean DataFrame
            df = pd.DataFrame({
                'timestamp': chunk[time_dim].values,
                # Flattening 3D arrays (time, lat, lon) requires care. 
                # xarray's to_dataframe handles the index alignment.
            })
            
            # Convert the specific chunk to a DataFrame correctly
            chunk_df = chunk.drop_vars(['t2m', 'u10', 'v10']) # Drop raw vars
            chunk_df['temperature'] = temp_c
            chunk_df['windSpeed'] = wind_speed
            chunk_df['windDirection'] = wind_dir
            
            # Reset index to get lat/lon/time as columns
            df = chunk_df.to_dataframe().reset_index()
            
            # Drop rows with NaN (oceans, etc)
            df = df.dropna(subset=['temperature', 'windSpeed', 'windDirection'])
            
            if df.empty:
                continue

            # --- BULK UPSERT PREPARATION ---
            operations = []
            for row in df.to_dict('records'):
                # Handle time column name variability
                ts = row.get(time_dim) or row.get('time')
                
                # Filter for identifying the document
                filter_doc = {
                    "timestamp": ts,
                    "latitude": row['latitude'],
                    "longitude": row['longitude']
                }
                
                # Fields to set
                update_doc = {
                    "$set": {
                        "temperature": float(row['temperature']),
                        "temperatureUnit": "C",
                        "windSpeed": float(row['windSpeed']),
                        "windSpeedUnit": "m/s",
                        "windDirection": int(round(row['windDirection'])),
                        "windDirectionUnit": "degrees"
                    }
                }
                operations.append(UpdateOne(filter_doc, update_doc, upsert=True))

                # Batch Execute
                if len(operations) >= BATCH_SIZE:
                    _execute_batch(collection, operations)
                    operations = []

            # Execute remaining
            if operations:
                _execute_batch(collection, operations)
            
            # Memory cleanup
            del chunk
            del df
            gc.collect()

        ds.close()
        
        # Optional: Delete processed .nc file
        if not KEEP_DOWNLOADED_FILES:
            logging.info(f"Deleting processed file: {nc_file.name}")
            nc_file.unlink()

    except Exception as e:
        logging.error(f"Error processing {nc_file.name}: {e}", exc_info=True)

def _execute_batch(collection, operations):
    try:
        collection.bulk_write(operations, ordered=False)
    except BulkWriteError as bwe:
        logging.warning(f"Batch wrote with some errors: {bwe.details['nUpserted']} upserted, {bwe.details['nMatched']} matched.")
    except Exception as e:
        logging.error(f"Batch write failed: {e}")

# ==========================================
#              MAIN LOOP
# ==========================================

def main():
    logging.info("Starting Weather ETL Process")
    
    # 1. Setup Data Directory
    DATA_DIR.mkdir(parents=True, exist_ok=True)
    
    # 2. Setup Clients
    cds = setup_cds_client()
    init_mongo()
    
    # 3. Process Loop
    for month in TARGET_MONTHS:
        # A. Download
        raw_file = download_data(cds, TARGET_YEAR, month)
        if not raw_file:
            continue
            
        # B. Extract
        clean_file = extract_if_zip(raw_file)
        if not clean_file:
            continue
            
        # C. Transform & Load
        logging.info(f"Connecting to Mongo for loading {clean_file.name}...")
        with MongoClient(MONGO_URI) as client:
            db = client[MONGO_DB]
            coll = db[MONGO_COLLECTION]
            process_and_load(clean_file, coll)
        
    logging.info("ETL Process Complete.")

if __name__ == "__main__":
    main()