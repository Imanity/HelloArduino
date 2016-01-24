/*

By Zhaoyang, Jan 2016.

*/


class Sensor{
public:
    int ID;
    RTIMU *imu;                      // the IMU object
    RTFusionRTQF fusion;             // the fusion object
    RTIMUSettings settings;          // the settings object

    void init(int ID_ = 0){
        #ifdef VERBOSE
            Serial.print("Initializing sensor #");
            Serial.println(ID_);
        #endif
        
        ID = ID_;
        settings.m_I2CSlaveAddress = 0x68;
        if(ID%2 == 1)
            settings.m_I2CSlaveAddress = 0x69; 
        
        //dirty fix: lowers default sample rate
        settings.m_MPU9250GyroAccelSampleRate = 60; /*MAGIC*/
        settings.m_MPU9250CompassSampleRate = 30; /*MAGIC*/
        
        imu = RTIMU::createIMU(&settings);// create the imu object
        
        int errcode = imu->IMUInit();
        
        #ifdef VERBOSE
            Serial.print("ArduinoIMU starting using device "); Serial.println(imu->IMUName());
            if (errcode < 0) {
                Serial.print("Failed to init IMU: "); Serial.println(errcode);
            }
            if (imu->getCalibrationValid())
                Serial.println("Using compass calibration");
            else
                Serial.println("No valid compass calibration data");
        #endif

        // use of sensors in the fusion algorithm can be controlled here
        // change any of these to false to disable that sensor

        fusion.setGyroEnable(false);
        fusion.setAccelEnable(true);
        fusion.setCompassEnable(true);
    }
    int refresh(){
        if (imu->IMURead()) { // get the latest data if ready yet
            fusion.newIMUData(imu->getGyro(), imu->getAccel(), imu->getCompass(), imu->getTimestamp());
        }
    }
    
    String getString() {
      RTVector3 vec = fusion.getFusionPose();
      char x = 'x', y = x+1, z = x+2;
      x -= ID*3;
      y -= ID*3;
      z -= ID*3;
      //x, y, z: roll, pitch, yaw
      String line;
      line += "<";
      line += x;
      line += ":" ;
      line += (vec.x() * RTMATH_RAD_TO_DEGREE);
      line += ">";
      line += "<";
      line += y;
      line += ":" ;
      line += (vec.y() * RTMATH_RAD_TO_DEGREE);
      line += ">";
      line += "<";
      line += z;
      line += ":" ;
      line += (vec.z() * RTMATH_RAD_TO_DEGREE);
      line += ">";
      return line;
    }
    
    void sendToSerial(){
      Serial.print(getString());
    }

};
