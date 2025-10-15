using System;
using System.Windows;

namespace AthStitcher.Views
{
    public partial class NumberOfHeatsDialog : Window
    {
        public int HeatsCount { get; private set; } = 1;
        public int InitialHeats { get; set; } = 1;

        public NumberOfHeatsDialog()
        {
            InitializeComponent();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            if (InitialHeats < 1) InitialHeats = 1;
            HeatsBox.Text = InitialHeats.ToString();
            HeatsBox.SelectAll();
            HeatsBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var text = (HeatsBox.Text ?? string.Empty).Trim();
            if (!int.TryParse(text, out var n) || n < 1)
            {
                MessageBox.Show("Please enter a whole number >= 1.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            HeatsCount = n;
            DialogResult = true;
        }
    }
}
