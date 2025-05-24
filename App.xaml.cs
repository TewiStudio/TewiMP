using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NAudio.Wave;
using WinRT.Interop;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using TewiMP.Pages;
using TewiMP.Media;
using TewiMP.Helpers;
using TewiMP.Plugin;
using TewiMP.Windowed;
using TewiMP.DataEditor;
using TewiMP.Background;
using TewiMP.Background.HotKeys;

namespace TewiMP
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public static MediaPlayer BMP { get; private set; } = null;
        public static SystemMediaTransportControls SMTC { get; private set; } = null;
        public static CacheManager cacheManager { get; private set; } = null;
        public static AudioPlayer audioPlayer { get; private set; } = null;
        public static PlayingList playingList { get; private set; } = null;
        public static LyricManager lyricManager { get; private set; } = null;
        public static DownloadManager downloadManager { get; private set; } = null;
        public static PlayListReader playListReader { get; private set; } = null;
        public static LocalMusicManager localMusicManager { get; private set; } = null;
        public static HotKeyManager hotKeyManager { get; private set; } = null;
        public static LogManager logManager { get; private set; } = null;
        public static App AppStatic { get; private set; } = null;
        public static string AppName { get; } = "TewiMP";
        public static VersionData StableVersion { get; } = new()
        {
            Available = false,
            SuffixType = SuffixType.Stable,
            Version = "0",
            VersionF = 0f,
            ReleaseTime = DateTime.MinValue,
            ExtendMessage = null
        };
        public static VersionData PreviewVersion { get; } = new()
        {
            Available = false,
            SuffixType = SuffixType.Preview,
            Version = "0",
            VersionF = 0f,
            ReleaseTime = DateTime.MinValue,
            ExtendMessage = null
        };
        public static VersionData BetaVersion { get; } = new()
        {
            Available = false,
            SuffixType = SuffixType.Beta,
            Version = "0",
            VersionF = 0f,
            ReleaseTime = DateTime.MinValue,
            ExtendMessage = null
        };
        public static VersionData Version { get; set; } = new()
        {
            Available = true,
            SuffixType = SuffixType.Beta,
            Version = "0.0.1",
            VersionF = 1f,
            ReleaseTime = 1744627462L.ToDateTime(),
            ExtendMessage = null
        };
        public static string AppVersion => Version.Version;
        public static float AppVersionF => Version.VersionF;
        public static DateTime AppVersionReleaseDate => Version.ReleaseTime;



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
                    App.logManager.Log("App", "UnobservedTaskError: " + excArgs.Exception.ToString(), LogLevel.Error);
    #endif
                };
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            LogHelper.WriteLog("UnhandledError", e.Exception.ToString(), false);
#if DEBUG
            App.logManager.Log("App", "UnhandledError: " + e.ToString(), LogLevel.Error);
#endif
        }

        public static List<string> LaunchArgs = null;
        public static JObject StartingSettings = null;
        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open settingData specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);
            logManager = new();
            DataFolderBase.InitFiles();
            LogManager.InitNowLog();
            cacheManager = new();
            audioPlayer = new();
            playingList = new();
            localMusicManager = new();
            lyricManager = new();
            downloadManager = new();
            playListReader = new();
            hotKeyManager = new();

            BMP = BackgroundMediaPlayer.Current;
            BMP.AudioCategory = MediaPlayerAudioCategory.Media;

            SMTC = BMP?.SystemMediaTransportControls;
            SMTC.IsPlayEnabled = true;
            SMTC.IsPauseEnabled = true;
            SMTC.IsNextEnabled = true;
            SMTC.IsPreviousEnabled = true;
            SMTC.IsStopEnabled = true;
            SMTC.DisplayUpdater.Type = MediaPlaybackType.Music;
            SMTC.DisplayUpdater.AppMediaId = AppName;
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
                if (_.MusicData is null)
                {
                    SMTC.DisplayUpdater.MusicProperties.Title = _.FileReader?.FileName;
                    MainWindow.AppWindowInstance.Title = AppName;
                }
                else
                {
                    SMTC.DisplayUpdater.MusicProperties.Title = _.MusicData.Title;
                    SMTC.DisplayUpdater.MusicProperties.Artist = _.MusicData.ButtonName;
                    MainWindow.AppWindowInstance.Title = $"{_.MusicData.Title} - {_.MusicData.ArtistName} · {AppName}";
                }
                SMTC.DisplayUpdater.Update();
            };
            audioPlayer.PlayStateChanged += (_) =>
            {
                if (_.PlaybackState == PlaybackState.Playing)
                {
                    SMTC.PlaybackStatus = MediaPlaybackStatus.Playing;
                }
                else
                {
                    SMTC.PlaybackStatus = MediaPlaybackStatus.Paused;
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

                //App.logManager.Log(Current.Resources["SystemAccentColorLight2"].GetType());
            }

            // WinUI Bug: 获取不到启动参数
            //LAE = args;
            LaunchArgs = [.. Environment.GetCommandLineArgs()];
            LaunchArgs.Remove(LaunchArgs.First());

            m_window = new MainWindow();
            WindowLocal = m_window;
            hotKeyManager.Init(WindowLocal);
            if (loadFailed)
            {
                ShowErrorDialog();
                return;
            }
            DelayOpenWindows();
            PluginManager.Init();
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

        public static void ExitApp()
        {
            SaveSettings();
            MainWindow.SetBackdrop(MainWindow.BackdropType.DefaultColor); // 在App.Exit前将MainWindow的Backdrop释放，否则会报错
            MainWindow.SaveNowPlaying();
            MainWindow.DesktopLyricWindow?.Close();
            NotifyIconWindow.HideIcon();
            NotifyIconWindow.Close();
            taskBarInfoWindow.Close();
            MainWindow.SWindow.Close();
            SMTC.DisplayUpdater.ClearAll();
            SMTC.DisplayUpdater.Update();
            audioPlayer.DisposeAll();
            hotKeyManager.UnregisterHotKeys([.. hotKeyManager.RegisteredHotKeys]);
            logManager.Log("App", "正在退出程序...");
            LogManager.DisposeNowLogStream();
            Current.Exit();
        }

        public static async void ShowErrorDialog()
        {
            MessageDialog messageDialog = new("设置文件出现了一些错误，且程序尝试 5 次后也无法恢复默认配置。\n" +
                $"请尝试删除 文档->{AppName}->UserData 里的 Setting 文件。\n" +
                "如果仍然出现问题，请到 GitHub 里向项目提出 Issues。", $"{AppName} - 程序无法启动");
            var hwnd = WindowNative.GetWindowHandle(WindowLocal);
            InitializeWithWindow.Initialize(messageDialog, hwnd);
            await messageDialog.ShowAsync();
            Current.Exit();
        }

        static bool loadFailed = false;
        static int retryCount = 0;
        public static void LoadSettings(bool loadDefaultSettings = false)
        {
            logManager.Log("App", "正在读取设置...");
            try
            {
                JObject settingData = loadDefaultSettings ? DataFolderBase.SettingDefault : DataFolderBase.JSettingData;
                JObject audioEffectData = loadDefaultSettings ? DataFolderBase.AudioEffectDefault : DataFolderBase.JAudioEffectData;

                var cd = SettingEditHelper.GetSetting<string>(settingData, DataFolderBase.SettingParams.CacheFolderPath);
                if (!string.IsNullOrEmpty(cd))
                {
                    if (Path.Exists(cd))
                    {
                        DataFolderBase.CacheFolder = cd;
                    }
                }
                DataFolderBase.DownloadFolder = SettingEditHelper.GetSetting<string>(settingData, DataFolderBase.SettingParams.DownloadFolderPath);
                //DataFolderBase.AudioCacheFolder = SettingEditHelper.GetSetting<string>(settingData, DataFolderBase.SettingParams.AudioCacheFolderPath);
                //DataFolderBase.ImageCacheFolder = SettingEditHelper.GetSetting<string>(settingData, DataFolderBase.SettingParams.ImageCacheFolderPath);
                //DataFolderBase.LyricCacheFolder = SettingEditHelper.GetSetting<string>(settingData, DataFolderBase.SettingParams.LyricCacheFolderPath);

                audioPlayer.Volume = SettingEditHelper.GetSetting<float>(settingData, DataFolderBase.SettingParams.Volume);
                audioPlayer.EqEnabled = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.EqualizerEnable);
                MainWindow.SMusicPage.ShowLrcPage = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.MusicPageShowLyricPage);

                downloadManager.DownloadQuality = (DataFolderBase.DownloadQuality)SettingEditHelper.GetSetting<int>(settingData, DataFolderBase.SettingParams.DownloadQuality);
                downloadManager.DownloadingMaximum = SettingEditHelper.GetSetting<int>(settingData, DataFolderBase.SettingParams.DownloadMaximum);
                downloadManager.DownloadNamedMethod = (DataFolderBase.DownloadNamedMethod)SettingEditHelper.GetSetting<int>(settingData, DataFolderBase.SettingParams.DownloadNamedMethod);
                downloadManager.IDv3WriteImage = (bool)SettingEditHelper.GetSetting<JArray>(settingData, DataFolderBase.SettingParams.DownloadOptions)[0];
                downloadManager.IDv3WriteArtistImage = (bool)SettingEditHelper.GetSetting<JArray>(settingData, DataFolderBase.SettingParams.DownloadOptions)[1];
                downloadManager.IDv3WriteLyric = (bool)SettingEditHelper.GetSetting<JArray>(settingData, DataFolderBase.SettingParams.DownloadOptions)[2];
                downloadManager.SaveLyricToLrcFile = (bool)SettingEditHelper.GetSetting<JArray>(settingData, DataFolderBase.SettingParams.DownloadOptions)[3];
                playingList.PlayBehavior = (PlayBehavior)SettingEditHelper.GetSetting<int>(settingData, DataFolderBase.SettingParams.PlayBehavior);
                playingList.PauseWhenPreviousPause = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.PlayPauseWhenPreviousPause);
                playingList.NextWhenPlayError = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.PlayNextWhenPlayError);
                MainWindow.SWindowGridBaseTop.RequestedTheme = (ElementTheme)SettingEditHelper.GetSetting<int>(settingData, DataFolderBase.SettingParams.ThemeColorMode);
                MainWindow.SMusicPage.RequestedTheme = (ElementTheme)SettingEditHelper.GetSetting<int>(settingData, DataFolderBase.SettingParams.ThemeMusicPageColorMode);
                MainWindow.m_currentBackdrop = (MainWindow.BackdropType)SettingEditHelper.GetSetting<int>(settingData, DataFolderBase.SettingParams.ThemeBackdropEffect);
                MainWindow.ImagePath = SettingEditHelper.GetSetting<string>(settingData, DataFolderBase.SettingParams.ThemeBackdropImagePath);
                MainWindow.SBackgroundMass.Opacity = SettingEditHelper.GetSetting<double>(settingData, DataFolderBase.SettingParams.ThemeBackdropImageMassOpacity);
                //Accent Color
                DesktopLyricWindow.PauseButtonVisible = (bool)SettingEditHelper.GetSetting<JArray>(settingData, DataFolderBase.SettingParams.DesktopLyricOptions)[0];
                DesktopLyricWindow.ProgressUIVisible = (bool)SettingEditHelper.GetSetting<JArray>(settingData, DataFolderBase.SettingParams.DesktopLyricOptions)[1];
                DesktopLyricWindow.ProgressUIPercentageVisible = (bool)SettingEditHelper.GetSetting<JArray>(settingData, DataFolderBase.SettingParams.DesktopLyricOptions)[2];
                DesktopLyricWindow.MusicChangeUIVisible = (bool)SettingEditHelper.GetSetting<JArray>(settingData, DataFolderBase.SettingParams.DesktopLyricOptions)[3];
                DesktopLyricWindow.LyricTextBehavior = (LyricTextBehavior)(int)SettingEditHelper.GetSetting<JArray>(settingData, DataFolderBase.SettingParams.DesktopLyricText)[0];
                DesktopLyricWindow.LyricTextPosition = (LyricTextPosition)(int)SettingEditHelper.GetSetting<JArray>(settingData, DataFolderBase.SettingParams.DesktopLyricText)[1];
                DesktopLyricWindow.LyricTranslateTextBehavior = (LyricTranslateTextBehavior)(int)SettingEditHelper.GetSetting<JArray>(settingData, DataFolderBase.SettingParams.DesktopLyricTranslateText)[0];
                DesktopLyricWindow.LyricTranslateTextPosition = (LyricTranslateTextPosition)(int)SettingEditHelper.GetSetting<JArray>(settingData, DataFolderBase.SettingParams.DesktopLyricTranslateText)[1];
                DesktopLyricWindow.LyricOpacity = SettingEditHelper.GetSetting<double>(settingData, DataFolderBase.SettingParams.DesktopLyricOpacity);
                NotifyIconWindow.IsVisible = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.TaskbarShowIcon);
                MainWindow.RunInBackground = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.BackgroundRun);
                Controls.ImageEx.ImageDarkMass = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.ImageDarkMass);
                LoadLastExitPlayingSongAndSongList = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.LoadLastExitPlayingSongAndSongList);
                MainWindow.SNavView.PaneDisplayMode = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.TopNavigationStyle) ? NavigationViewPaneDisplayMode.Top : NavigationViewPaneDisplayMode.Auto;
                LocalAudioPage.ItemSortBy = SettingEditHelper.GetSetting<int>(settingData, DataFolderBase.SettingParams.LocalMusicPageItemSortBy);
                JArray hkd = SettingEditHelper.GetSetting<JArray>(settingData, DataFolderBase.SettingParams.HotKeySettings);
                HotKeyManager.WillRegisterHotKeysList = hkd.ToObject<List<HotKey>>();
                hotKeyManager.EnableHotKey = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.HotKeyEnable);

                var audioEffects = SettingEditHelper.GetSetting<JArray>(audioEffectData, DataFolderBase.AudioEffectFlag.AudioEffectDatas);

                AudioFilterStatic.GraphicEqEnable = SettingEditHelper.GetSetting<bool>(audioEffectData, DataFolderBase.AudioEffectFlag.GraphicEqEnable);
                AudioFilterStatic.ParametricEqEnable = SettingEditHelper.GetSetting<bool>(audioEffectData, DataFolderBase.AudioEffectFlag.ParametricEqEnable);
                AudioFilterStatic.PassFilterEqEnable = SettingEditHelper.GetSetting<bool>(audioEffectData, DataFolderBase.AudioEffectFlag.PassFilterEqEnable);
                AudioFilterStatic.EffectEnable = SettingEditHelper.GetSetting<bool>(audioEffectData, DataFolderBase.AudioEffectFlag.EffectEnable);
                audioPlayer.WasapiOnly = SettingEditHelper.GetSetting<bool>(audioEffectData, DataFolderBase.AudioEffectFlag.WasapiOnlyEnable);
                audioPlayer.Latency = SettingEditHelper.GetSetting<int>(audioEffectData, DataFolderBase.AudioEffectFlag.Latency);
                audioPlayer.Pitch = (double)audioEffects[0];
                audioPlayer.Tempo = (double)audioEffects[1];
                audioPlayer.Rate = (double)audioEffects[2];
                audioPlayer.EqualizerBand = AudioEqualizerBands.GetBandFromString(SettingEditHelper.GetSetting<string>(audioEffectData, DataFolderBase.AudioEffectFlag.GraphicEqString));
                var bData = SettingEditHelper.GetSetting<string>(audioEffectData, DataFolderBase.AudioEffectFlag.GraphicEqDatas).Split(','); 
                for (int i = 0; i < 10; i++) AudioEqualizerBands.CustomBands[i][2] = float.Parse(bData[i]);
                AudioFilterStatic.ParametricEqDatas = SettingEditHelper.GetSetting<JArray>(audioEffectData, DataFolderBase.AudioEffectFlag.ParametricEqDatas).ToObject<ObservableCollection<EQData>>();
                AudioFilterStatic.PassFilterDatas = SettingEditHelper.GetSetting<JArray>(audioEffectData, DataFolderBase.AudioEffectFlag.PassFilterEqDatas).ToObject<ObservableCollection<PassFilterData>>();
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
                LoadSettings(true);
            }
            logManager.Log("App", "读取设置完成。");
        }

        public static Windows.UI.Color AccentColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
        public static void SaveSettings()
        {
            logManager.Log("App", "正在保存设置...");
            var settingData = DataFolderBase.JSettingData;
            var audioEffectData = DataFolderBase.JAudioEffectData;
            if (DataFolderBase.CacheFolder != DataFolderBase.DefaultCacheFolder)
            {
                SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.CacheFolderPath, DataFolderBase.CacheFolder);
            }
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.Volume, audioPlayer.Volume == 0 ? MainWindow.NoVolumeValue : audioPlayer.Volume);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.DownloadFolderPath, DataFolderBase.DownloadFolder);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.AudioCacheFolderPath, DataFolderBase.AudioCacheFolder);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.ImageCacheFolderPath, DataFolderBase.ImageCacheFolder);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.LyricCacheFolderPath, DataFolderBase.LyricCacheFolder);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.DownloadOptions,
                new JArray() {
                    downloadManager.IDv3WriteImage,
                    downloadManager.IDv3WriteArtistImage,
                    downloadManager.IDv3WriteLyric,
                    downloadManager.SaveLyricToLrcFile
                    });
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.DownloadNamedMethod, (int)downloadManager.DownloadNamedMethod);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.DownloadQuality, (int)downloadManager.DownloadQuality);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.DownloadMaximum, downloadManager.DownloadingMaximum);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.PlayBehavior, (int)playingList.PlayBehavior);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.PlayPauseWhenPreviousPause, playingList.PauseWhenPreviousPause);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.PlayNextWhenPlayError, playingList.NextWhenPlayError);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.DownloadMaximum, downloadManager.DownloadingMaximum);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.EqualizerEnable, audioPlayer.EqEnabled);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.EqualizerString, AudioEqualizerBands.GetNameFromBands(audioPlayer.EqualizerBand));
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.WasapiOnly, audioPlayer.WasapiOnly);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.AudioLatency, audioPlayer.Latency < 50 ? 50 : audioPlayer.Latency);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.MusicPageShowLyricPage, MainWindow.SMusicPage.ShowLrcPage);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.ThemeColorMode, (int)MainWindow.SWindowGridBaseTop.RequestedTheme);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.ThemeMusicPageColorMode, (int)MainWindow.SMusicPage.pageRoot.RequestedTheme);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.ThemeBackdropEffect, (int)MainWindow.m_currentBackdrop);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.ThemeBackdropImagePath, MainWindow.ImagePath);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.ThemeBackdropImageMassOpacity, MainWindow.SBackgroundMass.Opacity);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.ThemeAccentColor, AccentColor == Windows.UI.Color.FromArgb(0,0,0,0) ? null : AccentColor.ToString());
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.DesktopLyricOptions, new JArray()
            {
                DesktopLyricWindow.PauseButtonVisible, DesktopLyricWindow.ProgressUIVisible,
                DesktopLyricWindow.ProgressUIPercentageVisible, DesktopLyricWindow.MusicChangeUIVisible
            });
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.DesktopLyricText, new JArray()
            {
                DesktopLyricWindow.LyricTextBehavior,
                DesktopLyricWindow.LyricTextPosition
            });
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.DesktopLyricTranslateText, new JArray()
            {
                DesktopLyricWindow.LyricTranslateTextBehavior,
                DesktopLyricWindow.LyricTranslateTextPosition
            });
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.DesktopLyricOpacity, DesktopLyricWindow.LyricOpacity);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.TaskbarShowIcon, NotifyIconWindow.IsVisible);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.BackgroundRun, MainWindow.RunInBackground);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.ImageDarkMass, Controls.ImageEx.ImageDarkMass);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.LoadLastExitPlayingSongAndSongList, LoadLastExitPlayingSongAndSongList);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.HotKeyEnable, hotKeyManager.EnableHotKey);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.HotKeySettings, JArray.FromObject(App.hotKeyManager.RegisteredHotKeys));
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.TopNavigationStyle, MainWindow.SNavView.PaneDisplayMode == NavigationViewPaneDisplayMode.Top);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.LocalMusicPageItemSortBy, LocalAudioPage.ItemSortBy);
            
            List<float> c = [];
            foreach (var d in AudioEqualizerBands.CustomBands) c.Add(d[2]);
            string b = string.Join(",", c.ToArray());
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.GraphicEqEnable, AudioFilterStatic.GraphicEqEnable);
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.ParametricEqEnable, AudioFilterStatic.ParametricEqEnable);
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.PassFilterEqEnable, AudioFilterStatic.PassFilterEqEnable);
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.EffectEnable, AudioFilterStatic.EffectEnable);
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.WasapiOnlyEnable, audioPlayer.WasapiOnly);
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.Latency, audioPlayer.Latency < 50 ? 50 : audioPlayer.Latency);
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.AudioEffectDatas, new JArray() { audioPlayer.Pitch, audioPlayer.Tempo, audioPlayer.Rate });
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.GraphicEqString, AudioEqualizerBands.GetNameFromBands(audioPlayer.EqualizerBand));
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.GraphicEqDatas, b);
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.ParametricEqDatas, AudioFilterStatic.ParametricEqDatas);
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.PassFilterEqDatas, AudioFilterStatic.PassFilterDatas);

            PluginManager.SavePluginInfoSettings();

            DataFolderBase.JSettingData = settingData;
            DataFolderBase.JAudioEffectData = audioEffectData;
            logManager.Log("App", "设置配置已存储。");
        }

        public static bool LoadLastExitPlayingSongAndSongList = true;

        public static void SetFramePerSecondViewer(bool visible = false)
        {
            AppStatic.DebugSettings.EnableFrameRateCounter = visible;
        }

        public static async Task CheckUpdate(bool addNotify = true)
        {
            var data = await WebHelper.GetStringAsync("https://data.tewi.top/datas/TewiMP/update.json");
            if (string.IsNullOrEmpty(data)) return;
            logManager.Log("App", $"Update datas: {data}");
            var json = JObject.Parse(data);

            var stable = json["stable"];
            var preview = json["preview"];
            var beta = json["beta"];

            StableVersion.Available =     (bool)  stable["available"];
            StableVersion.Version =       (string)stable["version"];
            StableVersion.VersionF =      (float) stable["versionF"];
            StableVersion.ReleaseTime =   ((long) stable["releaseDate"]).ToDateTime();
            StableVersion.Url =           (string)stable["url"];
            StableVersion.ExtendMessage = (string)stable["extendMessage"];
            
            PreviewVersion.Available =     (bool)  preview["available"];
            PreviewVersion.Version =       (string)preview["version"];
            PreviewVersion.VersionF =      (float) preview["versionF"];
            PreviewVersion.ReleaseTime =   ((long) preview["releaseDate"]).ToDateTime();
            PreviewVersion.Url =           (string)preview["url"];
            PreviewVersion.ExtendMessage = (string)preview["extendMessage"];
            
            BetaVersion.Available =     (bool)  beta["available"];
            BetaVersion.Version =       (string)beta["version"];
            BetaVersion.VersionF =      (float) beta["versionF"];
            BetaVersion.ReleaseTime =   ((long) beta["releaseDate"]).ToDateTime();
            BetaVersion.Url =           (string)beta["url"];
            BetaVersion.ExtendMessage = (string)beta["extendMessage"];

            if (AppVersionIsNewest()) return;
            var newestVersion = GetNewVersionByReleaseData(Version.SuffixType);

            if (addNotify)
            {
                MainWindow.AddNotify(
                    "有新版本！",
                    $"可更新到版本 {newestVersion.Version} {newestVersion.SuffixType}，当前版本为 {Version.Version} {Version.SuffixType}。" +
                        (string.IsNullOrEmpty(newestVersion.ExtendMessage) ? "" : $"\n{newestVersion.ExtendMessage}"),
                    NotifySeverity.Warning, TimeSpan.FromMilliseconds(10000),
                    "前往下载页面 ⨠", async () =>
                    {
                        var success = await CodeHelper.OpenInBrowser(newestVersion.Url);
                    });
            }
        }

        /// <summary>
        /// 传入版本类型 <see cref="SuffixType"/>，返回此版本类型的最新版本 <see cref="VersionData"/>。需要访问服务器才能正常判断。
        /// </summary>
        /// <param name="releaseType"></param>
        /// <returns>此版本类型的最新版本 <see cref="VersionData"/></returns>
        public static VersionData GetNewVersionByReleaseData(SuffixType releaseType) => releaseType switch
        {
            SuffixType.Stable => StableVersion,
            SuffixType.Preview => PreviewVersion,
            SuffixType.Beta => BetaVersion,
            _ => Version
        };

        public static bool AppVersionIsNewest()
        {
            var newestVersion = GetNewVersionByReleaseData(Version.SuffixType);
            if (!newestVersion.Available) return false;
            return newestVersion.VersionF <= Version.VersionF;
        }

        public static async void SetStartupWithWindows(bool startup)
        {
            await Task.Run(() =>
            {
                if (startup)
                {
                    var location = System.Reflection.Assembly.GetEntryAssembly().Location;
                    location = location.Replace($"{AppName}.dll", $"{AppName}.exe");
                    if (File.Exists(DataFolderBase.StartupShortcutPath)) return;
                    FileHelper.CreateShortcut(DataFolderBase.StartupShortcutPath, location, "-OpenWithWindows");
                }
                else
                {
                    File.Delete(DataFolderBase.StartupShortcutPath);
                }
            });
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

    public enum SuffixType { Stable, Preview, Beta }

    public class VersionData
    {
        public SuffixType SuffixType { get; set; }
        public bool Available { get; set; }
        public string Version { get; set; }
        public float VersionF { get; set; }
        public string Url { get; set; }
        public DateTime ReleaseTime { get; set; }
        public string ExtendMessage { get; set; }

        public override string ToString()
        {
            return $"{Version} {SuffixType}";
        }
    }

    public static class DateConverter
    {
        public static long ToTimestamp(this DateTime time)
        {
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            TimeSpan ts = time - startTime;
            var timestamp = Convert.ToInt64(ts.TotalSeconds);
            return timestamp;
        }

        public static DateTime ToDateTime(this long unix)
        {
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            var time = startTime.AddSeconds(unix);
            return time;
        }
    }
}
