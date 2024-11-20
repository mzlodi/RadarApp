using System.Windows;
using TargetInfo = RadarLibrary.RadarController.TargetInfo;
using RadarLibrary;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Net;
using System.Windows.Threading;

namespace RadarApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private RadarController radarController;
        private DispatcherTimer updateTimer;
        private List<TargetInfo> lastTargets = new();
        private string radarIpAddress;
        private bool messageBoxDisplayed = false;

        public MainWindow()
        {
            InitializeComponent();

            if (!ShowConnectionDialog())
            {
                Application.Current.Shutdown();
                return;
            }

            radarController = new RadarController();
            _ = ConnectToRadar();
        }

        private bool ShowConnectionDialog()
        {
            var connectionDialog = new ConnectionDialog();
            bool? result = connectionDialog.ShowDialog();

            if (result == true && !string.IsNullOrWhiteSpace(connectionDialog.IpAddress))
            {
                radarIpAddress = connectionDialog.IpAddress;
                return true;
            }

            return false;
        }

        private async Task ConnectToRadar()
        {
            try
            {
                uint ipAddress = BitConverter.ToUInt32(IPAddress.Parse(radarIpAddress).GetAddressBytes());
                bool isConnected = await radarController.ConnectAsync(ipAddress);

                if (isConnected) {
                    MessageBox.Show("Radar connected!");
                    StartUpdateTimer();
                    UpdateRadarStatus("Online");
                }
                else
                {
                    HandleDisconnection("Failed to connect to radar.");
                }

            }
            catch (Exception ex)
            {
                HandleDisconnection($"Error connecting to radar: {ex.Message}");
            }
        }

        private void StartUpdateTimer()
        {
            updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            updateTimer.Tick += async (s, e) => await UpdateRadarAsync();
            updateTimer.Start();
        }

        private void StopUpdateTimer()
        {
            updateTimer?.Stop();
        }

        private async Task UpdateRadarAsync()
        {
            try
            {
                if (!await radarController.GetRadarStatus())
                {
                    HandleDisconnection("Radar is offline.");
                    return;
                }

                var targets = await radarController.GetDetectedTargetsAsync();
                lastTargets = targets;

                Dispatcher.Invoke(() =>
                {
                    UpdateRadarStatus("Online");
                    LastTargetsText.Visibility = Visibility.Hidden;
                    ReconnectButton.Visibility = Visibility.Hidden;
                    DrawRadarTargets(targets);
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => HandleDisconnection($"Error updating radar: {ex.Message}"));
            }
        }

        private void HandleDisconnection(string message)
        {
            StopUpdateTimer();

            Dispatcher.Invoke(() =>
            {
                if (messageBoxDisplayed) return;

                messageBoxDisplayed = true;

                try
                {
                    UpdateRadarStatus("Offline");
                    ClearRadarCanvas();
                    ShowLastTargets();
                    ReconnectButton.Visibility = Visibility.Visible;
                    MessageBox.Show(message);
                }
                finally
                {
                    messageBoxDisplayed = false;
                }
            });
        }

        private void UpdateRadarStatus(string status)
        {
            StatusText.Text = $"Status: {status}";
        }

        private void ShowLastTargets()
        {
            LastTargetsText.FontSize = 14;
            LastTargetsText.Visibility = Visibility.Visible;
            LastTargetsText.Text = $"Last Targets:\n{string.Join(Environment.NewLine, lastTargets.Select(t => $"#{t.Id}: {{ {t.Distance:F2}, {t.Angle:F2} }}"))}";
        }

        private void DrawRadarTargets(List<TargetInfo> targets)
        {
            ClearRadarCanvas();

            const double centerX = 450, centerY = 450;
            const double radarMaxRadius = 450, radarMaxDistance = 100.0;
            double scale = radarMaxRadius / radarMaxDistance;

            foreach (var target in targets) {
                if (target.Distance < 0 || target.Distance > radarMaxDistance || target.Angle < -45 || target.Angle > 45)
                {
                    continue;
                }

                double targetX = centerX + target.Distance * scale * Math.Sin(target.Angle * Math.PI / 180);
                double targetY = centerY - target.Distance * scale * Math.Cos(target.Angle * Math.PI / 180);

                Polygon triangle = CreateTriangle(targetX, targetY, 10, target.Angle);
                triangle.Fill = new SolidColorBrush(Colors.Transparent);
                triangle.Stroke = new SolidColorBrush(Colors.Red);
                triangle.StrokeThickness = 2;

                RadarCanvas.Children.Add(triangle);
            }
        }

        private void ClearRadarCanvas()
        {
            var targetElements = RadarCanvas.Children.OfType<Polygon>().ToList();

            foreach (var element in targetElements)
            {
                RadarCanvas.Children.Remove(element);
            }
        }

        private Polygon CreateTriangle(double targetX, double targetY, int sizePixel, double targetAngle)
        {
            double height = (Math.Sqrt(3) * sizePixel) / 2;
            double halfBase = sizePixel / 2;

            Point tip = new(targetX, targetY - height);
            Point left = new(targetX - halfBase, targetY);
            Point right = new(targetX + halfBase, targetY);

            return new Polygon
            {
                Points = new PointCollection { tip, left, right },
                RenderTransform = new RotateTransform(targetAngle, targetX, targetY)
            };
        }

        private async void ReconnectButton_Click(object sender, RoutedEventArgs e)
        {
            ReconnectButton.Visibility = Visibility.Hidden;

            try
            {
                await ConnectToRadar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reconnecting: {ex.Message}");
            }
        }
    }
}