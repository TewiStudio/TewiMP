using System;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using TewiMP.Media;
using TewiMP.Helpers;
using TewiMP.DataEditor;
using static TewiMP.Windowed.RoundWindow;
using Windows.UI;
using WinRT;
using NAudio.Wave;

namespace TewiMP.Windowed
{
    public sealed partial class NotifyIconWindow : Window
    {
        static bool isVisible = true;
        public static bool IsVisible
        {
            get { return isVisible; }
            set
            {
                isVisible = value;
                if (App.NotifyIconWindow != null)
                {
                    App.NotifyIconWindow.notifyIcon.Visible = value;
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
            notifyIcon.Text = App.AppName;
            notifyIcon.Icon = new(Path.Combine(Directory.GetCurrentDirectory(), "Images", "Icons", "icon.ico"));
            notifyIcon.Visible = isVisible;

            #region others
            if (MicaController.IsSupported())
            {
                hwnd = WindowHelperzn.WindowHelper.GetWindowHandle(this);
                var preference = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
                RoundWindow.DwmSetWindowAttribute(hwnd,
                    RoundWindow.DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, ref preference,
                    sizeof(uint));
            }
            presenter = OverlappedPresenter.Create();
            AppWindow.SetPresenter(presenter);
            AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            AppWindow.IsShownInSwitchers = false;
            AppWindow.Title = $"NotifyIcon Window";
            AppWindow.SetIcon(Path.Combine("Images", "Icons", "icon_nobackground.ico"));

            AppWindow.TitleBar.ButtonBackgroundColor = Color.FromArgb(0, 0, 0, 0);
            AppWindow.TitleBar.ButtonForegroundColor = Color.FromArgb(0, 255, 255, 255);

            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;
            presenter.IsAlwaysOnTop = true;
            presenter.SetBorderAndTitleBar(false, false);

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
        private void Root_ActualThemeChanged(FrameworkElement sender, object args)
        {
            SetBackdrop(BackdropType.DesktopAcrylic);
        }

        public void UpdateDatas()
        {
            AudioPlayer_SourceChanged(App.audioPlayer);
            AudioPlayer_PlayStateChanged(App.audioPlayer);
            AudioPlayer_TimingChanged(App.audioPlayer);
            AudioPlayer_VolumeChanged(App.audioPlayer, App.audioPlayer.Volume);
            PlayingList_NowPlayingImageLoaded(App.playingList.NowPlayingImage, null);
            App.audioPlayer.ReCallTiming();

            isCodeChangedDesktopLyricWindow = true;
            TB_Lyric.IsChecked = MainWindow.DesktopLyricWindow != null;
            isCodeChangedDesktopLyricWindow = false;
        }

        private void NotifyIconWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
#if !DEBUG
                AppWindow.Hide();
#endif
                App.audioPlayer.CacheLoadingChanged -= AudioPlayer_CacheLoadingChanged;
                App.audioPlayer.CacheLoadedChanged -= AudioPlayer_CacheLoadedChanged;
                App.audioPlayer.SourceChanged -= AudioPlayer_SourceChanged;
                App.audioPlayer.PlayStateChanged -= AudioPlayer_PlayStateChanged;
                App.audioPlayer.TimingChanged -= AudioPlayer_TimingChanged;
                App.audioPlayer.VolumeChanged -= AudioPlayer_VolumeChanged;
                App.playingList.NowPlayingImageLoaded -= PlayingList_NowPlayingImageLoaded;
                MainWindow.DesktopLyricWindowOpenedEvent -= MainWindow_DesktopLyricWindowOpenedEvent;
                MainWindow.DesktopLyricWindowClosedEvent -= MainWindow_DesktopLyricWindowClosedEvent;
                TitleTBBase.Pause = true;
                ArtistTBBase.Pause = true;
                AlbumTBBase.Pause = true;
                System.Diagnostics.Debug.WriteLine("[NotifyIconWindow]: Removed Events");
            }
            else
            {
                App.audioPlayer.CacheLoadingChanged += AudioPlayer_CacheLoadingChanged;
                App.audioPlayer.CacheLoadedChanged += AudioPlayer_CacheLoadedChanged;
                App.audioPlayer.SourceChanged += AudioPlayer_SourceChanged;
                App.audioPlayer.PlayStateChanged += AudioPlayer_PlayStateChanged;
                App.audioPlayer.TimingChanged += AudioPlayer_TimingChanged;
                App.audioPlayer.VolumeChanged += AudioPlayer_VolumeChanged;
                App.playingList.NowPlayingImageLoaded += PlayingList_NowPlayingImageLoaded;
                MainWindow.DesktopLyricWindowOpenedEvent += MainWindow_DesktopLyricWindowOpenedEvent;
                MainWindow.DesktopLyricWindowClosedEvent += MainWindow_DesktopLyricWindowClosedEvent;
                TitleTBBase.Pause = false;
                ArtistTBBase.Pause = false;
                AlbumTBBase.Pause = false;
                UpdateDatas();
                System.Diagnostics.Debug.WriteLine("[NotifyIconWindow]: Added Events");
            }
        }

        private async void AudioPlayer_CacheLoadedChanged(AudioPlayer audioPlayer)
        {
            LoadingRoot.Opacity = 0;
            await Task.Delay(250);
            LoadingRing.IsIndeterminate = false;
            LoadingRoot.Visibility = Visibility.Collapsed;

            if (audioPlayer.MusicData == null || true)
            {
                notifyIcon.Text = App.AppName;
            }
            else
            {
                try
                {
                    notifyIcon.Text = $"{App.AppName}\n正在播放：{audioPlayer.MusicData.Title}\n · 艺术家：{audioPlayer.MusicData.ArtistName}\n · 专辑：{audioPlayer.MusicData.Album.Title}";
                }
                catch
                {
                    notifyIcon.Text = App.AppName;
                }
            }
        }

        private void AudioPlayer_CacheLoadingChanged(AudioPlayer audioPlayer, object data)
        {
            LoadingRing.IsIndeterminate = true;
            LoadingRoot.Visibility = Visibility.Visible;
            LoadingRoot.Opacity = 1;
        }

        bool isCodeChangedVolumeValue = false;
        private void AudioPlayer_VolumeChanged(AudioPlayer audioPlayer, object data)
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
        private void AudioPlayer_TimingChanged(AudioPlayer audioPlayer)
        {
            try
            {
                if (audioPlayer.FileReader != null)
                {
                    isCodeChangedSliderValue = true;
                    TimeSD.Minimum = 0;
                    TimeSD.Maximum = audioPlayer.TotalTime.Ticks;
                    TimeSD.Value = audioPlayer.CurrentTime.Ticks;
                    isCodeChangedSliderValue = false;

                    TimeTB.Text =
                        $"{audioPlayer.CurrentTime:mm\\:ss}/{audioPlayer.TotalTime.ToString(@"mm\:ss")}";
                }
            }
            catch { }
        }

        private void AudioPlayer_PlayStateChanged(AudioPlayer audioPlayer)
        {
            MPV.PlaybackState = audioPlayer.PlaybackState;
        }

        private void PlayingList_NowPlayingImageLoaded(ImageSource imageSource, string path)
        {
            if (imageSource == LogoImage.Source) return;
            LogoImage.Source = imageSource;
            LogoImage.SaveName = $"{MusicData.Title} - {MusicData.ButtonName}";
        }

        MusicData MusicData = null;
        private void AudioPlayer_SourceChanged(AudioPlayer audioPlayer)
        {
            if (audioPlayer.MusicData == null) return;
            if (audioPlayer.MusicData == MusicData) return;
            PlayingBarRoot.Visibility = Visibility.Visible;
            MusicData = audioPlayer.MusicData;
            TitleTB.Text = string.IsNullOrEmpty(audioPlayer.MusicData.Title2) ? audioPlayer.MusicData.Title :
                $"{audioPlayer.MusicData.Title}（{audioPlayer.MusicData.Title2}）";
            ArtistTB.Text = audioPlayer.MusicData.ArtistName;
            AlbumTB.Text = string.IsNullOrEmpty(audioPlayer.MusicData.Album.Title2) ? audioPlayer.MusicData.Album.Title :
                $"{audioPlayer.MusicData.Album.Title}（{audioPlayer.MusicData.Album.Title2}）";

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
        }

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            args.Cancel = true;
            if (Visible)
                AppWindow.Hide();
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            MainWindow.AppWindowLocal.Show();
            MainWindow.OverlappedPresenter.Restore();
            PInvoke.User32.SetForegroundWindow(MainWindow.Handle);
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
                MainWindow.AppWindowLocal.Show();
                MainWindow.OverlappedPresenter.Restore();
                PInvoke.User32.SetForegroundWindow(MainWindow.Handle);
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
                        var uiSettings = new Windows.UI.ViewManagement.UISettings();
                        var defaultthemecolor = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
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

        static WindowHelperzn.WindowsSystemDispatcherQueueHelper m_wsdqHelper;
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

                if (m_acrylicController == null)
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
                    break;
                case "off":
                    notifyIcon.Visible = false;
                    // :(
                    App.Current.Exit();
                    App.Current.Exit();
                    App.Current.Exit();
                    break;
                case "returnBack":
                    MainWindow.AppWindowLocal.Show();
                    MainWindow.OverlappedPresenter.Restore();
                    PInvoke.User32.SetForegroundWindow(MainWindow.Handle);
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
                    App.playingList.PlayPrevious();
                    break;
                case "1":
                    if (App.audioPlayer.PlaybackState == PlaybackState.Playing)
                    {
                        App.audioPlayer.SetPause();
                    }
                    else
                    {
                        App.audioPlayer.SetPlay();
                    }
                    break;
                case "2":
                    App.playingList.PlayNext();
                    break;
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {

        }

        private void VolumeSD_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!isCodeChangedVolumeValue)
                App.audioPlayer.Volume = (float)VolumeSD.Value;
        }

        private void TimeSD_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!isCodeChangedSliderValue)
            {
                App.audioPlayer.CurrentTime = TimeSpan.FromTicks((long)TimeSD.Value);
            }
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {

        }

        bool isCodeChangedDesktopLyricWindow = false;
        private void MainWindow_DesktopLyricWindowClosedEvent()
        {
            isCodeChangedDesktopLyricWindow = true;
            TB_Lyric.IsChecked = MainWindow.DesktopLyricWindow == null;
            isCodeChangedDesktopLyricWindow = false;
        }

        private void MainWindow_DesktopLyricWindowOpenedEvent()
        {
            isCodeChangedDesktopLyricWindow = true;
            TB_Lyric.IsChecked = MainWindow.DesktopLyricWindow != null;
            isCodeChangedDesktopLyricWindow = false;
        }
        
        private void TB_Lyric_Click(object sender, RoutedEventArgs e)
        {
            if (!isCodeChangedDesktopLyricWindow)
            {
                MainWindow.OpenDesktopLyricWindow();
            }
        }

        private void root_Loaded(object sender, RoutedEventArgs e)
        {
            if (!MicaController.IsSupported())
            {
                root.BorderThickness = new(1);
            }
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

            var exStyle = Vanara.PInvoke.User32.GetWindowLongAuto(windowHandle, Vanara.PInvoke.User32.WindowLongFlags.GWL_EXSTYLE).ToInt32();
            if ((exStyle & (int)Vanara.PInvoke.User32.WindowStylesEx.WS_EX_LAYERED) == 0)
            {
                exStyle |= (int)Vanara.PInvoke.User32.WindowStylesEx.WS_EX_LAYERED;
                Vanara.PInvoke.User32.SetWindowLong(windowHandle, Vanara.PInvoke.User32.WindowLongFlags.GWL_EXSTYLE, exStyle);
                Vanara.PInvoke.User32.SetLayeredWindowAttributes(
                    windowHandle,
                    (uint)System.Drawing.ColorTranslator.ToWin32(System.Drawing.Color.FromArgb(255, 99, 99, 99)), 255,
                    Vanara.PInvoke.User32.LayeredWindowAttributes.LWA_COLORKEY);
            }
            Helpers.TransparentWindowHelper.TransparentHelper.SetTransparent(window, true);
        }

        private IntPtr SubClassWndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, uint dwRefData)
        {
            if (uMsg == (uint)Vanara.PInvoke.User32.WindowMessage.WM_ERASEBKGND)
            {
                if (Vanara.PInvoke.User32.GetClientRect(hWnd, out var rect))
                {
                    using var brush = Vanara.PInvoke.Gdi32.CreateSolidBrush((uint)System.Drawing.ColorTranslator.ToWin32(System.Drawing.Color.FromArgb(255, 99, 99, 99)));
                    Vanara.PInvoke.User32.FillRect(wParam, rect, brush);
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

    public class RoundWindow
    {
        public enum DWMWINDOWATTRIBUTE
        {
            DWMWA_WINDOW_CORNER_PREFERENCE = 33
        }

        public enum DWM_WINDOW_CORNER_PREFERENCE
        {
            DWMWCP_DEFAULT = 0,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2,
            DWMWCP_ROUNDSMALL = 3
        }

        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void DwmSetWindowAttribute(IntPtr hwnd,
                                                         DWMWINDOWATTRIBUTE attribute,
                                                         ref DWM_WINDOW_CORNER_PREFERENCE pvAttribute,
                                                         uint cbAttribute);
    }
}
