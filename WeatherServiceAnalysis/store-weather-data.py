import glob
import logging
import os
import sys
import json
from pathlib import Path
import numpy as np
import pandas as pd
import xarray as xr
from dotenv import load_dotenv
from pymongo import MongoClient, UpdateOne, ASCENDING, GEOSPHERE
from pymongo.errors import ConnectionFailure, BulkWriteError
import gc # Import garbage collector

# --- 1. Configuration and Logging ---

# Load environment variables from a.env file in the same folder as this script (e.g., WeatherServiceAnalysis/.env)
load_dotenv(dotenv_path=Path(__file__).parent / '.env')

# --- MongoDB Configuration ---
# Defaults
DEFAULT_MONGO_URI = "mongodb://localhost:27017/"
DEFAULT_DB_NAME = "WeatherDb"
DEFAULT_COLLECTION_NAME = "Forecasts"

# Try loading from appsettings.json relative to the script location
# Assumes this script is in WeatherServiceAnalysis/ and appsettings is in../WeatherService.Api/
api_appsettings_path = Path(__file__).parent / '..' / 'WeatherService.Api' / 'appsettings.json'
cfg = {}
if api_appsettings_path.exists():
    try:
        with open(api_appsettings_path, 'r') as f:
            cfg = json.load(f)
        print(f"Loaded settings from {api_appsettings_path}")
    except Exception as e:
        print(f"Warning: Could not read {api_appsettings_path}. Error: {e}")
else:
    print(f"Warning: {api_appsettings_path} not found. Using defaults or environment variables.")

# Determine final MongoDB settings (Env vars override appsettings, which override defaults)
# Prefer notebook-style env names (MONGODB_*) for consistency, then fallback to legacy names and appsettings
MONGO_URI = (
    os.getenv('MONGODB_URI') # Matches notebook convention
    or os.getenv('MONGO_URI') # Matches previous script.env convention
    or cfg.get('MongoDb', {}).get('ConnectionUri', DEFAULT_MONGO_URI)
)
DB_NAME = (
    os.getenv('MONGODB_DB') # Matches notebook convention
    or os.getenv('MONGO_DB_NAME')
    or cfg.get('MongoDb', {}).get('DatabaseName', DEFAULT_DB_NAME)
)
COLLECTION_NAME = (
    os.getenv('MONGODB_COLLECTION') # Matches notebook convention
    or os.getenv('MONGO_COLLECTION_NAME')
    or cfg.get('MongoDb', {}).get('Collections', {}).get('Forecasts', DEFAULT_COLLECTION_NAME)
)

# --- NetCDF Configuration ---
# Path pattern to find all NetCDF files (from.env or default)
# Assumes 'era5_land_data' directory is at the same level as this script
DEFAULT_NETCDF_PATH_PATTERN = "era5_land_data/*-extracted.nc"
NETCDF_PATH_PATTERN = os.environ.get("NETCDF_PATH_PATTERN", DEFAULT_NETCDF_PATH_PATTERN)
# Define chunk size for memory management (e.g., process 1 week = 24 * 7 time steps at a time)
TIME_CHUNK_SIZE = 24 * 7

def setup_logging():
    """Configures a basic logger."""
    logging.basicConfig(
        level=logging.INFO,
        format="%(asctime)s [%(levelname)s] %(message)s",
        # *** THIS IS THE CORRECTED LINE USING YOUR PROVIDED FIX ***
        handlers=[logging.StreamHandler()], # Ensure logs go to console
    )

def _is_probably_netcdf(path: Path) -> bool:
    """Quick magic-bytes check for NetCDF (classic) or HDF5-based NetCDF4 files."""
    try:
        with open(path, 'rb') as f:
            head = f.read(8)
        # NetCDF classic starts with 'CDF\x01' or 'CDF\x02'
        if head.startswith(b'CDF'):
            return True
        # HDF5 signature: 0x89 0x48 0x44 0x46 0x0D 0x0A 0x1A 0x0A
        if head == b"\x89HDF\r\n\x1a\n":
            return True
        return False
    except Exception as e:
        logging.warning(f"Could not read header for file {path}: {e}")
        return False

# --- 2. Database Connection and Index Assertion ---

def get_mongo_client(uri: str) -> MongoClient:
    """Connects to MongoDB and returns the client."""
    if not uri:
        logging.error("MONGO_URI not set. Please check your config (appsettings.json or.env).")
        sys.exit(1)
    try:
        # Increased timeout useful for potentially slower Atlas connections or startup
        client = MongoClient(uri, serverSelectionTimeoutMS=10000)
        client.admin.command('ping') # Test connection using ping command
        logging.info(f"Successfully connected to MongoDB.")
        # Mask credentials if present in URI for logging security
        log_uri = uri.split('@')[-1] if '@' in uri else uri
        logging.info(f"  Target: {log_uri}")
        return client
    except ConnectionFailure as e:
        logging.error(f"MongoDB connection failed: {e}")
        sys.exit(1)
    except Exception as e: # Catch other potential errors like DNS resolution
        logging.error(f"An error occurred during MongoDB connection attempt: {e}")
        sys.exit(1)


def setup_indexes(collection):
    """
    Asserts that the required database indexes exist for performance and integrity.
    Takes the collection object as input.
    """
    # 1. The Integrity Index (Unique Compound Key)
    # Ensures no duplicate timestamp/location pairs. Enables upsert efficiency.
    # Matches the fields used in the `op_filter` in `prepare_bulk_operations`.
    index_name_unique = "timestamp_lat_lon_unique"
    index_keys_unique = [("timestamp", ASCENDING), ("latitude", ASCENDING), ("longitude", ASCENDING)]
    try:
        logging.info(f"Asserting unique integrity index: {index_name_unique} on {index_keys_unique}")
        # Note: MongoDB automatically ignores index creation if an identical index already exists.
        collection.create_index(index_keys_unique, unique=True, name=index_name_unique)
        logging.info(f"Index {index_name_unique} ensured.")
    except Exception as e:
        logging.error(f"Failed to create/ensure unique index {index_name_unique}: {e}")
        # Depending on requirements, you might want to exit if index creation fails
        # sys.exit(1)

    # Optional: Add a geospatial index if you plan geospatial queries later
    # (Requires location stored as GeoJSON sub-document)
    # index_name_geo = "location_geospatial"
    # index_keys_geo = # Use GEOSPHERE for GeoJSON points
    # try:
    #     logging.info(f"Asserting geospatial query index: {index_name_geo} on {index_keys_geo}")
    #     collection.create_index(index_keys_geo, name=index_name_geo)
    #     logging.info(f"Index {index_name_geo} ensured.")
    # except Exception as e:
    #     logging.error(f"Failed to create/ensure geospatial index {index_name_geo}: {e}")


# --- 3. Data Extraction and Transformation ---

def process_netcdf_file(filepath: str, collection):
    """
    Processes a single NetCDF file in chunks and loads it into MongoDB.
    Includes basic validation checks before opening the file.
    """
    logging.info(f"Attempting to process file: {filepath}")

    # --- Basic File Validation ---
    file_path_obj = Path(filepath)
    if not file_path_obj.exists():
        logging.error(f"File not found: {filepath}. Skipping.")
        return

    size_bytes = file_path_obj.stat().st_size
    # Adjust threshold if very small valid files are expected
    if size_bytes < 1024 * 10: # Check if less than 10KB (arbitrary small size)
        logging.error(f"File is suspiciously small: {filepath} ({size_bytes} bytes). Possible incomplete download or error page. Skipping.")
        return

    if not _is_probably_netcdf(file_path_obj):
        logging.error(
            "File does not appear to be NetCDF/HDF5 based on header bytes. "
            "It might be corrupted, an HTML error page, or another format. Skipping: %s", filepath
        )
        return

    # --- Open with xarray (Try multiple engines) ---
    ds = None
    open_errors = []
    # Try common engines. 'scipy' might be needed for older NetCDF3 formats.
    for engine in ("netcdf4", "h5netcdf", "scipy"):
        try:
            # Determine the time dimension name from the file
            # Open without chunks first to check dimensions
            test_ds = xr.open_dataset(filepath, engine=engine)
            time_dim = 'valid_time' if 'valid_time' in test_ds.dims else 'time'
            test_ds.close()
            
            # Open the dataset with Dask-backed chunking for the time dimension.
            # This reads file metadata but defers reading data chunks, keeping memory low initially.
            ds = xr.open_dataset(filepath, engine=engine, chunks={time_dim: TIME_CHUNK_SIZE})
            logging.info(f"Successfully opened {filepath} using engine='{engine}'.")
            break # Stop trying engines once one works
        except Exception as e:
            open_errors.append((engine, str(e)))

    if ds is None:
        logging.error(f"Failed to open NetCDF file {filepath} with any available engine.")
        for engine, error in open_errors:
            logging.error(f"  - Engine '{engine}': {error}")
        logging.error(
            "Troubleshooting tips: Verify the CDS download completed successfully without errors. "
            "Ensure you accepted the dataset license on the CDS website before downloading. "
            "Check if the file content is actually an HTML/JSON error message from the server. Consider re-downloading the file."
        )
        return # Skip this file

    # --- Process Chunks ---
    try:
        # ERA5-Land uses 'valid_time' instead of 'time'
        time_dim = 'valid_time' if 'valid_time' in ds.dims else 'time'
        total_steps = len(ds[time_dim])
        total_chunks = (total_steps + TIME_CHUNK_SIZE - 1) // TIME_CHUNK_SIZE
        logging.info(f"Total time steps: {total_steps}, processing in {total_chunks} chunk(s) of size {TIME_CHUNK_SIZE}...")

        processed_records_in_file = 0
        # Iterate through the dataset one time-chunk at a time
        for i in range(0, total_steps, TIME_CHUNK_SIZE):
            chunk_num = i // TIME_CHUNK_SIZE + 1
            start_step = i
            end_step = min(i + TIME_CHUNK_SIZE, total_steps)
            logging.info(f"  Processing chunk {chunk_num}/{total_chunks} (time steps {start_step} to {end_step - 1})...")

            try:
                # Select the time slice (this is still lazy)
                chunk_ds = ds.isel({time_dim: slice(start_step, end_step)})

                # --- Transform ---
                # Trigger Dask computation by loading ONLY this chunk's data into memory
                logging.info(f"    Loading chunk data into memory...")
                chunk_ds.load()
                logging.info(f"    Chunk loaded. Performing transformations...")

                # Extract source variables for clarity
                temp_k = chunk_ds['t2m'] # 2m temperature in Kelvin
                u10 = chunk_ds['u10']    # 10m U-component of wind (Eastward) in m/s
                v10 = chunk_ds['v10']    # 10m V-component of wind (Northward) in m/s

                # 1. Convert Temperature (Kelvin -> Celsius)
                temp_c = temp_k - 273.15

                # 2. Derive Wind Speed (magnitude of u/v vectors) in m/s
                wind_speed_mps = np.sqrt(u10**2 + v10**2)

                # 3. Derive Wind Direction (Meteorological Degrees from North, clockwise)
                # Correct formula using atan2(u, v): (180 + degrees(atan2(u, v))) mod 360
                # (Angle FROM which the wind blows, 0=N, 90=E, 180=S, 270=W)
                wind_direction_deg = (np.degrees(np.arctan2(u10, v10)) + 180) % 360

                # Assign transformed data back with specific names to avoid confusion
                chunk_ds['temperature_c'] = temp_c
                chunk_ds['wind_speed_mps'] = wind_speed_mps
                chunk_ds['wind_direction_deg'] = wind_direction_deg

                logging.info(f"    Transformations complete. Pivoting to DataFrame...")
                # --- Pivot to DataFrame ---
                # Select only the needed transformed variables + coordinates for the dataframe
                # Keep original coordinates ('valid_time' or 'time', 'latitude', 'longitude')
                df = chunk_ds[['temperature_c', 'wind_speed_mps', 'wind_direction_deg']].to_dataframe()

                # Drop rows where ALL selected weather variables are NaN
                # ERA5-Land uses NaN for non-land grid points (e.g., oceans).
                # Use the transformed variable names here
                df = df.dropna(subset=['temperature_c', 'wind_speed_mps', 'wind_direction_deg'], how='all').reset_index()

                if df.empty:
                    logging.warning("    Chunk resulted in empty DataFrame after dropping NaNs. Skipping.")
                    continue

                num_records_in_chunk = len(df)
                processed_records_in_file += num_records_in_chunk
                logging.info(f"    DataFrame created with {num_records_in_chunk} valid records. Preparing DB operations...")

                # --- Load ---
                # Prepare the list of MongoDB bulk write operations
                operations = prepare_bulk_operations(df)

                if operations:
                    logging.info(f"    Prepared {len(operations)} operations. Writing to MongoDB...")
                    load_data_to_mongo(collection, operations)
                else:
                    logging.warning("    No database operations generated for this chunk (unexpected).")

                # Clean up memory explicitly (helps in long-running loops)
                del df
                del chunk_ds
                del operations
                gc.collect() # Trigger garbage collection

            except KeyError as ke:
                logging.error(f"    Missing expected variable in chunk {chunk_num}: {ke}. Check NetCDF file contents. Skipping chunk.")
                continue # Skip this chunk
            except Exception as e:
                logging.error(f"    An error occurred processing chunk {chunk_num} of file {filepath}: {e}", exc_info=True)
                continue # Continue to next chunk if possible

        logging.info(f"Finished processing file: {filepath}. Total valid records processed in file: {processed_records_in_file}")

    except Exception as e:
        # Catch errors occurring during file opening or chunk iteration setup
         logging.error(f"An error occurred outside chunk processing for file {filepath}: {e}", exc_info=True)
    finally:
        # Ensure the dataset file handle is closed even if errors occur
        if ds is not None:
            try:
                ds.close()
                logging.info(f"Closed file handle for {filepath}")
            except Exception as e:
                logging.error(f"Error closing file handle for {filepath}: {e}")

# --- 4. Data Loading ---

def prepare_bulk_operations(df: pd.DataFrame) -> list:
    """
    Converts a DataFrame chunk into a list of PyMongo UpdateOne operations
    for use with bulk_write, matching the target C# MongoDB schema.
    """
    operations = []
    # Support both 'time' and 'valid_time' column names
    time_col = 'valid_time' if 'valid_time' in df.columns else 'time'
    required_cols = {time_col, 'latitude', 'longitude', 'temperature_c', 'wind_speed_mps', 'wind_direction_deg'}
    if not required_cols.issubset(df.columns):
        logging.error(f"DataFrame is missing required columns for MongoDB operations. Found: {df.columns}. Required: {required_cols}")
        return operations # Return empty list

    # Use to_dict('records') for generally faster iteration compared to df.apply or iterrows
    for record in df.to_dict('records'):
        try:
            # Ensure timestamp is a Python native datetime object (required by PyMongo)
            ts = pd.to_datetime(record[time_col]).to_pydatetime()
            lat = record['latitude']
            lon = record['longitude']
            temp_c_val = record['temperature_c']
            wind_speed_val = record['wind_speed_mps']
            wind_dir_val = record['wind_direction_deg']

            # Basic validation (optional but recommended)
            if not all(isinstance(val, (int, float, np.number)) and not np.isnan(val) for val in [lat, lon]):
                 logging.warning(f"Skipping record due to invalid or NaN coordinate: lat={lat}, lon={lon}")
                 continue
            # Check if all relevant weather data is NaN - if so, skip (already handled by dropna, but as safeguard)
            all_weather_nan = np.isnan(temp_c_val) and np.isnan(wind_speed_val) and np.isnan(wind_dir_val)
            if all_weather_nan:
                 logging.debug(f"Skipping record with all NaN data values after check: {record}") # Debug level
                 continue

            # Filter uses the fields defined in our unique index ('timestamp_lat_lon_unique')
            op_filter = {
                "timestamp": ts,
                "latitude": float(lat), # Ensure float type
                "longitude": float(lon) # Ensure float type
            }

            # The $set operation defines the fields to insert or update.
            # Match field names exactly with your C# MongoDbForecast class attributes.
            # Handle potential NaN values by converting them to None (which MongoDB handles, or omits)
            op_update_set = {
                "timestamp": ts,
                "latitude": float(lat),
                "longitude": float(lon),
                "temperature": None if np.isnan(temp_c_val) else float(temp_c_val),
                "temperatureUnit": "C", # Hardcoded based on our conversion
                "windSpeed": None if np.isnan(wind_speed_val) else float(wind_speed_val),
                "windSpeedUnit": "m/s", # Hardcoded based on source units
                "windDirection": None if np.isnan(wind_dir_val) else int(round(wind_dir_val)), # Store as rounded integer degrees
                "windDirectionUnit": "degrees" # Hardcoded based on our calculation
                # "sunrise": None, # Example if needed
            }
            # Remove keys where value is None if your schema doesn't require them or if you prefer cleaner docs
            # op_update_set = {k: v for k, v in op_update_set.items() if v is not None}

            # If after removing Nones, only coordinates/timestamp remain, maybe skip? Depends on use case.
            # Example check: if len(op_update_set) <= 3: continue

            op_update = { "$set": op_update_set }

            # Create an UpdateOne operation with upsert=True.
            operations.append(UpdateOne(op_filter, op_update, upsert=True))

        except Exception as e:
            # Log errors during record processing but continue with others
            logging.error(f"Error preparing operation for record (Time: {record.get(time_col)}, Lat: {record.get('latitude')}, Lon: {record.get('longitude')}). Error: {e}", exc_info=False)
            continue # Skip this record

    return operations

def load_data_to_mongo(collection, operations: list):
    """
    Executes a pymongo bulk_write operation to efficiently load/update data in MongoDB.
    """
    if not operations:
        logging.warning("    Load function called but received no operations to execute.")
        return

    try:
        # `ordered=False` is crucial for performance:
        result = collection.bulk_write(operations, ordered=False)

        # Log summary of the bulk write operation result
        logging.info(
            f"    MongoDB Bulk Write Result: "
            f"Upserted (New): {result.upserted_count}, "
            f"Matched (Existing): {result.matched_count}, "
            f"Modified: {result.modified_count}"
        )

    except BulkWriteError as bwe:
        error_count = len(bwe.details.get('writeErrors',))
        logging.error(f"    Bulk write operation completed with {error_count} errors.")
        # Optionally log the first few errors for diagnostics
        for i, err in enumerate(bwe.details.get('writeErrors',)[:5]):
             logging.debug(f"      Error {i+1}: Index={err.get('index')}, Code={err.get('code')}, Msg={err.get('errmsg')}")
    except Exception as e:
        logging.error(f"    An unexpected error occurred during MongoDB bulk write execution: {e}", exc_info=True)


# --- 5. Main Execution Block ---

def main():
    """Main ETL orchestration function."""
    setup_logging()
    logging.info("Starting ERA5-Land NetCDF to MongoDB ETL Process...")

    # Log the configuration being used
    logging.info("--- Configuration ---")
    log_uri = MONGO_URI.split('@')[-1] if '@' in MONGO_URI else MONGO_URI # Mask credentials
    logging.info(f"Mongo URI: {'***' + log_uri if MONGO_URI!= DEFAULT_MONGO_URI else log_uri}")
    logging.info(f"Database Name: {DB_NAME}")
    logging.info(f"Collection Name: {COLLECTION_NAME}")
    logging.info(f"NetCDF Path Pattern: {NETCDF_PATH_PATTERN}")
    logging.info(f"Time Chunk Size: {TIME_CHUNK_SIZE} steps")
    logging.info("---------------------")

    client = None # Initialize client to None for finally block
    try:
        # 1. Connect to MongoDB
        client = get_mongo_client(MONGO_URI)
        db = client[DB_NAME]         # Select the database
        collection = db[COLLECTION_NAME] # Select the collection

        # 2. Assert Indexes (Crucial for performance and uniqueness)
        logging.info("Setting up database indexes (if they don't exist)...")
        setup_indexes(collection)

        # 3. Find NetCDF files to process
        logging.info(f"Searching for NetCDF files using pattern: {NETCDF_PATH_PATTERN}")
        script_dir = Path(__file__).parent
        # Resolve the pattern relative to the script directory
        absolute_pattern = str(script_dir / NETCDF_PATH_PATTERN)
        file_list = sorted(glob.glob(absolute_pattern)) # Use glob to find files matching the pattern

        if not file_list:
            logging.error(f"No NetCDF files found matching pattern: {absolute_pattern}")
            logging.error("Please ensure the NETCDF_PATH_PATTERN in your.env file points to the correct directory relative to this script (e.g., 'era5_land_data/*.nc'), and that the.nc files exist in that directory.")
            sys.exit(1)

        logging.info(f"Found {len(file_list)} NetCDF files to process.")

        # 4. Loop through files and process each one
        total_files = len(file_list)
        for index, filepath in enumerate(file_list):
            logging.info(f"--- Processing file {index + 1}/{total_files}: {Path(filepath).name} ---")
            # Wrap file processing in a try-except to catch errors specific to one file
            try:
                process_netcdf_file(filepath, collection)
            except Exception as e:
                logging.error(f"An unhandled error occurred while processing file {filepath}. Error: {e}", exc_info=True)
                logging.warning(f"Skipping to next file due to error in {Path(filepath).name}.")
                continue # Move to the next file

        logging.info("--- ETL pipeline finished processing all found files. ---")

    except Exception as e:
        # Catch errors during setup (e.g., DB connection, initial file search)
        logging.critical(f"An unrecoverable error occurred during script setup or execution: {e}", exc_info=True)
    finally:
        # Ensure the MongoDB connection is closed regardless of success or failure
        if client:
            client.close()
            logging.info("MongoDB connection closed.")

if __name__ == "__main__":
    main()