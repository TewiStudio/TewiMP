using System;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using TewiMP.Helpers;
using TewiMP.Background;

namespace TewiMP.Windowed
{
    public partial class LogWindow : Window
    {
        public nint Handle { get; private set; }
        OverlappedPresenter overlappedPresenter = null;

        public LogWindow()
        {
            InitializeComponent();
            Handle = WindowHelperzn.WindowHelper.GetWindowHandle(this);

            overlappedPresenter = OverlappedPresenter.Create();

            AppWindow.Title = "Log Viewer";
            AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.BackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
            AppWindow.TitleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
            AppWindow.TitleBar.ButtonInactiveBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
            AppWindow.SetIcon(Path.Combine("Images", "Icons", "icon.ico"));
            AppWindow.Resize(new(800, 600));
            AppWindow.SetPresenter(overlappedPresenter);
            SystemBackdrop = new DesktopAcrylicBackdrop();

            Closed += LogWindow_Closed;
        }

        private void LogWindow_Closed(object sender, WindowEventArgs args)
        {
            logWindowStatic = null;
            Closed -= LogWindow_Closed;

        }

        static LogWindow logWindowStatic;
        public static void ShowWindow()
        {
            if (logWindowStatic is null)
            {
                logWindowStatic = new();
                logWindowStatic.Activate();
            }
        }

        ScrollViewer scrollViewer;
        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            scrollViewer = (VisualTreeHelper.GetChild(LogList, 0) as Border).Child as ScrollViewer;
            scrollViewer.LayoutUpdated -= ScrollViewer_LayoutUpdated;
            scrollViewer.LayoutUpdated += ScrollViewer_LayoutUpdated;
            LogList.ItemsSource = App.logManager.LogDatas;
        }

        private void Grid_Unloaded(object sender, RoutedEventArgs e)
        {
            scrollViewer.LayoutUpdated -= ScrollViewer_LayoutUpdated;
            LogList.ItemsSource = null;
        }

        bool canScrollAuto = true;
        private async void ScrollViewer_LayoutUpdated(object sender, object e)
        {
            if (!canScrollAuto || !(bool)AutoScrollCheckBox.IsChecked) return;
            scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null, false);
            canScrollAuto = false;
            await Task.Delay(300);
            canScrollAuto = true;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await FileHelper.ExploreFile(LogManager.NowLogFilePath);
        }
    }
}
