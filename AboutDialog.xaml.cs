using System.Collections.Generic;
using System.Windows;
using KSO.Helpers; // 1. ضفنا ده

namespace KSO
{
    public partial class AboutDialog : Window
    {
        public List<ShortcutItem> Shortcuts { get; } = new();

        public AboutDialog()
        {
            InitializeComponent();
            LoadShortcuts(); // 2. حمل الاختصارات باللغة الحالية
            lvShortcuts.ItemsSource = Shortcuts; // ربط الداتا
        }

        private void LoadShortcuts() // 3. دي الجديدة
        {
            Shortcuts.Clear();
            if(Lang.Current == Lang._ar) // لو عربي
            {
                Shortcuts.Add(new ShortcutItem("Ctrl + Shift + K", "إظهار البرنامج"));
                Shortcuts.Add(new ShortcutItem("Ctrl + Shift + Q", "إغلاق البرنامج وحفظ الحالة"));
                Shortcuts.Add(new ShortcutItem("Ctrl + Shift + D", "تحميل الرابط من الحافظة"));
                Shortcuts.Add(new ShortcutItem("Ctrl + Shift + H", "إخفاء البرنامج من شريط المهام"));
                Shortcuts.Add(new ShortcutItem("Enter", "إضافة الرابط من صندوق الإضافة"));
                Shortcuts.Add(new ShortcutItem("Drag & Drop", "اسحب ملف أو رابط وأفلته في البرنامج"));
            }
            else // لو انجليزي
            {
                Shortcuts.Add(new ShortcutItem("Ctrl + Shift + K", "Show the program"));
                Shortcuts.Add(new ShortcutItem("Ctrl + Shift + Q", "Close program and save state"));
                Shortcuts.Add(new ShortcutItem("Ctrl + Shift + D", "Download link from clipboard"));
                Shortcuts.Add(new ShortcutItem("Ctrl + Shift + H", "Hide program from taskbar"));
                Shortcuts.Add(new ShortcutItem("Enter", "Add link from input box"));
                Shortcuts.Add(new ShortcutItem("Drag & Drop", "Drag a file or link and drop it here"));
            }
        }

        public static void ShowDialogWindow(Window owner)
        {
            var dlg = new AboutDialog { Owner = owner };
            dlg.ShowDialog();
        }
    }

    public class ShortcutItem 
    { 
        public string Key { get; set; } 
        public string Description { get; set; } 
        public ShortcutItem(string key, string desc) 
        { 
            Key = key; 
            Description = desc; 
        } 
    }
}