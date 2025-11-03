using System;
using System.Windows;
using AthStitcher.Data;

namespace AthStitcher.Views
{
    public partial class SchedulingDialog : Window
    {
        public Scheduling Model { get;  set; }

        internal SchedulingDialog(Scheduling? existing)
        {
            InitializeComponent();
            Model = existing ?? new Scheduling();
            this.DataContext = Model;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            // Initialize fields
            //MeetCutoffBox.Text = Model.MeetCutoff.ToString();
            //EventCutoffBox.Text = Model.EventCutoff.ToString();
            CanAddHeatsOnDay.IsChecked = Model.CanAddHeatsOnDayOfMeet;
            //MeetCutoffBox.SelectAll();
            MeetCutoffBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {

            if (Model.MeetCutoff<Model.EventCutoff)
            {
                MessageBox.Show("Event Cut Off days must be =< Meet Cut Off", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Model.CanAddHeatsOnDayOfMeet = CanAddHeatsOnDay.IsChecked == true;
            DialogResult = true;
        }
    }
}
