using System;
using System.IO;
using System.Reflection;
using System.Windows;
using KSO.Helpers;

namespace KSO
{
    public partial class App : Application
    {
        // 1. فولدر Resources جوه البرنامج. ده اللي هنفك فيه
        public static string AppDataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");

        // 2. مسارات مباشرة عشان نستخدمها في كل البرنامج
        public static string YtDlpPath => Path.Combine(AppDataFolder, "yt-dlp.exe");
        public static string FfmpegPath => Path.Combine(AppDataFolder, "ffmpeg.exe");

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                Directory.CreateDirectory(AppDataFolder);

                // 3. فك كل الملفات المضمنة في Resources اول مرة بس
                ExtractResource("KSO.Resources.ffmpeg.exe", FfmpegPath);
                ExtractResource("KSO.Resources.yt-dlp.exe", YtDlpPath);
                ExtractResource("KSO.Resources.config.json", Path.Combine(AppDataFolder, "config.json"));
                ExtractResource("KSO.Resources.lang.json", Path.Combine(AppDataFolder, "lang.json"));

                // 4. حمل اللغة بعد ما فكيت الملف
                Lang.Load(); 
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في بدء التشغيل: {ex.Message}", "KSO Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExtractResource(string resourceName, string outputPath)
        {
            // لو الملف موجود اصلا متعملش Overwrite عشان منبوظش الاعدادات
            if (File.Exists(outputPath)) return;

            var assembly = Assembly.GetExecutingAssembly();
            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    MessageBox.Show($"خطأ: لم يتم العثور على الملف المضمن {resourceName}\nاتأكد ان Build Action = EmbeddedResource", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
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