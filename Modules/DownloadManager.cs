using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using KSO.Helpers;

namespace KSO.Modules
{
    public class DownloadManager : INotifyPropertyChanged
    {
        private readonly ObservableCollection<DownloadItem> _downloads;
        private readonly DownloadHistory _history;
        private readonly ConfigData _config;
        private HttpClient _httpClient;
        private string _statusMessage = "جاهز";
        private DateTime _lastTestTime = DateTime.MinValue;

        public double _lastSpeedTest = 20.0; // 1. خليتها public عشان MainWindow يقراها

        public string StatusMessage
        {
            get => _statusMessage;
            private set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
        }

        public ObservableCollection<DownloadItem> Downloads => _downloads;
        public event Action<int, string> RowAdded;

        public DownloadManager(ObservableCollection<DownloadItem> downloads, DownloadHistory history, ConfigData config, HttpClient httpClient)
        {
            _downloads = downloads;
            _history = history;
            _config = config;
            _httpClient = httpClient;
        }

        public async Task UpdateSpeedCacheAsync() // كاش 30 دقيقة
        {
            if(_config.SmartMode && (DateTime.Now - _lastTestTime).TotalMinutes > 30)
            {
                StatusMessage = "جاري اختبار السرعة...";
                _lastSpeedTest = await SpeedTest.MeasureDownloadSpeedAsync();
                _lastTestTime = DateTime.Now;
                StatusMessage = $"تم تحديث السرعة: {_lastSpeedTest:F1} Mbps";
            }
        }

        public void AddDownload(string url, string quality, int threads, string outputPath)
        {
            if (_config.NoDuplicate && _downloads.Any(d => d.Url == url))
            {
                StatusMessage = "التحميل موجود بالفعل";
                return;
            }

            // التقسيمة الذكية الجديدة
            int downloadingCount = _downloads.Count(d=>d.Status.Contains("Downloading")) + 1;
            int finalThreads = _config.SmartMode 
                ? CalculateOptimalThreads(_lastSpeedTest, downloadingCount)
                : Math.Clamp(_config.MaxThreads, 16, 1000000);

            var item = new DownloadItem(url, finalThreads, _httpClient, _config)
            {
                Quality = quality,
                DownloadFolder = outputPath
            };
            
            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(DownloadItem.Status))
                {
                    StatusMessage = $"{item.FileName}: {item.Status}";
                    if(item.Status == "Completed" || item.Status == "Compressed")
                        _history.Add(item);
                }
            };

            _downloads.Add(item);
            StatusMessage = $"بدء تحميل: {item.FileName} - {finalThreads:N0} خيط - {quality}";
            RowAdded?.Invoke(_downloads.Count - 1, item.FileName);
            
            if(url.Contains("youtube.com") || url.Contains("youtu.be") || url.Contains("tiktok.com") || url.Contains("facebook.com"))
                _ = DownloadWithYtDlpAsync(item, url, quality, finalThreads, outputPath);
            else
                _ = item.StartDownloadAsync();
        }

        private async Task DownloadWithYtDlpAsync(DownloadItem item, string url, string quality, int threads, string outputPath)
        {
            try
            {
                item.Status = "Connecting...";
                string ytDlpPath = App.YtDlpPath; // 2. اتغير: بقينا نستخدم المسار من App
                string ffmpegPath = App.FfmpegPath; // 3. عشان الدمج

                if(!File.Exists(ytDlpPath))
                {
                    item.Status = "Error: yt-dlp.exe not found in Resources";
                    return;
                }
                if(!File.Exists(ffmpegPath))
                {
                    item.Status = "Error: ffmpeg.exe not found in Resources";
                    return;
                }

                string format = GetYtDlpFormat(quality);
                
                string args = $"-N {threads} " + 
                              $"--ffmpeg-location \"{ffmpegPath}\" " + // 4. ضفنا مكان ffmpeg عشان الدمج
                              $"-f \"{format}\" " +
                              $"--merge-output-format mp4 " +
                              $"--no-playlist " +
                              $"--newline " +
                              $"--progress " +
                              $"--concurrent-fragments {threads} " + // مهمة عشان الخيوط تشتغل
                              $"-o \"{Path.Combine(outputPath, "%(title)s.%(ext)s")}\" \"{url}\"";

                var psi = new ProcessStartInfo
                {
                    FileName = ytDlpPath,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = psi };
                process.OutputDataReceived += (s, e) => 
                {
                    if(e.Data != null && (e.Data.Contains("%") || e.Data.Contains("MiB/s")))
                        item.Status = e.Data.Trim();
                };
                process.ErrorDataReceived += (s, e) => { if(e.Data != null) item.Status = e.Data; };
                
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();

                if(process.ExitCode == 0)
                {
                    item.Status = "Completed";
                    var files = Directory.GetFiles(outputPath).OrderByDescending(f => new FileInfo(f).CreationTime).FirstOrDefault();
                    if(files != null) item.FileName = Path.GetFileName(files);
                    
                    if (_config.AutoCompress && File.Exists(ffmpegPath)) // 5. اتغير: بنتأكد من المسار الجديد
                        await item.AutoCompressAsync(ffmpegPath); // 6. ابعت المسار للضغط
                }
                else item.Status = $"Error: yt-dlp exited with code {process.ExitCode}";
            }
            catch(Exception ex) { item.Status = $"Error: {ex.Message}"; }
        }

        private string GetYtDlpFormat(string quality)
        {
            return quality switch
            {
                "best" => "bestvideo+bestaudio/best",
                "8K" => "bestvideo[height<=4320]+bestaudio/best",
                "4K" => "bestvideo[height<=2160]+bestaudio/best",
                "2K" => "bestvideo[height<=1440]+bestaudio/best",
                "1080p" => "bestvideo[height<=1080]+bestaudio/best",
                "720p" => "bestvideo[height<=720]+bestaudio/best",
                "480p" => "bestvideo[height<=480]+bestaudio/best",
                "360p" => "bestvideo[height<=360]+bestaudio/best",
                "240p" => "bestvideo[height<=240]+bestaudio/best",
                "144p" => "bestvideo[height<=144]+bestaudio/best",
                "audio" => "bestaudio/best",
                _ => "bestvideo+bestaudio/best"
            };
        }

        public void StopAll()
        {
            foreach (var item in _downloads)
                item.Cancel();
            StatusMessage = "تم إيقاف جميع التحميلات";
        }

        public void PauseAll()
        {
            foreach (var item in _downloads.Where(i => i.Status.Contains("Downloading")))
                item.Pause();
        }

        public void ResumeAll()
        {
            foreach (var item in _downloads.Where(i => i.Status == "Paused"))
                item.Resume();
        }

        // التقسيمة الذكية
        public static int CalculateOptimalThreads(double speedMbps, int totalFiles)
        {
            int totalThreads = (int)(speedMbps / 2.0); // كل 2 ميجا = خيط
            if(totalThreads < 16) totalThreads = 16;
            if(totalThreads > 1000000) totalThreads = 1000000;

            int perFile = totalThreads / Math.Max(1, totalFiles);
            return Math.Clamp(perFile, 16, 1000000);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}