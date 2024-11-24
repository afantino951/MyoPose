import os
import pandas as pd
import numpy as np
from datetime import datetime

def parse_timestamp(timestamp, millis):
    """Convert timestamp and millis to total milliseconds since epoch"""
    dt = datetime.strptime(timestamp, '%a %b %d %H:%M:%S %Y')
    return dt.timestamp() * 1000 + millis

def create_aligned_intervals(start_time, end_time, interval=5):
    """Create time intervals aligned to seconds"""
    # Convert to datetime for easier manipulation
    start_dt = datetime.fromtimestamp(start_time / 1000)
    end_dt = datetime.fromtimestamp(end_time / 1000)
    
    # Align to the start of the second
    start_dt = start_dt.replace(microsecond=0)
    end_dt = end_dt.replace(microsecond=0)
    
    # Create array for all seconds in range
    seconds = np.arange(start_dt.timestamp(), end_dt.timestamp() + 1)
    
    # Create millisecond offsets (0, 5, 10, ..., 995)
    millis_offsets = np.arange(0, 1000, interval)
    
    # Create all timestamps
    all_times = np.array([
        (second * 1000 + offset)
        for second in seconds
        for offset in millis_offsets
    ])
    
    # Filter to only include times within original range
    return all_times[(all_times >= start_time) & (all_times <= end_time)]

def interpolate_data(file_path, target_interval=5):
    """Load and interpolate data to specified millisecond intervals"""
    # Read CSV
    df = pd.read_csv(file_path)
    
    # Convert timestamp and millis to total milliseconds
    df['total_millis'] = df.apply(lambda row: parse_timestamp(row['Timestamp'], row['Millis']), axis=1)
    
    # Sort by total milliseconds
    df = df.sort_values('total_millis')
    
    # Create regular time intervals aligned to seconds
    regular_intervals = create_aligned_intervals(
        df['total_millis'].min(),
        df['total_millis'].max(),
        target_interval
    )

    # Create new dataframe with regular intervals
    interpolated_df = pd.DataFrame({'total_millis': regular_intervals})
    
    # Interpolate all numeric columns except Millis
    numeric_cols = df.select_dtypes(include=[np.number]).columns.tolist()
    numeric_cols.remove('Millis')
    
    for col in numeric_cols:
        interpolated_values = np.interp(regular_intervals, df['total_millis'], df[col])
        interpolated_df[col] = np.round(interpolated_values, decimals=3)
    
    return interpolated_df

def rename_csv(output_dir, ble_file_path, leap_file_path):
    # Load and interpolate both files
    df1 = interpolate_data(ble_file_path)
    df2 = interpolate_data(leap_file_path)
    
    # Find common time range
    start_time = max(df1['total_millis'].min(), df2['total_millis'].min())
    end_time = min(df1['total_millis'].max(), df2['total_millis'].max())
    
    # Trim both dataframes to common time range
    df1 = df1[(df1['total_millis'] >= start_time) & (df1['total_millis'] <= end_time)]
    df2 = df2[(df2['total_millis'] >= start_time) & (df2['total_millis'] <= end_time)]
    
    # Convert total_millis back to Timestamp and Millis
    def millis_to_timestamp(total_millis):
        seconds = total_millis / 1000
        dt = datetime.fromtimestamp(seconds)
        millis = int(total_millis % 1000)
        return pd.Series({
            'Timestamp': dt.strftime('%a %b %d %H:%M:%S %Y'),
            'Millis': millis
        })
    
    # Apply conversion to both dataframes
    timestamp_cols1 = df1['total_millis'].apply(millis_to_timestamp)
    timestamp_cols2 = df2['total_millis'].apply(millis_to_timestamp)
    
    # Add timestamp columns back to dataframes
    df1 = pd.concat([timestamp_cols1, df1.drop('total_millis', axis=1)], axis=1)
    df2 = pd.concat([timestamp_cols2, df2.drop('total_millis', axis=1)], axis=1)
    
    # Save interpolated and aligned data
    output_ble_path = os.path.join(output_dir, ble_file_path.split("/")[-1])
    output_leap_path = os.path.join(output_dir, leap_file_path.split("/")[-1])

    df1.to_csv(output_ble_path, index=False, float_format='%.3f')
    df2.to_csv(output_leap_path, index=False, float_format='%.3f')

if __name__ == "__main__":

    input_dir = "../../data/myo4_env/dirty"
    output_dir = "../../data/myo4_env/clean"

    for i in range(1,10):
        sample = i
        ble_file_path = os.path.join(input_dir, f"myo4_env_s{sample}_bleData.csv")
        leap_file_path = os.path.join(input_dir, f"myo4_env_s{sample}_leapData.csv")

        rename_csv(output_dir, ble_file_path, leap_file_path)
