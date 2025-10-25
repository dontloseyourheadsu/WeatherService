import cdsapi
import os

# Define the output directory
output_dir = 'era5_land_data'
os.makedirs(output_dir, exist_ok=True)

# Initialize the CDS API client
client = cdsapi.Client()

# Define the geographical area for Mexico
# These coordinates roughly cover the extent of Mexico.
mexico_bbox = [33, -119, 14, -86]

# Define the year and months to download
target_year = '2022'
months = [
    '01', '02', '03', '04', '05', '06',
    '07', '08', '09', '10', '11', '12',
]

print(f"Starting batch download for ERA5-Land data for Mexico, year {target_year}...")

# Loop through each month and make a separate request
for month in months:
    
    # Define a unique output filename for each month
    output_filename = os.path.join(output_dir, f'era5-land-mexico-{target_year}-{month}.nc')
    
    print(f"\nSubmitting request for month: {month}")

    try:
        # Execute the data retrieval request for one month
        client.retrieve(
            'reanalysis-era5-land',
            {
                'product_type': 'reanalysis',
                'variable': [
                    '2m_temperature', 
                    '10m_u_component_of_wind', 
                    '10m_v_component_of_wind',
                ],
                'year': target_year,
                'month': month,  # Request only the current month in the loop
                'day': [
                    '01', '02', '03', '04', '05', '06', '07', '08', '09', '10',
                    '11', '12', '13', '14', '15', '16', '17', '18', '19', '20',
                    '21', '22', '23', '24', '25', '26', '27', '28', '29', '30', '31',
                ],
                'time': [
                    '00:00', '01:00', '02:00', '03:00', '04:00', '05:00',
                    '06:00', '07:00', '08:00', '09:00', '10:00', '11:00',
                    '12:00', '13:00', '14:00', '15:00', '16:00', '17:00',
                    '18:00', '19:00', '20:00', '21:00', '22:00', '23:00',
                ],
                'area': mexico_bbox,
                'format': 'netcdf', # Recommended format for gridded data
            },
            output_filename
        )
        print(f"Download complete for month {month}. Data saved to {output_filename}")

    except Exception as e:
        print(f"An error occurred while downloading month {month}: {e}")

print(f"\nAll downloads for {target_year} complete.")