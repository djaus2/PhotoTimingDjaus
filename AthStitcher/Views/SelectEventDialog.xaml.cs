using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AthStitcher.Data;

namespace AthStitcher.Views
{
    public partial class SelectEventDialog : Window
    {
        public Event? SelectedEvent { get; private set; }
        public int MeetId { get; set; }

        public SelectEventDialog(int meetId)
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
                    existing.Distance = dlg.DistanceValue;
                    existing.Time = dlg.EventTime;
                    existing.TrackType = dlg.TrackTypeValue;
                    existing.Gender = dlg.GenderValue;
                    existing.AgeGrouping = dlg.AgeGroupingValue;
                    existing.UnderAgeGroup = dlg.UnderAgeGroupValue;
                    existing.MastersAgeGroup = dlg.MastersAgeGroupValue;
                    ctx.SaveChanges();

                    // Prompt to edit number of heats for this event
                    int currentHeats = ctx.Heats.Count(h => h.EventId == existing.Id);
                    var heatsDlg = new NumberOfHeatsDialog { Owner = this, InitialHeats = Math.Max(1, currentHeats) };
                    if (heatsDlg.ShowDialog() == true)
                    {
                        var desired = Math.Max(1, heatsDlg.HeatsCount);
                        if (desired > currentHeats)
                        {
                            // Add heats from currentHeats+1..desired
                            for (int h = currentHeats + 1; h <= desired; h++)
                            {
                                if (!ctx.Heats.Any(x => x.EventId == existing.Id && x.HeatNo == h))
                                    ctx.Heats.Add(new Heat { EventId = existing.Id, HeatNo = h });
                            }
                            ctx.SaveChanges();
                        }
                        else if (desired < currentHeats)
                        {
                            // Remove highest-numbered heats down to desired, skipping any with results
                            bool warned = false;
                            for (int h = currentHeats; h > desired; h--)
                            {
                                var heat = ctx.Heats.SingleOrDefault(x => x.EventId == existing.Id && x.HeatNo == h);
                                if (heat == null) continue;
                                bool hasResults = ctx.Results.Any(r => r.HeatId == heat.Id);
                                if (hasResults)
                                {
                                    if (!warned)
                                    {
                                        MessageBox.Show("Some heats could not be removed because they contain results.", "Heats", MessageBoxButton.OK, MessageBoxImage.Information);
                                        warned = true;
                                    }
                                    continue;
                                }
                                ctx.Heats.Remove(heat);
                            }
                            ctx.SaveChanges();
                        }
                    }
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

        private void EventsGrid_Select2(object sender, System.Windows.Input.KeyEventArgs e)
        {
            EventsGrid_Select(sender, e);
        }

        private void EventsGrid_Select(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                Done_Click(sender, e);
            }
        }
    }
}
