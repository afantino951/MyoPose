import asyncio
import argparse
import utils
from constants import *
from datetime import datetime
from bleak import BleakScanner, BleakClient

def parse_arguments():
    """
    Parse command line arguments
    """
    parser = argparse.ArgumentParser(description='Log BLE MyoPose data to CSV file.')
    parser.add_argument('output_file', type=str, help='Output CSV file path')
    return parser.parse_args()


def parse_data(data: bytes):
    """
    Parse the data packet consisting of 16-bit integers.
    """
    values = [int.from_bytes(data[i:i+2], byteorder='little', signed=True) for i in range(0, len(data), 2)]
    return values

async def main(output_file):
    print("Scanning for devices...")
    devices = await BleakScanner.discover()

    # Find the device with the specified name
    target_device = next((device for device in devices if device.name == BLE_DEVICE_NAME), None)
    if not target_device:
        print(f"Device named '{BLE_DEVICE_NAME}' not found.")
        return

    print(f"Found device: {target_device.name} ({target_device.address})")

    async with BleakClient(target_device.address) as client:
        print("Connected to device.")

        # Ensure the characteristic exists
        characteristic_uuids = [c.uuid for i, c in client.services.characteristics.items()]
        if DATA_CHARACTERISTIC_UUID not in characteristic_uuids:
            print(f"Characteristic UUID {DATA_CHARACTERISTIC_UUID} not found on the device.")
            print(f"Device Characteristics: {client.services.characteristics}")
            return

        print(f"Listening for data on characteristic {DATA_CHARACTERISTIC_UUID}...")

        def notification_handler(_, data):
            """Handle incoming notifications."""
            current_time = datetime.now()
            timestamp = current_time.strftime("%a %b %d %H:%M:%S %Y")  # Format: Tue Nov 19 13:07:43 2024
            millis = current_time.microsecond // 1000  # Convert microseconds to milliseconds
            values = parse_data(data)
            print(f"{timestamp},{millis} - {values}")
            utils.write_to_csv(output_file, timestamp, millis, values)

        # Subscribe to the characteristic
        await client.start_notify(DATA_CHARACTERISTIC_UUID, notification_handler)

        try:
            # Keep the script running to listen for notifications
            print("Press Ctrl+C to stop.")
            while True:
                await asyncio.sleep(1)
        except KeyboardInterrupt:
            print("Stopping notifications...")
            await client.stop_notify(DATA_CHARACTERISTIC_UUID)


if __name__ == "__main__":
    args = parse_arguments()

    # Check if file exists and get user confirmation
    if not utils.check_file_exists(args.output_file):
        print(f"Exiting without overwriting {args.output_file}")
        exit(1)
        
    # Write header and start main loop
    header = ["Timestamp", "Millis"]
    utils.write_csv_header(args.output_file, header)
    asyncio.run(main(args.output_file))
