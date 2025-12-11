namespace TewiMP.UI.Windows;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.WinUI;
using WinUIEx;
using TewiMP.Helpers;
using TewiMP.Services;
using TewiMP.Core;
using TewiMP.Services.Storage;

public partial class LogWindow : Window
{
    public static LogLevel VisibleLogLevel { get; set; } = LogLevel.Info | LogLevel.Warning | LogLevel.Error;
    public nint Handle { get; private set; }
    OverlappedPresenter overlappedPresenter = null;
    private ObservableCollection<LogData> finalList { get; set; } = null;

    public LogWindow()
    {
        InitializeComponent();
        Handle = WindowHelpers.WindowHelper.GetWindowHandle(this);

        overlappedPresenter = OverlappedPresenter.Create();

        AppWindow.Title = "Log Viewer";
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.BackgroundColor = global::Windows.UI.Color.FromArgb(0, 0, 0, 0);
        AppWindow.TitleBar.ButtonBackgroundColor = global::Windows.UI.Color.FromArgb(0, 0, 0, 0);
        AppWindow.TitleBar.ButtonInactiveBackgroundColor = global::Windows.UI.Color.FromArgb(0, 0, 0, 0);
        AppWindow.SetIcon(DataFolderBase.IconPath);
        AppWindow.Resize(new(800, 600));
        AppWindow.SetPresenter(overlappedPresenter);
        SystemBackdrop = new DesktopAcrylicBackdrop();
    }

    private void LogWindow_Closed(object sender, WindowEventArgs args)
    {
        logWindowStatic = null;

    }

    static LogWindow logWindowStatic;
    public static void ShowWindow()
    {
        if (logWindowStatic is null)
        {
            logWindowStatic = new();
            logWindowStatic.Activate();
        }
        else
        {
            logWindowStatic.Activate();
            logWindowStatic.Restore();
        }
    }

    private void UpdateLogListItemSource(LogData newData = null)
    {
        //LogList.ItemsSource = null;
        if (VisibleLogLevel == LogLevel.All)
        {
            LogList.ItemsSource = App.Instance.LogService.LogDatas;
        }
        else
        {
            if (newData is not null)
            {
                if (VisibleLogLevel.HasFlag(newData.LogLevel)) finalList.Add(newData);
            }
            else
            {
                var processList = App.Instance.LogService.LogDatas.ToList();
                if (!VisibleLogLevel.HasFlag(LogLevel.Info)) processList.RemoveAll(t => t.LogLevel == LogLevel.Info);
                if (!VisibleLogLevel.HasFlag(LogLevel.Warning)) processList.RemoveAll(t => t.LogLevel == LogLevel.Warning);
                if (!VisibleLogLevel.HasFlag(LogLevel.Error)) processList.RemoveAll(t => t.LogLevel == LogLevel.Error);
                finalList = [.. processList];
                LogList.ItemsSource = finalList;
                if (finalList.Count != 0)
                {
                    LogList.SmoothScrollIntoViewWithIndexAsync(finalList.Count - 1, disableAnimation: true);
                }
            }
        }
    }

    private void UpdateBtnContent()
    {
        AllLogCounter.Content = $"共 {App.Instance.LogService.LogDatas.Count} 条";
        var l = App.Instance.LogService.LogDatas.GroupBy(t => t.LogLevel);
        foreach (var i in l)
        {
            CommunityToolkit.Labs.WinUI.TokenItem tokenItem = null;
            if (i.Key == LogLevel.Info)
            {
                tokenItem = InfoBtn;
            }
            else if (i.Key == LogLevel.Warning)
            {
                tokenItem = WarningBtn;
            }
            else if (i.Key == LogLevel.Error)
            {
                tokenItem = ErrorBtn;
            }
            tokenItem.Content = $"{i.Key} ({i.Count()})";
        }
    }

    ScrollViewer scrollViewer;
    private void Grid_Loaded(object sender, RoutedEventArgs e)
    {
        scrollViewer = (VisualTreeHelper.GetChild(LogList, 0) as Border).Child as ScrollViewer;
        scrollViewer.LayoutUpdated -= ScrollViewer_LayoutUpdated;
        scrollViewer.LayoutUpdated += ScrollViewer_LayoutUpdated;
        App.Instance.LogService.LogListAdded -= LogManager_LogListAdded;
        App.Instance.LogService.LogListAdded += LogManager_LogListAdded;
        UpdateLogListItemSource();
        UpdateBtnContent();
        isLoading = true;
        if (VisibleLogLevel.HasFlag(LogLevel.Info)) ShowLevelControl.SelectedItems.Add(InfoBtn);
        if (VisibleLogLevel.HasFlag(LogLevel.Warning)) ShowLevelControl.SelectedItems.Add(WarningBtn);
        if (VisibleLogLevel.HasFlag(LogLevel.Error)) ShowLevelControl.SelectedItems.Add(ErrorBtn);
        isLoading = false;
    }

    private void Grid_Unloaded(object sender, RoutedEventArgs e)
    {
        scrollViewer.LayoutUpdated -= ScrollViewer_LayoutUpdated;
        App.Instance.LogService.LogListAdded -= LogManager_LogListAdded;
        LogList.ItemsSource = null;
    }

    bool canScrollAuto = true;
    bool notScrollAnimation = false;
    private async void ScrollViewer_LayoutUpdated(object sender, object e)
    {
        if (!canScrollAuto || !(bool)AutoScrollCheckBox.IsChecked) return;
        scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null, notScrollAnimation);
        canScrollAuto = false;
        await Task.Delay(300);
        canScrollAuto = true;
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        await FileHelper.ExploreFile(LogService.NowLogFilePath);
    }

    private void LogManager_LogListAdded(LogData logData)
    {
        UpdateLogListItemSource(logData);
        UpdateBtnContent();
    }

    bool isLoading = false;
    private void ShowLevelControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isLoading) return;
        if (ShowLevelControl.SelectedItems.Contains(InfoBtn))
        {
            VisibleLogLevel |= LogLevel.Info;
        }
        else
        {
            VisibleLogLevel &= ~LogLevel.Info;
        }

        if (ShowLevelControl.SelectedItems.Contains(WarningBtn))
        {
            VisibleLogLevel |= LogLevel.Warning;
        }
        else
        {
            VisibleLogLevel &= ~LogLevel.Warning;
        }

        if (ShowLevelControl.SelectedItems.Contains(ErrorBtn))
        {
            VisibleLogLevel |= LogLevel.Error;
        }
        else
        {
            VisibleLogLevel &= ~LogLevel.Error;
        }
        UpdateLogListItemSource();
    }
}
