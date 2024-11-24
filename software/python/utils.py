import os
import csv
from constants import *


def check_file_exists(filepath):
    """
    Check if file exists and get user confirmation for overwrite
    Returns True if we should proceed, False if we should abort
    """
    if os.path.exists(filepath):
        while True:
            response = input(f"\nWarning: {filepath} already exists. Overwrite? (Y/n): ").lower()
            if response in ['y', 'n', '']:
                return response in ['y','']
            print("Please enter 'y' or 'n'")
    return True


def write_csv_header(output_file, header):
    """
    Write the CSV file header
    """
    with open(output_file, mode='a', newline='') as file:
        writer = csv.writer(file)

        for i in range(NUM_MYO):
            header.append(f"Myo{i}")

        writer.writerow(header)

def write_to_csv(output_file, timestamp, millis, values):
    """
    Write a timestamp and parsed values to the CSV file.
    """
    with open(output_file, mode='a', newline='') as file:
        writer = csv.writer(file)
        writer.writerow([timestamp, millis] + values)
