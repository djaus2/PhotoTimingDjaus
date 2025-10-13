using AthStitcher.Data;
using System;
using System.Windows;

namespace AthStitcher.Views
{
    public partial class ChangePasswordDialog : Window
    {
        public string Username { get; set; } = "admin";
        public string Current => CurrentPassword.Password ?? string.Empty;
        public string NewPwd => NewPassword.Password ?? string.Empty;
        public string ConfirmPwd => ConfirmPassword.Password ?? string.Empty;

        public ChangePasswordDialog()
        {
            InitializeComponent();
            Loaded += (_, __) => UsernameLabel.Text = Username;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewPwd) || NewPwd.Length < 12)
            {
                MessageBox.Show("New password must be at least 12 characters.", "Change Password", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!string.Equals(NewPwd, ConfirmPwd, StringComparison.Ordinal))
            {
                MessageBox.Show("New password and confirmation do not match.", "Change Password", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
            Close();
        }
    }
}
