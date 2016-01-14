/*

Read sensor data (MPU9250 on I2C ports A4/A5), 
wrap them, and send to Serial (COM, or Bluetooth on TX/RX).

By Zhaoyang, Jan 2016.

*/

//uncomment this to get detailed status report on Serial
//#define VERBOSE

////////////////////////////////////////////////////////////////////////////
//
// This file is part of RTIMULib-Arduino
//
// Copyright (c) 2014, richards-tech
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the
// Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


#include <Wire.h>
#include "I2Cdev.h"
#include "RTIMUSettings.h"
#include "RTIMU.h"
#include "RTFusionRTQF.h"
#include "CalLib.h"
#include <EEPROM.h>

RTIMU *imu;                      // the IMU object
RTFusionRTQF fusion;             // the fusion object
RTIMUSettings settings;          // the settings object

// DISPLAY_INTERVAL sets the rate at which results are displayed

#define DISPLAY_INTERVAL 50      // interval between pose displays

// SERIAL_PORT_SPEED defines the speed to use for the debug serial port

#define SERIAL_PORT_SPEED 9600

unsigned long lastDisplay;
unsigned long lastRate;
int sampleCount;

void sendToSerial(RTVector3& vec){
  String line;
  line += "<x:";
  line += (vec.x() * RTMATH_RAD_TO_DEGREE);
  line += ">";
  line += "<y:" ;
  line += (vec.y() * RTMATH_RAD_TO_DEGREE);
  line += ">";
  line += "<z:" ;
  line += (vec.z() * RTMATH_RAD_TO_DEGREE);
  line += ">";
  Serial.print(line);
  /*
  Serial.print("<x:"); Serial.print(vec.x() * RTMATH_RAD_TO_DEGREE);
  Serial.print("><y:"); Serial.print(vec.y() * RTMATH_RAD_TO_DEGREE);
  Serial.print("><z:"); Serial.print(vec.z() * RTMATH_RAD_TO_DEGREE);
  Serial.println(">");
  */
}

void setup(){
  int errcode;

  Serial.begin(SERIAL_PORT_SPEED);
  Wire.begin();
  imu = RTIMU::createIMU(&settings);// create the imu object
#ifdef VERBOSE
  Serial.print("ArduinoIMU starting using device "); Serial.println(imu->IMUName());
#endif
  if ((errcode = imu->IMUInit()) < 0) {
#ifdef VERBOSE
    Serial.print("Failed to init IMU: "); Serial.println(errcode);
#endif
  }
#ifdef VERBOSE
  if (imu->getCalibrationValid())
    Serial.println("Using compass calibration");
  else
    Serial.println("No valid compass calibration data");
#endif
  lastDisplay = lastRate = millis();
  sampleCount = 0;

  // use of sensors in the fusion algorithm can be controlled here
  // change any of these to false to disable that sensor

  fusion.setGyroEnable(true);
  fusion.setAccelEnable(true);
  fusion.setCompassEnable(true);
}

void loop(){
  unsigned long now = millis();
  unsigned long delta;

  if (imu->IMURead()) { // get the latest data if ready yet
    fusion.newIMUData(imu->getGyro(), imu->getAccel(), imu->getCompass(), imu->getTimestamp());
    sampleCount++;
    if ((delta = now - lastRate) >= 1000) {
#ifdef VERBOSE
      Serial.print("Sample rate: "); Serial.print(sampleCount);
      if (imu->IMUGyroBiasValid())
        Serial.println(", gyro bias valid");
      else
        Serial.println(", calculating gyro bias");
#endif
      sampleCount = 0;
      lastRate = now;
    }
    if ((now - lastDisplay) >= DISPLAY_INTERVAL) {
      lastDisplay = now;
      sendToSerial((RTVector3&)fusion.getFusionPose()); // formatted output
    }
  }
}
