#### ESP8266

Configurar a board
1. Abra o **Arduino IDE**.
2. Vá em **Arquivo** > **Preferências**.
3. No campo **URLs Adicionais para Gerenciadores de Placas**, cole a URL a seguir. (Se já houver outras URLs, separe-as com uma vírgula).
4. https://arduino.esp8266.com/stable/package_esp8266com_index.json
5. Clique em **OK**.
6. Vá em **Ferramentas** > **Placa** > **Gerenciador de Placas**.
7. Procure por **esp8266** e clique em **Instalar** no pacote fornecido por _ESP8266 Community_.
 8. Em Tools  > Board. vamos selecionar o modelo NodeMCU 1.0 (ESP 2E Module) )
9. Depois vá em Tools > Ports e seleciona a porta usb

Para validar a comunicação envie o código com os métodos vazios.
#### Instalação das bibliotecas

Vamos instalar as seguintes bibliotecas, vá em Tools > Library Manager, digite como descrito e selecione o autor:

1. ArduinoJson (Benoit Blanchon) 
2. TinyGPSPlus (Mikal Hart)

#### Código

Cole o código a seguir no editor do Arduino IDE

```C++
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
            if (file.size() > 50000) { // Limite de 50KB para o arquivo
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
```

#### Módulo GPS 

O módulo que estou usando é o "Hiletgo Gy-neo6mv2 Neo-6m Módulo Controlador De Vuelo Gps" é um módulo que é utilizado em drones.

Link do mercado livre
https://www.mercadolivre.com.br/hiletgo-gy-neo6mv2-neo-6m-modulo-controlador-de-vuelo-gps-/p/MLB2089249896

Este módulo possui 4 pinos, e funciona com 3.3v, os pinos são VCC, RX, TX, GND. Preciamos conectar os pinos da seguinte forma:

| Módulo GPS | ESP8266 |
| ---------- | ------- |
| VCC        | 3.3v    |
| GND        | G       |
| RX         | D1      |
| TX         | D2      |
Depois conecte a antena do GPS ao conector do módulo de GPS

#### Sincronizaçào com o satélite

Em média para uma conexão inicial demora-se de 5 a 10 minutos para sincronizar com o satélite pela primeira vez, e nas próximas se torna quase instantaneo.

#### Fazendo o downlod dos dados

1. Conecte-se ao Wifi do ESP8266 que vai ser listado nas suas redes wifi, não precisa de senha
2. Para fazer o download dos dados acesse pelo navegador ou programaticamente via GET a url:
	http://192.168.4.1/download
3. O ESP8266 grava um arquivo na memmória flash que tem um limite de tamanho, se vc deixar ligado por muito. Com a configuração atual ele vai gravar mais de 18 horas para sobreescrever o arquivo de novo.

#### Limpando os dados da memória

Assim que vc baixar os dados via GET usando a url de download vc precisa limpar os dados do log chamando a url a seguir
	http://192.168.4.1/clear

#### Criar projeto no Visual Studio

  - Abra o visual studio
- Clique em Create a New Project
- Selecione nos templates C#, Windows, Desktop
- Escolha Windows Forms App
- Forneá um nome, um caminho e o nome da Solution
- No Framework selecione .NET 8
- Clique em Tools > Nuget Package Manage > Nuget Package Manager for solution
- Selecione a aba Browse
- Instale os seguintes pacotes HelixToolkit e HelixToolKit.Wpf

- Clique cm o botao direito na arvore do projeto e selecione Edit Project File

Altere seu arquivo para se parecer com o exemplo abaixo:

```C++
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
	 <UseWPF>true</UseWPF>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="HelixToolkit.Wpf" Version="3.1.2" />
    <PackageReference Include="MathNet.Numerics" Version="5.0.0" />
    <PackageReference Include="NAudio" Version="2.3.0" />
    <PackageReference Include="System.IO.Ports" Version="10.0.8" />
  </ItemGroup>

</Project>
```

Clique em Build

Crie um arquivo de classe chamado MyEngine3D.cs e cole o seguinte codigo:

```CSharp
using HelixToolkit.Wpf;
using MathNet.Numerics;
using Microsoft.VisualBasic.Logging;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace Helix3DSample
{
    public class MyEngine3D
    {
        private Form _form;
        private ElementHost _elementHost;
        private HelixViewport3D _viewport;
        private PerspectiveCamera? _cam;
        private TubeVisual3D _lineTrail3D;
        private Point3DCollection _lineTrailCollection;
        private List<GeospacialData> _gpsData;
        private List<GeospacialData> _drawData;
        private static readonly HttpClient client = new HttpClient();


        private string addressAnterior = "";

        private bool _isDrawing;
        private bool _isLiving;
        private const int MAX_PONTOS = 500;

        public MyEngine3D(Form form) {
            _viewport = new HelixViewport3D();
            _elementHost = new ElementHost { Dock = DockStyle.Fill };
            _form = form;
            _lineTrail3D = new TubeVisual3D();
            _lineTrailCollection = new Point3DCollection();
            _gpsData = new List<GeospacialData>();
            _drawData = new List<GeospacialData>();
        }

        public void StartViewPort()
        {
            try
            {
                _form.Controls.Add(_elementHost);
         
                _viewport.Background = System.Windows.Media.Brushes.Black;
                _cam = _viewport.Camera as PerspectiveCamera;
                _viewport.Children.Add(new DefaultLights());
                _viewport.Children.Add(new DefaultLights());
                _elementHost.Child = _viewport;

                _lineTrail3D = new TubeVisual3D
                {
                    ThetaDiv = 2,
                    AddCaps = true,
                    Diameter = 0.5, // Espessura da linha
                    Material = new EmissiveMaterial(System.Windows.Media.Brushes.Orange),
                  };

                var grid = new GridLinesVisual3D
                {
                    Width = 1000,
                    Length = 1000,
                    MinorDistance = 1,
                    MajorDistance = 5,
                    Fill = System.Windows.Media.Brushes.White,
                    Material = MaterialHelper.CreateMaterial(System.Windows.Media.Brushes.ForestGreen),
                    BackMaterial = MaterialHelper.CreateMaterial(System.Windows.Media.Brushes.ForestGreen),
                    Transform = new TranslateTransform3D(100, 0.01, 0)
                };

                if (_cam != null)
                {
                    _cam.Position = new Point3D(80, 80, 80);
                    _cam.LookDirection = new Vector3D(-80, -80, -40);
                    _cam.UpDirection = new Vector3D(0, 0, 1);
                    _cam.FieldOfView = 30;
                }
                else
                {
                    _viewport.Camera.Position = new Point3D(15, 30, 4);
                    _viewport.Camera.LookDirection = new Vector3D(-15, -50, 2);
                    _viewport.Camera.UpDirection = new Vector3D(0, 0, 1);
                }

                _viewport.Children.Add(_lineTrail3D);
                _viewport.Children.Add(new CoordinateSystemVisual3D());
                _viewport.Children.Add(grid);

            }
            catch (Exception ex)
            {


            }
        }

        public void SetMouseDrawing()
        {
            _viewport.MouseDown += (s, e) => { _isDrawing = true; };
            _viewport.MouseUp += (s, e) => { _isDrawing = false; };

            _viewport.MouseMove += async (s, e) =>
            {
                if (_isDrawing)
                {
                    Point3D ponto = GetPointOnPlan(e.GetPosition(_viewport), 0);

                    _lineTrailCollection.Add(ponto);

                    _form.Invoke(new Action(() =>
                    {
                        _lineTrail3D.Path = _lineTrailCollection;
                    }));
                }
            };
        }

        public async void SaveDraw(string filename)
        {
            try
            {
                if (!_lineTrailCollection.Any())
                    return;

                foreach (var item in _lineTrailCollection)
                    _gpsData.Add(new GeospacialData(new Point3D(item.X, item.Y, item.Z)));

                foreach(var item in _drawData)
                {
                    string json = JsonSerializer.Serialize(item) + Environment.NewLine;
                    await File.AppendAllTextAsync(filename, json);
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task Clear()
        {
            try
            {
                _lineTrailCollection.Clear();

                _form.Invoke(new Action(() =>
                {
                    _lineTrail3D.Path = _lineTrailCollection;
                }));
            }
            catch
            {
                throw;
            }
        }

        public async Task OpenDraw(string filename)
        {
            try
            {
                if (File.Exists(filename))
                {
                    using (StreamReader sr = File.OpenText(filename))
                    {
                        string line;

                        _gpsData.Clear();

                        while ((line = await sr.ReadLineAsync()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            var drawData = JsonSerializer.Deserialize<GeospacialData>(line);
                            if (drawData != null)
                            {
                                _drawData.Add(drawData);
                            }
                        }
                    }

                    _lineTrailCollection.Clear();

                    foreach (var item in _gpsData)
                    {
                        _lineTrailCollection.Add(new Point3D(item.X, item.Y, item.Z));
                    }
     
                    _form.Invoke(new Action(() =>
                    {
                        _lineTrail3D.Material = MaterialHelper.CreateMaterial(System.Windows.Media.Brushes.Orange);
                        _lineTrail3D.BackMaterial = MaterialHelper.CreateMaterial(System.Windows.Media.Brushes.Orange);
                        _lineTrail3D.Path = _lineTrailCollection;
                    }));
                }
            }
            catch(Exception ex)
            {

            }
        }

        public async Task SyncWithDevice()
        {
            await Task.Run(async () =>
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync("http://192.168.4.1/download");
                    response.EnsureSuccessStatusCode();

                    string fullContent = await response.Content.ReadAsStringAsync();
                    string[] lines = fullContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                    _gpsData = new List<GeospacialData>();

                    foreach (string line in lines)
                    {
                        try
                        {
                            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var data = JsonSerializer.Deserialize<GeospacialData>(line, options);
                            _gpsData.Add(data);
                        }
                        catch (JsonException)
                        {
                            continue;
                        }
                    }

                    var dataOrigem = _gpsData.FirstOrDefault();

                    foreach (var gpsData in _gpsData)
                    {
                        var point = GeoParaCartesiano(gpsData.Lat, gpsData.Lng, gpsData.Alt, dataOrigem.Lat, dataOrigem.Lng);
                        gpsData.X = point.X;
                        gpsData.Y = point.Y;
                        gpsData.Z = point.Z;
                    }

                    foreach (var item in _gpsData)
                    {
                        string json = JsonSerializer.Serialize(item) + Environment.NewLine;
                        await File.AppendAllTextAsync(GetCustomFilename("gpsdata_device"), json);
                    }

                    foreach (var gpsData in _gpsData)
                    {
                        _form.Invoke(new Action(() =>
                        {
                            _lineTrail3D.Material = MaterialHelper.CreateMaterial(System.Windows.Media.Brushes.Orange);
                            _lineTrail3D.BackMaterial = MaterialHelper.CreateMaterial(System.Windows.Media.Brushes.Orange);
                            _lineTrailCollection.Add(new Point3D(gpsData.X, gpsData.Y, gpsData.Z));
                            _lineTrail3D.Path = _lineTrailCollection;
                        }));
                    }



                    HttpResponseMessage responseClear = await client.GetAsync("http://192.168.4.1/clear");
                    response.EnsureSuccessStatusCode();

                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Erro na conexão: {ex.Message}");
                }

                await Task.Delay(2000);
            });
        }

        public async Task OpenGpsLog(string filename)
        {
            try
            {
                if (File.Exists(filename))
                {
                    using (StreamReader sr = File.OpenText(filename))
                    {
                        string line;

                        _gpsData.Clear();

                        while ((line = await sr.ReadLineAsync()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            var gpsData = JsonSerializer.Deserialize<GeospacialData>(line);
                            if (gpsData != null)
                            {
                                _gpsData.Add(gpsData);
                            }
                        }
                    }

                    _lineTrailCollection.Clear();

                    var dataOrigem = _gpsData.FirstOrDefault();

                    foreach (var gpsData in _gpsData)
                    {
                        var point = GeoParaCartesiano(gpsData.Lat, gpsData.Lng, gpsData.Alt, dataOrigem.Lat, dataOrigem.Lng);
                        gpsData.X = point.X;
                        gpsData.Y = point.Y;
                        gpsData.Z = point.Z;
                    }

                    foreach (var item in _gpsData)
                        _lineTrailCollection.Add(new Point3D(item.X, item.Y, item.Z));

                    _form.Invoke(new Action(() =>
                    {
                        _lineTrail3D.Material = MaterialHelper.CreateMaterial(System.Windows.Media.Brushes.Orange);
                        _lineTrail3D.BackMaterial = MaterialHelper.CreateMaterial(System.Windows.Media.Brushes.Orange);
                        _lineTrail3D.Path = _lineTrailCollection;
                    }));
                }
            }
            catch (Exception ex)
            {

            }
        }

        public async Task ApplyLabels()
        {
            await Task.Run(async () =>
            {
                try
                {
                    foreach (var gpsData in _gpsData)
                    {
                        _form.Invoke(new Action(async () =>
                        {
                            var pos = new Point3D(gpsData.X, gpsData.Y, gpsData.Z);

                            // 1. Adiciona o ponto à linha normalmente
                            _lineTrailCollection.Add(pos);
                            _lineTrail3D.Path = _lineTrailCollection;

                            // 2. Define quanto você quer que o texto suba (ex: 5 unidades)
                            double alturaOffset = 2.0;
                            var posicaoTexto = new Point3D(gpsData.X, gpsData.Y, gpsData.Z + alturaOffset);

                            // Se o seu gráfico usar o eixo Y para cima, use:
                            // var posicaoTexto = new Point3D(gpsData.X, gpsData.Y + alturaOffset, gpsData.Z);

                            // 3. Cria o texto na nova posição ajustada
                            var address = await GetAddressFromCoords(gpsData.Lat, gpsData.Lng);

                            var text = new BillboardTextVisual3D
                            {
                                Text = $"Alt: {gpsData.Alt:F4}" + Environment.NewLine + address,
                                Position = posicaoTexto, // Usa a posição com o offset
                                Foreground = System.Windows.Media.Brushes.White,
                                FontSize = 9,
                                HorizontalAlignment = System.Windows.HorizontalAlignment.Center, // Centraliza horizontalmente
                                VerticalAlignment = System.Windows.VerticalAlignment.Bottom     // Alinha a base do texto
                            };
                            _viewport.Children.Add(text);
  
                            addressAnterior = address;
                        }));

                        await Task.Delay(10);
                    }

                }
                catch (Exception ex)
                {

                }
            });
        }

        public async Task Live()
        {
            if (_isLiving = false)
                _isLiving = true;
            else
                _isLiving = true;

            await Task.Run(async () =>
            {
                while (_isLiving)
                {
                    await SyncWithDevice();
                    await Task.Delay(2000);
                }
            });
        }

     

        public async Task<string> GetAddressFromCoords(double lat, double lon)
        {
            using (HttpClient client = new HttpClient())
            {
                // O Nominatim exige um User-Agent para identificar sua aplicação
                client.DefaultRequestHeaders.Add("User-Agent", "MinhaAplicacaoCsharp/1.0");

                string url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={lat}&lon={lon}";

                try
                {
                    var response = await client.GetStringAsync(url);
                    using (JsonDocument doc = JsonDocument.Parse(response))
                    {
                        var root = doc.RootElement;
                        if (root.TryGetProperty("display_name", out var displayName))
                        {
                            return displayName.GetString();
                        }
                    }
                }
                catch (Exception ex)
                {
                    return "Erro ao buscar endereço: " + ex.Message;
                }
            }
            return "Endereço não encontrado";
        }

        private Point3D GetPointOnPlan(System.Windows.Point mousePos, double zFixado)
        {
            Point3D camPos = _viewport.Camera.Position;
            Vector3D lookDir = _viewport.Camera.LookDirection;
            lookDir.Normalize();

            Vector3D up = _viewport.Camera.UpDirection;
            up.Normalize();
            Vector3D right = Vector3D.CrossProduct(lookDir, up);
            right.Normalize();

            double fov = 45;
            double aspect = _viewport.ActualWidth / _viewport.ActualHeight;
            double tanFov = Math.Tan(fov * Math.PI / 360.0);

            double screenX = (2.0 * mousePos.X / _viewport.ActualWidth - 1.0) * aspect * tanFov;
            double screenY = (1.0 - 2.0 * mousePos.Y / _viewport.ActualHeight) * tanFov;

            Vector3D dir = lookDir + (right * screenX) + (up * screenY);
            dir.Normalize();

            double t = (zFixado - camPos.Z) / dir.Z;
            return new Point3D(camPos.X + dir.X * t, camPos.Y + dir.Y * t, zFixado);
        }

        public void ExportToImage(string caminhoArquivo)
        {
            if (_viewport == null)
                return;

            int width = (int)_viewport.ActualWidth;
            int height = (int)_viewport.ActualHeight;

            RenderTargetBitmap rtb = new RenderTargetBitmap(
                width,
                height,
                0, 0, 
                PixelFormats.Pbgra32);

            rtb.Render(_viewport);

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using (FileStream stream = new FileStream(caminhoArquivo, FileMode.Create))
            {
                encoder.Save(stream);
            }
        }

        public Point3D GeoParaCartesiano(double lat, double lon, double altitude, double latOrigem, double lonOrigem)
        {
            double R = 6378137; 

            double dLat = (lat - latOrigem) * Math.PI / 180.0;
            double dLon = (lon - lonOrigem) * Math.PI / 180.0;

            double x = R * dLon * Math.Cos(latOrigem * Math.PI / 180.0);
            double y = R * dLat;

            double escala = 10.0;

            return new Point3D(x / escala, y / escala, altitude / escala);
        }

        public string GetCustomFilename(string prefixo = "backup", string extensao = ".jsonl")
        {
            string dataFormatada = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

            if (!extensao.StartsWith("."))
                extensao = "." + extensao;

            return $"{prefixo}_{dataFormatada}{extensao}";
        }
    }

    public class GeospacialData
    {
        public GeospacialData() {
            Lat = 0;
            Lng = 0;
            Alt = 0;
            Spd = 0;
            Hdop = 0;
            Dir = 0;
            Sat = 0;
        }

        public GeospacialData(Point3D point)
        {
            Lat = 0;
            Lng = 0;
            Alt = 0;
            Spd = 0;
            Hdop = 0;
            Dir = 0;
            Sat = 0;
            X = point.X; 
            Y = point.Y; 
            Z = point.Z;
        }

        public GeospacialData(double lat, double lng, double alt, double spd, double hdop, double dir, int sat, double x, double y, double z )
        {
            Lat = lat;
            Lng = lng;
            Alt = alt;
            Spd = spd;
            Hdop = hdop;
            Dir = dir;
            Sat = sat;
            X = x;
            Y = y;
            Z = z;
        }

        public double Lat { get; set; }
        public double Lng { get; set; }
        public double Alt { get; set; }
        public double Spd { get; set; }
        public double Hdop { get; set; }
        public double Dir { get; set; }
        public int Sat { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public string Time { get; set; } 
    }
}

```

Adicone no construtor a instanciação conforma o codigo abaixo:

```C++
using Helix3DSample;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        MyEngine3D myEngine3D;

        public Form1()
        {
            InitializeComponent();
            myEngine3D = new MyEngine3D(this);
            myEngine3D.StartViewPort();
            myEngine3D.SetMouseDrawing();
        }
    }
}
```

Adicione 8 botões para as funcionalidades que vamos precisar, e no clique de cada um coloque use o código abaixo como referencia:

```CSharp
namespace Helix3DSample
{
    public partial class Form1 : Form
    {
        MyEngine3D myEngine3D;

        public Form1()
        {
            InitializeComponent();
            myEngine3D = new MyEngine3D(this);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            myEngine3D.StartViewPort();
            myEngine3D.SetMouseDrawing();
        }

        private void btnSaveToImage_Click(object sender, EventArgs e)
        {
            myEngine3D.ExportToImage("D:\\teste.png");
        }

        private async void btnSincronizar_Click(object sender, EventArgs e)
        {
            await myEngine3D.SyncWithDevice();
        }

        private async void btnSaveDraw_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog salvarDialog = new SaveFileDialog())
            {
                salvarDialog.Filter = "Arquivos JSONL (*.jsonl)|*.jsonl|Todos os arquivos (*.*)|*.*";
                salvarDialog.Title = "Salvar arquivo como";
                salvarDialog.RestoreDirectory = true;
                salvarDialog.FileName = myEngine3D.GetCustomFilename("gpsdata_draw");

                if (salvarDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        myEngine3D.SaveDraw(salvarDialog.FileName);
                        MessageBox.Show("Arquivo salvo com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Erro ao salvar arquivo: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void btnDrawClear_Click(object sender, EventArgs e)
        {
            await myEngine3D.Clear();
        }

        private async void btnOpenDraw_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog abrirDialog = new OpenFileDialog())
            {
                abrirDialog.Filter = "Arquivos JSONL (*.jsonl)|*.jsonl|Todos os arquivos (*.*)|*.*";
                abrirDialog.Title = "Selecione o arquivo JSONL";
                abrirDialog.RestoreDirectory = true;

                if (abrirDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        await myEngine3D.OpenDraw(abrirDialog.FileName);

                        MessageBox.Show("Arquivo aberto com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Erro ao abrir arquivo: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void btnOpenLog_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog abrirDialog = new OpenFileDialog())
            {
                abrirDialog.Filter = "Arquivos JSONL (*.jsonl)|*.jsonl|Todos os arquivos (*.*)|*.*";
                abrirDialog.Title = "Selecione o arquivo JSONL";
                abrirDialog.RestoreDirectory = true;

                if (abrirDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        await myEngine3D.OpenGpsLog(abrirDialog.FileName);

                        MessageBox.Show("Arquivo aberto com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Erro ao abrir arquivo: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void btnTelemetry_Click(object sender, EventArgs e)
        {
            await myEngine3D.ApplyLabels();
        }

        private async void btnLive_Click(object sender, EventArgs e)
        {
            await myEngine3D.Live();
        }
    }
}
```

