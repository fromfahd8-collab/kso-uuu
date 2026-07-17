using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KSO.Modules
{
    public class Scheduler
    {
        private readonly DownloadManager _manager;
        private readonly ConfigManager _configManager;
        private Timer _timer;
        private bool _isRunning;
        private bool _shutdownTriggered = false; // عشان منعملش shutdown مرتين

        public Scheduler(DownloadManager manager)
        {
            _manager = manager;
            _configManager = new ConfigManager(Path.Combine(App.AppDataFolder, "config.json"));
        }

        public void Start()
        {
            _isRunning = true;
            _timer = new Timer(CheckSchedule, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        private void CheckSchedule(object state)
        {
            if (!_isRunning) return;

            _configManager.Load();
            var config = _configManager.Config;

            if (config.ScheduleTime.HasValue)
            {
                var now = DateTime.Now;
                var scheduledTime = config.ScheduleTime.Value;

                // نقارن الساعة والدقيقة بس. مش التاريخ
                bool timeMatch = now.Hour == scheduledTime.Hour && now.Minute == scheduledTime.Minute;

                // نشغل لو جه الوقت ولسه مفيش تحميل شغال
                if (timeMatch && !_manager.Downloads.Any(d => d.Status.Contains("Downloading")))
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        _manager.StatusMessage = "بدء الجدولة: تشغيل التحميلات";
                        ResumeAllPaused(); // 1. دي البديل بتاع StartAll
                    });

                    // 2. الاغلاق مرة واحدة بس
                    if(config.ShutdownOnComplete && !_shutdownTriggered)
                    {
                        _shutdownTriggered = true;
                        _manager.PropertyChanged += OnDownloadStatusChanged;
                    }
                }

                // نعمل Reset للـ shutdown لو عدى الوقت
                if(!timeMatch) _shutdownTriggered = false;
            }
        }

        private void ResumeAllPaused()
        {
            foreach(var item in _manager.Downloads.Where(d => d.Status == "Paused" || d.Status == "Queued"))
            {
                item.Resume();
            }
        }

        private void OnDownloadStatusChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(DownloadManager.StatusMessage))
            {
                bool allDone = !_manager.Downloads.Any(d => d.Status.Contains("Downloading"));
                if(allDone)
                {
                    _manager.PropertyChanged -= OnDownloadStatusChanged; // شيل الاشتراك
                    Task.Delay(5000).ContinueWith(_ => 
                    {
                        try { Process.Start("shutdown", "/s /t 60"); } catch{}
                    });
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _timer?.Dispose();
        }
    }
}