using System;
using System.Globalization;
using System.Windows;

namespace AthStitcherGUI
{
    public partial class GunTimeInputDialog : Window
    {
        public double GunTimeSeconds { get; private set; }

        public GunTimeInputDialog(double initialValue)
        {
            InitializeComponent();
            GunTimeTextBox.Text = initialValue.ToString("F3", CultureInfo.InvariantCulture);
            GunTimeTextBox.Focus();
            GunTimeTextBox.SelectAll();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(GunTimeTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
            {
                GunTimeSeconds = val;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please enter a valid number (seconds.milliseconds)", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}