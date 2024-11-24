#include <Arduino.h>
#include <ArduinoJson.h>
#include <ArduinoBLE.h>
#include <MyoWare.h>

#include "myopose.h"


const unsigned long SERIAL_BAUD_RATE = 115200;

const int WRITE_BUFFER_SIZE = 256;
const char* nameOfDevice = "MyoPose";
const char* nameOfPeripheral = "MyoPose 33 IOT";
const char* uuidOfService    = "16480000-0525-4ad5-b4fb-6dd83f49546b";
const char* uuidOfConfigChar = "16480001-0525-4ad5-b4fb-6dd83f49546b";
const char* uuidOfDataChar   = "16480002-0525-4ad5-b4fb-6dd83f49546b";

bool WRITE_BUFFER_FIXED_LENGTH = false;

// BLE Service
BLEService sensorService(uuidOfService);

// RX / TX Characteristics
BLECharacteristic configChar(uuidOfConfigChar,
                             BLERead,
                             WRITE_BUFFER_SIZE,
                             WRITE_BUFFER_FIXED_LENGTH);
BLEDescriptor     configNameDescriptor("2901", "Sensor Configuration");
BLECharacteristic sensorDataChar(uuidOfDataChar,
                                 BLERead | BLENotify,
                                 80,
                                 WRITE_BUFFER_FIXED_LENGTH);
BLEDescriptor     sensorDataDescriptor("2901", "Sensor Data TX");

static int8_t ble_output_buffer[WRITE_BUFFER_SIZE];

// static bool          config_received = false;
static unsigned long currentMs, previousMs;
static long          interval = 0;
extern volatile int  samplesRead;

DynamicJsonDocument config_message(256);
// JsonDocument config_message();
auto& dataOutSerial = Serial;


int column_index = 0;

static void sendJsonConfig()
{
  serializeJson(config_message, ble_output_buffer, WRITE_BUFFER_SIZE);
  configChar.writeValue(ble_output_buffer, WRITE_BUFFER_SIZE);
}

/*
 * LEDS
 */
void connectedLight()
{
  digitalWrite(LED_BUILTIN, HIGH);
}

void disconnectedLight()
{
  digitalWrite(LED_BUILTIN, LOW);
}

void onBLEConnected(BLEDevice central)
{
  Serial.print("Connected event, central: ");
  Serial.println(central.address());
  connectedLight();
}

void onBLEDisconnected(BLEDevice central)
{
  Serial.print("Disconnected event, central: ");
  Serial.println(central.address());
  disconnectedLight();
  BLE.setConnectable(true);
}

static void setup_ble()
{
  if (!BLE.begin())
  {
    Serial.println("starting BLE failed!");
    for (;;) {
      // Loop forever
    }
  }

  BLE.setDeviceName(nameOfDevice);
  BLE.setLocalName(nameOfPeripheral);
  BLE.setAdvertisedService(sensorService);
  BLE.setConnectionInterval(0x0006, 0x0007);  // 1.25 to 2.5ms
  BLE.noDebug();

  configChar.addDescriptor(configNameDescriptor);
  sensorDataChar.addDescriptor(sensorDataDescriptor);
  sensorService.addCharacteristic(configChar);
  sensorService.addCharacteristic(sensorDataChar);

  delay(1000);
  BLE.addService(sensorService);

  // Bluetooth LE connection handlers.
  BLE.setEventHandler(BLEConnected, onBLEConnected);
  BLE.setEventHandler(BLEDisconnected, onBLEDisconnected);

  BLE.advertise();

  Serial.println("Bluetooth device active, waiting for connections...");
}

void setup() {
    Serial.begin(SERIAL_BAUD_RATE);
    delay(2000);
    Serial.println("Setting up...");

    setup_ble();

#if ENABLE_MYOS
    column_index += setup_myos(config_message, column_index);
    // Every 5ms (200Hz) send a new BLE message
    interval = 5;
#endif
    config_message["samples_per_packet"] = MAX_SAMPLES_PER_PACKET;

    delay(1000);
    sendJsonConfig();
}

static int packetNum      = 0;
static int sensorRawIndex = 0;
void loop() {
  currentMs = millis();

  BLEDevice central = BLE.central();
  if (central) {
    if (central.connected()) {
      connectedLight();
    }
  }
  else {
    disconnectedLight();
  }

  if (currentMs - previousMs >= interval) {
    // save the last time you blinked the LED
    previousMs = currentMs;
#if ENABLE_MYOS
    sensorRawIndex = update_myos(sensorRawIndex);
    packetNum++;
    int16_t* pData = get_myos_pointer();
    sensorDataChar.writeValue((void*) pData, sensorRawIndex * sizeof(int16_t));

    // Serial.print(sensorRawIndex);
    // Serial.print("\t");
    // int16_t* nData = pData;
    // for (int i = 0; i < sensorRawIndex; i++) {
    //   int16_t d = nData[0];
    //   Serial.print(d);
    //   Serial.print("\t");
    //   nData++;
    // }
    // Serial.println();

    // Reset the buffer
    sensorRawIndex = 0;
    memset(pData, 0, MAX_NUMBER_OF_COLUMNS * MAX_SAMPLES_PER_PACKET * sizeof(int16_t));
#endif
  }
}
