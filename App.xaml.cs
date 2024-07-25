using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TewiMP.Media;
using TewiMP.Helpers;
using TewiMP.DataEditor;
using TewiMP.Windowed;
using TewiMP.Background;
using TewiMP.Background.HotKeys;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Newtonsoft.Json.Linq;
using TewiMP.Pages;

namespace TewiMP
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public static Windows.Media.Playback.MediaPlayer BMP { get; private set; } = null;
        public static Windows.Media.SystemMediaTransportControls SMTC { get; private set; } = null;
        public static MetingServices metingServices { get; private set; } = null;
        public static CacheManager cacheManager { get; private set; } = null;
        public static AudioPlayer audioPlayer { get; private set; } = null;
        public static PlayingList playingList { get; private set; } = null;
        public static LyricManager lyricManager { get; private set; } = null;
        public static DownloadManager downloadManager { get; private set; } = null;
        public static PlayListReader playListReader { get; private set; } = null;
        public static LocalMusicManager localMusicManager { get; private set; } = null;
        public static HotKeyManager hotKeyManager { get; private set; } = null;
        public static App AppStatic { get; private set; } = null;
        public static string AppName { get; } = "TewiMP";
        public static string AppVersion { get; } = "0.4.3 Preview";

        public static Window WindowLocal;
        public static NotifyIconWindow NotifyIconWindow;
        public static TaskBarInfoWindow taskBarInfoWindow;


        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            //Media.Decoder.FFmpeg.FFmpegBinariesHelper.InitFFmpeg();
            InitializeComponent();
            AppStatic = this;
            UnhandledException += App_UnhandledException;
            TaskScheduler.UnobservedTaskException +=
                (object sender, UnobservedTaskExceptionEventArgs excArgs) =>
                {
                    LogHelper.WriteLog("UnobservedTaskError", excArgs.Exception.ToString(), false);
    #if DEBUG
                    Debug.WriteLine("UnobservedTaskError: " + excArgs.Exception.ToString());
    #endif
                };
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            LogHelper.WriteLog("UnhandledError", e.Exception.ToString(), false);
#if DEBUG
            Debug.WriteLine("UnhandledError: " + e.ToString());
#endif
        }

        public static List<string> LaunchArgs = null;
        public static JObject StartingSettings = null;
        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);
            DataFolderBase.InitFiles();
            metingServices = new();
            cacheManager = new();
            audioPlayer = new();
            playingList = new();
            localMusicManager = new();
            lyricManager = new();
            downloadManager = new();
            playListReader = new();
            hotKeyManager = new();

            BMP = Windows.Media.Playback.BackgroundMediaPlayer.Current;
            SMTC = BMP?.SystemMediaTransportControls;

            SMTC.IsPlayEnabled = true;
            SMTC.IsPauseEnabled = true;
            SMTC.IsNextEnabled = true;
            SMTC.IsPreviousEnabled = true;
            SMTC.IsStopEnabled = true;
            SMTC.DisplayUpdater.Type = Windows.Media.MediaPlaybackType.Music;
            SMTC.DisplayUpdater.MusicProperties.Title = AppName;
            SMTC.DisplayUpdater.MusicProperties.Artist = "没有正在播放的歌曲";
            SMTC.DisplayUpdater.Update();

            audioPlayer.CacheLoadingChanged += (_, __) =>
            {
                SMTC.DisplayUpdater.MusicProperties.Title = _.MusicData?.Title;
                SMTC.DisplayUpdater.MusicProperties.Artist = "加载中...";
                SMTC.DisplayUpdater.Update();
            };
            audioPlayer.CacheLoadedChanged += (_) =>
            {
                if (_.MusicData == null)
                {
                    SMTC.DisplayUpdater.MusicProperties.Title = _.FileReader?.FileName;
                    MainWindow.AppWindowLocal.Title = AppName;
                }
                else
                {
                    SMTC.DisplayUpdater.MusicProperties.Title = _.MusicData.Title;
                    SMTC.DisplayUpdater.MusicProperties.Artist = _.MusicData.ButtonName;
                    MainWindow.AppWindowLocal.Title = $"{_.MusicData.Title} - {_.MusicData.ArtistName} · {AppName}";
                }
                SMTC.DisplayUpdater.Update();
            };
            audioPlayer.PlayStateChanged += (_) =>
            {
                if (_.PlaybackState == NAudio.Wave.PlaybackState.Playing)
                {
                    SMTC.PlaybackStatus = Windows.Media.MediaPlaybackStatus.Playing;
                }
                else
                {
                    SMTC.PlaybackStatus = Windows.Media.MediaPlaybackStatus.Paused;
                }
            };
            playingList.NowPlayingImageLoading += (_, __) =>
            {
                SMTC.DisplayUpdater.Thumbnail = null;
                SMTC.DisplayUpdater.Update();
            };
            playingList.NowPlayingImageLoaded += async (_, __) =>
            {
                if (string.IsNullOrEmpty(__))
                {
                    SMTC.DisplayUpdater.Thumbnail = null;
                }
                else
                {
                    try
                    {
                        SMTC.DisplayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromFile(await StorageFile.GetFileFromPathAsync(__));
                    }
                    catch { }
                }

                SMTC.DisplayUpdater.Update();
            };

            StartingSettings = DataFolderBase.JSettingData;
            var accentColor = StartingSettings[DataFolderBase.SettingParams.ThemeAccentColor.ToString()];
            if (accentColor != null)
            {
                /*Current.Resources["SystemAccentColor"] = Windows.UI.Color.FromArgb(255, 2,255,2);
                Current.Resources["SystemAccentColorLight2"] = Windows.UI.Color.FromArgb(255, 2, 255, 2);
                Current.Resources["SystemAccentColorDark1"] = Windows.UI.Color.FromArgb(255, 2, 255, 2);*/


                Debug.WriteLine(Current.Resources["SystemAccentColorLight2"].GetType());
            }

            // WinUI Bug: 获取不到启动参数
            //LAE = args;
            LaunchArgs = Environment.GetCommandLineArgs().ToList();
            LaunchArgs.Remove(LaunchArgs.First());

            m_window = new MainWindow();
            WindowLocal = m_window;
            hotKeyManager.Init(WindowLocal);
            /*
                        List<Microsoft.WindowsAPICodePack.Taskbar.ThumbnailToolBarButton> buttons = new()
                        {
                            new (null, "上一首"),
                            new(null, "播放/暂停"),
                            new(null, "下一首")
                        };
                        Microsoft.WindowsAPICodePack.Taskbar.TaskbarManager.Instance.ThumbnailToolBars.AddButtons(AppWindowLocalHandle, buttons.ToArray());
            */

            if (loadFailed)
            {
                ShowErrorDialog();
                return;
            }

            m_window.Closed += M_window_Closed;
            //AppWindowLocal.SetPresenter(AppWindowLocalPresenter);
            DelayOpenWindows();
        }

        public async void DelayOpenWindows()
        {
#if DEBUG
            //await Task.Delay(2000);
#endif
            // 在 Windows App SDK 1.4 的版本一直闪退，1.3 则不会
            // 似乎有两个以上的窗口一起启动会导致崩溃，微软你干的好事😡
            await Task.Delay(1000);
            NotifyIconWindow = new();

            await Task.Delay(1000);
            taskBarInfoWindow = new();
        }

        private void M_window_Closed(object sender, WindowEventArgs args)
        {
            if (MainWindow.DesktopLyricWindow != null)
            {
                MainWindow.DesktopLyricWindow.Close();
            }

            NotifyIconWindow.HideIcon();
            MainWindow.SaveNowPlaying();
            WindowLocal.Close();
            SMTC.DisplayUpdater.ClearAll();
            SMTC.DisplayUpdater.Update();
            audioPlayer.DisposeAll();
            SaveSettings();
            Exit();
        }

        public static async void ShowErrorDialog()
        {
            MessageDialog messageDialog = new("设置文件出现了一些错误，且程序尝试 5 次后也无法恢复默认配置。\n" +
                "请尝试删除 文档->TewiMP->UserData 里的 Setting 文件。\n" +
                "如果仍然出现问题，请到 GitHub 里向项目提出 Issues。", "TewiMP - 程序无法启动");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(WindowLocal);
            WinRT.Interop.InitializeWithWindow.Initialize(messageDialog, hwnd);
            await messageDialog.ShowAsync();
            App.Current.Exit();
        }

        static bool loadFailed = false;
        static int retryCount = 0;
        public static void LoadSettings(JObject data)
        {
            try
            {
                JObject b = data;
                DataFolderBase.DownloadFolder = SettingEditHelper.GetSetting<string>(b, DataFolderBase.SettingParams.DownloadFolderPath);
                DataFolderBase.AudioCacheFolder = SettingEditHelper.GetSetting<string>(b, DataFolderBase.SettingParams.AudioCacheFolderPath);
                DataFolderBase.ImageCacheFolder = SettingEditHelper.GetSetting<string>(b, DataFolderBase.SettingParams.ImageCacheFolderPath);
                DataFolderBase.LyricCacheFolder = SettingEditHelper.GetSetting<string>(b, DataFolderBase.SettingParams.LyricCacheFolderPath);

                var bData = SettingEditHelper.GetSetting<string>(b, DataFolderBase.SettingParams.EqualizerCustomData).Split(',');
                for (int i = 0; i < 10; i++)
                {
                    AudioEqualizerBands.CustomBands[i][2] = float.Parse(bData[i]);
                }

                audioPlayer.Volume = SettingEditHelper.GetSetting<float>(b, DataFolderBase.SettingParams.Volume);
                audioPlayer.EqEnabled = SettingEditHelper.GetSetting<bool>(b, DataFolderBase.SettingParams.EqualizerEnable);
                audioPlayer.EqualizerBand = AudioEqualizerBands.GetBandFromString(SettingEditHelper.GetSetting<string>(b, DataFolderBase.SettingParams.EqualizerString));
                audioPlayer.WasapiOnly = SettingEditHelper.GetSetting<bool>(b, DataFolderBase.SettingParams.WasapiOnly);
                audioPlayer.Latency = SettingEditHelper.GetSetting<int>(b, DataFolderBase.SettingParams.AudioLatency);
                MainWindow.SMusicPage.ShowLrcPage = SettingEditHelper.GetSetting<bool>(b, DataFolderBase.SettingParams.MusicPageShowLyricPage);

                string nmc = "NeteaseMusicCookie";
                if (b.ContainsKey(nmc))
                {
                    metingServices.NeteaseCookie = (string)b[nmc];
                }

                downloadManager.DownloadQuality = (DataFolderBase.DownloadQuality)SettingEditHelper.GetSetting<int>(b, DataFolderBase.SettingParams.DownloadQuality);
                downloadManager.DownloadingMaximum = SettingEditHelper.GetSetting<int>(b, DataFolderBase.SettingParams.DownloadMaximum);
                downloadManager.DownloadNamedMethod = (DataFolderBase.DownloadNamedMethod)SettingEditHelper.GetSetting<int>(b, DataFolderBase.SettingParams.DownloadNamedMethod);
                downloadManager.IDv3WriteImage = (bool)SettingEditHelper.GetSetting<JArray>(b, DataFolderBase.SettingParams.DownloadOptions)[0];
                downloadManager.IDv3WriteArtistImage = (bool)SettingEditHelper.GetSetting<JArray>(b, DataFolderBase.SettingParams.DownloadOptions)[1];
                downloadManager.IDv3WriteLyric = (bool)SettingEditHelper.GetSetting<JArray>(b, DataFolderBase.SettingParams.DownloadOptions)[2];
                downloadManager.SaveLyricToLrcFile = (bool)SettingEditHelper.GetSetting<JArray>(b, DataFolderBase.SettingParams.DownloadOptions)[3];
                playingList.PlayBehavior = (PlayBehavior)SettingEditHelper.GetSetting<int>(b, DataFolderBase.SettingParams.PlayBehavior);
                playingList.PauseWhenPreviousPause = SettingEditHelper.GetSetting<bool>(b, DataFolderBase.SettingParams.PlayPauseWhenPreviousPause);
                playingList.NextWhenPlayError = SettingEditHelper.GetSetting<bool>(b, DataFolderBase.SettingParams.PlayNextWhenPlayError);
                MainWindow.SWindowGridBaseTop.RequestedTheme = (ElementTheme)SettingEditHelper.GetSetting<int>(b, DataFolderBase.SettingParams.ThemeColorMode);
                MainWindow.SMusicPage.RequestedTheme = (ElementTheme)SettingEditHelper.GetSetting<int>(b, DataFolderBase.SettingParams.ThemeMusicPageColorMode);
                MainWindow.m_currentBackdrop = (MainWindow.BackdropType)SettingEditHelper.GetSetting<int>(b, DataFolderBase.SettingParams.ThemeBackdropEffect);
                MainWindow.ImagePath = SettingEditHelper.GetSetting<string>(b, DataFolderBase.SettingParams.ThemeBackdropImagePath);
                MainWindow.SBackgroundMass.Opacity = SettingEditHelper.GetSetting<double>(b, DataFolderBase.SettingParams.ThemeBackdropImageMassOpacity);
                //Accent Color
                DesktopLyricWindow.PauseButtonVisible = (bool)SettingEditHelper.GetSetting<JArray>(b, DataFolderBase.SettingParams.DesktopLyricOptions)[0];
                DesktopLyricWindow.ProgressUIVisible = (bool)SettingEditHelper.GetSetting<JArray>(b, DataFolderBase.SettingParams.DesktopLyricOptions)[1];
                DesktopLyricWindow.ProgressUIPercentageVisible = (bool)SettingEditHelper.GetSetting<JArray>(b, DataFolderBase.SettingParams.DesktopLyricOptions)[2];
                DesktopLyricWindow.MusicChangeUIVisible = (bool)SettingEditHelper.GetSetting<JArray>(b, DataFolderBase.SettingParams.DesktopLyricOptions)[3];
                DesktopLyricWindow.LyricTextBehavior = (LyricTextBehavior)(int)SettingEditHelper.GetSetting<JArray>(b, DataFolderBase.SettingParams.DesktopLyricText)[0];
                DesktopLyricWindow.LyricTextPosition = (LyricTextPosition)(int)SettingEditHelper.GetSetting<JArray>(b, DataFolderBase.SettingParams.DesktopLyricText)[1];
                DesktopLyricWindow.LyricTranslateTextBehavior = (LyricTranslateTextBehavior)(int)SettingEditHelper.GetSetting<JArray>(b, DataFolderBase.SettingParams.DesktopLyricTranslateText)[0];
                DesktopLyricWindow.LyricTranslateTextPosition = (LyricTranslateTextPosition)(int)SettingEditHelper.GetSetting<JArray>(b, DataFolderBase.SettingParams.DesktopLyricTranslateText)[1];
                DesktopLyricWindow.LyricOpacity = SettingEditHelper.GetSetting<double>(b, DataFolderBase.SettingParams.DesktopLyricOpacity);
                NotifyIconWindow.IsVisible = SettingEditHelper.GetSetting<bool>(b, DataFolderBase.SettingParams.TaskbarShowIcon);
                MainWindow.RunInBackground = SettingEditHelper.GetSetting<bool>(b, DataFolderBase.SettingParams.BackgroundRun);
                Controls.ImageEx.ImageDarkMass = SettingEditHelper.GetSetting<bool>(b, DataFolderBase.SettingParams.ImageDarkMass);
                LoadLastExitPlayingSongAndSongList = SettingEditHelper.GetSetting<bool>(b, DataFolderBase.SettingParams.LoadLastExitPlayingSongAndSongList);
                MainWindow.SNavView.PaneDisplayMode = SettingEditHelper.GetSetting<bool>(b, DataFolderBase.SettingParams.TopNavigationStyle) ? NavigationViewPaneDisplayMode.Top : NavigationViewPaneDisplayMode.Auto;
                LocalAudioPage.ItemSortBy = SettingEditHelper.GetSetting<int>(b, DataFolderBase.SettingParams.LocalMusicPageItemSortBy);
                JArray hkd = SettingEditHelper.GetSetting<JArray>(b, DataFolderBase.SettingParams.HotKeySettings);
                HotKeyManager.WillRegisterHotKeysList = hkd.ToObject<List<HotKey>>();

                metingServices.InitMeting();
            }
            catch (Exception e)
            {
                LogHelper.WriteLog("SettingError", e.ToString(), false);
                if (retryCount >= 5)
                {
                    loadFailed = true;
                    return;
                }
                retryCount++;
                DataFolderBase.JSettingData = DataFolderBase.SettingDefault;
                LoadSettings(DataFolderBase.JSettingData);
            }
        }

        public static Windows.UI.Color AccentColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
        public static void SaveSettings()
        {
            var a = DataFolderBase.JSettingData;
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.Volume, audioPlayer.Volume == 0 ? MainWindow.NoVolumeValue : audioPlayer.Volume);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.DownloadFolderPath, DataFolderBase.DownloadFolder);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.AudioCacheFolderPath, DataFolderBase.AudioCacheFolder);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.ImageCacheFolderPath, DataFolderBase.ImageCacheFolder);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.LyricCacheFolderPath, DataFolderBase.LyricCacheFolder);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.DownloadOptions,
                new JArray() {
                    downloadManager.IDv3WriteImage,
                    downloadManager.IDv3WriteArtistImage,
                    downloadManager.IDv3WriteLyric,
                    downloadManager.SaveLyricToLrcFile
                    });
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.DownloadNamedMethod, (int)downloadManager.DownloadNamedMethod);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.DownloadQuality, (int)downloadManager.DownloadQuality);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.DownloadMaximum, downloadManager.DownloadingMaximum);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.PlayBehavior, (int)playingList.PlayBehavior);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.PlayPauseWhenPreviousPause, playingList.PauseWhenPreviousPause);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.PlayNextWhenPlayError, playingList.NextWhenPlayError);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.DownloadMaximum, downloadManager.DownloadingMaximum);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.EqualizerEnable, audioPlayer.EqEnabled);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.EqualizerString, AudioEqualizerBands.GetNameFromBands(audioPlayer.EqualizerBand));
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.WasapiOnly, audioPlayer.WasapiOnly);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.AudioLatency, audioPlayer.Latency < 50 ? 50 : audioPlayer.Latency);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.MusicPageShowLyricPage, MainWindow.SMusicPage.ShowLrcPage);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.ThemeColorMode, (int)MainWindow.SWindowGridBaseTop.RequestedTheme);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.ThemeMusicPageColorMode, (int)MainWindow.SMusicPage.pageRoot.RequestedTheme);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.ThemeBackdropEffect, (int)MainWindow.m_currentBackdrop);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.ThemeBackdropImagePath, MainWindow.ImagePath);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.ThemeBackdropImageMassOpacity, MainWindow.SBackgroundMass.Opacity);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.ThemeAccentColor, AccentColor == Windows.UI.Color.FromArgb(0,0,0,0) ? null : AccentColor.ToString());
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.DesktopLyricOptions, new JArray()
            {
                DesktopLyricWindow.PauseButtonVisible, DesktopLyricWindow.ProgressUIVisible,
                DesktopLyricWindow.ProgressUIPercentageVisible, DesktopLyricWindow.MusicChangeUIVisible
            });
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.DesktopLyricText, new JArray()
            {
                DesktopLyricWindow.LyricTextBehavior,
                DesktopLyricWindow.LyricTextPosition
            });
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.DesktopLyricTranslateText, new JArray()
            {
                DesktopLyricWindow.LyricTranslateTextBehavior,
                DesktopLyricWindow.LyricTranslateTextPosition
            });
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.DesktopLyricOpacity, DesktopLyricWindow.LyricOpacity);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.TaskbarShowIcon, NotifyIconWindow.IsVisible);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.BackgroundRun, MainWindow.RunInBackground);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.ImageDarkMass, Controls.ImageEx.ImageDarkMass);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.LoadLastExitPlayingSongAndSongList, LoadLastExitPlayingSongAndSongList);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.HotKeySettings, JArray.FromObject(App.hotKeyManager.RegistedHotKeys));
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.TopNavigationStyle, MainWindow.SNavView.PaneDisplayMode == NavigationViewPaneDisplayMode.Top);
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.LocalMusicPageItemSortBy, LocalAudioPage.ItemSortBy);

            List<float> c = new();
            foreach (var d in AudioEqualizerBands.CustomBands)
            {
                c.Add(d[2]);
            }
            string b = string.Join(",", c.ToArray());
            SettingEditHelper.EditSetting(a, DataFolderBase.SettingParams.EqualizerCustomData, b);
            DataFolderBase.JSettingData = a;
#if DEBUG
            Debug.WriteLine("[SaveSettingData]: 设置配置已存储！");
#endif
        }

        public static bool LoadLastExitPlayingSongAndSongList = true;

        public static void SetFramePerSecondViewer(bool visible = false)
        {
            AppStatic.DebugSettings.EnableFrameRateCounter = visible;
        }

        private Window m_window;
        public static string[] SupportedMediaFormats = new string[] {
            // 3GP
            ".3g2", ".3gp", ".3gp2", ".3gpp",
            // ASF
            ".asf", ".wma", ".wmv",
            // ADTS
            ".aac", ".adts",
            // MP3
            ".mp3",
            // MPEG-4
            ".m4a", ".m4v", ".mov", ".mp4", ".mkv",
            // SAMI
            ".sami", ".smi",
            // other
            ".wav", ".ogg", ".flac", ".aiff", ".aif", ".mid", ".cue", ".dts"
        };
    }
}
