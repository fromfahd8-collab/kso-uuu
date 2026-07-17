using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace KSO.Modules
{
    public class LocalizationData
    {
        public string Title { get; set; } = "KSO Download Turbo Ultra V1.0 PRO";
        public string Quality { get; set; } = "الجودة:";
        public string Path { get; set; } = "المسار:";
        public string SpeedLabel { get; set; } = "السرعة:";
        public string ThreadCount { get; set; } = "الخيوط:";
        public string Schedule { get; set; } = "جدولة:";
        public string SpeedLimit { get; set; } = "حد السرعة:";
        public string Shutdown { get; set; } = "إغلاق الجهاز";
        public string About { get; set; } = "حول";
        public string AddDownload { get; set; } = "إضافة تحميل";
        public string Name { get; set; } = "الاسم";
        public string Progress { get; set; } = "التقدم";
        public string Speed { get; set; } = "السرعة";
        public string Size { get; set; } = "الحجم";
        public string Remaining { get; set; } = "الوقت المتبقي";
        public string Status { get; set; } = "الحالة";
        public string Actions { get; set; } = "إجراءات";
        public string DownloadsTab { get; set; } = "التحميلات";
        public string StudyTab { get; set; } = "المذاكرة";
        public string BrowserTab { get; set; } = "المتصفح";
        public string ReDownload { get; set; } = "إعادة تحميل";
        public string SearchPlaceholder { get; set; } = "بحث في التحميلات...";
        public string Filter { get; set; } = "فلترة:";
        public string ExportCsv { get; set; } = "تصدير CSV";
        public string ClearCache { get; set; } = "مسح الكاش";
        public string ClearTemp { get; set; } = "تنظيف مؤقت";
        public string Password { get; set; } = "كلمة السر";
        public string Proxy { get; set; } = "وكيل:";
        public string SmartMode { get; set; } = "وضع ذكي";
        public string AutoCompress { get; set; } = "ضغط تلقائي";
        public string CompressMode { get; set; } = "وضع الضغط";
        public string DeleteOriginal { get; set; } = "حذف الأصلي";
        public string NoDuplicate { get; set; } = "منع التكرار";
        public string Browser { get; set; } = "المتصفح";
        public string DownloadQ { get; set; } = "هل تريد تحميل هذا الرابط؟";
        public string EnterPassword { get; set; } = "أدخل كلمة السر";
        public string WrongPassword { get; set; } = "كلمة سر خاطئة";
        public string AutoCapture { get; set; } = "تم العثور على فيديو. هل تريد إضافته للتحميل؟";
    }

    public class LocalizationManager
    {
        private readonly string _langPath;
        private Dictionary<string, LocalizationData> _languages = new(); // 1. تهيئة عشان ميعملش Null
        private string _currentCode = "ar";
        public LocalizationData CurrentLang { get; private set; }
        public string CurrentCode => _currentCode;

        public LocalizationManager(string langPath)
        {
            _langPath = langPath;
            LoadLanguages();
            SetLanguage(ConfigManagerStatic.LoadLangCode(_langPath));
        }

        private void LoadLanguages()
        {
            EnsureLangExists();

            if (File.Exists(_langPath))
            {
                try { _languages = JsonConvert.DeserializeObject<Dictionary<string, LocalizationData>>(File.ReadAllText(_langPath))?? new(); }
                catch { _languages = new Dictionary<string, LocalizationData>(); }
            }

            if(_languages.Count == 0)
                CreateDefaultLanguages();
        }

        private void CreateDefaultLanguages()
        {
            _languages =
            {
                ["ar"] = new LocalizationData(),
                ["en"] = new LocalizationData
                {
                    Title = "KSO Download Turbo Ultra V1.0 PRO",
                    Quality = "Quality:", Path = "Path:", SpeedLabel = "Speed:", ThreadCount = "Threads:",
                    Schedule = "Schedule:", SpeedLimit = "Speed Limit:", Shutdown = "Shutdown PC", About = "About",
                    AddDownload = "Add Download", Name = "Name", Progress = "Progress", Speed = "Speed", Size = "Size",
                    Remaining = "Remaining", Status = "Status", Actions = "Actions", DownloadsTab = "Downloads",
                    StudyTab = "Study", BrowserTab = "Browser", ReDownload = "Re-Download", SearchPlaceholder = "Search downloads...",
                    Filter = "Filter:", ExportCsv = "Export CSV", ClearCache = "Clear Cache", ClearTemp = "Clear Temp",
                    Password = "Password", Proxy = "Proxy:", SmartMode = "Smart Mode", AutoCompress = "Auto Compress",
                    CompressMode = "Compress Mode", DeleteOriginal = "Delete Original", NoDuplicate = "No Duplicate",
                    Browser = "Browser", DownloadQ = "Download this link?", EnterPassword = "Enter password",
                    WrongPassword = "Wrong password", AutoCapture = "Video found. Add to downloads?"
                }
            };
            SaveLanguages();
        }

        public void SetLanguage(string code)
        {
            if (_languages.ContainsKey(code))
            {
                _currentCode = code;
                CurrentLang = _languages[code];
            }
            else CurrentLang = _languages["ar"];
        }

        public void ToggleLanguage()
        {
            SetLanguage(_currentCode == "ar"? "en" : "ar");
            ConfigManagerStatic.SaveLangCode(_langPath, _currentCode);
        }

        private void SaveLanguages() => File.WriteAllText(_langPath, JsonConvert.SerializeObject(_languages, Formatting.Indented));

        private void EnsureLangExists()
        {
            if (File.Exists(_langPath)) return;
            var assembly = typeof(LocalizationManager).Assembly;
            using var stream = assembly.GetManifestResourceStream("KSO.Resources.lang.json");
            if (stream == null) return;
            Directory.CreateDirectory(Path.GetDirectoryName(_langPath)!); // 2.! عشان ميعملش warning
            using var fileStream = new FileStream(_langPath, FileMode.Create);
            stream.CopyTo(fileStream);
        }
    }

    internal static class ConfigManagerStatic
    {
        public static string LoadLangCode(string langPath)
        {
            string configPath = Path.Combine(Path.GetDirectoryName(langPath)!, "config.json"); // 3.!
            if(File.Exists(configPath))
                try { var cfg = JsonConvert.DeserializeObject<ConfigData>(File.ReadAllText(configPath)); return cfg?.Language?? "ar"; } catch{}
            return "ar";
        }
        public static void SaveLangCode(string langPath, string code)
        {
            string configPath = Path.Combine(Path.GetDirectoryName(langPath)!, "config.json"); // 3.!
            if(File.Exists(configPath))
                try { var cfg = JsonConvert.DeserializeObject<ConfigData>(File.ReadAllText(configPath)); if(cfg!= null){cfg.Language = code; File.WriteAllText(configPath, JsonConvert.SerializeObject(cfg, Formatting.Indented));} } catch{}
        }
    }
}