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
