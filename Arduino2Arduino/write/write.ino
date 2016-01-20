#include <SPI.h>
 
void setup (void)
{
  Serial.begin(9600);        // 开始串口通讯
  digitalWrite(SS, HIGH);
  SPI.begin ();              // PI通讯开始
  //SPI.setClockDivider(SPI_CLOCK_DIV8); //？？
}
 
void loop (void)
{
  char c;
 
  // 片选为从机
  digitalWrite(SS, LOW);    // SS - pin 10
 
  // 发送字串
  for (const char * p = "Hello, world!\n" ; c = *p; p++) {
    SPI.transfer (c);
    Serial.print(c);
  }
 
  // 取消从机
  digitalWrite(SS, HIGH);
  delay (1000);
}

