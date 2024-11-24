#ifndef MYOPOSE_H
#define MYOPOSE_H

#include <ArduinoJson.h>
#include <MyoWare.h>

#define ENABLE_MYOS 1
#define ENABLE_BLE 1


/**
 * Myoware Sensor Settings
 */
#define NUM_MYO 4

void configure_myo(MyoWare *m, int raw_pin, int env_pin=0, int ref_pin=0, int rect_pin=0);

int setup_myos(JsonDocument& config_message, int column_start);
int update_myos(int startIndex);
int16_t* get_myos_pointer();


/**
 * BLE Settings
 */
#if ENABLE_BLE
#define MAX_NUMBER_OF_COLUMNS 10
#define MAX_SAMPLES_PER_PACKET 1
#else
#define MAX_NUMBER_OF_COLUMNS 20
#define MAX_SAMPLES_PER_PACKET 6
#endif  // ENABLE_BLE


#endif // MYOPOSE_H
