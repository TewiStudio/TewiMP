using WinRT;
using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.Graphics;
using Windows.Media.Core;
using Windows.Media.Playback;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Composition;
using TewiMP.Helpers;

namespace TewiMP.Windowed
{
    public sealed partial class MediaPlayerWindow : Window
    {
        AppWindow appWindow = null;
        OverlappedPresenter overlappedPresenter = null;
        string mediaUri = null;
        public string MediaUri
        {
            get => mediaUri;
            set
            {
                if (mediaUri != value)
                {
                    mediaUri = value;
                    SetSMTC();
                }
            }
        }

        public void SetSMTC()
        {
            var _mediaSource = MediaSource.CreateFromUri(new Uri(mediaUri));
            var _mediaPlaybackItem = new MediaPlaybackItem(_mediaSource);/*
            var props = _mediaPlaybackItem.GetDisplayProperties();
            props.Type = MediaPlaybackType.Music;
            props.MusicProperties.Title = "TewiMP";
            props.MusicProperties.Artist = "请问您今天要来点兔唇吗";
            props.Thumbnail = Windows.Storage.Streams.RandomAccessStreamReference.CreateFromUri(new Uri("C:\\Users\\zilong\\Pictures\\2023-01-05 (3).png"));
            _mediaPlaybackItem.ApplyDisplayProperties(props);*/
            var _mediaPlayer = new MediaPlayer();
            _mediaPlayer.Source = _mediaPlaybackItem;
            MPE.SetMediaPlayer(_mediaPlayer);
            /*
                        _mediaPlaybackItem.AudioTracksChanged += PlaybackItem_AudioTracksChanged;
                        _mediaPlaybackItem.VideoTracksChanged += MediaPlaybackItem_VideoTracksChanged;
                        _mediaPlaybackItem.TimedMetadataTracksChanged += MediaPlaybackItem_TimedMetadataTracksChanged;
            */
        }

        public MediaPlayerWindow(string videoUri = null)
        {
            InitializeComponent();
            Title = "Media Player Window";
            appWindow = WindowHelperzn.WindowHelper.GetAppWindowForCurrentWindow(this);
            appWindow.SetIcon("icon.ico");

            overlappedPresenter = OverlappedPresenter.Create();
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                appWindow.SetPresenter(overlappedPresenter);
                appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            }
            //this.SystemBackdrop = new MicaBackdrop() { Kind = MicaKind.BaseAlt };

            MediaUri = videoUri;
            Activate();
        }

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

                m_acrylicController = new DesktopAcrylicController()
                {
                    TintColor = Color.FromArgb(255, 40, 40, 40),
                    LuminosityOpacity = 0.9f,
                    TintOpacity = 0f
                };

                m_acrylicController.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
                m_acrylicController.SetSystemBackdropConfiguration(m_configurationSource);
                return true;
            }

            return false;
        }
        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MPE.IsFullWindow = true;
            //appWindow.SetPresenter(appWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen ? AppWindowPresenterKind.Default : AppWindowPresenterKind.FullScreen);
            //FullScreenIcon.Symbol = appWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen ? Symbol.Redo : Symbol.FullScreen;
            //(sender as Button).Margin = appWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen ? new(0) : new(0, 0, 138, 0);
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (AppWindowTitleBar.IsCustomizationSupported()
                && appWindow.TitleBar.ExtendsContentIntoTitleBar)
            {
                double scaleAdjustment = CodeHelper.GetScaleAdjustment(this);
                double rpc = appWindow.TitleBar.RightInset / scaleAdjustment;
                double lpc = appWindow.TitleBar.LeftInset / scaleAdjustment;

                //RightPaddingColumn.Width = new GridLength(appWindow.TitleBar.RightInset / scaleAdjustment);
                //LeftPaddingColumn.Width = new GridLength(appWindow.TitleBar.LeftInset / scaleAdjustment);

                List<RectInt32> dragRectsList = new();

                RectInt32 dragRectL;
                dragRectL.X = (int)(lpc * scaleAdjustment);
                dragRectL.Y = 0;
                dragRectL.Height = (int)(32 * scaleAdjustment);
                dragRectL.Width = (int)((lpc + appWindow.Size.Width - 46 * 4 - 14) * scaleAdjustment);
                dragRectsList.Add(dragRectL);

                RectInt32 dragRectR;
                // TOWAIT: when microsoft fix this winui3 bug
                dragRectR.X = (int)((lpc + appWindow.Size.Width - 52 * 4) * scaleAdjustment);
                dragRectR.Y = 0;
                dragRectR.Height = (int)(32 * scaleAdjustment);
                dragRectR.Width = 0;
                dragRectsList.Add(dragRectR);

                RectInt32[] dragRects = dragRectsList.ToArray();

                appWindow.TitleBar.SetDragRectangles(dragRects);
            }
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            MPE.MediaPlayer.Source = null;
        }
    }
}
