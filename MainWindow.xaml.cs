using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Diagnostics;
using System.Timers;
using System.Collections.Generic; // 1. ضفنا دي
using KSO.Modules;
using KSO.Helpers;
using Microsoft.WebView2.Core;

namespace KSO
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly DownloadManager _downloadManager;
        private readonly ConfigManager _configManager;
        private readonly DownloadHistory _history;
        private readonly Scheduler _scheduler;
        private readonly System.Timers.Timer _resourceTimer;
        private readonly HttpClient _httpClient = new();
        private bool _isHidden = false;
        private readonly HashSet<string> _capturedUrls = new(); // 2. عشان ميتكررش الاشعار

        // 3. اتغير: بقينا نفك في فولدر البرنامج مش AppData
        private static readonly string AppFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");

        public ObservableCollection<DownloadItem> Downloads { get; } = new();
        public ICollectionView DownloadsView { get; private set; }
        public SnapTubeTab snapTubeTab;

        public ConfigData Config => _configManager.Config;

        private string _statusBarText = "جاهز";
        public string StatusBarText
        {
            get => _statusBarText;
            set { _statusBarText = value; OnPropertyChanged(nameof(StatusBarText)); }
        }

        public string SelectedQuality => (cmbQuality.SelectedItem as ComboBoxItem)?.Content.ToString()?? "1080p";

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            _configManager = new ConfigManager(Path.Combine(App.AppDataFolder, "config.json"));
            _history = new DownloadHistory(Path.Combine(App.AppDataFolder, "downloads_history.json"));
            _downloadManager = new DownloadManager(Downloads, _history, Config, _httpClient);
            _scheduler = new Scheduler(_downloadManager);

            snapTubeTab = new SnapTubeTab(this);

            DownloadsView = CollectionViewSource.GetDefaultView(Downloads);
            DownloadsView.Filter = FilterDownloads;

            _downloadManager.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(DownloadManager.StatusMessage))
                    Dispatcher.Invoke(() => StatusBarText = _downloadManager.StatusMessage);
            };

            UpdateLanguage();
            LoadSettingsFromConfig();
            _ = SmartSpeedTest();
            RestoreHistory();
            CheckPassword();
            _scheduler.Start();
            ApplyTheme();

            _ = browser.EnsureCoreWebView2Async(null);
            browser.CoreWebView2InitializationCompleted += Browser_CoreWebView2InitializationCompleted;
            browser.NavigationCompleted += Browser_NavigationCompleted;

            _resourceTimer = new System.Timers.Timer(2000);
            _resourceTimer.Elapsed += UpdateResourceUsage;
            _resourceTimer.Start();

            this.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.H && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)) ToggleHiddenMode();
                if (e.Key == Key.K && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)) { this.Show(); this.WindowState = WindowState.Normal; }
                if (e.Key == Key.Q && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)) this.Close();
                if (e.Key == Key.D && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)) ClipboardDownload();
            };
        }

        public void PlayVideoInBrowser(string url)
        {
            MainTabControl.SelectedIndex = 1;
            if (browser.CoreWebView2!= null) browser.CoreWebView2.Navigate(url);
            else browser.Source = new Uri(url);
        }

        public void AddDownload(string url, string quality = "best", int threads = 16, string outputPath = null)
        {
            if(Config.NoDuplicate && Downloads.Any(d => d.Url == url))
            {
                MessageBox.Show(Lang.DownloadQ, Lang.Title);
                return;
            }

            int downloadingCount = Downloads.Count(d => d.Status.Contains("Downloading")) + 1;

            int finalThreads = Config.SmartMode
            ? DownloadManager.CalculateOptimalThreads(_downloadManager._lastSpeedTest, downloadingCount)
                : Math.Clamp(Config.MaxThreads, 16, 1000000);

            outputPath??= Config.DownloadPath;

            _downloadManager.AddDownload(url, quality, finalThreads, outputPath);
        }

        private void RestoreHistory()
        {
            foreach(var item in _history.Load())
            {
                var dl = new DownloadItem(item.Url, item.Threads, _httpClient, Config);
                dl.FileName = item.FileName;
                dl.Status = item.Status;
                Downloads.Add(dl);
            }
        }

        private void CheckPassword()
        {
            if(!string.IsNullOrEmpty(Config.Password) &&!Config.WelcomeShown)
            {
                var dlg = new PasswordWindow();
                if(dlg.ShowDialog()!= true) this.Close();
                Config.WelcomeShown = true;
                _configManager.Save();
            }
        }

        private void LoadSettingsFromConfig()
        {
            spinThreads.Value = Config.MaxThreads;
            timeSchedule.Value = Config.ScheduleTime;
            spinSpeedLimit.Value = Config.SpeedLimitKB;
            txtProxy.Text = Config.Proxy;
            txtPassword.Password = Config.Password;
            chkSmartMode.IsChecked = Config.SmartMode;
            spinThreads.IsEnabled =!Config.SmartMode;
            chkAutoCompress.IsChecked = Config.AutoCompress;
            chkDeleteOriginal.IsChecked = Config.DeleteOriginal;
            chkNoDuplicate.IsChecked = Config.NoDuplicate;
            chkShutdown.IsChecked = Config.ShutdownOnComplete;
            btnPath.Content = Config.DownloadPath;

            cmbCompressMode.SelectedIndex = cmbCompressMode.Items.Cast<ComboBoxItem>().ToList().FindIndex(i => i.Content.ToString() == Config.CompressMode);
            if(cmbCompressMode.SelectedIndex == -1) cmbCompressMode.SelectedIndex = 2;

            cmbQuality.SelectedIndex = cmbQuality.Items.Cast<ComboBoxItem>().ToList().FindIndex(i => i.Content.ToString() == Config.Quality);
            if(cmbQuality.SelectedIndex == -1) cmbQuality.SelectedIndex = 4;
        }

        private async Task SmartSpeedTest()
        {
            if(!Config.SmartMode) return;
            try
            {
                StatusBarText = "جاري اختبار السرعة...";
                await _downloadManager.UpdateSpeedCacheAsync();
                int threads = DownloadManager.CalculateOptimalThreads(_downloadManager._lastSpeedTest, 1);
                Dispatcher.Invoke(() =>
                {
                    spinThreads.Value = threads;
                    StatusBarText = $"السرعة: {_downloadManager._lastSpeedTest:F1} Mbps - خيوط: {threads}";
                });
                _configManager.Save();
            }
            catch { StatusBarText = "فشل اختبار السرعة. تم استخدام 32 خيط"; }
        }

        // 1. التقاط التلقائي من المتصفح المدمج
        private void Browser_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (browser.CoreWebView2!= null)
            {
                browser.CoreWebView2.WebMessageReceived += Browser_WebMessageReceived;
                browser.CoreWebView2.Navigating += Browser_Navigating;

                // 3. الجديد: صيد الشبكة
                browser.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.Media);
                browser.CoreWebView2.WebResourceRequested += Browser_WebResourceRequested;
            }
        }

        private async void Browser_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess) return;
            _capturedUrls.Clear(); // نفضي اللي اتلقط اول ما نغير الصفحة
            string script = @"
            function addKSOButton() {
                document.querySelectorAll('video').forEach((video) => {
                    if (video.parentElement.querySelector('.kso-download-btn')) return;

                    let btn = document.createElement('button');
                    btn.innerText = 'تحميل ب KSO';
                    btn.className = 'kso-download-btn';
                    btn.style.cssText = 'position:absolute;top:10px;right:10px;z-index:999;background:#00C8FF;color:#000;border:none;padding:8px 15px;border-radius:6px;font-weight:bold;cursor:pointer;font-size:14px;box-shadow:0 2px 8px rgba(0,0,0,0.3);';

                    video.parentElement.style.position = 'relative';
                    video.parentElement.appendChild(btn);

                    btn.onclick = (ev) => {
                        ev.stopPropagation();
                        let url = video.currentSrc || video.src || window.location.href;
                        window.chrome.webview.postMessage(url);
                    };
                });
            }
            setInterval(addKSOButton, 1500);
            addKSOButton();
            ";
            await browser.CoreWebView2.ExecuteScriptAsync(script);
        }

        // 4. الجديد: صيد اي طلب فيديو من الشبكة
        private void Browser_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            string url = e.Request.Uri;

            if ((url.Contains(".mp4") || url.Contains(".m3u8") || url.Contains(".ts") || url.Contains("videoplayback"))
                &&!_capturedUrls.Contains(url))
            {
                _capturedUrls.Add(url);
                Dispatcher.Invoke(() => {
                    var result = MessageBox.Show(
                        $"KSO لقط فيديو من الصفحة ✅\n\n{url}\n\nتحب تحطه في التحميلات؟",
                        "تم التقاط فيديو",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        txtUrl.Text = url;
                        MainTabControl.SelectedItem = tabDownloads;
                    }
                });
            }
        }

        private void Browser_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string url = e.TryGetWebMessageAsString();
            if (!string.IsNullOrEmpty(url))
            {
                Dispatcher.Invoke(() => {
                    txtUrl.Text = url;
                    MainTabControl.SelectedItem = tabDownloads;
                    MessageBox.Show($"تم لقط اللينك بنجاح ✅\n{url}\n\nدوس 'اضافة تحميل' دلوقتي", "KSO");
                });
            }
        }

        private bool FilterDownloads(object obj)
        {
            if (obj is not DownloadItem item) return false;
            string filter = (cmbFilter.SelectedItem as ComboBoxItem)?.Content.ToString()?? "All";
            if (filter == "Downloading" &&!item.Status.Contains("Downloading")) return false;
            if (filter == "Finished" && item.Status!= "Completed" && item.Status!= "Compressed") return false;
            if (filter == "Error" && item.Status.Contains("Error")) return false;
            if (!string.IsNullOrEmpty(txtSearch.Text)) return item.FileName.ToLower().Contains(txtSearch.Text.ToLower());
            return true;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e) { if (!string.IsNullOrEmpty(txtUrl.Text)) { AddDownload(txtUrl.Text.Trim(), SelectedQuality); txtUrl.Clear(); } }
        private void TxtUrl_KeyDown(object sender, KeyEventArgs e) { if(e.Key == Key.Enter) BtnAdd_Click(null, null); }
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => DownloadsView.Refresh();
        private void CmbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => DownloadsView.Refresh();
        private void BtnPath_Click(object sender, RoutedEventArgs e) { var dialog = new System.Windows.Forms.FolderBrowserDialog(); if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) { Config.DownloadPath = dialog.SelectedPath; _configManager.Save(); btnPath.Content = dialog.SelectedPath; } }

        private void BtnLang_Click(object sender, RoutedEventArgs e)
        {
            Lang.ToggleLanguage();
            UpdateLanguage();
            Config.Language = Lang.Current == Lang._ar? "ar" : "en";
            _configManager.Save();
        }

        private void UpdateLanguage()
        {
            foreach (Window window in Application.Current.Windows)
            {
                window.DataContext = null;
                window.DataContext = this;
            }

            this.Title = Lang.Title;
            btnAdd.Content = Lang.AddDownload;
            tabDownloads.Header = Lang.DownloadsTab;
            tabBrowser.Header = Lang.BrowserTab;
            tabSnapTube.Header = Lang.SnapTubeTab;
            lblQuality.Content = Lang.Quality;
            lblPath.Content = Lang.Path;
            lblThreads.Content = Lang.ThreadCount;
            lblSpeedLimit.Content = Lang.SpeedLimit;
            chkSmartMode.Content = Lang.SmartMode;
            chkAutoCompress.Content = Lang.AutoCompress;
            chkDeleteOriginal.Content = Lang.DeleteOriginal;
            chkNoDuplicate.Content = Lang.NoDuplicate;
            btnAbout.Content = Lang.About;
            txtSearch.ToolTip = Lang.SearchPlaceholder;
            btnReDownload.Content = Lang.ReDownload;
            btnExportCsv.Content = Lang.ExportCsv;
            btnClearCache.Content = Lang.ClearCache;
            btnClearTemp.Content = Lang.ClearTemp;
            StatusBarText = "جاهز";
        }

        private void ApplyTheme()
        {
            if(Config.DarkMode)
            {
                this.Background = System.Windows.Media.Brushes.Black;
                this.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e) { _downloadManager.StopAll(); _scheduler.Stop(); _resourceTimer?.Stop(); _httpClient.Dispose(); _configManager.Save(); _history.Save(Downloads.ToList()); }
        private void UpdateResourceUsage(object sender, ElapsedEventArgs e) { Dispatcher.Invoke(() => { try { using var proc = Process.GetCurrentProcess(); lblResources.Text = $"RAM: {proc.WorkingSet64 / 1024 / 1024} MB"; } catch { } }); }
        private void ToggleHiddenMode() { _isHidden =!_isHidden; this.Visibility = _isHidden? Visibility.Hidden : Visibility.Visible; this.ShowInTaskbar =!_isHidden; }
        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e) { Config.Password = txtPassword.Password; _configManager.Save(); }

        private void BtnAbout_Click(object sender, RoutedEventArgs e) { AboutDialog.ShowDialogWindow(this); }
        private void BtnReDownload_Click(object sender, RoutedEventArgs e) { if(lvDownloads.SelectedItem is DownloadItem item) AddDownload(item.Url, SelectedQuality); }
        private void BtnExportCsv_Click(object sender, RoutedEventArgs e) { var dlg = new Microsoft.Win32.SaveFileDialog(){Filter="CSV|*.csv"}; if(dlg.ShowDialog()==true) _history.ExportToCsv(dlg.FileName); }
        private void BtnClearCache_Click(object sender, RoutedEventArgs e) { try{ Directory.Delete(Path.Combine(App.AppDataFolder,"Cache"),true);}catch{} }
        private void BtnClearTemp_Click(object sender, RoutedEventArgs e) { try{ Directory.Delete(Path.GetTempPath()+"KSO_Downloads",true);}catch{} }
        private void Window_DragEnter(object sender, DragEventArgs e) { if(e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effects = DragDropEffects.Copy; }
        private void Window_Drop(object sender, DragEventArgs e) { if(e.Data.GetData(DataFormats.FileDrop) is string[] files) foreach(var f in files) if(File.Exists(f)) AddDownload(f, SelectedQuality); }
        private void ClipboardDownload() { if(Clipboard.ContainsText()) AddDownload(Clipboard.GetText(), SelectedQuality); }

        private void ChkSmartMode_Checked(object sender, RoutedEventArgs e) { Config.SmartMode = true; spinThreads.IsEnabled = false; _configManager.Save(); _ = SmartSpeedTest(); StatusBarText = "الوضع الذكي مفعل"; }
        private void ChkSmartMode_Unchecked(object sender, RoutedEventArgs e) { Config.SmartMode = false; spinThreads.IsEnabled = true; _configManager.Save(); StatusBarText = "الوضع اليدوي مفعل"; }

        private void ChkAutoCompress_Checked(object sender, RoutedEventArgs e) { Config.AutoCompress = true; _configManager.Save(); }
        private void ChkAutoCompress_Unchecked(object sender, RoutedEventArgs e) { Config.AutoCompress = false; _configManager.Save(); }
        private void ChkDeleteOriginal_Checked(object sender, RoutedEventArgs e) { Config.DeleteOriginal = true; _configManager.Save(); }
        private void ChkDeleteOriginal_Unchecked(object sender, RoutedEventArgs e) { Config.DeleteOriginal = false; _configManager.Save(); }
        private void ChkNoDuplicate_Checked(object sender, RoutedEventArgs e) { Config.NoDuplicate = true; _configManager.Save(); }
        private void ChkNoDuplicate_Unchecked(object sender, RoutedEventArgs e) { Config.NoDuplicate = false; _configManager.Save(); }
        private void ChkShutdown_Checked(object sender, RoutedEventArgs e) { Config.ShutdownOnComplete = true; _configManager.Save(); }
        private void ChkShutdown_Unchecked(object sender, RoutedEventArgs e) { Config.ShutdownOnComplete = false; _configManager.Save(); }
        private void ChkBrowser_Checked(object sender, RoutedEventArgs e) => browser.Visibility = Visibility.Visible;
        private void ChkBrowser_Unchecked(object sender, RoutedEventArgs e) => browser.Visibility = Visibility.Collapsed;
        private void CmbCompressMode_SelectionChanged(object sender, SelectionChangedEventArgs e) { Config.CompressMode = (cmbCompressMode.SelectedItem as ComboBoxItem)?.Content.ToString(); _configManager.Save(); }
        private void CmbQuality_SelectionChanged(object sender, SelectionChangedEventArgs e) { Config.Quality = (cmbQuality.SelectedItem as ComboBoxItem)?.Content.ToString(); _configManager.Save(); }
        private void SpinThreads_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) { if(spinThreads.Value.HasValue) { Config.MaxThreads = spinThreads.Value; _configManager.Save(); } }
        private void SpinSpeedLimit_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) { if(spinSpeedLimit.Value.HasValue) { Config.SpeedLimitKB = spinSpeedLimit.Value; _configManager.Save(); } }
        private void TxtProxy_TextChanged(object sender, TextChangedEventArgs e) { Config.Proxy = txtProxy.Text; _configManager.Save(); }

        // الزرارين بتوع التحديث - اتعدلو عشان يستخدمو AppFolder الجديد
        private async void BtnUpdateYtDlp_Click(object sender, RoutedEventArgs e)
        {
            btnUpdateYtDlp.IsEnabled = false;
            btnUpdateYtDlp.Content = "جاري التحديث...";
            await CheckAndUpdateYtDlp();
            btnUpdateYtDlp.Content = "تحديث yt-dlp";
            btnUpdateYtDlp.IsEnabled = true;
            MessageBox.Show("تم تحديث yt-dlp بنجاح ✅");
        }

        private async void BtnUpdateFfmpeg_Click(object sender, RoutedEventArgs e)
        {
            btnUpdateFfmpeg.IsEnabled = false;
            btnUpdateFfmpeg.Content = "جاري التحديث...";
            await CheckAndUpdateFfmpeg();
            btnUpdateFfmpeg.Content = "تحديث ffmpeg";
            btnUpdateFfmpeg.IsEnabled = true;
            MessageBox.Show("تم تحديث ffmpeg بنجاح ✅");
        }

        private async Task CheckAndUpdateYtDlp()
        {
            try
            {
                if (!Directory.Exists(AppFolder)) Directory.CreateDirectory(AppFolder);
                string url = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
                string path = Path.Combine(AppFolder, "yt-dlp.exe"); // 4. بقى في Resources
                using HttpClient client = new HttpClient() { Timeout = TimeSpan.FromMinutes(5) };
                var data = await client.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(path, data);
            }
            catch (Exception ex) { MessageBox.Show("فشل تحديث yt-dlp: " + ex.Message, "خطأ"); }
        }

        private async Task CheckAndUpdateFfmpeg()
        {
            try
            {
                if (!Directory.Exists(AppFolder)) Directory.CreateDirectory(AppFolder);
                string url = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";
                string zipPath = Path.Combine(AppFolder, "ffmpeg_temp.zip"); // 5. بقى في Resources
                string extractPath = Path.Combine(AppFolder, "ffmpeg_temp");
                string finalPath = Path.Combine(AppFolder, "ffmpeg.exe");

                using HttpClient client = new HttpClient() { Timeout = TimeSpan.FromMinutes(10) };
                var data = await client.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(zipPath, data);

                if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
                ZipFile.ExtractToDirectory(zipPath, extractPath);
                var ffmpegFile = Directory.GetFiles(extractPath, "ffmpeg.exe", SearchOption.AllDirectories)[0];
                File.Copy(ffmpegFile, finalPath, true);

                File.Delete(zipPath);
                Directory.Delete(extractPath, true);
            }
            catch (Exception ex) { MessageBox.Show("فشل تحديث ffmpeg: " + ex.Message, "خطأ"); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}