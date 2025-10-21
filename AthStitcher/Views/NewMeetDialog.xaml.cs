using AthStitcher.Data;
using System;
using System.Windows;
using System.Windows.Controls;

namespace AthStitcher.Views
{
    public partial class NewMeetDialog : Window
    {
        private Meet? _meet;
        public Meet? Meet 
        {
            get{ return _meet; }
            set{ _meet = value; this.DataContext = Meet; }

        }
        public string? DescriptionValue { get;  set; }
        public DateTime? DateValue { get;  set; }
        public string? LocationValue { get;  set; }

        public DateTime CutOff { get; set; } = DateTime.Today;

        public NewMeetDialog()
        {
            InitializeComponent();           
            this.Loaded += (_, __) =>
            {
                MeetDatePicker.DisplayDateStart = CutOff;
            };
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var desc = Meet.Description.Trim();
            if (string.IsNullOrEmpty(desc))
            {
                MessageBox.Show("Description is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            //DescriptionValue = desc;
            //DateValue = MeetDatePicker.SelectedDate;
            //LocationValue = string.IsNullOrWhiteSpace(LocationBox.Text) ? null : LocationBox.Text.Trim();
            DialogResult = true;
        }
    }
}
