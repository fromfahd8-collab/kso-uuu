using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace KSO
{
    public partial class SnapTubeTab : UserControl
    {
        private readonly MainWindow _main;
        public ObservableCollection<SnapItem> Results { get; } = new();
        private string _lastQuery = "";
        private string _currentTab = "videos";
        private int _offset = 0;
        private bool _isLoading = false;
        // 1. ظبطنا مسار yt-dlp الجديد
        private static readonly string AppFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");

        public SnapTubeTab(MainWindow main)
        {
            InitializeComponent();
            _main = main;
            lvSnapResults.ItemsSource = Results;
            Results.CollectionChanged += (s, e) => txtSelectedCount.Text = Results.Count(x => x.IsSelected).ToString();
        }

        private async void BtnSearchCourse_Click(object sender, RoutedEventArgs e)
        {
            _lastQuery = txtCourseSearch.Text.Trim();
            if (string.IsNullOrEmpty(_lastQuery)) return;
            Results.Clear();
            _offset = 0;
            await SearchAsync(_lastQuery, _currentTab);
        }

        private async Task SearchAsync(string query, string type)
        {
            if (_isLoading) return;
            _isLoading = true;
            btnSearchCourse.IsEnabled = false;
            btnSearchCourse.Content = "جاري البحث...";
            try
            {
                string searchArg = type switch
                {
                    "videos" => $"ytsearch10:{query}",
                    "playlists" => $"ytsearch10:{query} playlist",
                    "channels" => $"ytsearch10:{query} channel",
                    _ => $"ytsearch10:{query}"
                };

                var psi = new ProcessStartInfo
                {
                    FileName = Path.Combine(AppFolder, "yt-dlp.exe"), // 2. اتعدل ل Resources
                    Arguments = $"--flat-playlist --get-id --get-title --get-duration --get-thumbnail \"{searchArg}\" --playlist-items {_offset + 1}-{_offset + 10}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) throw new Exception("فشل تشغيل yt-dlp");

                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = line.Split('\t');
                    if (parts.Length >= 4)
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.UriSource = new Uri(parts[3]);
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        bmp.Freeze();

                        Results.Add(new SnapItem
                        {
                            Title = parts[1],
                            Url = $"https://youtube.com/watch?v={parts[0]}",
                            Duration = double.TryParse(parts[2], out double sec)? TimeSpan.FromSeconds(sec).ToString(@"mm\:ss") : parts[2],
                            Thumbnail = bmp,
                            IsSelected = true
                        });
                    }
                }
                _offset += 10;
            }
            catch (Exception ex) { MessageBox.Show($"خطأ: {ex.Message}\nتأكد ان yt-dlp.exe موجود في {AppFolder}"); }
            finally { btnSearchCourse.IsEnabled = true; btnSearchCourse.Content = "بحث"; _isLoading = false; }
        }

        private void BtnCreateFolder_Click(object sender, RoutedEventArgs e)
        {
            string folderName = txtCourseSearch.Text.Trim();
            if (string.IsNullOrEmpty(folderName)) folderName = "SnapTube";
            string path = Path.Combine(_main.Config.DownloadPath, folderName);
            Directory.CreateDirectory(path);
            Process.Start("explorer.exe", path);
        }

        private void BtnDownloadSelected_Click(object sender, RoutedEventArgs e)
        {
            string folderName = txtCourseSearch.Text.Trim();
            if (string.IsNullOrEmpty(folderName)) folderName = "SnapTube";
            string folder = Path.Combine(_main.Config.DownloadPath, folderName);
            Directory.CreateDirectory(folder);

            // 3. متصلحة: تجيب الجودة من الـ ComboBox صح
            string quality = (_main.cmbQuality.SelectedItem as ComboBoxItem)?.Content.ToString()?? "1080p";

            // 4. متصلحة: نستخدم AddDownload وهو هيحسب الثريدز لوحده
            int count = 0;
            foreach (var item in Results.Where(x => x.IsSelected))
            {
                _main.AddDownload(item.Url, quality, 0, folder); // 0 عشان يخلي MainWindow يحسب
                count++;
            }

            MessageBox.Show($"تمت اضافة {count} للتحميل في {folder}");
        }

        private void LvSnapResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lvSnapResults.SelectedItem is SnapItem item)
            {
                _main.PlayVideoInBrowser(item.Url);
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabSnap == null || TabSnap.SelectedItem == null) return;
            _currentTab = ((TabItem)TabSnap.SelectedItem).Tag.ToString();
            if (!string.IsNullOrEmpty(_lastQuery)) BtnSearchCourse_Click(null, null);
        }

        private async void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scroll = sender as ScrollViewer;
            if (scroll!= null && scroll.VerticalOffset >= scroll.ScrollableHeight - 10)
            {
                if (!_isLoading &&!string.IsNullOrEmpty(_lastQuery))
                    await SearchAsync(_lastQuery, _currentTab);
            }
        }
    }

    public class SnapItem : System.ComponentModel.INotifyPropertyChanged // 5. ضفنا INotify عشان الـ CheckBox يتحدث
    {
        private bool _isSelected = true;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsSelected))); }
        }
        public string Title { get; set; }
        public string Url { get; set; }
        public string Duration { get; set; }
        public BitmapImage Thumbnail { get; set; }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    }
}