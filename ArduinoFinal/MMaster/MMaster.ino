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

  //Get slave data
  int cnt = 0;
  while(Serial.available()){
    parseSerialData(Serial.read());
    cnt ++;
    if(cnt > 12) break;
  }
  
  // Get sensor data
  for (int i = 0; i < cntSensors; i++) {
    sensors[i].refresh();
  }
  if(millis() - lastDisplay >= DISPLAY_INTERVAL) {
    lastDisplay = millis();
    for (int i = 0; i < cntSensors; i++) {
      Serial.print(sensors[i].getString());
    }
    //Serial.println();
  }
}

char getName(char chr){
  // x,y,z->0,1,2
  // u,v,w->3,4,5
  char names[] = {'x','y','z','u','v','w'};
  return names[chr];
}



void parseSerialData(char chr){
  
  
  static char lower = 0;
  static bool lowerAssigned = false;
  
  // _ _ _ _ _ _ _ _
  // 7 6 5 4 3 2 1 0
  
  // 0 x x x x x x x  as input
  
  // 0 1 0 0 0 0 0 0  as mask
  
  // 0 ? 0 0 0 0 0 0
  if((chr & (1<<6))){
    if(!lowerAssigned){
      return ;
    }
    // 6th bit non-zero
    // higher
    // 0 1 x x x x x x 
    char higher = chr & 7; // 00000111 as mask
    char id = (chr & 56) >> 3;    // 00111000 as mask
    char value = higher * 64 + lower;
    char name_ = getName(id);
    lowerAssigned = false;
    if(id<0 || id>5){
      //Serial.println(chr, BIN);
      return;
    }
    Serial.print("<" + String(name_) + ":" + String((int)value) + ">");
    //if(name_ == 'z') Serial.println();
    return ;
  }else{
    //6th bit zero
    // lower
    lower = chr;
    lowerAssigned = true;
    return ;
  }
}
