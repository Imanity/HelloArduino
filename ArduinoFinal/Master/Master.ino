//By 唐人杰 on 2016.1.23
//Used on Arduino Uno
/* Function:
 *  Get a group of sensor data
 *  Communicate with two Arduino nano
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

#define cntSensors 1 // no more than 2
Sensor sensors[cntSensors];

#define DISPLAY_INTERVAL 50      // interval between pose displays
#define SERIAL_PORT_SPEED 9600

unsigned long lastDisplay;
String strBuffer;

#define mySS 9

// SPI Transfer.
byte SPItransfer(byte value) {
  SPDR = value;
  while(!(SPSR & (1<<SPIF)));
  delay(10);
  return SPDR;
}

// The setup() function runs after reset.
void setup() {
  Serial.begin(SERIAL_PORT_SPEED);
  Wire.begin();
  for(int i=0; i<cntSensors; i++)
    sensors[i].init(i);
  lastDisplay = millis();
  Serial.println("ending setup");
  // Initialize SPI.
  SPI.begin();
  pinMode(MISO, INPUT);
  pinMode(mySS, OUTPUT);
  digitalWrite(mySS, HIGH);
}

// The loop() function runs continuously after setup().
void loop() {
  digitalWrite(mySS, LOW);
  byte ch = SPItransfer(255);
  if (ch != 255) {
    if (ch == 'u') {
      Serial.print(strBuffer);
      strBuffer = "<u";
    } else {
      strBuffer += char(ch);
    }
  }
  // Disable slave.
  digitalWrite(mySS, HIGH);
  
  //Send sensor data
  unsigned long now = millis();
  bool itIsTimeToSendData = (now - lastDisplay) >= DISPLAY_INTERVAL;
  for(int i=0; i<cntSensors; i++){
    sensors[i].refresh();
  }
  if(itIsTimeToSendData){
    lastDisplay = now;
    for(int j=0; j<cntSensors; j++) {
       sensors[j].sendToSerial();
    }
  }
}

