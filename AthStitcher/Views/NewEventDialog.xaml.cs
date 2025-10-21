using AthStitcher.Data;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace AthStitcher.Views
{
    public partial class NewEventDialog : Window
    {

        public Event _event {get; set;}
        public DateTime? BaseDate { get; set; } = DateTime.Today;

        int MinLaneValue { get; set; }
        int MaxLaneValue { get; set; }

        public NewEventDialog()
        {
            _event = new Event();
            _event.Time = BaseDate;
            InitializeComponent();
            this.DataContext = _event;
        }

        public void InitializeForEdit(Event existing)
        {
            _event = existing;
            _event.GetMastersAgeGenderGroup();
            this.DataContext = _event;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            _event.SetMastersAgeGenderGroup();
            //var desc = DescriptionBox.Text?.Trim();
            var desc = _event.Description.Trim(); //Description is now option. TrackType covers that.
            //if (string.IsNullOrEmpty(desc))
            //{
            //    MessageBox.Show("Description is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    return;
            //}

            int eventNo = _event.EventNumber ?? 0;

            if (eventNo < 1)
            {
                MessageBox.Show("Event No must be a positive integer.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int distanceNo = _event.Distance ?? 0;

            if (distanceNo < 1)
            {
                MessageBox.Show("Distance must be a positive integer.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Make sure Time is set to BaseDate with time-of-day from input
            DateTime time = _event.Time ?? DateTime.MinValue;
            if (time == DateTime.MinValue)
            {
                MessageBox.Show("Please provide a valid Time in HH:mm:ss format.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            TimeSpan ts = time.TimeOfDay;
            time = BaseDate!.Value.Add(ts);
            _event.Time = time;

            // Validate lanes
            var minLane = _event.MinLane!.Value;
            var maxLane = _event.MaxLane!.Value;
            if ((minLane < 1) || (minLane > 12) || (maxLane < 1) || (maxLane > 12) || (maxLane < minLane))
            {
                MessageBox.Show("Please provide valid Min and Max Lane values (1-12, Min <= Max).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }



            // Capture enum selections
            //_event.Gender
            //_event.AgeGrouping
            //_event.UnderAgeGroup
            //_event.MastersAgeGroup
            //
            var TrackTypeValue = _event.TrackType;

            var GenderValue = _event.Gender;
      
            var AgeGroupingValue = _event.AgeGrouping;
            if (AgeGroupingValue == AgeGrouping.junior)
            {
                var UnderAgeGroupValue = _event.UnderAgeGroup;
                _event.MastersAgeGroup = null;
                // Require selection for junior
                if (_event.UnderAgeGroup == null)
                {
                    MessageBox.Show("Please select an Under Age Group.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else if (AgeGroupingValue == AgeGrouping.masters)
            {
                var MastersAgeGroupValue = _event.MastersAgeGroup;;
                _event.UnderAgeGroup = null;
                // Require selection for masters
                if (MastersAgeGroupValue == null)
                {
                    MessageBox.Show("Please select a Masters Age Group.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                _event.UnderAgeGroup = null;
                _event.MastersAgeGroup = null;
            }

            DialogResult = true;
        }

        private void AgeGroupingBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // On any change, clear both subgroup selections/values
            //_event.AgeGrouping =  AgeGrouping.none ;
            /*_event.MastersAgeGroup = MastersAgeGroup.other;
            _event.UnderAgeGroup = null;
            _event.MastersAgeGroup = null;

            var ag = (AgeGrouping)(AgeGroupingBox.SelectedItem ?? AgeGrouping.none);
            UpdateAgeGroupVisibility(ag);*/
        }

        private void UpdateAgeGroupVisibility(AgeGrouping ag)
        {/*
            if (ag == AgeGrouping.junior)
            {
                StandardAgeGroupBox.Visibility = Visibility.Visible;
                MastersAgeGroupBox.Visibility = Visibility.Collapsed;
                // Clear incompatible selection/value
                MastersAgeGroupBox.SelectedItem = null;
                MastersAgeGroupValue = null;
            }
            else if (ag == AgeGrouping.masters)
            {
                StandardAgeGroupBox.Visibility = Visibility.Collapsed;
                MastersAgeGroupBox.Visibility = Visibility.Visible;
                // Clear incompatible selection/value
                StandardAgeGroupBox.SelectedItem = null;
                UnderAgeGroupValue = null;
                // Refilter masters list based on current gender
                ApplyMastersGenderFilter();
            }
            else
            {
                StandardAgeGroupBox.Visibility = Visibility.Collapsed;
                MastersAgeGroupBox.Visibility = Visibility.Collapsed;
                // Clear both when not applicable (e.g., senior/open or none)
                StandardAgeGroupBox.SelectedItem = null;
                MastersAgeGroupBox.SelectedItem = null;
                UnderAgeGroupValue = null;
                MastersAgeGroupValue = null;
            }*/
        }

        private void GenderBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            /*
            // Clear subgroup selections when gender changes and re-apply filter
            StandardAgeGroupBox.SelectedItem = null;
            MastersAgeGroupBox.SelectedItem = null;
            UnderAgeGroupValue = null;
            MastersAgeGroupValue = null;
            ApplyMastersGenderFilter();*/
        }

        private void ApplyMastersGenderFilter()
        {
            /*var g = (Gender)(GenderBox.SelectedItem ?? Gender.none);
            var all = Enum.GetValues(typeof(MastersAgeGroup)).Cast<MastersAgeGroup>();
            // Filter by gender: male => M*, female => W*, mixed/none => all
            if (g == Gender.male)
                MastersAgeGroupBox.ItemsSource = all.Where(x => x.ToString().StartsWith("M"));
            else if (g == Gender.female)
                MastersAgeGroupBox.ItemsSource = all.Where(x => x.ToString().StartsWith("W"));
            else
                MastersAgeGroupBox.ItemsSource = all;*/
        }

        // Code-behind for the dialog/window


        private static readonly Regex _digitsOnly = new(@"^\d+$");
        private static readonly Regex _digitsTyping = new(@"^\d*$");

        private void DistanceBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !_digitsTyping.IsMatch(e.Text);
        }

        private void DistanceBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                var text = (string)e.DataObject.GetData(DataFormats.Text);
                if (!_digitsOnly.IsMatch(text)) e.CancelCommand();
            }
            else e.CancelCommand();

            var x = new AthStitcherGUI.Converters.UnderAgeGroupByGenderConverter();
        }
    }


}
