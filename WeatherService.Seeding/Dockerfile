# Use a slim python image to keep size down
FROM python:3.10-slim

# Install system dependencies required for NetCDF and HDF5
# These are necessary for the python 'netCDF4' library to compile/run
RUN apt-get update && apt-get install -y \
    libnetcdf-dev \
    libhdf5-serial-dev \
    gcc \
    && rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /app

# Copy requirements and install python dependencies
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy the ETL script
COPY etl_weather.py .

# Create a directory for data storage (mounted as volume)
RUN mkdir -p /app/data

# Command to run the script
CMD ["python", "etl_weather.py"]