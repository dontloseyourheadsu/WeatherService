#!/bin/bash
# Extract NetCDF files from ZIP archives downloaded by mongodb-download-datasets.py
# This script extracts data_0.nc from each .nc zip file and renames it appropriately

set -e  # Exit on error

# Define the data directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DATA_DIR="$SCRIPT_DIR/era5_land_data"

# Check if the data directory exists
if [ ! -d "$DATA_DIR" ]; then
    echo "Error: Directory $DATA_DIR does not exist."
    echo "Please run mongodb-download-datasets.py first to download the data."
    exit 1
fi

# Find all .nc files (which are actually ZIP archives)
# Exclude files that end with -extracted.nc
shopt -s nullglob
nc_files=()
for file in "$DATA_DIR"/*.nc; do
    # Skip files that already have -extracted.nc suffix
    if [[ ! "$file" =~ -extracted\.nc$ ]]; then
        nc_files+=("$file")
    fi
done

if [ ${#nc_files[@]} -eq 0 ]; then
    echo "No .nc files found in $DATA_DIR"
    echo "Please run mongodb-download-datasets.py first to download the data."
    exit 1
fi

echo "Found ${#nc_files[@]} .nc archive(s) to extract"
echo "========================================"

extracted_count=0
skipped_count=0

for nc_file in "${nc_files[@]}"; do
    # Get the base filename without extension
    base_name=$(basename "$nc_file" .nc)
    output_file="$DATA_DIR/${base_name}-extracted.nc"
    
    # Skip if already extracted
    if [ -f "$output_file" ]; then
        echo "Skipping $base_name (already extracted)"
        ((skipped_count++))
        continue
    fi
    
    echo "Extracting: $base_name"
    
    # Check if it's actually a ZIP file
    if unzip -l "$nc_file" > /dev/null 2>&1; then
        # Extract data_0.nc from the ZIP to a temporary location
        temp_dir=$(mktemp -d)
        if unzip -q "$nc_file" "data_0.nc" -d "$temp_dir"; then
            # Move the extracted file to the final location with proper name
            mv "$temp_dir/data_0.nc" "$output_file"
            rm -rf "$temp_dir"
            echo "  ✓ Extracted to: ${base_name}-extracted.nc"
            ((extracted_count++))
        else
            echo "  ✗ Failed to extract data_0.nc from $nc_file"
            rm -rf "$temp_dir"
        fi
    else
        echo "  ✗ File is not a valid ZIP archive, skipping"
    fi
done

echo "========================================"
echo "Extraction complete!"
echo "  Extracted: $extracted_count file(s)"
echo "  Skipped: $skipped_count file(s) (already extracted)"
echo ""
if [ $extracted_count -gt 0 ]; then
    echo "You can now run store-weather-data.py to load the data into MongoDB."
else
    echo "All files have already been extracted. Ready to run store-weather-data.py!"
fi
