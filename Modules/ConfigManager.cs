using Newtonsoft.Json;
using System;
using System.IO;
using KSO.Helpers;

namespace KSO.Modules
{
    public class ConfigData
    {
        public string DownloadPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        public int MaxThreads { get; set; } = 32;
        public bool ShutdownOnComplete { get; set; } = false;
        public string Language { get; set; } = "ar";
        public string Password { get; set; } = "";
        public bool HideOnMinimize { get; set; } = false;
        public DateTime? ScheduleTime { get; set; } = null;
        public int SpeedLimitKB { get; set; } = 0;
        public string Proxy { get; set; } = "";
        public bool SmartMode { get; set; } = true;
        public bool AutoCompress { get; set; } = true;
        public string CompressMode { get; set; } = "Ultra";
        public bool DeleteOriginal { get; set; } = true;
        public bool NoDuplicate { get; set; } = true;
        public string Quality { get; set; } = "1080p"; // هنا هتختار 480p لو عايز
        public bool WelcomeShown { get; set; } = false;
        public bool DarkMode { get; set; } = false; // ضفناها عشان ApplyTheme
    }

    public class ConfigManager
    {
        private readonly string _configPath;
        public ConfigData Config { get; private set; }

        public ConfigManager(string configPath)
        {
            _configPath = configPath;
            Load();
        }

        public void Load()
        {
            EnsureConfigExists();

            if (File.Exists(_configPath))
            {
                try 
                { 
                    var json = File.ReadAllText(_configPath);
                    Config = JsonConvert.DeserializeObject<ConfigData>(json) ?? new ConfigData();
                    
                    // فك التشفير بعد التحميل
                    if(!string.IsNullOrEmpty(Config.Password))
                        Config.Password = EncryptionHelper.Decrypt(Config.Password);
                    if(!string.IsNullOrEmpty(Config.Proxy))
                        Config.Proxy = EncryptionHelper.Decrypt(Config.Proxy);
                }
                catch { Config = new ConfigData(); }
            }
            else Config = new ConfigData();
        }

        public void Save() 
        {
            var cfgToSave = new ConfigData
            {
                DownloadPath = Config.DownloadPath,
                MaxThreads = Config.MaxThreads,
                ShutdownOnComplete = Config.ShutdownOnComplete,
                Language = Config.Language,
                Password = EncryptionHelper.Encrypt(Config.Password),
                HideOnMinimize = Config.HideOnMinimize,
                ScheduleTime = Config.ScheduleTime,
                SpeedLimitKB = Config.SpeedLimitKB,
                Proxy = EncryptionHelper.Encrypt(Config.Proxy),
                SmartMode = Config.SmartMode,
                AutoCompress = Config.AutoCompress,
                CompressMode = Config.CompressMode,
                DeleteOriginal = Config.DeleteOriginal,
                NoDuplicate = Config.NoDuplicate,
                Quality = Config.Quality,
                WelcomeShown = Config.WelcomeShown,
                DarkMode = Config.DarkMode
            };

            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!); 
            File.WriteAllText(_configPath, JsonConvert.SerializeObject(cfgToSave, Formatting.Indented));
        }

        private void EnsureConfigExists()
        {
            if (File.Exists(_configPath)) return;

            // 1. اتغير: هنفك من Resources المضمنة لو مش موجود
            var assembly = typeof(ConfigManager).Assembly;
            using var stream = assembly.GetManifestResourceStream("KSO.Resources.config.json");
            if (stream == null) return;

            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
            using var fileStream = new FileStream(_configPath, FileMode.Create);
            stream.CopyTo(fileStream);
        }
    }
}