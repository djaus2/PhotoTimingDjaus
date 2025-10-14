using System.Windows;
using System.Windows.Controls;

namespace AthStitcherGUI
{
    public partial class ExifToolNamingDialog : Window
    {
        public string InputText => InputTextBox.Text;
        public string InputText2 => InputTextBox2.Text;
        public ExifToolNamingDialog(string initialText = "")
        {
            InitializeComponent();
            InputTextBox.Text = initialText;
            InputTextBox.SelectAll();
            InputTextBox.Focus();
            InputTextBox2.Text = initialText+"(-k)";
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            InputTextBox2.Text = InputTextBox.Text +"(-k)";
        }
    }
}