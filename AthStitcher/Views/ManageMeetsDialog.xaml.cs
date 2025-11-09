using AthStitcher.Data;
using AthStitcherGUI.ViewModels;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace AthStitcher.Views
{
    public partial class ManageMeetsDialog : Window
    {
        public AthStitcherGUI.ViewModels.AthStitcherModel vm { get; set; }
        public Meet? SelectedMeet { get; private set; }
        public ManageMeetsDialog()
        {
            InitializeComponent();
            LoadMeets();
            Loaded += (_, __) =>
            {

            };

        }

        private void LoadMeets()
        {
            using var ctx = new AthStitcherDbContext();
            var items = ctx.Meets
                .OrderByDescending(m => m.Date.HasValue)
                .ThenByDescending(m => m.Date)
                .ToList();
            MeetsGrid.ItemsSource = items;
            // Default sort: Date desc
            var view = CollectionViewSource.GetDefaultView(MeetsGrid.ItemsSource);
            if (view != null)
            {
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription(nameof(Meet.Date), ListSortDirection.Descending));
                view.Filter = FilterPredicate;
            }
            // Show glyph on Date column
            var dateCol = MeetsGrid.Columns
                .OfType<DataGridTextColumn>()
                .FirstOrDefault(c => c.SortMemberPath == nameof(Meet.Date));
            if (dateCol != null) dateCol.SortDirection = ListSortDirection.Descending;
        }

        private bool FilterPredicate(object obj)
        {
            if (obj is not Meet m) return false;
            string descFilter = (DescriptionFilter?.Text ?? string.Empty).Trim();
            string dateFilter = (DateFilter?.Text ?? string.Empty).Trim();
            string locFilter = (LocationFilter?.Text ?? string.Empty).Trim();
            string roundFilter = (RoundFilter?.Text ?? string.Empty).Trim();

            // Description contains (case-insensitive)
            if (!string.IsNullOrEmpty(descFilter))
            {
                if (string.IsNullOrEmpty(m.Description) || m.Description.IndexOf(descFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }

            // Date contains: format to yyyy-MM-dd if present
            if (!string.IsNullOrEmpty(dateFilter))
            {
                var dateStr = m.Date?.ToString("yyyy-MM-dd") ?? string.Empty;
                if (dateStr.IndexOf(dateFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }

            // Location contains (case-insensitive)
            if (!string.IsNullOrEmpty(locFilter))
            {
                var loc = m.Location ?? string.Empty;
                if (loc.IndexOf(locFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }

            if (!string.IsNullOrEmpty(roundFilter))
            {
                var round = m.Round.ToString() ?? string.Empty;
                if (round.IndexOf(roundFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }
            return true;
        }

        private void Filter_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var view = CollectionViewSource.GetDefaultView(MeetsGrid.ItemsSource);
            view?.Refresh();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {

            DateTime meetCutoff = DateTime.Now.Date;
            if (this.DataContext is AthStitcherModel vm)
            {
                int cutoff = vm.Scheduling?.MeetCutoff ?? 0;
                meetCutoff = DateTime.Now.AddDays(cutoff);
            }
            Meet meet = new Meet
            {
                Description = "<Enter Meet description>",
                Date = meetCutoff,
                Location = "<Enter Meet Location>",
                Round = 1

            };
            var dlg = new NewMeetDialog { Owner = this };
            dlg.Meet = meet;
            dlg.CutOff = meetCutoff;
            if (dlg.ShowDialog() == true)
            {
                using var ctx = new AthStitcherDbContext();
                meet = dlg.Meet;
                string desc = meet.Description;
                if (string.IsNullOrWhiteSpace(desc))
                {
                    MessageBox.Show("Description cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                int? round = meet.Round; //Not mandatory so no checks except >0
                if ((round != null) && (round <= 0))
                {
                    MessageBox.Show("Round must be greater than zero.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var date = meet.Date;
                if ((date == null) || (date < meetCutoff))
                {
                    MessageBox.Show("Date before cut-off date {meetCutoff}.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var loc = meet.Location;
                if (string.IsNullOrWhiteSpace(loc))
                {
                    MessageBox.Show("Location cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Duplicate check: same Description + Date
                bool exists = ctx.Meets.Any(m => m.Description == desc && m.Date == date && m.Location == loc);
                if (exists)
                {
                    MessageBox.Show("A meet with the same Description and Date already exists.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                //var meet = new Meet { Description = desc, Date = date, Location = loc };
                ctx.Meets.Add(meet);
                ctx.SaveChanges();
                LoadMeets();
            }
        }


        private void EditRow_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not Meet meet) return;

            // Check wrt Cut-off
            DateTime meetCutoffDay = DateTime.Now.Date;
            if (vm != null)
            {
                int cutoff = vm.Scheduling?.MeetCutoff ?? 0;
                DateTime MeetDate = meet.Date ?? DateTime.Now;
                meetCutoffDay = MeetDate.AddDays(-cutoff).Date;
                if (meetCutoffDay < DateTime.Now.Date)
                {
                    MessageBox.Show($"Too late to [Edit] meet based on current cut-off settings. Cut-off is  {cutoff} days before Meet.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var dlg = new NewMeetDialog { Owner = this };
            dlg.Meet = meet;

            if (dlg.ShowDialog() == true)
            {
                using var ctx = new AthStitcherDbContext();
                meet = dlg.Meet;
                string desc = meet.Description;
                if (string.IsNullOrWhiteSpace(desc))
                {
                    MessageBox.Show("Description cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                int? round = meet.Round; //Not mandatory so no checks except >0
                if ((round != null) && (round <= 0))
                {
                    MessageBox.Show("Round must be greater than zero.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var date = meet.Date;
                if (date == null) 
                {
                    MessageBox.Show("Date before cut-off date {meetCutoff}.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                DateTime newMeetCutoffDay = DateTime.Now.Date;
                if (vm != null)
                {
                    int cutoff = vm.Scheduling?.MeetCutoff ?? 0;
                    DateTime MeetDate = meet.Date ?? DateTime.Now;
                    newMeetCutoffDay = MeetDate.AddDays(-cutoff).Date;
                    if (newMeetCutoffDay < DateTime.Now.Date)
                    {
                        MessageBox.Show($"Too late to [Edit] meet based on current cut-off settings. Cut-off is  {cutoff} days before Meet.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                var loc = meet.Location;
                if (string.IsNullOrWhiteSpace(loc))
                {
                    MessageBox.Show("Location cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Duplicate check: same Description + Date
                // No duplicate Check as this is an UPDATE
                //bool exists = ctx.Meets.Any(m => m.Description == desc && m.Date == date && m.Location == loc);
                //if (exists)
                //{
                //    MessageBox.Show("A meet with the same Description and Date already exists.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                //    return;
                //};
                ctx.Meets.Update(meet);
                ctx.SaveChanges();
                LoadMeets();
            }
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not Meet row) return;

            // Check wrt Cut-off
            DateTime meetCutoffDay = DateTime.Now.Date;
            if (vm != null)
            {
                int cutoff = vm.Scheduling?.MeetCutoff ?? 0;
                DateTime MeetDate = row.Date ?? DateTime.Now;
                meetCutoffDay = MeetDate.AddDays(-cutoff).Date;
                if (meetCutoffDay < DateTime.Now.Date)
                {
                    MessageBox.Show($"Too late to [Delete] meet based on current cut-off settings. Cut-off is  {cutoff} days before Meet.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var confirm = MessageBox.Show($"Delete meet '{row.Description} Round:{row.Round}' ?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            using var ctx = new AthStitcherDbContext();
            var existing = ctx.Meets.SingleOrDefault(m => m.Id == row.Id);
            if (existing != null)
            {
                ctx.Meets.Remove(existing);
                ctx.SaveChanges();
                LoadMeets();
            }
        }

        private void MeetsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (MeetsGrid.SelectedItem is Meet row)
            {
                SelectedMeet = row;
                this.DialogResult = true;
                this.Close();
            }
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            if (MeetsGrid.SelectedItem is Meet row)
            {
                SelectedMeet = row;
            }
            this.DialogResult = true;
            this.Close();
        }
    }
}
