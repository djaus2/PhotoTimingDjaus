using System;
using System.Linq;
using System.Windows;
using AthStitcher.Data;

namespace AthStitcher.Views
{
    public partial class NewEventDialog : Window
    {
        public string? DescriptionValue { get; private set; }
        public int? EventNumberValue { get; private set; }
        public int? HeatNumberValue { get; private set; }
        public int DistanceValue { get; private set; }
        public int MinLaneValue { get; private set; } = 1;
        public int MaxLaneValue { get; private set; } = 8;
        public DateTime? EventTime { get; private set; }
        public DateTime? BaseDate { get; set; }
        // Selected enums
        public TrackType TrackTypeValue { get; private set; } = TrackType.na;
        public Gender GenderValue { get; private set; } = Gender.none;
        public AgeGrouping AgeGroupingValue { get; private set; } = AgeGrouping.none;
        public UnderAgeGroup? UnderAgeGroupValue { get; private set; }
        public MastersAgeGroup? MastersAgeGroupValue { get; private set; }

        public NewEventDialog()
        {
            InitializeComponent();
            TimeBox.Text = DateTime.Now.ToString("HH:mm:ss");

            // Populate enum dropdowns
            TrackTypeBox.ItemsSource = Enum.GetValues(typeof(TrackType)).Cast<TrackType>();
            TrackTypeBox.SelectedItem = TrackType.na;

            GenderBox.ItemsSource = Enum.GetValues(typeof(Gender)).Cast<Gender>();
            GenderBox.SelectedItem = Gender.none;
            GenderBox.SelectionChanged += GenderBox_SelectionChanged;

            AgeGroupingBox.ItemsSource = Enum.GetValues(typeof(AgeGrouping)).Cast<AgeGrouping>();
            AgeGroupingBox.SelectionChanged += AgeGroupingBox_SelectionChanged;
            AgeGroupingBox.SelectedItem = AgeGrouping.none;

            StandardAgeGroupBox.ItemsSource = Enum.GetValues(typeof(UnderAgeGroup)).Cast<UnderAgeGroup>();
            MastersAgeGroupBox.ItemsSource = Enum.GetValues(typeof(MastersAgeGroup)).Cast<MastersAgeGroup>();
            // Hide both until an appropriate AgeGrouping is selected
            StandardAgeGroupBox.Visibility = Visibility.Collapsed;
            MastersAgeGroupBox.Visibility = Visibility.Collapsed;
        }

        public void InitializeForEdit(Event existing)
        {
            // Pre-fill for edit callers
            DescriptionBox.Text = existing.Description ?? string.Empty;
            EventNumberValue = existing.EventNumber;
            HeatNumberValue = existing.HeatNumber;
            DistanceValue = existing.Distance ?? 0;
            TimeBox.Text = existing.Time?.ToString("HH:mm:ss") ?? TimeBox.Text;
            TrackTypeBox.SelectedItem = existing.TrackType;
            GenderBox.SelectedItem = existing.Gender;
            AgeGroupingBox.SelectedItem = existing.AgeGrouping;
            // Set visibility and selection for age sub-groups
            UpdateAgeGroupVisibility(existing.AgeGrouping);
            StandardAgeGroupBox.SelectedItem = existing.UnderAgeGroup ?? UnderAgeGroup.other;
            MastersAgeGroupBox.SelectedItem = existing.MastersAgeGroup ?? MastersAgeGroup.other;
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
            if (!int.TryParse(DistanceBox.Text, out var dist) || dist <= 0)
            {
                MessageBox.Show("Distance must be a positive integer.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DistanceValue = dist;

            if (int.TryParse(EventNumberBox.Text, out var ev)) EventNumberValue = ev; else EventNumberValue = null;
            if (int.TryParse(HeatNumberBox.Text, out var ht)) HeatNumberValue = ht; else HeatNumberValue = null;

            // Time only; date is from meet (use BaseDate if provided, else today)
            var tText = (TimeBox.Text ?? string.Empty).Trim();
            if (TimeSpan.TryParse(tText, out var ts))
            {
                var dateUsed = (BaseDate?.Date) ?? DateTime.Today;
                EventTime = dateUsed + ts;
            }
            else
            {
                MessageBox.Show("Time must be in HH:mm:ss or HH:mm:ss.fff format.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(MinLaneBox.Text, out var minLane)) minLane = 1;
            if (!int.TryParse(MaxLaneBox.Text, out var maxLane)) maxLane = 8;
            if (minLane < 1) minLane = 1; if (maxLane < minLane) maxLane = minLane;
            MinLaneValue = minLane; MaxLaneValue = maxLane;

            // Capture enum selections
            TrackTypeValue = (TrackType)(TrackTypeBox.SelectedItem ?? TrackType.na);
            GenderValue = (Gender)(GenderBox.SelectedItem ?? Gender.none);
            AgeGroupingValue = (AgeGrouping)(AgeGroupingBox.SelectedItem ?? AgeGrouping.none);
            if (AgeGroupingValue == AgeGrouping.junior)
            {
                UnderAgeGroupValue = (UnderAgeGroup?)(StandardAgeGroupBox.SelectedItem ?? UnderAgeGroup.other);
                MastersAgeGroupValue = null;
                // Require selection for junior
                if (UnderAgeGroupValue == null)
                {
                    MessageBox.Show("Please select an Under Age Group.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else if (AgeGroupingValue == AgeGrouping.masters)
            {
                MastersAgeGroupValue = (MastersAgeGroup?)(MastersAgeGroupBox.SelectedItem ?? MastersAgeGroup.other);
                UnderAgeGroupValue = null;
                // Require selection for masters
                if (MastersAgeGroupValue == null)
                {
                    MessageBox.Show("Please select a Masters Age Group.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                UnderAgeGroupValue = null;
                MastersAgeGroupValue = null;
            }

            DialogResult = true;
        }

        private void AgeGroupingBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // On any change, clear both subgroup selections/values
            StandardAgeGroupBox.SelectedItem = null;
            MastersAgeGroupBox.SelectedItem = null;
            UnderAgeGroupValue = null;
            MastersAgeGroupValue = null;

            var ag = (AgeGrouping)(AgeGroupingBox.SelectedItem ?? AgeGrouping.none);
            UpdateAgeGroupVisibility(ag);
        }

        private void UpdateAgeGroupVisibility(AgeGrouping ag)
        {
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
            }
        }

        private void GenderBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Clear subgroup selections when gender changes and re-apply filter
            StandardAgeGroupBox.SelectedItem = null;
            MastersAgeGroupBox.SelectedItem = null;
            UnderAgeGroupValue = null;
            MastersAgeGroupValue = null;
            ApplyMastersGenderFilter();
        }

        private void ApplyMastersGenderFilter()
        {
            var g = (Gender)(GenderBox.SelectedItem ?? Gender.none);
            var all = Enum.GetValues(typeof(MastersAgeGroup)).Cast<MastersAgeGroup>();
            // Filter by gender: male => M*, female => W*, mixed/none => all
            if (g == Gender.male)
                MastersAgeGroupBox.ItemsSource = all.Where(x => x.ToString().StartsWith("M"));
            else if (g == Gender.female)
                MastersAgeGroupBox.ItemsSource = all.Where(x => x.ToString().StartsWith("W"));
            else
                MastersAgeGroupBox.ItemsSource = all;
        }
    }
}
