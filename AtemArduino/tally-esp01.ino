#include <Arduino.h>
#include <ESP8266WiFi.h>
#include <WebSocketsClient.h>
#include <ArduinoJson.h>

WebSocketsClient webSocket;

/* WIFI */
#ifndef STASSID
#define STASSID "LAB"
#define STAPSK  "TEST8806"
#define TALLY   "Kamera3"
#define HUB     "ReceiveTally"
#endif

const char* ssid     = STASSID;
const char* password = STAPSK;
const char* tally = TALLY;
const char* hub = HUB;

bool blink = false;
byte endcharachter[34] = { 0x7b , 0x22 , 0x70 , 0x72 , 0x6f , 0x74 , 0x6f , 0x63 , 0x6f , 0x6c , 0x22 , 0x3a , 0x20 , 0x22 , 0x6a , 0x73 , 0x6f , 0x6e , 0x22 , 0x2c , 0x22 , 0x76 , 0x65 , 0x72 , 0x73 , 0x69 , 0x6f , 0x6e , 0x22 , 0x3a , 0x20 , 0x31 , 0x7d, 0x1e };
StaticJsonDocument<163> doc;
DeserializationError err;

/**
   Wait for Wifi to connect
*/
void reconnectWifi() {
  if (WiFi.status() != WL_CONNECTED) {
    bool wifiBlink = true;
    Serial.println("reconnectWifi: Wifi not connected");
    
    while (WiFi.status() != WL_CONNECTED) {
      if (wifiBlink) {
        digitalWrite(0, HIGH);
        digitalWrite(2, LOW);
        wifiBlink = false;
      } else {
        digitalWrite(0, LOW);
        digitalWrite(2, HIGH);
        wifiBlink = true;
      }
      delay(250);
    }

    Serial.println("reconnectWifi: Wifi is reconnected");

    /* Turn off lamps */
    digitalWrite(0, HIGH);
    digitalWrite(2, HIGH);
  }
}

void connectWebsocket() {
  webSocket.begin("192.168.50.52", 5000, "/tally");
  webSocket.onEvent(webSocketEvent);
  webSocket.setReconnectInterval(5000);
  webSocket.enableHeartbeat(15000, 3000, 2);
}

void sendMessage(String & msg) {
  webSocket.sendTXT(msg.c_str(), msg.length() + 1);
}

void webSocketEvent(WStype_t type, uint8_t * payload, size_t length) {
  const char* target;
  const char* program;
  const char* preview;
  
  switch (type) {
    case WStype_DISCONNECTED:
      Serial.println("webSocketEvent: disconnected");
      digitalWrite(0, LOW);
      digitalWrite(2, LOW);
      break;
    case WStype_CONNECTED:
      Serial.println("webSocketEvent: connected");
      webSocket.sendBIN(endcharachter, sizeof(endcharachter));
      digitalWrite(0, HIGH);
      digitalWrite(2, HIGH);
      break;
    case WStype_TEXT:
      Serial.println("webSocketEvent: Text recieved");
      /* {"type":1,"target":"ReceiveTally","arguments":[{"program":"Kamera3","preview":"Kamera3"}]} */
      err = deserializeJson(doc, (char *) payload);
      Serial.println("webSocketEvent: Text deserialized");
      
      if (err) {
        Serial.println("webSocketEvent: deserialized error, quiting.");
        digitalWrite(2, LOW);
        return;
      } else {
        digitalWrite(2, HIGH);
        Serial.println("webSocketEvent: deserialized ok");
      }

        
      target = doc["target"];

      if (target) {
        Serial.print("webSocketEvent: target equals");
        Serial.println(target);

      } else {
        Serial.println("ERROR data did not exist");
        return;
      }
      
      Serial.print("webSocketEvent: target equals");
      Serial.println(target);
     
      if(strcmp(target,hub) == 0) {
        Serial.println("webSocketEvent: target is correct");

        program = doc["arguments"][0]["program"];
        Serial.print("webSocketEvent: program equals ");
        Serial.println(program);
        
        preview = doc["arguments"][0]["preview"];
        Serial.print("webSocketEvent: preview equals ");
        Serial.println(preview);

        if(strcmp(program,tally) == 0) {
          Serial.println("webSocketEvent: program is on");
          digitalWrite(0, LOW);
        } else {
          Serial.println("webSocketEvent: program is off");
          digitalWrite(0, HIGH);
        }
      } else {
        Serial.println("webSocketEvent: target is incorrect");
      }

      break;
    case WStype_BIN:
      Serial.println("webSocketEvent: binary recieved");
      if (blink) {
        digitalWrite(2, LOW);
      } else {
        digitalWrite(2, HIGH);
      }

      blink = !blink;
      break;
  }
}

void setup() {
  Serial.begin(115200);
  Serial.println("Setup: started");
  
  pinMode(0, OUTPUT);     // Initialize the LED_BUILTIN pin as an output
  pinMode(2, OUTPUT);     // Initialize the LED_BUILTIN pin as an output
  digitalWrite(0, LOW);   // Turn the LED on (Note that LOW is the voltage level
  digitalWrite(2, LOW);   // Turn the LED on (Note that LOW is the voltage level
  Serial.println("Setup: GPIO Configured");
  
  WiFi.mode(WIFI_STA);
  WiFi.begin(ssid, password);
  Serial.println("Setup: WIFI Configured");
  
  reconnectWifi();
  Serial.println("Setup: Wifi Connected");
  
  connectWebsocket();
  Serial.println("Setup: Websocket Configured");
}

// the loop function runs over and over again forever
void loop() {
  Serial.println("Loop: Started");
  reconnectWifi();
  webSocket.loop();
  
}
