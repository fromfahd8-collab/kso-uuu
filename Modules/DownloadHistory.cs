using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KSO.Modules
{
    public class HistoryItem
    {
        public string Url { get; set; }
        public string FileName { get; set; }
        public long TotalBytes { get; set; }
        public long DownloadedBytes { get; set; }
        public long OriginalSize { get; set; } // جديد للضغط
        public long CompressedSize { get; set; } // جديد للضغط
        public int Threads { get; set; }
        public string Status { get; set; }
        public DateTime Date { get; set; }
        public string Md5 { get; set; }
        public string Quality { get; set; } // جديد
    }

    public class DownloadHistory
    {
        private readonly string _historyPath;

        public DownloadHistory(string historyPath = null)
        {
            _historyPath = historyPath ?? Path.Combine(App.AppDataFolder, "downloads_history.json");
        }

        public List<HistoryItem> Load()
        {
            if (File.Exists(_historyPath))
            {
                try { return JsonConvert.DeserializeObject<List<HistoryItem>>(File.ReadAllText(_historyPath)) ?? new List<HistoryItem>(); }
                catch { return new List<HistoryItem>(); }
            }
            return new List<HistoryItem>();
        }

        public void Add(DownloadItem item)
        {
            var history = Load();
            history.RemoveAll(h => h.Url == item.Url);
            
            history.Add(new HistoryItem
            {
                Url = item.Url,
                FileName = item.FileName,
                TotalBytes = item.TotalBytes,
                DownloadedBytes = item.DownloadedBytes,
                OriginalSize = item.OriginalSize, // جديد
                CompressedSize = item.CompressedSize, // جديد
                Threads = item.Threads,
                Status = item.Status,
                Date = DateTime.Now,
                Md5 = item.Md5,
                Quality = item.Quality // جديد
            });
            
            if(history.Count > 1000) history = history.Skip(history.Count - 1000).ToList();
            Save(history);
        }

        public void Save(List<DownloadItem> items)
        {
            var history = items.Select(item => new HistoryItem
            {
                Url = item.Url,
                FileName = item.FileName,
                TotalBytes = item.TotalBytes,
                DownloadedBytes = item.DownloadedBytes,
                OriginalSize = item.OriginalSize,
                CompressedSize = item.CompressedSize,
                Threads = item.Threads,
                Status = item.Status,
                Date = DateTime.Now,
                Md5 = item.Md5,
                Quality = item.Quality
            }).ToList();
            Save(history);
        }

        public void Save(IEnumerable<HistoryItem> items)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_historyPath));
            File.WriteAllText(_historyPath, JsonConvert.SerializeObject(items, Formatting.Indented));
        }

        public void ExportToCsv(string path)
        {
            var items = Load();
            using var writer = new StreamWriter(path);
            writer.WriteLine("URL,FileName,OriginalSize,CompressedSize,Saved,Status,Date,Quality");
            foreach (var item in items)
            {
                long saved = item.OriginalSize - item.CompressedSize;
                double percent = item.OriginalSize > 0 ? (saved * 100.0 / item.OriginalSize) : 0;
                writer.WriteLine($"\"{item.Url}\",\"{item.FileName}\",{item.OriginalSize},{item.CompressedSize},{saved} ({percent:F1}%),\"{item.Status}\",{item.Date:yyyy-MM-dd HH:mm:ss},\"{item.Quality}\"");
            }
        }
    }
}