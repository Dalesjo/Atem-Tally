#include <Arduino.h>
#include <ESP8266WiFi.h>
#include <WebSocketsClient.h>
#include <ArduinoJson.h>


WebSocketsClient webSocket;
#define WEBSOCKETS_MAX_DATA_SIZE (8*1024)

/* WIFI */
#ifndef STASSID
#define STASSID     "LAB"
#define STAPSK      "TEST8806"

#define SERVER      "192.168.50.52"
#define PORT        5000
#define HUB         "/tally"
#define MESSAGE     "ReceiveTally"

#define TALLY       "Kamera3"
#define ATEM2        true
#endif

const char* ssid     = STASSID;
const char* password = STAPSK;

const char* server = SERVER;
const uint16_t port = PORT;
const char* hub = HUB;
const char* message = MESSAGE;

const char* tally = TALLY;

bool blink = false;
byte endcharachter[34] = { 0x7b , 0x22 , 0x70 , 0x72 , 0x6f , 0x74 , 0x6f , 0x63 , 0x6f , 0x6c , 0x22 , 0x3a , 0x20 , 0x22 , 0x6a , 0x73 , 0x6f , 0x6e , 0x22 , 0x2c , 0x22 , 0x76 , 0x65 , 0x72 , 0x73 , 0x69 , 0x6f , 0x6e , 0x22 , 0x3a , 0x20 , 0x31 , 0x7d, 0x1e };


StaticJsonDocument<8000> doc;
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
  webSocket.begin(server, port, hub);
  webSocket.onEvent(webSocketEvent);
  webSocket.setReconnectInterval(5000);
  webSocket.enableHeartbeat(15000, 3000, 2);
}

void sendMessage(String & msg) {
  webSocket.sendTXT(msg.c_str(), msg.length() + 1);
}


bool checkPreview(const JsonDocument& doc) {
    const char* preview;

    preview = doc["arguments"][0]["me1"]["g"];
    if(strcmp(preview,tally) == 0) {
      Serial.println("checkProgram: preview is on (ME1)");  
      digitalWrite(2, LOW);
      return true;
    }

    preview = doc["arguments"][0]["me2"]["g"];
    if(strcmp(preview,tally) == 0) {
      Serial.println("checkProgram: preview is on (ME2)");  
      digitalWrite(2, LOW);
      return true;
    }

    return false;
}

bool checkProgram(const JsonDocument& doc) {
    
    const char* program;

    program = doc["arguments"][0]["me1"]["r"];
    if(strcmp(program,tally) == 0) {
      Serial.println("checkProgram: program is on (ME1)");  
      digitalWrite(0, LOW);
      return true;
    }

    program = doc["arguments"][0]["me2"]["r"];
    if(strcmp(program,tally) == 0) {
      Serial.println("checkProgram: program is on (ME2)");  
      digitalWrite(0, LOW);
      return true;
    }

    return false;
}

void deserializeMessage(uint8_t * payload) {
  const char* target;

  
  Serial.println("deserializeMessage: Text deserialized");
  err = deserializeJson(doc, (char *) payload);
  if (err) {
    Serial.println("deserializeMessage: deserialized error, quiting.");
    return;
  } else {
    Serial.println("deserializeMessage: deserialized ok");
  }

  target = doc["target"];
  
  if (target) {
    Serial.println("deserializeMessage: target exists");
    if(strcmp(target,message) == 0) {
      Serial.println("deserializeMessage: target is correct");
      const char* program;
      bool isProgram = checkProgram(doc);
      bool isPreview = checkPreview(doc);

      if(isProgram && isPreview) {
        Serial.println("deserializeMessage: Both program or preview is on, quiting"); 
        return;
      }
     
      JsonArray inputs = doc["arguments"][0]["inputs"].as<JsonArray>();

      for(JsonVariant v : inputs) {
        program = v["n"].as<char*>();
        if(strcmp(program,tally) == 0) {
          Serial.println("Input: Found input matching Tally"); 
      
          if(v["r"].as<bool>()) {
              Serial.println("Input: Program is on");  
              isProgram = true;
              digitalWrite(0, LOW);
          }

          if(v["g"].as<bool>()) {
              Serial.println("Input: Preview is on");  
              isPreview = true;
              digitalWrite(2, LOW); 
          }

          break;
        }
      }
        
      if(!isProgram) {
        Serial.println("deserializeMessage: Tally never turned on. Turning off Program.");  
        digitalWrite(0, HIGH);
      }

      if(!isPreview) {
        Serial.println("deserializeMessage: Tally never turned on. Turning off Preview.");  
        digitalWrite(2, HIGH);
      }

    } else {
      Serial.println("deserializeMessage: target is incorrect, quiting");
      return;
    }
  } else {
    Serial.println("deserializeMessage: target is missing, quiting");
    return;
  }
}

void webSocketEvent(WStype_t type, uint8_t * payload, size_t length) {
 
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
      deserializeMessage(payload);
      Serial.println("webSocketEvent: Text recieved. done!");
      break;
    case WStype_BIN:
      Serial.println("webSocketEvent: binary recieved");
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
  reconnectWifi();
  webSocket.loop();
}
