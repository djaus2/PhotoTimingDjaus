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
            return true;
        }

        private void Filter_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var view = CollectionViewSource.GetDefaultView(MeetsGrid.ItemsSource);
            view?.Refresh();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new NewMeetDialog { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                using var ctx = new AthStitcherDbContext();
                var desc = (dlg.DescriptionValue ?? string.Empty).Trim();
                var date = dlg.DateValue;
                var loc = string.IsNullOrWhiteSpace(dlg.LocationValue) ? null : dlg.LocationValue!.Trim();
                // Duplicate check: same Description + Date
                bool exists = ctx.Meets.Any(m => m.Description == desc && m.Date == date);
                if (exists)
                {
                    MessageBox.Show("A meet with the same Description and Date already exists.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var meet = new Meet { Description = desc, Date = date, Location = loc };
                ctx.Meets.Add(meet);
                ctx.SaveChanges();
                LoadMeets();
            }
        }

        private void EditRow_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not Meet row) return;

            // Check wrt Cut-off
            DateTime meetCuttoffDay = DateTime.Now.Date;
            if (vm != null)
            {
                int cuttoff = vm.Scheduling?.MeetCutoff ?? 0;
                DateTime MeetDate = row.Date ?? DateTime.Now;
                meetCuttoffDay = MeetDate.AddDays(-cuttoff).Date;
                if (meetCuttoffDay < DateTime.Now.Date)
                {
                    MessageBox.Show($"Too late to [Edit] meet based on current cut-off settings. Cut-off is  {cuttoff} days before Meet.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var dlg = new NewMeetDialog { Owner = this };
            dlg.FindName("DescriptionBox");
            dlg.FindName("MeetDatePicker");
            dlg.FindName("LocationBox");
            // Initialize fields via logical names
            if (dlg.Content is FrameworkElement root)
            {
                var desc = (TextBox)root.FindName("DescriptionBox");
                var date = (DatePicker)root.FindName("MeetDatePicker");
                var loc = (TextBox)root.FindName("LocationBox");
                desc.Text = row.Description;
                date.SelectedDate = row.Date;
                //date.Text = row.Date?.ToString("yyyy-MM-dd") ?? string.Empty;
                loc.Text = row.Location ?? string.Empty;
            }

            if (dlg.ShowDialog() == true)
            {
                using var ctx = new AthStitcherDbContext();
                var existing = ctx.Meets.SingleOrDefault(m => m.Id == row.Id);
                if (existing != null)
                {
                    var newDesc = (dlg.DescriptionValue ?? existing.Description).Trim();
                    var newDate = dlg.DateValue;
                    var newLoc = string.IsNullOrWhiteSpace(dlg.LocationValue) ? null : dlg.LocationValue!.Trim();
                    // Duplicate check excluding current row
                    bool dup = ctx.Meets.Any(m => m.Id != row.Id && m.Description == newDesc && m.Date == newDate);
                    if (dup)
                    {
                        MessageBox.Show("Another meet with the same Description and Date already exists.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    existing.Description = newDesc;
                    existing.Date = newDate;
                    existing.Location = newLoc;
                    ctx.SaveChanges();
                    LoadMeets();
                }
            }
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not Meet row) return;

            // Check wrt Cut-off
            DateTime meetCuttoffDay = DateTime.Now.Date;
            if (vm != null)
            {
                int cuttoff = vm.Scheduling?.MeetCutoff ?? 0;
                DateTime MeetDate = row.Date ?? DateTime.Now;
                meetCuttoffDay = MeetDate.AddDays(-cuttoff).Date;
                if (meetCuttoffDay < DateTime.Now.Date)
                {
                    MessageBox.Show($"Too late to [Delete] meet based on current cut-off settings. Cut-off is  {cuttoff} days before Meet.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var confirm = MessageBox.Show($"Delete meet '{row.Description}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
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
