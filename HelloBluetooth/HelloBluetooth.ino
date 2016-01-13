int value = 0;

void setup()
{
  Serial.begin(9600);
  Serial.println(F("L298N Program start..."));
}

void loop()
{
  if (value <= 100) {
    value = value + 1;
  } else {
    value = 0;
  }
  Serial.println(value);
  delay(500);
}

