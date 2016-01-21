#include <SPI.h>

//Initialize SPI slave.
void SlaveInit(void) {
  // Initialize SPI pins.
  pinMode(SCK, INPUT);
  pinMode(MOSI, INPUT);
  pinMode(MISO, OUTPUT);
  pinMode(SS, INPUT);
  // Enable SPI as slave.
  SPCR = (1 << SPE);
}

// The setup() function runs after reset.
void setup() {
  // Initialize serial for troubleshooting.
  Serial.begin(9600);
  // Initialize SPI Slave.
  SlaveInit();
}

// The loop function runs continuously after setup().
void loop() {
  // Slave Enabled?
  if (!digitalRead(SS)) {
    SPI.transfer('c');
  }
  delay(100);
}
