#include <myopose.h>


// int myo_env_pins[] = {A2, A3, A4, A5};
int myo_raw_pins[] = {A0, A1, A2, A3};
int myo_env_pins[] = {A4, A5, A6, A7};

MyoWare myos[NUM_MYO];
int16_t myoRawData[MAX_SAMPLES_PER_PACKET*MAX_NUMBER_OF_COLUMNS];

void configure_myo(MyoWare *m, int raw_pin, int env_pin, int ref_pin, int rect_pin)
{
  m->setConvertOutput(false);   // Set to true to convert ADC output to the amplitude of
                                // of the muscle activity as it appears at the electrodes
                                // in millivolts
  m->setADCResolution(12.);     // ADC bits (shield default = 12-bit)
  m->setADCVoltage(3.3);        // ADC reference voltage (shield default = 3.3V)
  m->setENVPin(env_pin);        // Arduino pin connected to ENV
  m->setRAWPin(raw_pin);        // Arduino pin connected to RAW
  m->setRECTPin(raw_pin);       // Arduino pin connected to RAW
}

int setup_myos(JsonDocument& config_message, int column_start)
{
  int column_index = column_start;

  for (int i = 0; i < NUM_MYO; i++) {
    configure_myo(&myos[i], myo_raw_pins[i], myo_env_pins[i]);

    String message_index_name = "Myo";
    message_index_name += String(i);
    config_message["column_location"][message_index_name] = column_index++;
  }
  config_message["sample_rate"] = 0;


  return column_index;
}

int update_myos(int startIndex)
{
  int sensorRawIndex = startIndex;
#if ENABLE_MYOS
    for (; sensorRawIndex < startIndex+NUM_MYO; sensorRawIndex++) {
      myoRawData[sensorRawIndex] = myos[sensorRawIndex].readSensorOutput(MyoWare::ENVELOPE);
    //   myoRawData[sensorRawIndex] = myos[sensorRawIndex].readSensorOutput(MyoWare::RECTIFIED);
    }
#else
#endif // ENABLE_MYOS

  return sensorRawIndex;
}

int16_t* get_myos_pointer()
{
  return &myoRawData[0];
}