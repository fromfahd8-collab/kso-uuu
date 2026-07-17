using System;
using System.Diagnostics;
using System.IO;

namespace KSO.Modules
{
    public static class FfmpegHelper
    {
        // 1. شلنا المسار الثابت. بقينا نستقبله كـ parameter

        public static bool CheckFfmpeg(string ffmpegPath)
        {
            if (!File.Exists(ffmpegPath)) return false;
            try
            {
                using var p = Process.Start(new ProcessStartInfo(ffmpegPath, "-version")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                p.WaitForExit(1000);
                return true; // لو فتح خلاص
            }
            catch { return false; }
        }

        public static bool CompressVideo(string inputPath, string outputPath, int crf = 28, string preset = "fast", string ffmpegPath = null)
        {
            if (ffmpegPath == null) ffmpegPath = App.FfmpegPath; // 2. fallback للمسار من App
            if (!File.Exists(ffmpegPath)) return false;
            
            // H.264 افضل للتوافق بدل x265
            var args = $"-i \"{inputPath}\" -vcodec libx264 -crf {crf} -preset {preset} -acodec aac -b:a 128k -threads 0 -y \"{outputPath}\"";
            return RunFfmpeg(ffmpegPath, args);
        }

        public static bool ConvertToMp3(string inputPath, string outputPath, string ffmpegPath = null)
        {
            if (ffmpegPath == null) ffmpegPath = App.FfmpegPath;
            if (!File.Exists(ffmpegPath)) return false;
            return RunFfmpeg(ffmpegPath, $"-i \"{inputPath}\" -vn -b:a 320k \"{outputPath}\"");
        }

        public static bool ConvertTo720p(string inputPath, string outputPath, string ffmpegPath = null)
        {
            if (ffmpegPath == null) ffmpegPath = App.FfmpegPath;
            if (!File.Exists(ffmpegPath)) return false;
            return RunFfmpeg(ffmpegPath, $"-i \"{inputPath}\" -vf scale=1280:720 -c:a copy -y \"{outputPath}\"");
        }

        public static bool TrimFirst30Sec(string inputPath, string outputPath, string ffmpegPath = null)
        {
            if (ffmpegPath == null) ffmpegPath = App.FfmpegPath;
            if (!File.Exists(ffmpegPath)) return false;
            return RunFfmpeg(ffmpegPath, $"-i \"{inputPath}\" -ss 0 -t 30 -c copy -y \"{outputPath}\"");
        }

        public static bool MergeVideoAudio(string videoPath, string audioPath, string outputPath, string ffmpegPath = null)
        {
            if (ffmpegPath == null) ffmpegPath = App.FfmpegPath;
            if (!File.Exists(ffmpegPath)) return false;
            return RunFfmpeg(ffmpegPath, $"-i \"{videoPath}\" -i \"{audioPath}\" -c:v copy -c:a aac -map 0:v -map 1:a -shortest -y \"{outputPath}\"");
        }

        private static bool RunFfmpeg(string ffmpegPath, string arguments)
        {
            try
            {
                using var process = new Process();
                process.StartInfo.FileName = ffmpegPath; // 3. استخدم المسار اللي جاي
                process.StartInfo.Arguments = arguments;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch { return false; }
        }
    }
}