using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using AthStitcherGUI.ViewModels;

namespace AthStitcherGUI
{
    public partial class JsonEditorWindow : Window
    {
        public ObservableCollection<JsonNodeViewModel> RootNodes { get; set; } = new();
        private string _jsonFilePath = null;

        public JsonEditorWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void LoadJson(string filePath)
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var token = JToken.Parse(json);
                RootNodes.Clear();

                if (token is JObject obj)
                {
                    foreach (var prop in obj.Properties())
                    {
                        RootNodes.Add(ConvertJTokenToViewModel(prop.Name, prop.Value));
                    }
                }
            }
        }

        private JsonNodeViewModel ConvertJTokenToViewModel(string key, JToken token)
        {
            var node = new JsonNodeViewModel { Key = key, ExpectedType = token.Type };

            if (token is JObject obj)
            {
                foreach (var prop in obj.Properties())
                {
                    node.Children.Add(ConvertJTokenToViewModel(prop.Name, prop.Value));
                }
            }
            else if (token is JArray array)
            {
                for (int i = 0; i < array.Count; i++)
                {
                    node.Children.Add(ConvertJTokenToViewModel($"[{i}]", array[i]));
                }
            }
            else
            {
                node.Value = token.ToString();
            }

            return node;
        }

        private JToken ConvertViewModelToJToken(JsonNodeViewModel node)
        {
            if (node.HasChildren)
            {
                if (node.Key.StartsWith("["))
                {
                    var array = new JArray();
                    foreach (var child in node.Children)
                        array.Add(ConvertViewModelToJToken(child));
                    return array;
                }
                else
                {
                    var obj = new JObject();
                    foreach (var child in node.Children)
                        obj[child.Key] = ConvertViewModelToJToken(child);
                    return obj;
                }
            }
            else
            {
                return node.ExpectedType switch
                {
                    JTokenType.Integer => int.TryParse(node.Value, out var i) ? new JValue(i) : new JValue(node.Value),
                    JTokenType.Float => double.TryParse(node.Value, out var d) ? new JValue(d) : new JValue(node.Value),
                    JTokenType.Boolean => bool.TryParse(node.Value, out var b) ? new JValue(b) : new JValue(node.Value),
                    JTokenType.Date => System.DateTime.TryParse(node.Value, out var dt) ? new JValue(dt) : new JValue(node.Value),
                    _ => new JValue(node.Value)
                };
            }
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                _jsonFilePath = dlg.FileName;
                var mp4Path = Path.ChangeExtension(_jsonFilePath, ".mp4");
                if (!File.Exists(mp4Path))
                {
                    MessageBox.Show($"No matching .mp4 file found for {_jsonFilePath}.", "Missing MP4", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                LoadJson(_jsonFilePath);
            }
        }

        private void MenuSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_jsonFilePath))
            {
                var dlg = new SaveFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*"
                };
                if (dlg.ShowDialog() == true)
                {
                    _jsonFilePath = dlg.FileName;
                }
                else
                {
                    return;
                }
            }

            var rootJson = new JObject();
            foreach (var node in RootNodes)
            {
                rootJson[node.Key] = ConvertViewModelToJToken(node);
            }

            File.WriteAllText(_jsonFilePath, rootJson.ToString(Newtonsoft.Json.Formatting.Indented));
            MessageBox.Show("JSON saved.");
            Close();
        }

        private void MenuCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
