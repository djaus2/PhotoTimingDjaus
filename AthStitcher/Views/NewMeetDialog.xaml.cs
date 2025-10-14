using System;
using System.Windows;
using AthStitcher.Data;

namespace AthStitcher.Views
{
    public partial class NewMeetDialog : Window
    {
        public string? DescriptionValue { get; private set; }
        public DateTime? DateValue { get; private set; }
        public string? LocationValue { get; private set; }

        public NewMeetDialog()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var desc = DescriptionBox.Text?.Trim();
            if (string.IsNullOrEmpty(desc))
            {
                MessageBox.Show("Description is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DescriptionValue = desc;
            DateValue = DatePicker.SelectedDate;
            LocationValue = string.IsNullOrWhiteSpace(LocationBox.Text) ? null : LocationBox.Text.Trim();
            DialogResult = true;
        }
    }
}
