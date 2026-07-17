using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace KSO.Modules
{
    public class DownloadItem : INotifyPropertyChanged
    {
        private readonly string _url;
        private readonly int _threads;
        private readonly HttpClient _httpClient;
        private readonly ConfigData _config;
        private readonly string _tempDir;
        private string _outputPath;
        private string _fileName;
        private string _quality = "1080p";
        private string _downloadFolder;
        private long _totalBytes;
        private long _downloadedBytes;
        private double _speed;
        private string _status = "Pending";
        private bool _isPaused;
        private bool _isCancelled;
        private CancellationTokenSource _cts;
        private DateTime _lastUpdate;
        private long _lastBytes;
        private TimeSpan _timeLeft;
        private bool _compressed;
        private string _md5;
        private long _originalSize; // جديد
        private long _compressedSize; // جديد

        private readonly List<PartDownload> _parts = new();

        public string Url => _url;
        public int Threads => _threads;
        public string Quality { get => _quality; set { _quality = value; OnPropertyChanged(); } }
        public string DownloadFolder { get => _downloadFolder; set { _downloadFolder = value; _outputPath = Path.Combine(_downloadFolder, FileName); OnPropertyChanged(); } }
        public string FileName { get => _fileName; set { _fileName = value; _outputPath = Path.Combine(_downloadFolder, _fileName); OnPropertyChanged(); } }
        public long TotalBytes { get => _totalBytes; private set { _totalBytes = value; OnPropertyChanged(); OnPropertyChanged(nameof(SizeDisplay)); } }
        public long DownloadedBytes { get => _downloadedBytes; private set { _downloadedBytes = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProgressPercentage)); OnPropertyChanged(nameof(SizeDisplay)); } }
        public double Speed { get => _speed; private set { _speed = value; OnPropertyChanged(); OnPropertyChanged(nameof(SpeedDisplay)); } }
        public string Status { get => _status; private set { _status = value; OnPropertyChanged(); } }
        public TimeSpan TimeLeft { get => _timeLeft; private set { _timeLeft = value; OnPropertyChanged(); OnPropertyChanged(nameof(TimeLeftDisplay)); } }
        public bool Compressed { get => _compressed; set { _compressed = value; OnPropertyChanged(); OnPropertyChanged(nameof(SavedText)); } }
        public string Md5 { get => _md5; set { _md5 = value; OnPropertyChanged(); } }
        public long OriginalSize { get => _originalSize; private set { _originalSize = value; OnPropertyChanged(); OnPropertyChanged(nameof(SavedText)); } } // جديد
        public long CompressedSize { get => _compressedSize; private set { _compressedSize = value; OnPropertyChanged(); OnPropertyChanged(nameof(SavedText)); } } // جديد

        public double ProgressPercentage => TotalBytes > 0? (DownloadedBytes * 100.0 / TotalBytes) : 0;
        public string SizeDisplay => TotalBytes > 0? FormatSize(TotalBytes) : "?";
        public string SpeedDisplay => Speed > 0? FormatSize((long)Speed) + "/s" : "--";
        public string TimeLeftDisplay => Status == "Downloading"? TimeLeft.ToString(@"hh\:mm\:ss") : "--";

        // جديد: نص التوفير
        public string SavedText
        {
            get
            {
                if(!Compressed || OriginalSize == 0) return "";
                long saved = OriginalSize - CompressedSize;
                double percent = (saved * 100.0 / OriginalSize);
                return $"وفرنا {FormatSize(saved)} -{percent:F1}%";
            }
        }

        public ICommand PauseCommand { get; }
        public ICommand ResumeCommand { get; }
        public ICommand CancelCommand { get; }

        public DownloadItem(string url, int threads, HttpClient client, ConfigData config)
        {
            _url = url;
            _threads = threads;
            _httpClient = client;
            _config = config;
            _downloadFolder = config.DownloadPath;
            _tempDir = Path.Combine(Path.GetTempPath(), "KSO_Downloads", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);

            FileName = Path.GetFileName(new Uri(url).LocalPath);
            if (string.IsNullOrEmpty(FileName)) FileName = "download_" + DateTime.Now.Ticks;
            _outputPath = Path.Combine(_downloadFolder, FileName);
            Directory.CreateDirectory(_downloadFolder);

            PauseCommand = new RelayCommand(() => Pause(), () => Status == "Downloading");
            ResumeCommand = new RelayCommand(() => Resume(), () => Status == "Paused");
            CancelCommand = new RelayCommand(() => Cancel(), () => Status!= "Completed" && Status!= "Cancelled" && Status!= "Compressed");
        }

        public async Task StartDownloadAsync()
        {
            _cts = new CancellationTokenSource();
            _lastUpdate = DateTime.Now;
            try
            {
                Status = "Connecting...";
                using var headReq = new HttpRequestMessage(HttpMethod.Head, _url);
                var headResp = await _httpClient.SendAsync(headReq, _cts.Token);
                headResp.EnsureSuccessStatusCode();
                long? contentLength = headResp.Content.Headers.ContentLength;
                if (!contentLength.HasValue) { Status = "Error: No Content-Length"; return; }
                TotalBytes = contentLength.Value;
                OriginalSize = TotalBytes; // نخزن الحجم الاصلي

                long partSize = TotalBytes / _threads;
                for (int i = 0; i < _threads; i++)
                {
                    long start = i * partSize;
                    long end = (i == _threads - 1)? TotalBytes - 1 : start + partSize - 1;
                    _parts.Add(new PartDownload(i, start, end));
                }

                foreach (var part in _parts)
                {
                    string partFile = GetPartFilePath(part.Index);
                    if (File.Exists(partFile))
                    {
                        part.Downloaded = new FileInfo(partFile).Length;
                        DownloadedBytes += part.Downloaded;
                        if (part.Downloaded >= part.End - part.Start + 1) part.Completed = true;
                    }
                }

                Status = "Downloading...";
                await DownloadPartsAsync();

                if (!_isCancelled && _parts.All(p => p.Completed))
                {
                    Status = "Merging...";
                    MergeParts();
                    Status = "Completed";
                    DownloadedBytes = TotalBytes;
                    CleanupTemp();
                    _ = Task.Run(CalculateMd5);

                    // 1. اتغير: الضغط العادي هيستخدم ffmpeg من App
                    if (_config.AutoCompress && File.Exists(App.FfmpegPath))
                        _ = AutoCompressAsync(App.FfmpegPath);
                }
                else if (_isCancelled) Status = "Cancelled";
                else if (_isPaused) Status = "Paused";
                else Status = "Error";
            }
            catch (OperationCanceledException) { Status = "Cancelled"; }
            catch (Exception ex) { Status = $"Error: {ex.Message}"; }
        }

        // 2. اتغير: بقينا نستقبل المسار بدل ما ندور عليه
        private async Task AutoCompressAsync(string ffmpegPath)
        {
            try
            {
                Status = "Compressing...";
                string inputFile = _outputPath;
                string ext = Path.GetExtension(inputFile);
                string outputFile = Path.Combine(_downloadFolder, Path.GetFileNameWithoutExtension(FileName) + "_KSO_Auto" + ext);

                var settings = new Dictionary<string, (int crf, string preset)>
                {
                    ["Fast"] = (23, "ultrafast"),
                    ["Balanced"] = (28, "fast"),
                    ["Ultra"] = (30, "veryfast"), // ضفنا Ultra
                    ["Max"] = (32, "slow")
                };

                var (crf, preset) = settings.GetValueOrDefault(_config.CompressMode, settings["Balanced"]);

                // حفظ الحجم قبل الضغط
                OriginalSize = new FileInfo(inputFile).Length;

                bool success = await Task.Run(() => FfmpegHelper.CompressVideo(inputFile, outputFile, crf, preset, ffmpegPath)); // 3. بعتنا المسار
                if (success)
                {
                    CompressedSize = new FileInfo(outputFile).Length; // حجم بعد الضغط
                    Compressed = true;
                    FileName = Path.GetFileName(outputFile);
                    Status = "Compressed";
                    if (_config.DeleteOriginal && File.Exists(inputFile)) File.Delete(inputFile);
                }
                else Status = "Compress Failed";
            }
            catch { Status = "Compress Error"; }
        }

        private async Task DownloadPartsAsync()
        {
            var tasks = new List<Task>();
            var semaphore = new SemaphoreSlim(_threads);
            foreach (var part in _parts.Where(p =>!p.Completed))
                tasks.Add(DownloadPartAsync(part, semaphore));
            await Task.WhenAll(tasks);
        }

        private async Task DownloadPartAsync(PartDownload part, SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync(_cts.Token);
            try
            {
                string partFile = GetPartFilePath(part.Index);
                long start = part.Start + part.Downloaded;
                if (start > part.End) { part.Completed = true; return; }

                using var req = new HttpRequestMessage(HttpMethod.Get, _url);
                req.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, part.End);
                using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, _cts.Token);
                resp.EnsureSuccessStatusCode();
                using var stream = await resp.Content.ReadAsStreamAsync(_cts.Token);
                using var fs = new FileStream(partFile, FileMode.Append, FileAccess.Write, FileShare.None);

                byte[] buffer = new byte[8192];
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token)) > 0)
                {
                    if (_isCancelled || _isPaused) break;
                    await fs.WriteAsync(buffer, 0, bytesRead, _cts.Token);
                    part.Downloaded += bytesRead;
                    DownloadedBytes += bytesRead;

                    var now = DateTime.Now;
                    if ((now - _lastUpdate).TotalMilliseconds > 500)
                    {
                        var deltaBytes = DownloadedBytes - _lastBytes;
                        Speed = deltaBytes / (now - _lastUpdate).TotalSeconds;
                        _lastBytes = DownloadedBytes;
                        _lastUpdate = now;
                        if (Speed > 0) TimeLeft = TimeSpan.FromSeconds((TotalBytes - DownloadedBytes) / Speed);
                    }

                    if (_config.SpeedLimitKB > 0)
                        await Throttle(_config.SpeedLimitKB * 1024, bytesRead, _cts.Token);
                }

                if (!_isCancelled &&!_isPaused && part.Downloaded >= (part.End - part.Start + 1))
                    part.Completed = true;
            }
            catch { }
            finally { semaphore.Release(); }
        }

        private async Task Throttle(long limitBytesPerSec, int bytesJustRead, CancellationToken token)
        {
            var delay = (bytesJustRead * 1000.0 / limitBytesPerSec);
            if (delay > 1) await Task.Delay((int)delay, token);
        }

        private void MergeParts()
        {
            using var output = new FileStream(_outputPath, FileMode.Create, FileAccess.Write);
            foreach (var part in _parts.OrderBy(p => p.Index))
            {
                using var input = File.OpenRead(GetPartFilePath(part.Index));
                input.CopyTo(output);
            }
        }

        private void CalculateMd5()
        {
            try
            {
                using var fs = File.OpenRead(_outputPath);
                using var md5 = MD5.Create();
                var hash = md5.ComputeHash(fs);
                Md5 = BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
            catch { }
        }

        private string GetPartFilePath(int index) => Path.Combine(_tempDir, $"part_{index:D4}.tmp");
        private void CleanupTemp() { try { Directory.Delete(_tempDir, true); } catch { } }

        public void Pause() { _isPaused = true; Status = "Paused"; }
        public void Resume() { if (Status == "Paused") { _isPaused = false; Status = "Resuming..."; _ = StartDownloadAsync(); } }
        public void Cancel() { _isCancelled = true; _cts?.Cancel(); Status = "Cancelled"; CleanupTemp(); }

        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes; int order = 0;
            while (len >= 1024 && order < sizes.Length - 1) { order++; len /= 1024; }
            return $"{len:F1} {sizes[order]}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    internal class PartDownload { public int Index; public long Start; public long End; public long Downloaded; public bool Completed; public PartDownload(int i, long s, long e) { Index=i; Start=s; End=e; } }
    public class RelayCommand : ICommand { private readonly Action _execute; private readonly Func<bool> _canExecute; public RelayCommand(Action e, Func<bool> c=null){_execute=e;_canExecute=c;} public bool CanExecute(object p)=>_canExecute==null||_canExecute(); public void Execute(object p)=>_execute(); public event EventHandler CanExecuteChanged{add{}remove{}} }
}