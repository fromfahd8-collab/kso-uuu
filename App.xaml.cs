using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace KSO
{
    public partial class App : Application
    {
        // 1. هنفك في AppData مش جنب الexe
        public static string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KSO");

        public static string YTdlpPath => Path.Combine(AppDataFolder, "yt-dlp.exe");
        public static string FfmpegPath => Path.Combine(AppDataFolder, "ffmpeg.exe");

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                Directory.CreateDirectory(AppDataFolder);

                // 2. فك كل الملفات اول مرة
                ExtractResource("KSO.Resources.ffmpeg.exe", FfmpegPath);
                ExtractResource("KSO.Resources.yt-dlp.exe", YTdlpPath);
                ExtractResource("KSO.Resources.config.json", Path.Combine(AppDataFolder, "config.json"));
                ExtractResource("KSO.Resources.lang.json", Path.Combine(AppDataFolder, "lang.json"));

                // 3. حمل اللغة
                Lang.Load();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حصل خطأ: {ex.Message}", "KSO Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void ExtractResource(string resourceName, string outputPath)
        {
            if (File.Exists(outputPath)) return; // لو موجود خلاص

            var assembly = Assembly.GetExecutingAssembly();
            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    MessageBox.Show($"لم يتم العثور على الملف: {resourceName}\nتأكد ان Build Action = EmbeddedResource", "خطأ");
                    return;
                }
                using (FileStream fileStream = new FileStream(outputPath, FileMode.Create))
                {
                    stream.CopyTo(fileStream);
                }
            }
        }
    }
}
