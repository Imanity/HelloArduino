/*

Read multiple sensor data (MPU9250 on I2C ports A4/A5),
wrap them, and send to Serial (COM, or Bluetooth on TX/RX).

By Zhaoyang, Jan 2016.

*/
/*
传感器连接：
两个传感器 SCL 连在一起，to arduino A5，同时经 10kΩ 上拉电阻 to 3.3V
两个传感器 SDA 连在一起，to arduino A4，同时经 10kΩ 上拉电阻 to 3.3V
0 号传感器 AD0 to 3.3V
1 号传感器 AD0 to GND
*/
/*
 * 传感器的放置：
 * 传感器的 xOy 平面与手臂背侧面重合，x 轴指向手指尖；z 轴指向体外。
 */

//uncomment this to get detailed status report on Serial
#define VERBOSE

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

void setup(){
  Serial.begin(SERIAL_PORT_SPEED);
  Wire.begin();
  for(int i=0; i<cntSensors; i++)
    sensors[i].init(i);
  lastDisplay = millis();
  Serial.println("ending setup");
}

void loop(){
  unsigned long now = millis();
  bool itIsTimeToSendData = (now - lastDisplay) >= DISPLAY_INTERVAL;
  for(int i=0; i<cntSensors; i++){
    sensors[i].refresh();
  }
  if(itIsTimeToSendData){
    lastDisplay = now;
    for(int j=0; j<cntSensors; j++)
       sensors[j].sendToSerial();
  }
  
}
