#include <Arduino.h>
#include <Wire.h>
#include <SPI.h>
#include <PubSubClient.h>
#include <WiFi.h>
#include <time.h>
#include <SparkFun_ISM330DHCX.h>
#include <MyoWare.h>
#include "arduino_secrets.h"


#if CONFIG_FREERTOS_UNICORE
#define TASK_RUNNING_CORE 0
#else
#define TASK_RUNNING_CORE 1
#endif

#define NUM_MYO 4


char sbuf[256] = { '\0' };

// IMU
SparkFun_ISM330DHCX myISM; 
sfe_ism_data_t accelData; 
sfe_ism_data_t gyroData; 

// Myoware Devices
MyoWare myos[NUM_MYO];
int myo_env_pins[] = {A2, A3, A4, A5};
int myo_raw_pins[] = {0, 0, 0, 0};

// int myo_env_pins[] = {0, 0, 0, 0};
// int myo_raw_pins[] = {A2, A3, A4, A5};

// WiFi
IPAddress local_IP(192, 168, 4, 115);
IPAddress gateway(192, 168, 4, 1);
IPAddress subnet(255, 255, 0, 0);
IPAddress primaryDNS(8, 8, 8, 8);    //optional
IPAddress secondaryDNS(8, 8, 4, 4);  //optional

const char *ssid = SECRET_SSID; // Enter your WiFi name
const char *password = SECRET_PASS;  // Enter WiFi password

// MQTT Broker motion
const char *mqtt_broker = "mqtt.eclipseprojects.io";
const char *topic = "afa/myo";
const int mqtt_port = 1883;

WiFiClient espClient;
PubSubClient client(espClient);

void connect_wifi() {
  // if (!WiFi.config(local_IP, gateway, subnet)) {
  //   Serial.println("STA Failed to configure");
  // }
  // connecting to a WiFi network
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
      delay(500);
      Serial.println("Connecting to WiFi..");
  }
  Serial.println("Connected to the WiFi network");
}

void configure_imu() {
  if( !myISM.begin() ){
    Serial.println("Did not begin.");
    while(1);
  }

  // Reset the device to default settings. This if helpful is you're doing multiple
  // uploads testing different settings. 
  myISM.deviceReset();

  // Wait for it to finish reseting
  while( !myISM.getDeviceReset() ){ 
    delay(1);
  } 

  Serial.println("IMU Reset.");
  Serial.println("Applying IMU settings.");
  delay(100);
  
  myISM.setDeviceConfig();
  myISM.setBlockDataUpdate();
  
  // Set the output data rate and precision of the accelerometer
  myISM.setAccelDataRate(ISM_XL_ODR_104Hz);
  myISM.setAccelFullScale(ISM_4g); 

  // Set the output data rate and precision of the gyroscope
  myISM.setGyroDataRate(ISM_GY_ODR_104Hz);
  myISM.setGyroFullScale(ISM_500dps); 

  // Turn on the accelerometer's filter and apply settings. 
  myISM.setAccelFilterLP2();
  myISM.setAccelSlopeFilter(ISM_LP_ODR_DIV_100);

  // Turn on the gyroscope's filter and apply settings. 
  myISM.setGyroFilterLP1();
  myISM.setGyroLP1Bandwidth(ISM_MEDIUM);
}

void configure_myo(MyoWare *m, int env_pin, int raw_pin, int ref_pin=0, int rect_pin=0) {
  // output conversion parameters - modify these values to match your setup
  m->setConvertOutput(false);     // Set to true to convert ADC output to the amplitude of
                                // of the muscle activity as it appears at the electrodes
                                // in millivolts
  m->setADCResolution(12.);      // ADC bits (shield default = 12-bit)
  m->setADCVoltage(3.3);         // ADC reference voltage (shield default = 3.3V)
  // m->setGainPotentiometer(50.);  // Gain potentiometer resistance in kOhms.
                                // adjust the potentiometer setting such that the
                                // max muscle reading is below 3.3V then update this
                                // parameter to the measured value of the potentiometer
  m->setENVPin(env_pin);        // Arduino pin connected to ENV
  m->setRAWPin(raw_pin);        // Arduino pin connected to RAW
  m->setREFPin(A7);             // Arduino pin connected to REF
  m->setRECTPin(A0);            // Arduino pin connected to RECT
}

void setup() {
  Wire.begin();
  Serial.begin(115200);

  // Network Connection Stuffs
  connect_wifi();
  configTime(-7 * 3600, 0 * 0, "pool.ntp.org", "time.nist.gov");
  Serial.println("\nWaiting for time");
  while (!time(nullptr)) {
    Serial.print(".");
    delay(1000);
  }
  Serial.println("");

  // Connect to a mqtt broker
  client.setServer(mqtt_broker, mqtt_port);

  // Set up the client
  while (!client.connected()) {
      String client_id = "esp32-client-";
      client_id += String(WiFi.macAddress());
      Serial.printf("The client %s connects to the public mqtt broker\n", client_id.c_str());
      if (client.connect(client_id.c_str())) { //, mqtt_username, mqtt_password)) {
          Serial.println("mqtt broker connected");
      } else {
          Serial.print("failed with state ");
          Serial.print(client.state());
          delay(2000);
      }
  }

  // Allow the hardware to sort itself out
  delay(1500);

  // Init peripherals
  configure_imu();
  for (int i = 0; i < NUM_MYO; i++) {
    configure_myo(&myos[i], myo_env_pins[i], myo_raw_pins[i]);
  }

  Serial.println("====================");
  Serial.println("   SETUP COMPLETE   ");
  Serial.println("====================");
}

char *my_ctime()
{
  static char wday_name[7][4] = {
      "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"
  };
  static char mon_name[12][4] = {
      "Jan", "Feb", "Mar", "Apr", "May", "Jun",
      "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
  };
  static char result[32];

  timeval tv;
  gettimeofday(&tv, 0);
  time_t curtime = tv.tv_sec;
  tm *timeptr = localtime(&curtime);
  sprintf(result, "%.3s %.3s%3d %.2d:%.2d:%.2d %d, %.3d",
      wday_name[timeptr->tm_wday],
      mon_name[timeptr->tm_mon],
      timeptr->tm_mday, timeptr->tm_hour,
      timeptr->tm_min, timeptr->tm_sec,
      1900 + timeptr->tm_year,
      (tv.tv_usec/1000));
  return result;
}

void emg_env_print(int ind, double value)
{
  snprintf(sbuf, sizeof(sbuf), ">env%d: %.3f", ind, value);
  Serial.println(sbuf);
}

void loop(){
  client.loop();

  // Check if both gyroscope and accelerometer data is available.
  if(myISM.checkStatus()){
    myISM.getAccel(&accelData);
    myISM.getGyro(&gyroData);
  }
  
  // Read myo env
  double env_val[NUM_MYO];
  for (int i = 0; i < NUM_MYO; i++) {
    env_val[i] = myos[i].readSensorOutput(MyoWare::ENVELOPE);
    // env_val[i] = myos[i].readSensorOutput(MyoWare::RAW);
    emg_env_print(i, env_val[i]);
  }

  // Write buffer
  char *time_str = my_ctime();
  // snprintf(sbuf, sizeof(sbuf), "%s, %.3f, %.3f, %.3f, %.3f, %.3f, %.3f, %.3f, %.3f, %.3f, %.3f", 
  //   time_str, env_val[0], env_val[1], env_val[2], env_val[3], accelData.xData, accelData.yData, accelData.zData,
  //   gyroData.xData, gyroData.yData, gyroData.zData
  // );
  snprintf(sbuf, sizeof(sbuf), "%s, %.3f, %.3f, %.3f, %.3f", 
    time_str, env_val[0], env_val[1], env_val[2], env_val[3]
  );
  // Serial.println(sbuf);
  // Serial.print(" ");
  // Serial.println(String(sbuf).length());

  unsigned int msglen = String(sbuf).length();
  client.publish(topic, sbuf, msglen);
  // delay(10);
}
