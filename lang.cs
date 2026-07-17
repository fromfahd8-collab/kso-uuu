using System;
using System.IO;
using System.Windows;
using Newtonsoft.Json;

namespace KSO.Helpers
{
    public static class Lang
    {
        private static LangData _ar = new();
        private static LangData _en = new();
        private static bool _isAr = true;

        public static void Load()
        {
            string path = Path.Combine(App.AppDataFolder, "lang.json");
            if (!File.Exists(path))
            {
                MessageBox.Show("lang.json not found");
                return;
            }
            string json = File.ReadAllText(path);
            dynamic data = JsonConvert.DeserializeObject(json);

            _ar = JsonConvert.DeserializeObject<LangData>(data.ar.ToString());
            _en = JsonConvert.DeserializeObject<LangData>(data.en.ToString());
        }

        public static void ToggleLanguage()
        {
            _isAr = !_isAr;
        }

        public static LangData Current => _isAr ? _ar : _en; // خليتها public عشان نحفظ اللغة في config

        // كل الخصائص هنا
        public static string Title => Current.Title;
        public static string Quality => Current.Quality;
        public static string Path => Current.Path;
        public static string SpeedLabel => Current.SpeedLabel;
        public static string ThreadCount => Current.ThreadCount;
        public static string Schedule => Current.Schedule;
        public static string SpeedLimit => Current.SpeedLimit;
        public static string Shutdown => Current.Shutdown;
        public static string About => Current.About;
        public static string AddDownload => Current.AddDownload;
        public static string Name => Current.Name;
        public static string Progress => Current.Progress;
        public static string Speed => Current.Speed;
        public static string Size => Current.Size;
        public static string Remaining => Current.Remaining;
        public static string Status => Current.Status;
        public static string Actions => Current.Actions;
        public static string DownloadsTab => Current.DownloadsTab;
        public static string StudyTab => Current.StudyTab;
        public static string BrowserTab => Current.BrowserTab;
        public static string ReDownload => Current.ReDownload;
        public static string SearchPlaceholder => Current.SearchPlaceholder;
        public static string Filter => Current.Filter;
        public static string ExportCsv => Current.ExportCsv;
        public static string ClearCache => Current.ClearCache;
        public static string ClearTemp => Current.ClearTemp;
        public static string Password => Current.Password;
        public static string Proxy => Current.Proxy;
        public static string SmartMode => Current.SmartMode;
        public static string AutoCompress => Current.AutoCompress;
        public static string CompressMode => Current.CompressMode;
        public static string DeleteOriginal => Current.DeleteOriginal;
        public static string NoDuplicate => Current.NoDuplicate;
        public static string Browser => Current.Browser;
        public static string DownloadQ => Current.DownloadQ;
        public static string EnterPassword => Current.EnterPassword;
        public static string WrongPassword => Current.WrongPassword;
        public static string AutoCapture => Current.AutoCapture;

        // بتوع السناب تيوب
        public static string SnapTubeTab => Current.SnapTubeTab;
        public static string SnapTubeSearch => Current.SnapTubeSearch;
        public static string SnapTubeDownload => Current.SnapTubeDownload;
        public static string SnapTubeNoResults => Current.SnapTubeNoResults;
        public static string SnapTubeVideos => Current.SnapTubeVideos;
        public static string SnapTubePlaylists => Current.SnapTubePlaylists;
        public static string SnapTubeChannels => Current.SnapTubeChannels;
        public static string SnapTubeAll => Current.SnapTubeAll;
        public static string SnapTubeSelected => Current.SnapTubeSelected;
        public static string SnapTubeCreateFolder => Current.SnapTubeCreateFolder;

        // بتوع صفحة حول الاساسية
        public static string AboutTitle => Current.AboutTitle;
        public static string AboutTab1 => Current.AboutTab1;
        public static string AboutTab2 => Current.AboutTab2;
        public static string AboutTab3 => Current.AboutTab3;
        public static string AboutProgrammer => Current.AboutProgrammer;
        public static string AboutVersion => Current.AboutVersion;
        public static string AboutDesc => Current.AboutDesc;

        // بتوع صفحة حول الجديدة - الاعمدة والنصوص
        public static string Key => Current.Key;
        public static string Function => Current.Function;
        public static string AboutDescLabel => Current.AboutDescLabel;
        public static string FeaturesNewLabel => Current.FeaturesNewLabel;
        public static string FeaturesAllLabel => Current.FeaturesAllLabel;
        public static string CompareLabel => Current.CompareLabel;
        public static string RequiredFilesLabel => Current.RequiredFilesLabel;
        public static string CompressModesLabel => Current.CompressModesLabel;
        public static string Feature1 => Current.Feature1;
        public static string Feature2 => Current.Feature2;
        public static string Feature3 => Current.Feature3;
        public static string Feature4 => Current.Feature4;
        public static string Feature5 => Current.Feature5;
        public static string Feature6 => Current.Feature6;
        public static string Feature7 => Current.Feature7;
        public static string Feature8 => Current.Feature8;
        public static string Feature9 => Current.Feature9;
        public static string Feature10 => Current.Feature10;
        public static string Feature11 => Current.Feature11;
        public static string Feature12 => Current.Feature12;
        public static string Feature13 => Current.Feature13;
        public static string Feature14 => Current.Feature14;
        public static string CompareIDM => Current.CompareIDM;
        public static string CompareSnap => Current.CompareSnap;
        public static string CompareKSO => Current.CompareKSO;
        public static string RequiredFile1 => Current.RequiredFile1;
        public static string RequiredFile2 => Current.RequiredFile2;
        public static string RequiredFile3 => Current.RequiredFile3;
        public static string CompressNormal => Current.CompressNormal;
        public static string CompressMax => Current.CompressMax;
        public static string CompressUltra => Current.CompressUltra;
    }

    // كلاس يطابق ملف json
    public class LangData
    {
        public string Title { get; set; }
        public string Quality { get; set; }
        public string Path { get; set; }
        public string SpeedLabel { get; set; }
        public string ThreadCount { get; set; }
        public string Schedule { get; set; }
        public string SpeedLimit { get; set; }
        public string Shutdown { get; set; }
        public string About { get; set; }
        public string AddDownload { get; set; }
        public string Name { get; set; }
        public string Progress { get; set; }
        public string Speed { get; set; }
        public string Size { get; set; }
        public string Remaining { get; set; }
        public string Status { get; set; }
        public string Actions { get; set; }
        public string DownloadsTab { get; set; }
        public string StudyTab { get; set; }
        public string BrowserTab { get; set; }
        public string ReDownload { get; set; }
        public string SearchPlaceholder { get; set; }
        public string Filter { get; set; }
        public string ExportCsv { get; set; }
        public string ClearCache { get; set; }
        public string ClearTemp { get; set; }
        public string Password { get; set; }
        public string Proxy { get; set; }
        public string SmartMode { get; set; }
        public string AutoCompress { get; set; }
        public string CompressMode { get; set; }
        public string DeleteOriginal { get; set; }
        public string NoDuplicate { get; set; }
        public string Browser { get; set; }
        public string DownloadQ { get; set; }
        public string EnterPassword { get; set; }
        public string WrongPassword { get; set; }
        public string AutoCapture { get; set; }

        // السناب تيوب
        public string SnapTubeTab { get; set; }
        public string SnapTubeSearch { get; set; }
        public string SnapTubeDownload { get; set; }
        public string SnapTubeNoResults { get; set; }
        public string SnapTubeVideos { get; set; }
        public string SnapTubePlaylists { get; set; }
        public string SnapTubeChannels { get; set; }
        public string SnapTubeAll { get; set; }
        public string SnapTubeSelected { get; set; }
        public string SnapTubeCreateFolder { get; set; }

        // بتوع صفحة حول الاساسية
        public string AboutTitle { get; set; }
        public string AboutTab1 { get; set; }
        public string AboutTab2 { get; set; }
        public string AboutTab3 { get; set; }
        public string AboutProgrammer { get; set; }
        public string AboutVersion { get; set; }
        public string AboutDesc { get; set; }

        // بتوع صفحة حول الجديدة - الاعمدة والنصوص
        public string Key { get; set; }
        public string Function { get; set; }
        public string AboutDescLabel { get; set; }
        public string FeaturesNewLabel { get; set; }
        public string FeaturesAllLabel { get; set; }
        public string CompareLabel { get; set; }
        public string RequiredFilesLabel { get; set; }
        public string CompressModesLabel { get; set; }
        public string Feature1 { get; set; }
        public string Feature2 { get; set; }
        public string Feature3 { get; set; }
        public string Feature4 { get; set; }
        public string Feature5 { get; set; }
        public string Feature6 { get; set; }
        public string Feature7 { get; set; }
        public string Feature8 { get; set; }
        public string Feature9 { get; set; }
        public string Feature10 { get; set; }
        public string Feature11 { get; set; }
        public string Feature12 { get; set; }
        public string Feature13 { get; set; }
        public string Feature14 { get; set; }
        public string CompareIDM { get; set; }
        public string CompareSnap { get; set; }
        public string CompareKSO { get; set; }
        public string RequiredFile1 { get; set; }
        public string RequiredFile2 { get; set; }
        public string RequiredFile3 { get; set; }
        public string CompressNormal { get; set; }
        public string CompressMax { get; set; }
        public string CompressUltra { get; set; }
    }
}