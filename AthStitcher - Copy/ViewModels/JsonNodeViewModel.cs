using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace AthStitcherGUI.ViewModels
{
    public class JsonNodeViewModel : INotifyPropertyChanged
    {
        public string Key { get; set; }

        private string _value;
        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                ValidateValue();
                OnPropertyChanged(nameof(Value));
            }
        }

        public ObservableCollection<JsonNodeViewModel> Children { get; set; } = new();
        public bool HasChildren => Children.Count > 0;

        public JTokenType ExpectedType { get; set; }
        public bool IsValid { get; private set; } = true;

        private void ValidateValue()
        {
            try
            {
                IsValid = ExpectedType switch
                {
                    JTokenType.Integer => int.TryParse(Value, out _),
                    JTokenType.Float => double.TryParse(Value, out _),
                    JTokenType.Boolean => bool.TryParse(Value, out _),
                    JTokenType.Date => System.DateTime.TryParse(Value, out _),
                    _ => true
                };
            }
            catch
            {
                IsValid = false;
            }

            OnPropertyChanged(nameof(IsValid));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}