using DevWinUI;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NAudio.Wave;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TewiMP.Core;
using TewiMP.Core.Music;
using TewiMP.Helpers;
using TewiMP.Services;
using TewiMP.Services.Media.Audio;
using TewiMP.Services.Storage;
using TewiMP.UI.Pages;
using TewiMP.UI.WindowHelpers;
using Vanara.PInvoke;
using Windows.UI;
using WinRT;
using WinUIEx;

namespace TewiMP.UI.Windows;

public sealed partial class NotifyIconWindow : Window
{
    static bool isVisible = true;
    public static bool IsVisible
    {
        get { return isVisible; }
        set
        {
            isVisible = value;
            if (App.Instance.NotifyIconWindow != null)
            {
                App.Instance.NotifyIconWindow.notifyIcon.Visible = value;
            }
        }
    }

    public void HideIcon()
    {
        notifyIcon.Visible = false;
    }

    private System.Windows.Forms.NotifyIcon notifyIcon;
    OverlappedPresenter presenter;
    nint hwnd = 0;
    public NotifyIconWindow()
    {
        InitializeComponent();

        notifyIcon = new System.Windows.Forms.NotifyIcon();
        notifyIcon.Text = App.Instance.AppName;
        notifyIcon.Icon = new(DataFolderBase.IconICOPath);
        notifyIcon.Visible = isVisible;

        #region others
        if (MicaController.IsSupported()) // 确认系统版本为 win11
        {
            hwnd = WindowHelpers.WindowHelper.GetWindowHandle(this);
            var preference = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
            WindowHelpers.WindowHelper.DwmSetWindowAttribute(hwnd,
                DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, ref preference,
                sizeof(uint));
        }
        
        presenter = OverlappedPresenter.CreateForContextMenu(); // FIX: https://github.com/microsoft/microsoft-ui-xaml/issues/9978#issuecomment-2456461855
        AppWindow.SetPresenter(presenter);
        AppWindow.SetIcon(DataFolderBase.IconICOPath);
        UpdateWindowDisplay();

        AppWindow.Closing += AppWindow_Closing;
        notifyIcon.Click += NotifyIcon_Click;
        notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
        notifyIcon.MouseClick += NotifyIcon_MouseClick;
        root.ActualThemeChanged += Root_ActualThemeChanged;
        Activated += NotifyIconWindow_Activated;

        SetBackdrop(BackdropType.DesktopAcrylic);
        //MoveToPosition();
        #endregion
    }

    #region others
    private void UpdateWindowDisplay()
    {
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppWindow.IsShownInSwitchers = false;
        AppWindow.Title = $"NotifyIcon Window";

        AppWindow.TitleBar.ButtonBackgroundColor = Color.FromArgb(0, 0, 0, 0);
        AppWindow.TitleBar.ButtonForegroundColor = Color.FromArgb(0, 255, 255, 255);

        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.IsAlwaysOnTop = true;
        presenter.IsResizable = false;
        presenter.SetBorderAndTitleBar(false, false);
    }

    private void Root_ActualThemeChanged(FrameworkElement sender, object args)
    {
        SetBackdrop(BackdropType.DesktopAcrylic);
    }

    public void UpdateDatas()
    {
        UpdateWindowDisplay();
        AudioService_SourceChanged(App.Instance.AudioService);
        AudioService_PlayStateChanged(App.Instance.AudioService);
        AudioService_TimingChanged(App.Instance.AudioService);
        AudioService_VolumeChanged(App.Instance.AudioService, App.Instance.AudioService.Volume);
        PlayingList_NowPlayingImageLoaded(App.Instance.PlayingListService.NowPlayingImage, null);
        App.Instance.AudioService.ReCallTiming();
        SetPlayModeIconAndName(App.Instance.PlayingListService.PlayBehavior);
        AudioService_CacheLoadedChanged(App.Instance.AudioService);

        isCodeChangedDesktopLyricWindow = true;
        TB_Lyric.IsChecked = App.MainWindowInstance.DesktopLyricWindow != null;
        isCodeChangedDesktopLyricWindow = false;
    }

    private void NotifyIconWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            root.Opacity = 0;
#if !DEBUG
            AppWindow.Hide();
#endif
            App.Instance.AudioService.CacheLoadingChanged -= AudioService_CacheLoadingChanged;
            App.Instance.AudioService.CacheLoadedChanged -= AudioService_CacheLoadedChanged;
            App.Instance.AudioService.SourceChanged -= AudioService_SourceChanged;
            App.Instance.AudioService.PlayStateChanged -= AudioService_PlayStateChanged;
            App.Instance.AudioService.TimingChanged -= AudioService_TimingChanged;
            App.Instance.AudioService.VolumeChanged -= AudioService_VolumeChanged;
            App.Instance.PlayingListService.NowPlayingImageLoaded -= PlayingList_NowPlayingImageLoaded;
            App.MainWindowInstance.DesktopLyricWindowOpenedEvent -= MainWindow_DesktopLyricWindowOpenedEvent;
            App.MainWindowInstance.DesktopLyricWindowClosedEvent -= MainWindow_DesktopLyricWindowClosedEvent;
            TitleTBBase.Pause = true;
            ArtistTBBase.Pause = true;
            AlbumTBBase.Pause = true;
            LogService.Log(nameof(NotifyIconWindow), "Removed Events");
        }
        else
        {
            App.Instance.AudioService.CacheLoadingChanged += AudioService_CacheLoadingChanged;
            App.Instance.AudioService.CacheLoadedChanged += AudioService_CacheLoadedChanged;
            App.Instance.AudioService.SourceChanged += AudioService_SourceChanged;
            App.Instance.AudioService.PlayStateChanged += AudioService_PlayStateChanged;
            App.Instance.AudioService.TimingChanged += AudioService_TimingChanged;
            App.Instance.AudioService.VolumeChanged += AudioService_VolumeChanged;
            App.Instance.PlayingListService.NowPlayingImageLoaded += PlayingList_NowPlayingImageLoaded;
            App.MainWindowInstance.DesktopLyricWindowOpenedEvent += MainWindow_DesktopLyricWindowOpenedEvent;
            App.MainWindowInstance.DesktopLyricWindowClosedEvent += MainWindow_DesktopLyricWindowClosedEvent;
            TitleTBBase.Pause = false;
            ArtistTBBase.Pause = false;
            AlbumTBBase.Pause = false;
            UpdateDatas();
            root.Opacity = 1;
            LogService.Log(nameof(NotifyIconWindow), "Added Events");
        }
    }

    private async void AudioService_CacheLoadedChanged(AudioService AudioService)
    {
        LoadingRoot.Opacity = 0;
        await Task.Delay(250);
        LoadingRing.IsIndeterminate = false;
        LoadingRoot.Visibility = Visibility.Collapsed;

        if (AudioService.MusicData is null || true)
        {
            notifyIcon.Text = App.Instance.AppName;
        }
        else
        {
            try
            {
                notifyIcon.Text = $"{App.Instance.AppName}\n正在播放：{AudioService.MusicData.Title}\n · 艺术家：{AudioService.MusicData.ArtistName}\n · 专辑：{AudioService.MusicData.Album.Title}";
            }
            catch
            {
                notifyIcon.Text = App.Instance.AppName;
            }
        }
    }

    private void AudioService_CacheLoadingChanged(AudioService AudioService, object data)
    {
        LoadingRing.IsIndeterminate = true;
        LoadingRoot.Visibility = Visibility.Visible;
        LoadingRoot.Opacity = 1;
    }

    bool isCodeChangedVolumeValue = false;
    private void AudioService_VolumeChanged(AudioService AudioService, object data)
    {
        isCodeChangedVolumeValue = true;
        float volume = (float)data;
        VolumeSD.Value = (int)volume;
        isCodeChangedVolumeValue = false;
        
        if (volume == 0)
        {
            VolumeIconBase.Glyph = "\xE198";
        }
        else
        {
            if (volume <= 100 && volume > 67)
                VolumeIconBase.Glyph = "\xE995";
            else if (volume <= 67 && volume > 33)
                VolumeIconBase.Glyph = "\xE994";
            else if (volume <= 33)
                VolumeIconBase.Glyph = "\xE993";
        }
    }

    bool isCodeChangedSliderValue = false;
    private void AudioService_TimingChanged(AudioService AudioService)
    {
        try
        {
            if (AudioService.FileReader != null)
            {
                isCodeChangedSliderValue = true;
                TimeSD.Minimum = 0;
                TimeSD.Maximum = AudioService.TotalTime.Ticks;
                TimeSD.Value = AudioService.CurrentTime.Ticks;
                isCodeChangedSliderValue = false;

                TimeTB.Text =
                    $"{AudioService.CurrentTime:mm\\:ss}/{AudioService.TotalTime.ToString(@"mm\:ss")}";
            }
        }
        catch { }
    }

    private void AudioService_PlayStateChanged(AudioService AudioService)
    {
        MPV.PlaybackState = AudioService.PlaybackState;
    }

    private void PlayingList_NowPlayingImageLoaded(Uri imageSource, string path)
    {
        if (imageSource == LogoImage.Source) return;
        LogoImage.Source = imageSource;
        LogoImage.SaveName = $"{MusicData.Title} - {MusicData.ButtonName}";
    }

    MusicData MusicData = null;
    private void AudioService_SourceChanged(AudioService AudioService)
    {
        if (AudioService.MusicData is null) return;
        if (AudioService.MusicData == MusicData) return;
        PlayingBarRoot.Visibility = Visibility.Visible;
        MusicData = AudioService.MusicData;
        TitleTB.Text = string.IsNullOrEmpty(AudioService.MusicData.Title2) ? AudioService.MusicData.Title :
            $"{AudioService.MusicData.Title}（{AudioService.MusicData.Title2}）";
        ArtistTB.Text = AudioService.MusicData.ArtistName;
        AlbumTB.Text = string.IsNullOrEmpty(AudioService.MusicData.Album.Title2) ? AudioService.MusicData.Album.Title :
            $"{AudioService.MusicData.Album.Title}（{AudioService.MusicData.Album.Title2}）";
        TB_OutputSelector_Name.Text = AudioService.NowOutDevice.ToString();

        MoveToPosition();
    }

    bool isCodeChangedHeight = false;
    private void MoveToPosition()
    {
        DisplayArea displayArea = CodeHelper.GetDisplayArea(this);
        IntPtr hMonitor = Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);
        int result = CodeHelper.GetDpiForMonitor(hMonitor, CodeHelper.Monitor_DPI_Type.MDT_Default, out uint dpiX, out uint _);
        var dpi = (uint)(((long)dpiX * 100 + (96 >> 1)) / 96) / 100.0;

        isCodeChangedHeight = true;
        ViewRoot.Height = double.NaN;
        int width = (int)(380 * dpi);
        int height = (int)(root.ActualHeight * dpi);
        if (height > displayArea.WorkArea.Height)
        {
            ViewRoot.MaxHeight = displayArea.WorkArea.Height / dpi - BottomBarRoot.ActualHeight - 24;
            height = displayArea.WorkArea.Height - (int)(24 * dpi);
        }
        isCodeChangedHeight = false;

        AppWindow.MoveAndResize(new(
            displayArea.WorkArea.Width - width - (int)(12 * dpi),
            displayArea.WorkArea.Height - height - (int)(12 * dpi),
            width, height));
        //AnimateWindowPosition();
    }

    private async void AnimateWindowPosition()
    {
        await Task.Run(async () =>
        {
            for (byte i = 0; i < 255; i++)
            {
                User32.SetLayeredWindowAttributes(
                    hwnd,
                    (uint)System.Drawing.ColorTranslator.ToWin32(System.Drawing.Color.FromArgb(i, 255, 255, 255)), i,
                    User32.LayeredWindowAttributes.LWA_COLORKEY);
                await Task.Delay(100);
            }
        });
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        args.Cancel = true;
        if (Visible)
            AppWindow.Hide();
    }

    private void NotifyIcon_DoubleClick(object sender, EventArgs e)
    {
        App.MainWindowInstance.Activate();
        App.MainWindowInstance.SetForegroundWindow();
    }

    private async void NotifyIcon_Click(object sender, EventArgs e)
    {
    }

    private void NotifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
    {
        if (e.Button == System.Windows.Forms.MouseButtons.Right)
        {
            if (Visible)
            {
                AppWindow.Hide();
                return;
            }

            AppWindow.Show(true);
            MoveToPosition();
            PInvoke.User32.SetForegroundWindow(hwnd);
        }
        else
        {
            App.MainWindowInstance.Restore();
            App.MainWindowInstance.SetForegroundWindow();
        }
    }
    #endregion

    #region Enable Window Backdrop
    public static ApplicationTheme ActualTheme = ApplicationTheme.Dark;
    static ElementTheme requestedTheme = ElementTheme.Default;
    public static ElementTheme RequestedTheme
    {
        get => requestedTheme;
        set
        {
            requestedTheme = value;

            switch (value)
            {
                case ElementTheme.Dark: ActualTheme = ApplicationTheme.Dark; break;
                case ElementTheme.Light: ActualTheme = ApplicationTheme.Light; break;
                case ElementTheme.Default:
                    var uiSettings = new global::Windows.UI.ViewManagement.UISettings();
                    var defaultthemecolor = uiSettings.GetColorValue(global::Windows.UI.ViewManagement.UIColorType.Background);
                    ActualTheme = defaultthemecolor == Colors.Black ? ApplicationTheme.Dark : ApplicationTheme.Light;
                    break;
            }
        }
    }

    public enum BackdropType
    {
        Mica,
        DesktopAcrylic,
        DefaultColor,
    }

    static WindowHelpers.WindowsSystemDispatcherQueueHelper m_wsdqHelper;
    static BackdropType m_currentBackdrop;
    static MicaController m_micaController;
    static DesktopAcrylicController m_acrylicController;
    static SystemBackdropConfiguration m_configurationSource;

    public void SetBackdrop(BackdropType type)
    {
        m_currentBackdrop = BackdropType.DefaultColor;
        if (m_micaController != null)
        {
            m_micaController.Dispose();
            m_micaController = null;
        }
        if (m_acrylicController != null)
        {
            m_acrylicController.Dispose();
            m_acrylicController = null;
        }
        this.Activated -= DesktopLyricWindow_Activated;
        this.Closed -= DesktopLyricWindow_Closed;
        m_configurationSource = null;

        if (type == BackdropType.Mica)
        {
            if (TrySetMicaBackdrop())
            {
                m_currentBackdrop = type;
            }
            else
            {
                type = BackdropType.DesktopAcrylic;
            }
        }
        if (type == BackdropType.DesktopAcrylic)
        {
            if (TrySetAcrylicBackdrop())
            {
                m_currentBackdrop = type;
            }
            else
            {
            }
        }
    }

    bool TrySetMicaBackdrop()
    {
        if (MicaController.IsSupported())
        {
            m_configurationSource = new SystemBackdropConfiguration();
            this.Activated += DesktopLyricWindow_Activated;
            this.Closed += DesktopLyricWindow_Closed;

            m_configurationSource.IsInputActive = true;
            switch (RequestedTheme)
            {
                case ElementTheme.Dark: m_configurationSource.Theme = SystemBackdropTheme.Dark; break;
                case ElementTheme.Light: m_configurationSource.Theme = SystemBackdropTheme.Light; break;
                case ElementTheme.Default: m_configurationSource.Theme = SystemBackdropTheme.Default; break;
            }
            m_micaController = new MicaController();
            m_micaController.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
            m_micaController.SetSystemBackdropConfiguration(m_configurationSource);
            return true;
        }

        return false;
    }

    private void DesktopLyricWindow_Closed(object sender, WindowEventArgs args)
    {
        if (m_micaController != null)
        {
            m_micaController.Dispose();
            m_micaController = null;
        }
        if (m_acrylicController != null)
        {
            m_acrylicController.Dispose();
            m_acrylicController = null;
        }
    }

    private void DesktopLyricWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (m_currentBackdrop != BackdropType.DesktopAcrylic)
        {
            m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }
    }

    bool TrySetAcrylicBackdrop()
    {
        if (DesktopAcrylicController.IsSupported())
        {
            m_configurationSource = new SystemBackdropConfiguration();
            this.Activated += DesktopLyricWindow_Activated;
            this.Closed += DesktopLyricWindow_Closed;

            m_configurationSource.IsInputActive = true;
            switch (RequestedTheme)
            {
                case ElementTheme.Dark: m_configurationSource.Theme = SystemBackdropTheme.Dark; break;
                case ElementTheme.Light: m_configurationSource.Theme = SystemBackdropTheme.Light; break;
                case ElementTheme.Default: m_configurationSource.Theme = SystemBackdropTheme.Default; break;
            }

            if (m_acrylicController is null)
                m_acrylicController = new DesktopAcrylicController();
            if (App.Current.RequestedTheme == ApplicationTheme.Dark)
            {
                m_acrylicController.TintColor = Color.FromArgb(255, 32, 32, 32);
                m_acrylicController.LuminosityOpacity = 0.96f;
                m_acrylicController.TintOpacity = 0.5f;
            }
            else
            {
                m_acrylicController.TintColor = Color.FromArgb(255, 243, 243, 243);
                m_acrylicController.LuminosityOpacity = 0.90f;
                m_acrylicController.TintOpacity = 0f;
            }

            m_acrylicController.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
            m_acrylicController.SetSystemBackdropConfiguration(m_configurationSource);
            return true;
        }

        return false;
    }
    #endregion

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        switch ((sender as FrameworkElement).Tag)
        {
            case "setting":
                App.MainWindowInstance.Restore();
                App.MainWindowInstance.SetForegroundWindow();
                App.MainWindowInstance.SetNavViewContent(typeof(SettingPage));
                break;
            case "off":
                notifyIcon.Visible = false;
                App.Instance.ExitApp();
                break;
            case "returnBack":
                App.MainWindowInstance.Restore();
                App.MainWindowInstance.SetForegroundWindow();
                PInvoke.User32.SetForegroundWindow(App.MainWindowInstance.Handle);
                break;
        }
    }

    private void root_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (isCodeChangedHeight) return;
        MoveToPosition();
    }

    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
        switch ((sender as Button).Tag)
        {
            case "0":
                App.Instance.PlayingListService.PlayPrevious();
                break;
            case "1":
                if (App.Instance.AudioService.PlaybackState == PlaybackState.Playing)
                {
                    App.Instance.AudioService.SetPause();
                }
                else
                {
                    App.Instance.AudioService.SetPlay();
                }
                break;
            case "2":
                App.Instance.PlayingListService.PlayNext();
                break;
        }
    }

    private void Button_Click_2(object sender, RoutedEventArgs e)
    {
        App.MainWindowInstance.MuteOrUnmuteVolume();
    }

    private void VolumeSD_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!isCodeChangedVolumeValue)
            App.Instance.AudioService.Volume = (float)VolumeSD.Value;
    }

    private void TimeSD_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!isCodeChangedSliderValue)
        {
            App.Instance.AudioService.CurrentTime = TimeSpan.FromTicks((long)TimeSD.Value);
        }
    }

    private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {

    }

    bool isCodeChangedDesktopLyricWindow = false;
    private void MainWindow_DesktopLyricWindowClosedEvent()
    {
        isCodeChangedDesktopLyricWindow = true;
        TB_Lyric.IsChecked = App.MainWindowInstance.DesktopLyricWindow is null;
        isCodeChangedDesktopLyricWindow = false;
    }

    private void MainWindow_DesktopLyricWindowOpenedEvent()
    {
        isCodeChangedDesktopLyricWindow = true;
        TB_Lyric.IsChecked = App.MainWindowInstance.DesktopLyricWindow != null;
        isCodeChangedDesktopLyricWindow = false;
    }
    
    private void TB_Lyric_Click(object sender, RoutedEventArgs e)
    {
        if (!isCodeChangedDesktopLyricWindow)
        {
            App.MainWindowInstance.OpenDesktopLyricWindow();
        }
    }

    private void root_Loaded(object sender, RoutedEventArgs e)
    {
        if (!MicaController.IsSupported())
        {
            root.BorderThickness = new(1);
        }
    }

    private void C_Click(object sender, RoutedEventArgs e)
    {
        var a = (OutDevice)(sender as MenuFlyoutItem).Tag;
        App.Instance.AudioService.NowOutDevice = a;
        TB_OutputSelector_Name.Text = App.Instance.AudioService.NowOutDevice.ToString();

        App.Instance.AudioService.SetReloadAsync();
    }
    
    private async void AddOutDeviceToFlyOut()
    {
        var a = await OutDevice.GetOutDevicesAsync();
        OutputFlyout.Items.Clear();
        foreach (var b in a)
        {
            var c = new MenuFlyoutItem() { Text = b.ToString(), Tag = b };
            c.Click += C_Click;
            OutputFlyout.Items.Add(c);
        }
    }

    private void TB_OutputSelector_Loaded(object sender, RoutedEventArgs e)
    {
        AddOutDeviceToFlyOut();
    }

    private void TB_OutputSelector_Click(object sender, RoutedEventArgs e)
    {
        AddOutDeviceToFlyOut();
    }

    private void B_Click(object sender, RoutedEventArgs e)
    {
        var a = (PlayBehavior)(sender as MenuFlyoutItem).Tag;
        App.Instance.PlayingListService.PlayBehavior = a;
        SetPlayModeIconAndName(App.Instance.PlayingListService.PlayBehavior);
    }

    private void SetPlayModeIconAndName(PlayBehavior playBehavior)
    {
        TB_PlayModeSelector_Icon.Glyph = playBehavior.GetIcon();
        TB_PlayModeSelector_Name.Text = playBehavior.ToString();
    }

    private void TB_PlayModeSelector_Click(object sender, RoutedEventArgs e)
    {
        var pList = Enum.GetNames(typeof(PlayBehavior)).ToList();
        PlayModeFlyout.Items.Clear();
        foreach (var p in pList)
        {
            var b = new MenuFlyoutItem() { Text = p, Tag = pList.IndexOf(p) };
            b.Click += B_Click;
            b.Unloaded += B_Unloaded;
            b.Icon = new FontIcon()
            {
                Glyph = ((PlayBehavior)b.Tag).GetIcon()
            };
            PlayModeFlyout.Items.Add(b);
        }
    }

    private void B_Unloaded(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item)
        {
            item.Click -= B_Click;
            item.Unloaded -= B_Unloaded;
        }
    }

    private void TB_PlayModeSelector_Base_Loaded(object sender, RoutedEventArgs e)
    {
        SetPlayModeIconAndName(App.Instance.PlayingListService.PlayBehavior);
    }
}

public class TransparentWindow
{
    Window window = null;
    public TransparentWindow(Window window)
    {
        this.window = window;
    }

    private SUBCLASSPROC subClassProc;
    public void TryTransparentWindow()
    {
        subClassProc = new SUBCLASSPROC(SubClassWndProc);
        var windowHandle = new IntPtr((long)window.AppWindow.Id.Value);
        SetWindowSubclass(windowHandle, subClassProc, 0, 0);

        var exStyle = User32.GetWindowLongAuto(windowHandle, User32.WindowLongFlags.GWL_EXSTYLE).ToInt32();
        if ((exStyle & (int)User32.WindowStylesEx.WS_EX_LAYERED) == 0)
        {
            exStyle |= (int)User32.WindowStylesEx.WS_EX_LAYERED;
            User32.SetWindowLong(windowHandle, User32.WindowLongFlags.GWL_EXSTYLE, exStyle);
            User32.SetLayeredWindowAttributes(
                windowHandle,
                (uint)System.Drawing.ColorTranslator.ToWin32(System.Drawing.Color.FromArgb(255, 99, 99, 99)), 255,
                User32.LayeredWindowAttributes.LWA_COLORKEY);
        }
        Helpers.TransparentWindowHelper.TransparentHelper.SetTransparent(window);
    }

    private IntPtr SubClassWndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, uint dwRefData)
    {
        if (uMsg == (uint)User32.WindowMessage.WM_ERASEBKGND)
        {
            if (User32.GetClientRect(hWnd, out var rect))
            {
                using var brush = Gdi32.CreateSolidBrush((uint)System.Drawing.ColorTranslator.ToWin32(System.Drawing.Color.FromArgb(255, 99, 99, 99)));
                User32.FillRect(wParam, rect, brush);
                return new IntPtr(1);
            }
        }

        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    private delegate IntPtr SUBCLASSPROC(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, uint dwRefData);

    [DllImport("Comctl32.dll", SetLastError = true)]
    private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("Comctl32.dll", SetLastError = true)]
    private static extern bool SetWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, uint uIdSubclass, uint dwRefData);
}
