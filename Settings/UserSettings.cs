// Settings/UserSettings.cs - User settings management
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ModernGallery.Settings
{
    public class UserSettings
    {
        // General settings
        public bool ScanSubdirectories { get; set; } = true;
        public int ThumbnailSize { get; set; } = 256;
        public string DefaultSaveLocation { get; set; }
        public List<string> RecentDirectories { get; set; } = new List<string>();
        
        // AI settings
        public bool UseGpuAcceleration { get; set; } = true;
        public int MaximumFacesPerImage { get; set; } = 20;
        public float MinimumFaceConfidence { get; set; } = 0.6f;
        
        // UI settings
        public int DefaultChatPanelWidth { get; set; } = 300;
        public bool ShowNotificationsOnCompletion { get; set; } = true;
        public bool DarkMode { get; set; } = false;
        
        // System settings
        public int MaximumThreads { get; set; } = Math.Max(1, Environment.ProcessorCount - 1);
        public string ModelsDirectory { get; set; }
        
        // Singleton instance
        private static UserSettings _instance;
        private static readonly string _settingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ModernGallery",
            "settings.json");
        
        public static UserSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }
                
                return _instance;
            }
        }
        
        private UserSettings()
        {
            // Set default models directory
            ModelsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ModernGallery",
                "Models");
            
            // Set default save location
            DefaultSaveLocation = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "ModernGallery");
            
            // Ensure directories exist
            Directory.CreateDirectory(ModelsDirectory);
            Directory.CreateDirectory(DefaultSaveLocation);
        }
        
        private static UserSettings Load()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<UserSettings>(json);
                    
                    // Ensure required directories exist
                    EnsureDirectoriesExist(settings);
                    
                    return settings;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error loading user settings");
            }
            
            // Create and save default settings if loading failed
            var defaultSettings = new UserSettings();
            defaultSettings.Save();
            
            return defaultSettings;
        }
        
        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(_settingsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error saving user settings");
            }
        }
        
        private static void EnsureDirectoriesExist(UserSettings settings)
        {
            if (!Directory.Exists(settings.ModelsDirectory))
            {
                Directory.CreateDirectory(settings.ModelsDirectory);
            }
            
            if (!Directory.Exists(settings.DefaultSaveLocation))
            {
                Directory.CreateDirectory(settings.DefaultSaveLocation);
            }
        }
    }
}