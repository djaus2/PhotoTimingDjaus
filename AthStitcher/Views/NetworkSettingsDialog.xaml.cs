using System;
using System.Windows;
using AthStitcherGUI.ViewModels;

namespace AthStitcher.Views
{
    public partial class NetworkSettingsDialog : Window
    {
        private readonly NetworkSettings _original;
        private readonly NetworkSettings _working;

        public NetworkSettingsDialog(NetworkSettings current)
        {
            InitializeComponent();

            // Defensive: ensure we always have an instance
            _original = current ?? new NetworkSettings();
            _working = new NetworkSettings
            {
                TargetHostOrIp = _original.TargetHostOrIp,
                TargetPort = _original.TargetPort,
                ConnectTimeoutMs = _original.ConnectTimeoutMs
            };

            DataContext = _working;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(_working.TargetHostOrIp))
            {
                MessageBox.Show("Please enter a Hostname or IP address.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_working.TargetPort <= 0 || _working.TargetPort > 65535)
            {
                MessageBox.Show("Port must be a number between 1 and 65535.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_working.ConnectTimeoutMs < 100)
            {
                var res = MessageBox.Show("Connect timeout is very small. Continue?", "Validation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res != MessageBoxResult.Yes)
                    return;
            }

            // Copy validated values back to original instance
            _original.TargetHostOrIp = _working.TargetHostOrIp;
            _original.TargetPort = _working.TargetPort;
            _original.ConnectTimeoutMs = _working.ConnectTimeoutMs;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}