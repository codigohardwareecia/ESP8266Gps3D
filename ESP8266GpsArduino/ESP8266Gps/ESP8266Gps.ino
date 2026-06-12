#include <ESP8266WiFi.h>
#include <ESP8266WebServer.h>
#include <FS.h>
#include <TinyGPSPlus.h>
#include <ArduinoJson.h>
#include <SoftwareSerial.h>

// --- CONFIGURAÇÃO DOS PINOS DO GPS (ESP8266) ---
// D1 (GPIO5) conectado no TX do GPS
// D2 (GPIO4) conectado no RX do GPS (opcional se só for ler)
#define GPS_RX D1
#define GPS_TX D2

// Limite do tamanho de arquivo
const long LIMITE_ARQUIVO = 1500000;

const char* ssid = "Nautilus_GPS_WiFi";
IPAddress local_IP(192, 168, 4, 1);
IPAddress gateway(192, 168, 4, 1);
IPAddress subnet(255, 255, 255, 0);

ESP8266WebServer server(80);
TinyGPSPlus gps;
SoftwareSerial ss(GPS_RX, GPS_TX); // Emulação de Serial para o GPS

JsonDocument doc; // Sintaxe atualizada para ArduinoJson v6/v7
volatile bool isDownloading = false;

void handleDownload() {
  isDownloading = true;
  File file = SPIFFS.open("/gps_log.txt", "r");
  if (!file) {
    server.send(404, "text/plain", "Arquivo vazio ou inexistente");
  } else {
    server.streamFile(file, "text/plain");
    file.close();
  }
  isDownloading = false;
}

void handleClear() {
  SPIFFS.remove("/gps_log.txt");
  server.send(200, "text/plain", "Historico limpo!");
}

void setup() {
  Serial.begin(115200);
  ss.begin(9600); // Inicializa a comunicação com o GPS

  Serial.println("\nInicializando Sistema...");

  // Inicializa o Sistema de Arquivos (SPIFFS) no ESP8266
  if (!SPIFFS.begin()) {
    Serial.println("Erro ao montar SPIFFS! Formatando...");
    SPIFFS.format();
    SPIFFS.begin();
  }

  // Configura o Ponto de Acesso WiFi (Access Point)
  WiFi.softAPConfig(local_IP, gateway, subnet);
  WiFi.softAP(ssid);
  Serial.print("Access Point online. IP: ");
  Serial.println(WiFi.softAPIP());
  
  // Rotas do Servidor Web
  server.on("/download", handleDownload);
  server.on("/clear", handleClear);
  server.begin();
  Serial.println("Servidor Web iniciado.");
}

void loop() {
  // O processamento do servidor web roda no loop principal no ESP8266
  server.handleClient();

  // Tratamento GPS: Monitora os dados vindos do módulo
  static unsigned long lastGpsData = millis();
  while (ss.available() > 0) {
    gps.encode(ss.read());
    lastGpsData = millis();
  }

  // Coleta e gravação de dados a cada 5 segundos
  static unsigned long lastTime = 0;
  if (millis() - lastTime > 5000) {
    
    if (gps.location.isValid()) {
      doc["lat"] = gps.location.lat();
      doc["lng"] = gps.location.lng();
      doc["sat"] = gps.satellites.value();
      doc["alt"] = gps.altitude.meters();
      doc["spd"] = gps.speed.kmph();
      doc["hdop"] = gps.hdop.hdop();
      doc["time"] = String(gps.time.hour()) + ":" + String(gps.time.minute());
      
      String payload;
      serializeJson(doc, payload);

      // Envia também para o Monitor Serial para você acompanhar sem o LCD
      Serial.print("Dados capturados: ");
      Serial.println(payload);

      if (!isDownloading) {
        // Verifica o espaço disponível na memória Flash do ESP8266
        FSInfo fs_info;
        SPIFFS.info(fs_info);
        size_t freeSpace = fs_info.totalBytes - fs_info.usedBytes;

        if (freeSpace > 2000) { 
          File file = SPIFFS.open("/gps_log.txt", "a");
          if (file) {
            if (file.size() > LIMITE_ARQUIVO) {// Limite de 50KB para o arquivo
              file.close();
              SPIFFS.remove("/gps_log.txt");
              Serial.println("Arquivo de log cheio, limpando...");
            } else {
              file.println(payload);
              file.close();
              Serial.println("Dados salvos no SPIFFS.");
            }
          }
        }
      }
    } else {
      Serial.print("Buscando satélites... Atuais: ");
      Serial.println(gps.satellites.value());
      if (millis() - lastGpsData > 10000) {
        Serial.println("AVISO: Sem comunicação física com o GPS (Verifique a fiação)!");
      }
    }
    lastTime = millis();
  }
}