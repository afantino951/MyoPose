import matplotlib.pyplot as plt
import numpy as np
import pandas as pd


if __name__ == "__main__":
    print("Hello")

    file_path = "../../data/leapData.csv"
    df = pd.read_csv(file_path, index_col=False)

    # Combine the timestamp milliseconds column into a pandas DateTime dtype
    timestamp_format = "%a %b %d %H:%M:%S %Y"  # Matches "Tue Nov 19 12:19:56 2024"
    df['Timestamp_millis'] = pd.to_datetime(df['Timestamp'], format=timestamp_format) + pd.to_timedelta(df['Millis'], unit='ms')

    print(df['Timestamp_millis'].iloc[-1] - df['Timestamp_millis'].iloc[0] )

    column_name = "index_mcp_flex"

    # Plot the data
    plt.figure(figsize=(10, 6))
    plt.plot(df['Timestamp_millis'], df[column_name], label=column_name)

    # Customize the plot
    plt.title(f"{column_name} Over Time")
    plt.xlabel("Time")
    plt.ylabel(column_name)
    plt.grid(True)
    plt.legend()
    plt.tight_layout()

    # Show the plot
    plt.show()

