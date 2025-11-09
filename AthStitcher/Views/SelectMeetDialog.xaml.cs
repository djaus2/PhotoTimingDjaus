using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Data;
using AthStitcher.Data;

namespace AthStitcher.Views
{
    public partial class SelectMeetDialog : Window
    {
        public Meet? SelectedMeet { get; private set; }
        public SelectMeetDialog()
        {
            InitializeComponent();
            LoadMeets();
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
        private void MeetsGrid_Select2(object sender, System.Windows.Input.KeyEventArgs e)
        {
            MeetsGrid_Select(sender, e);
        }

        private void MeetsGrid_Select(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                Done_Click(sender, e);
            }
        }
    }
}
