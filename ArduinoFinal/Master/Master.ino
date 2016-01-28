//By 唐人杰 on 2016.1.23
//Used on Master
/* function:
 *  Get two groups of sensor data
 *  Master of two arduino nano
 *  Send the data with bluetooth
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

// SPI Transfer.
byte SPItransfer(byte value) {
  SPDR = value;
  while(!(SPSR & (1<<SPIF)));
  return SPDR;
}

// The setup() function runs after reset.
void setup() {
  Serial.begin(SERIAL_PORT_SPEED);
  Wire.begin();
  for(int i = 0; i < cntSensors; i++)
    sensors[i].init(i + 2 * Offset_ID);
  lastDisplay = millis();
  Serial.println("ending setup");
  SPI.begin ();
  pinMode(MISO, INPUT);
  pinMode(SS, OUTPUT);
  digitalWrite(SS, HIGH);
}

// The loop function runs continuously after setup().
void loop() {
  int currentTime = 0;
  String str = "";
  byte ch;
  while(currentTime != 6) {
    digitalWrite(SS, LOW);
    ch = SPItransfer(255);
    digitalWrite(SS, HIGH);
    if (ch != 255) {
      str += char(ch);
      if (char(ch) == '>') {
        currentTime++;
      }
    }
  }
  Serial.print(str);
  // Get sensor data
  for (int i = 0; i < cntSensors; i++) {
    sensors[i].refresh();
  }
  for (int i = 0; i < cntSensors; i++) {
    sensors[i].sendToSerial();
  }
}

