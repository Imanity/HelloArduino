//By 唐人杰 on 2016.1.23
//Used on Arduino Nano
/* function:
 *  Get two groups of sensor data
 *  Communicate with Arduino Uno
 */

//uncomment this to get detailed status report on Serial
#define VERBOSE

#include <SPI.h>
#include <Wire.h>
#include "I2Cdev.h"
#include "RTIMUSettings.h"
#include "RTIMU.h"
#include "RTFusionRTQF.h"
#include "CalLib.h"
#include <EEPROM.h>

#include "Sensor.h"

#define cntSensors 2 // no more than 2
Sensor sensors[cntSensors];

#define DISPLAY_INTERVAL 50      // interval between pose displays
#define SERIAL_PORT_SPEED 9600

//传感器ID偏移,0号nano为0,1号nano为1
#define Offset_ID 0

//Initialize SPI slave.
void SlaveInit(void) {
  // Initialize SPI pins.
  pinMode(SCK, INPUT);
  pinMode(MISO, OUTPUT);
  pinMode(SS, INPUT);
  // Enable SPI as slave.
  SPCR = (1 << SPE);
}

// The setup() function runs after reset.
void setup() {
  Serial.begin(SERIAL_PORT_SPEED);
  Wire.begin();
  for(int i=0; i<cntSensors; i++)
    sensors[i].init(i + 1 + 2 * Offset_ID);
  Serial.println("ending setup");
  // Initialize SPI Slave.
  SlaveInit();
}

// The loop function runs continuously after setup().
void loop() {
  for(int i=0; i<cntSensors; i++){
    sensors[i].refresh();
  }
  
  if (!digitalRead(SS)) {
    //master wants me to check-in
    for(int j = 0; j < cntSensors; j++) {
      String line = sensors[j].getString();
      int len = line.length();
      for(int i = 0; i < len; ++i) {
        SPI.transfer(line.charAt(i));
      }
      Serial.println(line);
    }
  }
}

