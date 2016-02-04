//By 唐人杰 on 2016.1.23
//Used on Master
/* function:
 *  Get two groups of sensor data
 *  Master of two arduino nano
 *  Send the data with bluetooth
 */
/*
   Modified by Zhaoyang, Feb. 2016
   use serial communication between boards
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

unsigned long lastDisplay;

//传感器ID偏移
#define Offset_ID 1


String buffer;

// The setup() function runs after reset.
void setup() {
  Serial.begin(SERIAL_PORT_SPEED);
  Wire.begin();
  for(int i = 0; i < cntSensors; i++)
    sensors[i].init(i + 2 * Offset_ID);
  lastDisplay = millis();
  Serial.println("ending setup");
}

// The loop function runs continuously after setup().
void loop() {
  int cntData = 0;
  while(Serial.available()){
    cntData++;
    String str;
    str = Serial.readStringUntil('>') + '>';
    if(str[0]=='<')
      Serial.print(str);
    if(cntData >= 6) {
      Serial.flush();
      break;
    }
  }
  
  // Get sensor data
  for (int i = 0; i < cntSensors; i++) {
    sensors[i].refresh();
  }
  if(millis() - lastDisplay >= DISPLAY_INTERVAL) {
    lastDisplay = millis();
    for (int i = 0; i < cntSensors; i++) {
      sensors[i].sendToSerial();
      Serial.println();
    }
  }
}
