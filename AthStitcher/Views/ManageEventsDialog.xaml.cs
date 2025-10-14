using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AthStitcher.Data;

namespace AthStitcher.Views
{
    public partial class ManageEventsDialog : Window
    {
        public Event? SelectedEvent { get; private set; }
        public int MeetId { get; set; }

        public ManageEventsDialog(int meetId)
        {
            InitializeComponent();
            MeetId = meetId;
            LoadEvents();
        }

        private void LoadEvents()
        {
            using var ctx = new AthStitcherDbContext();
            var items = ctx.Events
                .Where(ev => ev.MeetId == MeetId)
                .OrderBy(ev => ev.Time)
                .ThenBy(ev => ev.EventNumber)
                .ThenBy(ev => ev.HeatNumber)
                .ToList();
            EventsGrid.ItemsSource = items;

            var view = CollectionViewSource.GetDefaultView(EventsGrid.ItemsSource);
            if (view != null)
            {
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription(nameof(Event.Time), ListSortDirection.Ascending));
                view.Filter = FilterPredicate;
            }
            var timeCol = EventsGrid.Columns
                .OfType<DataGridTextColumn>()
                .FirstOrDefault(c => c.SortMemberPath == nameof(Event.Time));
            if (timeCol != null) timeCol.SortDirection = ListSortDirection.Ascending;
        }

        private bool FilterPredicate(object obj)
        {
            if (obj is not Event e) return false;
            string descFilter = (DescriptionFilter?.Text ?? string.Empty).Trim();
            string timeFilter = (TimeFilter?.Text ?? string.Empty).Trim();
            string distFilter = (DistanceFilter?.Text ?? string.Empty).Trim();

            if (!string.IsNullOrEmpty(descFilter))
            {
                if (string.IsNullOrEmpty(e.Description) || e.Description.IndexOf(descFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }

            if (!string.IsNullOrEmpty(timeFilter))
            {
                var timeStr = e.TimeStr ?? string.Empty;
                if (timeStr.IndexOf(timeFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }

            if (!string.IsNullOrEmpty(distFilter))
            {
                var dStr = e.Distance?.ToString() ?? string.Empty;
                if (dStr.IndexOf(distFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }
            return true;
        }

        private void Filter_TextChanged(object sender, TextChangedEventArgs e)
        {
            var view = CollectionViewSource.GetDefaultView(EventsGrid.ItemsSource);
            view?.Refresh();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new NewEventDialog { Owner = this };
            // Use meet date as base date
            using (var ctx = new AthStitcherDbContext())
            {
                var meet = ctx.Meets.SingleOrDefault(m => m.Id == MeetId);
                dlg.BaseDate = meet?.Date;
            }
            if (dlg.ShowDialog() == true)
            {
                using var ctx = new AthStitcherDbContext();
                var ev = new Event
                {
                    MeetId = MeetId,
                    Description = dlg.DescriptionValue,
                    EventNumber = dlg.EventNumberValue,
                    HeatNumber = dlg.HeatNumberValue,
                    Distance = dlg.DistanceValue,
                    Time = dlg.EventTime,
                    TrackType = dlg.TrackTypeValue,
                    Gender = dlg.GenderValue,
                    AgeGrouping = dlg.AgeGroupingValue,
                    UnderAgeGroup = dlg.UnderAgeGroupValue,
                    MastersAgeGroup = dlg.MastersAgeGroupValue,
                };
                ctx.Events.Add(ev);
                ctx.SaveChanges();
                LoadEvents();
            }
        }

        private void EditRow_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not Event row) return;
            var dlg = new NewEventDialog { Owner = this };
            // Provide base date for composing time
            using (var ctx = new AthStitcherDbContext())
            {
                var meet = ctx.Meets.SingleOrDefault(m => m.Id == MeetId);
                dlg.BaseDate = meet?.Date;
            }
            dlg.InitializeForEdit(row);
            if (dlg.ShowDialog() == true)
            {
                using var ctx = new AthStitcherDbContext();
                var existing = ctx.Events.SingleOrDefault(ev => ev.Id == row.Id);
                if (existing != null)
                {
                    existing.Description = dlg.DescriptionValue;
                    existing.EventNumber = dlg.EventNumberValue;
                    existing.HeatNumber = dlg.HeatNumberValue;
                    existing.Distance = dlg.DistanceValue;
                    existing.Time = dlg.EventTime;
                    existing.TrackType = dlg.TrackTypeValue;
                    existing.Gender = dlg.GenderValue;
                    existing.AgeGrouping = dlg.AgeGroupingValue;
                    existing.UnderAgeGroup = dlg.UnderAgeGroupValue;
                    existing.MastersAgeGroup = dlg.MastersAgeGroupValue;
                    ctx.SaveChanges();
                    LoadEvents();
                }
            }
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not Event row) return;
            var confirm = MessageBox.Show($"Delete event '{row.Description}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
            using var ctx = new AthStitcherDbContext();
            var existing = ctx.Events.SingleOrDefault(ev => ev.Id == row.Id);
            if (existing != null)
            {
                ctx.Events.Remove(existing);
                ctx.SaveChanges();
                LoadEvents();
            }
        }

        private void EventsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (EventsGrid.SelectedItem is Event row)
            {
                SelectedEvent = row;
                this.DialogResult = true;
                this.Close();
            }
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            if (EventsGrid.SelectedItem is Event row)
            {
                SelectedEvent = row;
            }
            this.DialogResult = true;
            this.Close();
        }
    }
}
