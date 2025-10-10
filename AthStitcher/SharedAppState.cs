using System;
using System.IO;
using System.Text.Json;

namespace AthStitcherGUI
{
    public static class SharedAppState
    {
        private const string Company = "Sportronics";
        private const string Product = "AthStitcher";
        private const string SettingsFileName = "settings.json";

        private static readonly string AppDataRoot =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Company, Product);

        private static readonly string SettingsPath = Path.Combine(AppDataRoot, SettingsFileName);

        // Global folder used by all file/folder pickers
        public static string? GlobalFolder { get; private set; }

        static SharedAppState()
        {
            Load();
        }

        public static void SetGlobalFolder(string? folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                return;
            }
            GlobalFolder = folder;
            Save();
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var model = JsonSerializer.Deserialize<SharedSettings>(json);
                    if (model != null && !string.IsNullOrWhiteSpace(model.GlobalFolder) && Directory.Exists(model.GlobalFolder))
                    {
                        GlobalFolder = model.GlobalFolder;
                    }
                }
            }
            catch
            {
                // Ignore errors; start clean
                GlobalFolder = null;
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(AppDataRoot);
                var json = JsonSerializer.Serialize(new SharedSettings { GlobalFolder = GlobalFolder }, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // Ignore save errors
            }
        }

        private class SharedSettings
        {
            public string? GlobalFolder { get; set; }
        }
    }
}
