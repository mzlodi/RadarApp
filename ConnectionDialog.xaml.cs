using System.Windows;

namespace RadarApp
{
    /// <summary>
    /// Interaction logic for ConnectionDialog.xaml
    /// </summary>
    public partial class ConnectionDialog : Window
    {
        public string IpAddress { get; private set; }

        public ConnectionDialog()
        {
            InitializeComponent();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                ConnectButton_Click(sender, e);
            }
        }

        public void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string input = IpAddressTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(input)) {
                MessageBox.Show("IP address cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if(!ValidateIPv4(input))
            {
                MessageBox.Show("Invalid IP address format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            IpAddress = input;
            DialogResult = true;
            Close();
        }

        public bool ValidateIPv4(string ipString)
        {
            if (string.IsNullOrWhiteSpace(ipString))
            {
                return false;
            }

            string[] splitValues = ipString.Split('.');
            if (splitValues.Length != 4)
            {
                return false;
            }

            byte tempForParsing;

            return splitValues.All(r => byte.TryParse(r, out tempForParsing));
        }
    }
}
