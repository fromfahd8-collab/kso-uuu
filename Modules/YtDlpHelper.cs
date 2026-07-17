using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace KSO.Modules
{
    public static class YtDlpHelper
    {
        private static string YtDlpPath => Path.Combine(App.AppDataFolder, "yt-dlp.exe");

        // 1. تحميل بفيديو/بلاي ليست بالخيوط
        public static async Task DownloadAsync(string url, string outputPath, string quality, int threads, Action<string> onProgress = null)
        {
            string format = GetFormat(quality); // استخدمنا الفانكشن الجديدة
            
            string args = $"-N {threads} " +
                          $"-f \"{format}\" " + // مفيش مسافة
                          $"--merge-output-format mp4 " +
                          $"--no-playlist " +
                          $"--progress " +
                          $"--newline " +
                          $"-o \"{Path.Combine(outputPath, "%(title)s.%(ext)s")}\" \"{url}\"";

            var psi = new ProcessStartInfo
            {
                FileName = YtDlpPath,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.OutputDataReceived += (s, e) => { if(e.Data != null) onProgress?.Invoke(e.Data); };
            process.ErrorDataReceived += (s, e) => { if(e.Data != null) onProgress?.Invoke(e.Data); };
            
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
        }

        // 2. جيب معلومات الفيديو
        public static async Task<string> GetInfoAsync(string url)
        {
            var psi = new ProcessStartInfo
            {
                FileName = YtDlpPath,
                Arguments = $"--get-id --get-title --get-duration --get-thumbnail --get-filesize \"{url}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            return output;
        }

        // 3. دي الجديدة: بتحول الجودة لصيغة yt-dlp صح
        private static string GetFormat(string quality)
        {
            return quality switch
            {
                "best" => "bestvideo+bestaudio/best",
                "8K" => "bestvideo[height<=4320]+bestaudio/best",
                "4K" => "bestvideo[height<=2160]+bestaudio/best",
                "2K" => "bestvideo[height<=1440]+bestaudio/best",
                "1080p" => "bestvideo[height<=1080]+bestaudio/best",
                "720p" => "bestvideo[height<=720]+bestaudio/best",
                "480p" => "bestvideo[height<=480]+bestaudio/best", // دي كانت بايظة عندك
                "360p" => "bestvideo[height<=360]+bestaudio/best",
                "240p" => "bestvideo[height<=240]+bestaudio/best",
                "144p" => "bestvideo[height<=144]+bestaudio/best",
                "audio" => "bestaudio/best",
                _ => "bestvideo+bestaudio/best" // default
            };
        }
    }
}