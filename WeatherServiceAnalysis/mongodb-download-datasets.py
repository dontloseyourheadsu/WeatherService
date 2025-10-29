import os
import time
import calendar
import cdsapi

# Define the output directory (repo-root/WeatherServiceAnalysis/era5_land_data)
output_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'era5_land_data')
os.makedirs(output_dir, exist_ok=True)

# Initialize the CDS API client
client = cdsapi.Client()

# Define the geographical area for Mexico [N, W, S, E]
mexico_bbox = [33, -119, 14, -86]

# Define the year and months to download
target_year = 2022
months = [f"{m:02d}" for m in range(1, 13)]

def _days_for_month(year: int, month_str: str):
    m = int(month_str)
    _, last_day = calendar.monthrange(year, m)
    return [f"{d:02d}" for d in range(1, last_day + 1)]

def _is_probably_netcdf(path: str) -> bool:
    try:
        with open(path, 'rb') as f:
            head = f.read(8)
        if head.startswith(b'CDF'):
            return True
        if head == b"\x89HDF\r\n\x1a\n":
            return True
        return False
    except Exception:
        return False

print(f"Starting batch download for ERA5-Land data for Mexico, year {target_year}...")

# Loop through each month and make a separate request
for month in months:
    # Define a unique output filename for each month
    output_filename = os.path.join(output_dir, f'era5-land-mexico-{target_year}-{month}.nc')
    print(f"\nSubmitting request for month: {month}")

    attempts = 0
    while attempts < 3:
        attempts += 1
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
                    'year': f"{target_year}",
                    'month': month,
                    'day': _days_for_month(target_year, month),
                    'time': [
                        '00:00', '01:00', '02:00', '03:00', '04:00', '05:00',
                        '06:00', '07:00', '08:00', '09:00', '10:00', '11:00',
                        '12:00', '13:00', '14:00', '15:00', '16:00', '17:00',
                        '18:00', '19:00', '20:00', '21:00', '22:00', '23:00',
                    ],
                    'area': mexico_bbox,
                    'format': 'netcdf',
                },
                output_filename
            )

            if _is_probably_netcdf(output_filename):
                print(f"Download complete for month {month}. Data saved to {output_filename}")
                break
            else:
                print(f"Warning: Downloaded file for {month} is not recognized as NetCDF. Attempt {attempts}/3.")
                if attempts < 3:
                    time.sleep(5 * attempts)
                else:
                    bad_name = output_filename + '.invalid'
                    try:
                        os.replace(output_filename, bad_name)
                    except Exception:
                        pass
                    print(f"Gave up after 3 attempts for {month}. Saved invalid response as: {bad_name}")
        except Exception as e:
            print(f"An error occurred while downloading month {month} (attempt {attempts}/3): {e}")
            if attempts < 3:
                time.sleep(5 * attempts)
            else:
                print(f"Failed to download {month} after 3 attempts. Moving on.")

print(f"\nAll downloads for {target_year} complete.")