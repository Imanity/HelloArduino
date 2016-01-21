#include <SPI.h>

#define mySS 8

// SPI Transfer.
byte SPItransfer(byte value) {
  SPDR = value;
  while(!(SPSR & (1<<SPIF)));
  delay(10);
  return SPDR;
}

// The setup() function runs after reset.
void setup() {
  // Initialize serial for DEBUG.
  Serial.begin(9600);
  // Initialize SPI.
  // SCK, MOSI, SS pins into output mode
  // also put SCK, MOSI into LOW state, and SS into HIGH state.
  // Then put SPI hardware into Master mode and turn SPI on
  SPI.begin();
  pinMode(MISO, INPUT);
  pinMode(mySS, OUTPUT);
}

// The loop() function runs continuously after setup().
void loop() {
  digitalWrite(mySS, LOW);
  byte str = SPItransfer(255);
  if (str != 255)
    Serial.println(str);
  // Disable slave.
  digitalWrite(mySS, HIGH);
}

