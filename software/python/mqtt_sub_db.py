import time
import sqlite3
import pandas as pd
import paho.mqtt.client as mqtt

# Database setup
db_connection = sqlite3.connect("../../data/mqtt_data.db")
db_cursor = db_connection.cursor()
db_cursor.execute("""
    CREATE TABLE IF NOT EXISTS mqtt_messages (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        timestamp TEXT,
        myo0 INTEGER,
        myo1 INTEGER,
        myo2 INTEGER,
        myo3 INTEGER
    )
""")
db_connection.commit()

# Initialize DataFrame and buffer
columns = [
    "timestamp",
    "myo0",
    "myo1",
    "myo2",
    "myo3",
]  # Adjust columns as per your message structure
data_buffer = []
flush_interval = 0.5  # Flush buffer to DataFrame every 0.5 seconds
db_write_interval = 5  # Write DataFrame to database every 5 seconds
last_flush_time = time.time()
last_db_write_time = time.time()
dataframe = pd.DataFrame(columns=columns)


# 0. define callbacks - functions that run when events happen.
# The callback for when the client receives a CONNACK response from the server.
def on_connect(client, userdata, flags, rc, properties):
    # Subscribing in on_connect() means that if we lose the connection and
    # reconnect then subscriptions will be renewed.
    print("Connection returned result: " + str(rc))
    client.subscribe("afa/myo", qos=1)


# The callback of the client when it disconnects.
def on_disconnect(client, userdata, flags, rc, properties):
    if rc != 0:
        print("Unexpected Disconnect")
    else:
        print("Expected Disconnect")


# The default message callback.
# (you can create separate callbacks per subscribed topic)
def on_message(client, userdata, message):
    global last_flush_time, last_db_write_time, dataframe

    # Decode and split the message payload
    data = message.payload.decode().split(",")
    print(f"{data}")
    data_buffer.append(
        data[:5]
    )  # Add data to buffer (adjust slicing as per payload structure)

    # Periodically flush buffer to DataFrame
    if (time.time() - last_flush_time) >= flush_interval:
        new_data = pd.DataFrame(data_buffer, columns=columns)
        dataframe = pd.concat([dataframe, new_data], ignore_index=True)
        data_buffer.clear()  # Clear buffer after flushing
        last_flush_time = time.time()

    # Periodically write DataFrame to SQLite database
    if (time.time() - last_db_write_time) >= db_write_interval:
        if not dataframe.empty:
            dataframe.to_sql(
                "mqtt_messages", db_connection, if_exists="append", index=False
            )
            dataframe = pd.DataFrame(columns=columns)  # Clear DataFrame after writing
            last_db_write_time = time.time()


if __name__ == "__main__":
    # 1. create a client instance.
    client = mqtt.Client(mqtt.CallbackAPIVersion.VERSION2)
    # add additional client options (security, certifications, etc.)
    # many default options should be good to start off.

    client.on_connect = on_connect
    client.on_disconnect = on_disconnect
    client.on_message = on_message

    # 2. connect to a broker using one of the connect*() functions.
    client.connect("mqtt.eclipseprojects.io", 1883, 60)
    try:
        client.loop_forever()
    except KeyboardInterrupt:
        print("Disconnecting...")
        # Final flush of buffer to DataFrame
        if data_buffer:
            new_data = pd.DataFrame(data_buffer, columns=columns)
            dataframe = pd.concat([dataframe, new_data], ignore_index=True)

        # Final write to database
        if not dataframe.empty:
            dataframe.to_sql(
                "mqtt_messages", db_connection, if_exists="append", index=False
            )
    finally:
        db_connection.close()  # Close database connection
        client.disconnect()  # Disconnect mqtt client
