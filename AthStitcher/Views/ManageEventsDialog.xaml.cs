using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AthStitcher.Data;
using Xceed.Wpf.Toolkit.Panels;

namespace AthStitcher.Views
{
    public partial class ManageEventsDialog : Window
    {
        public AthStitcherGUI.ViewModels.AthStitcherModel vm { get; set; }
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

            string eventNumberFilter = (EventNumberFilter?.Text ?? string.Empty).Trim();
            string timeFilter = (TimeFilter?.Text ?? string.Empty).Trim();
            string trackTypeFilter = (TrackTypeFilter?.Text ?? string.Empty).Trim();
            string distFilter = (DistanceFilter?.Text ?? string.Empty).Trim();
            string ageGroupingFilter = (AgeGroupingFilter?.Text ?? string.Empty).Trim();
            string underAgeGroupFilter = (UnderAgeGroupFilter?.Text ?? string.Empty).Trim();
            string mastersAgeGroupFilter = (MastersAgeGroupFilter?.Text ?? string.Empty).Trim();

            string genderFilter = (GenderFilter?.Text ?? string.Empty).Trim();

            if (!string.IsNullOrEmpty(eventNumberFilter))
            {
                var dStr = e.EventNumber?.ToString() ?? string.Empty;
                if (dStr.IndexOf(eventNumberFilter, StringComparison.OrdinalIgnoreCase) < 0)
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
            if (!string.IsNullOrEmpty(trackTypeFilter))
            {
                var dStr = e.TrackType.ToString();
                if (dStr.IndexOf(trackTypeFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }
            if (!string.IsNullOrEmpty(genderFilter))
            {
                //Because of male/female overlap we do startswith
                var dStr = e.Gender.ToString();
                if (!string.Equals(dStr.Substring(0, genderFilter.Length), genderFilter, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            if (!string.IsNullOrEmpty(ageGroupingFilter))
            {
                var dStr = e.AgeGrouping.ToString();
                if (dStr.IndexOf(ageGroupingFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }
            if (!string.IsNullOrEmpty(underAgeGroupFilter))
            {
                var dStr = e.UnderAgeGroup.ToString();
                if (dStr.IndexOf(underAgeGroupFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }
            if (!string.IsNullOrEmpty(mastersAgeGroupFilter))
            {
                var dStr = e.MastersAgeGroup.ToString();
                if (dStr.IndexOf(mastersAgeGroupFilter, StringComparison.OrdinalIgnoreCase) < 0)
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
            Meet meet;
            using (var ctx = new AthStitcherDbContext())
            {
                meet = ctx.Meets.SingleOrDefault(m => m.Id == MeetId);
                //dlg.BaseDate = meet?.Date;
            }
            if (dlg.ShowDialog() == true)
            {
                using var ctx = new AthStitcherDbContext();
                var ev = dlg._event;
                ev.MeetId = MeetId;
                ctx.Events.Add(ev);
                ctx.SaveChanges();


                // Ask for number of heats; default and minimum is 1
                int heatsCount = 1;
                var heatsDlg = new NumberOfHeatsDialog { Owner = this };
                if (heatsDlg.ShowDialog() == true)
                {
                    heatsCount = Math.Max(1, heatsDlg.HeatsCount);
                }
                // Create heats 1..heatsCount, skip any that already exist
                for (int h = 1; h <= heatsCount; h++)
                {
                    if (!ctx.Heats.Any(x => x.EventId == ev.Id && x.HeatNo == h))
                    {
                        ctx.Heats.Add(new Heat { EventId = ev.Id, HeatNo = h });
                    }
                }
                ctx.SaveChanges();
                LoadEvents();
            }
        }

        private void EditRow_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not Event row) return;

            // Check wrt Cut-off
            DateTime meetCutoffDay = DateTime.Now.Date;
            if (vm != null)
            {
                int cutoff = vm.Scheduling?.EventCutoff ?? 0;
                DateTime MeetDate = vm.CurrentMeet.Date ?? DateTime.Now;
                meetCutoffDay = MeetDate.AddDays(-cutoff).Date;
                if (meetCutoffDay < DateTime.Now.Date)
                {
                    MessageBox.Show($"Too late to [Edit] event based on current cut-off settings. Cut-off is  {cutoff} days before Meet.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var dlg = new NewEventDialog { Owner = this };
            // Provide base date for composing time
            using (var ctx = new AthStitcherDbContext())
            {
                var meet = ctx.Meets.SingleOrDefault(m => m.Id == MeetId);
                dlg.BaseDate = meet!.Date;
            }
            dlg.InitializeForEdit(row);
            if (dlg.ShowDialog() == true)
            {
                using var ctx = new AthStitcherDbContext();
                //var existing = ctx.Events.SingleOrDefault(ev => ev.Id == row.Id);
                Event existing = dlg._event;

                ctx.Events.Update(existing);
                ctx.SaveChanges();
                return;
            }
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not Event row) return;

            // Check wrt Cut-off
            DateTime eventMeetCutoff = DateTime.Now.Date;
            if (vm != null)
            {
                int eventcutoff = vm.Scheduling?.EventCutoff ?? 0;
                DateTime MeetDate = vm.CurrentMeet.Date ?? DateTime.Now;
                eventMeetCutoff = MeetDate.AddDays(-eventcutoff).Date;
                if (eventMeetCutoff < DateTime.Now.Date)
                {
                    MessageBox.Show($"Too late to [Delete] event based on current cut-off settings. Cut-off is  {eventcutoff} days before Meet.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

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
