using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using WinRT.Interop;
using NAudio.Wave;
using TewiMP.Pages;
using TewiMP.Media;
using TewiMP.Helpers;
using TewiMP.Plugin;
using TewiMP.Windowed;
using TewiMP.DataEditor;
using TewiMP.Background;
using TewiMP.Background.HotKeys;
using Windows.ApplicationModel.Core;

namespace TewiMP
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// 用于 加载上次退出程序时播放的播放列表 设置
        /// </summary>
        public bool LoadLastExitPlayingSongAndSongList { get; set; } = true;

        #region Instances
        /// <summary>
        /// <see cref="App"/> 的单例实例。
        /// </summary>
        public static App Instance => (App)Application.Current;

        /// <summary>
        /// 从 <see cref="App.Instance"/> 获取 <see cref="MainWindow"/> 的实例。"
        /// </summary>
        public static MainWindow MainWindowInstance => (MainWindow)Instance.MainWindow;
        #endregion

        #region STMC
        public MediaPlayer BMP { get; private set; } = null;
        public SystemMediaTransportControls SMTC { get; private set; } = null;
        #endregion

        #region Backgrounds
        public CacheManager CacheManager { get; private set; } = null;
        public AudioPlayer AudioPlayer { get; private set; } = null;
        public PlayingList PlayingList { get; private set; } = null;
        public LyricManager LyricManager { get; private set; } = null;
        public DownloadManager DownloadManager { get; private set; } = null;
        public PlayListReader PlayListReader { get; private set; } = null;
        public LocalMusicManager LocalMusicManager { get; private set; } = null;
        public HotKeyManager HotKeyManager { get; private set; } = null;
        public LogManager LogManager { get; private set; } = null;
        #endregion

        #region Versions
        public string AppName { get; } = "TewiMP";
        public VersionData StableVersion { get; set; } = new()
        {
            Available = false,
            SuffixType = SuffixType.Stable,
            Version = new Version(0, 0, 0, 0),
            ReleaseTime = DateTime.MinValue,
            ExtendMessage = null
        };
        public VersionData PreviewVersion { get; set; } = new()
        {
            Available = false,
            SuffixType = SuffixType.Preview,
            Version = new Version(0, 0, 0, 0),
            ReleaseTime = DateTime.MinValue,
            ExtendMessage = null
        };
        public VersionData BetaVersion { get; set; } = new()
        {
            Available = false,
            SuffixType = SuffixType.Beta,
            Version = new Version(0, 0, 0, 0),
            ReleaseTime = DateTime.MinValue,
            ExtendMessage = null
        };
        public VersionData NowVersion { get; set; } = new()
        {
            Available = true,
            SuffixType = SuffixType.Beta,
            Version = Assembly.GetExecutingAssembly().GetName().Version,
            ReleaseTime = new(2025, 7, 11, 10, 45, 00),
            ExtendMessage = null
        };
        public Version AppVersion => NowVersion.Version;
        public DateTime AppVersionReleaseDate => NowVersion.ReleaseTime;
        #endregion

        #region Windows
        public static int MainWindowCount = 0;
        public Window MainWindow;
        public NotifyIconWindow NotifyIconWindow;
        public TaskBarInfoWindow taskBarInfoWindow;
        #endregion

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            UnhandledException += App_UnhandledException;
            CoreApplication.UnhandledErrorDetected += CoreApplication_UnhandledErrorDetected;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            InitializeComponent();
        }

        #region 异常处理
        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            LogManager.Error("App", $"UnhandledError: {e.Exception}");
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            LogManager.Error("App", $"UnobservedTaskError: {e.Exception}");
        }

        private void CoreApplication_UnhandledErrorDetected(object sender, UnhandledErrorDetectedEventArgs e)
        {
            LogManager.Error("App", $"CoreApplication UnhandledErrorDetected: {e.UnhandledError}");
        }

        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            LogManager.Error("App", $"AppDomain Fatal Error: {e.ExceptionObject}");
        }
        #endregion

        #region 启动和退出
        public List<string> LaunchArgs = null;
        public JObject StartingSettings = null;
        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open settingData specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);
            var time = DateTime.Now;
            LogManager = new();
            LogManager.Log("Staring", "准备初始化...");

            MainWindow = new MainWindow();

            DataFolderBase.InitFiles();
            LogManager.InitNowLog();
            CacheManager = new();
            AudioPlayer = new();
            PlayingList = new();
            LocalMusicManager = new();
            LyricManager = new();
            DownloadManager = new();
            PlayListReader = new();
            HotKeyManager = new();

            LogManager.Log("Starting", "初始化 SystemMediaTransportControls.");
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

            AudioPlayer.CacheLoadingChanged += (_, __) =>
            {
                SMTC.DisplayUpdater.MusicProperties.Title = _.MusicData?.Title;
                SMTC.DisplayUpdater.MusicProperties.Artist = "加载中...";
                SMTC.DisplayUpdater.Update();
            };
            AudioPlayer.CacheLoadedChanged += (_) =>
            {
                if (_.MusicData is null)
                {
                    SMTC.DisplayUpdater.MusicProperties.Title = _.FileReader?.FileName;
                    MainWindowInstance.AppWindow.Title = AppName;
                }
                else
                {
                    SMTC.DisplayUpdater.MusicProperties.Title = _.MusicData.Title;
                    SMTC.DisplayUpdater.MusicProperties.Artist = _.MusicData.ButtonName;
                    MainWindowInstance.AppWindow.Title = $"{_.MusicData.Title} - {_.MusicData.ArtistName} · {AppName}";
                }
                SMTC.DisplayUpdater.Update();
            };
            AudioPlayer.PlayStateChanged += (_) =>
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
            PlayingList.NowPlayingImageLoading += (_, __) =>
            {
                SMTC.DisplayUpdater.Thumbnail = null;
                SMTC.DisplayUpdater.Update();
            };
            SMTC.ButtonPressed += (_, __) =>
            {
                MainWindowInstance.Invoke(() =>
                {
                    switch (__.Button)
                    {
                        case SystemMediaTransportControlsButton.Play:
                            AudioPlayer.SetPlay();
                            break;
                        case SystemMediaTransportControlsButton.Pause:
                            AudioPlayer.SetPause();
                            break;
                        case SystemMediaTransportControlsButton.Previous:
                            PlayingList.PlayPrevious();
                            break;
                        case SystemMediaTransportControlsButton.Next:
                            PlayingList.PlayNext();
                            break;
                        case SystemMediaTransportControlsButton.Stop:
                            AudioPlayer.SetStop();
                            break;
                    }
                });
            };
            PlayingList.NowPlayingImageLoaded += async (_, __) =>
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

                //LogManager.Log(Current.Resources["SystemAccentColorLight2"].GetType());
            }
            LoadSettings();

            // WinUI Bug: 获取不到启动参数
            //LAE = args;=
            LaunchArgs = [.. Environment.GetCommandLineArgs()];
            LaunchArgs.Remove(LaunchArgs.First());
            LogManager.Log("Starting", $"启动参数：{string.Join(", ", LaunchArgs)}.");

            HotKeyManager.Init(MainWindow);
            if (loadFailed)
            {
                ShowErrorDialog();
                return;
            }
            NotifyIconWindow = new();
            taskBarInfoWindow = new();
            LaunchAsync();
            PluginManager.Init();
            LoadLastPlaying();
            LogManager.Log("Starting", $"初始化完成。耗时：{DateTime.Now - time}");
        }

        public async void LaunchAsync()
        {
            //await PlayListReader.Refresh();
            await PlayingList.UpdateImageColor();
            await LocalMusicManager.Refresh();
            await CheckUpdate();
        }

        public async Task ExitApp()
        {
            LogManager.Log("App", "正在退出程序...");
            SaveSettings();
            MainWindowInstance.SetBackdrop(BackdropType.DefaultColor); // 在App.Instance.Exit前将MainWindow的Backdrop释放，否则会报错
            MainWindowInstance.DesktopLyricWindow?.Close();
            NotifyIconWindow.HideIcon();
            NotifyIconWindow.Close();
            taskBarInfoWindow.Close();
            SMTC.DisplayUpdater.ClearAll();
            SMTC.DisplayUpdater.Update();
            AudioPlayer.DisposeAll();
            HotKeyManager.UnregisterHotKeys([.. HotKeyManager.RegisteredHotKeys]);
            await SaveNowPlaying();
            MainWindowInstance.Close();
            LogManager.DisposeNowLogStream();
            Current.Exit();
        }

        public async void SetStartupWithWindows(bool startup)
        {
            await Task.Run(() =>
            {
                if (startup)
                {
                    var location = Assembly.GetEntryAssembly().Location;
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
        #endregion

        #region 加载和保存上次播放设置
        public async void LoadLastPlaying()
        {
            if (!LoadLastExitPlayingSongAndSongList) return;
            //if (isOpeningMusicLoaded) return;

            var path = Path.Combine(DataFolderBase.UserDataFolder, "LastPlaying");
            if (!File.Exists(path)) return;

            MusicData musicData = null;
            JObject jObject = null;
            await Task.Run(() =>
            {
                var texts = File.ReadAllText(path);
                jObject = JObject.Parse(texts);
                musicData = JsonNewtonsoft.FromJSON<MusicData>(jObject["music"].ToString());
            });
            foreach (var m in jObject["list"])
            {
                var md = JsonNewtonsoft.FromJSON<MusicData>(m.ToString());
                PlayingList.NowPlayingList.Add(md);
            }

            if (musicData is null) return;
            if (PlayingList.PlayBehavior == PlayBehavior.随机播放)
            {
                PlayingList.SetRandomPlay(PlayBehavior.随机播放);
            }
            await PlayingList.Play(musicData, false);
        }

        public async Task SaveNowPlaying()
        {
            if (AudioPlayer.MusicData is null) return;

            var path = Path.Combine(DataFolderBase.UserDataFolder, "LastPlaying");
            if (!LoadLastExitPlayingSongAndSongList)
            {
                await Task.Run(() => File.Delete(path));
                return;
            }

            if (!await Task.Run(() => File.Exists(path))) await Task.Run(() => File.Create(path).Close());

            JObject jObject = null;
            await Task.Run(() =>
            {
                JArray array = [];
                foreach (var a in PlayingList.PlayBehavior == PlayBehavior.随机播放 ? PlayingList.RandomSavePlayingList : PlayingList.NowPlayingList)
                    array.Add(JObject.FromObject(a));
                jObject = new JObject() {
                    { "music", JObject.FromObject(AudioPlayer.MusicData) },
                    { "list", array }
                };
            });
            if (jObject is null) return;
            await File.WriteAllTextAsync(path, jObject.ToString());
            LogManager.Log("SaveNowPlaying", "正在播放列表已保存！");
        }
        #endregion

        #region 加载和保存设置
        bool loadFailed = false;
        int retryCount = 0;
        public Windows.UI.Color AccentColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);

        public void LoadSettings(bool loadDefaultSettings = false)
        {
            LogManager.Log("App", "正在读取设置...");
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

                AudioPlayer.Volume = SettingEditHelper.GetSetting<float>(settingData, DataFolderBase.SettingParams.Volume);
                AudioPlayer.EqEnabled = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.EqualizerEnable);
                //MainWindowInstance.SMusicPage.ShowLrcPage = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.MusicPageShowLyricPage);

                DownloadManager.DownloadQuality = (DataFolderBase.DownloadQuality)SettingEditHelper.GetSetting<int>(settingData, DataFolderBase.SettingParams.DownloadQuality);
                DownloadManager.DownloadingMaximum = SettingEditHelper.GetSetting<int>(settingData, DataFolderBase.SettingParams.DownloadMaximum);
                DownloadManager.DownloadNamedMethod = (DataFolderBase.DownloadNamedMethod)SettingEditHelper.GetSetting<int>(settingData, DataFolderBase.SettingParams.DownloadNamedMethod);
                DownloadManager.IDv3WriteImage = (bool)SettingEditHelper.GetSetting<JArray>(settingData, DataFolderBase.SettingParams.DownloadOptions)[0];
                DownloadManager.IDv3WriteArtistImage = (bool)SettingEditHelper.GetSetting<JArray>(settingData, DataFolderBase.SettingParams.DownloadOptions)[1];
                DownloadManager.IDv3WriteLyric = (bool)SettingEditHelper.GetSetting<JArray>(settingData, DataFolderBase.SettingParams.DownloadOptions)[2];
                DownloadManager.SaveLyricToLrcFile = (bool)SettingEditHelper.GetSetting<JArray>(settingData, DataFolderBase.SettingParams.DownloadOptions)[3];
                PlayingList.PlayBehavior = (PlayBehavior)SettingEditHelper.GetSetting<int>(settingData, DataFolderBase.SettingParams.PlayBehavior);
                PlayingList.PauseWhenPreviousPause = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.PlayPauseWhenPreviousPause);
                PlayingList.NextWhenPlayError = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.PlayNextWhenPlayError);
                MainWindowInstance.WindowGridBase.RequestedTheme = (ElementTheme)SettingEditHelper.GetSetting<int>(settingData, DataFolderBase.SettingParams.ThemeColorMode);
                MainWindowInstance.SMusicPage.RequestedTheme = (ElementTheme)SettingEditHelper.GetSetting<int>(settingData, DataFolderBase.SettingParams.ThemeMusicPageColorMode);
                MainWindowInstance.CurrentBackdrop = (BackdropType)SettingEditHelper.GetSetting<int>(settingData, DataFolderBase.SettingParams.ThemeBackdropEffect);
                MainWindowInstance.ImagePath = SettingEditHelper.GetSetting<string>(settingData, DataFolderBase.SettingParams.ThemeBackdropImagePath);
                MainWindowInstance.BackgroundMass.Opacity = SettingEditHelper.GetSetting<double>(settingData, DataFolderBase.SettingParams.ThemeBackdropImageMassOpacity);
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
                MainWindowInstance.RunInBackground = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.BackgroundRun);
                Controls.ImageEx.ImageDarkMass = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.ImageDarkMass);
                LoadLastExitPlayingSongAndSongList = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.LoadLastExitPlayingSongAndSongList);
                MainWindowInstance.NavView.PaneDisplayMode = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.TopNavigationStyle) ? NavigationViewPaneDisplayMode.Top : NavigationViewPaneDisplayMode.Auto;
                LocalAudioPage.ItemSortBy = SettingEditHelper.GetSetting<int>(settingData, DataFolderBase.SettingParams.LocalMusicPageItemSortBy);
                JArray hkd = SettingEditHelper.GetSetting<JArray>(settingData, DataFolderBase.SettingParams.HotKeySettings);
                HotKeyManager.WillRegisterHotKeysList = hkd.ToObject<List<HotKey>>();
                HotKeyManager.EnableHotKey = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.HotKeyEnable);
                LyricManager.UseRomajiLyric = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.UseRomajiLyric);

                var audioEffects = SettingEditHelper.GetSetting<JArray>(audioEffectData, DataFolderBase.AudioEffectFlag.AudioEffectDatas);

                AudioFilterStatic.GraphicEqEnable = SettingEditHelper.GetSetting<bool>(audioEffectData, DataFolderBase.AudioEffectFlag.GraphicEqEnable);
                AudioFilterStatic.ParametricEqEnable = SettingEditHelper.GetSetting<bool>(audioEffectData, DataFolderBase.AudioEffectFlag.ParametricEqEnable);
                AudioFilterStatic.PassFilterEqEnable = SettingEditHelper.GetSetting<bool>(audioEffectData, DataFolderBase.AudioEffectFlag.PassFilterEqEnable);
                AudioFilterStatic.EffectEnable = SettingEditHelper.GetSetting<bool>(audioEffectData, DataFolderBase.AudioEffectFlag.EffectEnable);
                AudioPlayer.WasapiOnly = SettingEditHelper.GetSetting<bool>(audioEffectData, DataFolderBase.AudioEffectFlag.WasapiOnlyEnable);
                AudioPlayer.Latency = SettingEditHelper.GetSetting<int>(audioEffectData, DataFolderBase.AudioEffectFlag.Latency);
                AudioPlayer.Pitch = (double)audioEffects[0];
                AudioPlayer.Tempo = (double)audioEffects[1];
                AudioPlayer.Rate = (double)audioEffects[2];
                AudioPlayer.EqualizerBand = AudioEqualizerBands.GetBandFromString(SettingEditHelper.GetSetting<string>(audioEffectData, DataFolderBase.AudioEffectFlag.GraphicEqString));
                var bData = SettingEditHelper.GetSetting<string>(audioEffectData, DataFolderBase.AudioEffectFlag.GraphicEqDatas).Split(','); 
                for (int i = 0; i < 10; i++) AudioEqualizerBands.CustomBands[i][2] = float.Parse(bData[i]);
                AudioFilterStatic.ParametricEqDatas = SettingEditHelper.GetSetting<JArray>(audioEffectData, DataFolderBase.AudioEffectFlag.ParametricEqDatas).ToObject<ObservableCollection<EQData>>();
                AudioFilterStatic.PassFilterDatas = SettingEditHelper.GetSetting<JArray>(audioEffectData, DataFolderBase.AudioEffectFlag.PassFilterEqDatas).ToObject<ObservableCollection<PassFilterData>>();
            }
            catch (Exception e)
            {
                LogManager.Log("SettingError", e.ToString());
                if (retryCount >= 5)
                {
                    loadFailed = true;
                    return;
                }
                retryCount++;
                DataFolderBase.JSettingData = DataFolderBase.SettingDefault;
                LoadSettings(true);
            }
            LogManager.Log("App", "读取设置完成。");
        }

        public void SaveSettings()
        {
            LogManager.Log("App", "正在保存设置...");
            var settingData = DataFolderBase.JSettingData;
            var audioEffectData = DataFolderBase.JAudioEffectData;
            if (DataFolderBase.CacheFolder != DataFolderBase.DefaultCacheFolder)
            {
                SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.CacheFolderPath, DataFolderBase.CacheFolder);
            }
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.Volume, AudioPlayer.Volume == 0 ? MainWindowInstance.NoVolumeValue : AudioPlayer.Volume);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.DownloadFolderPath, DataFolderBase.DownloadFolder);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.AudioCacheFolderPath, DataFolderBase.AudioCacheFolder);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.ImageCacheFolderPath, DataFolderBase.ImageCacheFolder);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.LyricCacheFolderPath, DataFolderBase.LyricCacheFolder);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.DownloadOptions,
                new JArray() {
                    DownloadManager.IDv3WriteImage,
                    DownloadManager.IDv3WriteArtistImage,
                    DownloadManager.IDv3WriteLyric,
                    DownloadManager.SaveLyricToLrcFile
                    });
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.DownloadNamedMethod, (int)DownloadManager.DownloadNamedMethod);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.DownloadQuality, (int)DownloadManager.DownloadQuality);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.DownloadMaximum, DownloadManager.DownloadingMaximum);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.PlayBehavior, (int)PlayingList.PlayBehavior);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.PlayPauseWhenPreviousPause, PlayingList.PauseWhenPreviousPause);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.PlayNextWhenPlayError, PlayingList.NextWhenPlayError);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.DownloadMaximum, DownloadManager.DownloadingMaximum);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.EqualizerEnable, AudioPlayer.EqEnabled);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.EqualizerString, AudioEqualizerBands.GetNameFromBands(AudioPlayer.EqualizerBand));
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.WasapiOnly, AudioPlayer.WasapiOnly);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.AudioLatency, AudioPlayer.Latency < 50 ? 50 : AudioPlayer.Latency);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.MusicPageShowLyricPage, MainWindowInstance.SMusicPage.ShowLrcPage);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.ThemeColorMode, (int)MainWindowInstance.WindowGridBase.RequestedTheme);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.ThemeMusicPageColorMode, (int)MainWindowInstance.SMusicPage.pageRoot.RequestedTheme);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.ThemeBackdropEffect, (int)MainWindowInstance.CurrentBackdrop);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.ThemeBackdropImagePath, MainWindowInstance.ImagePath);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.ThemeBackdropImageMassOpacity, MainWindowInstance.BackgroundMass.Opacity);
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
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.BackgroundRun, MainWindowInstance.RunInBackground);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.ImageDarkMass, Controls.ImageEx.ImageDarkMass);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.LoadLastExitPlayingSongAndSongList, LoadLastExitPlayingSongAndSongList);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.HotKeyEnable, HotKeyManager.EnableHotKey);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.HotKeySettings, JArray.FromObject(App.Instance.HotKeyManager.RegisteredHotKeys));
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.TopNavigationStyle, MainWindowInstance.NavView.PaneDisplayMode == NavigationViewPaneDisplayMode.Top);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.LocalMusicPageItemSortBy, LocalAudioPage.ItemSortBy);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.UseRomajiLyric, LyricManager.UseRomajiLyric);
            
            List<float> c = [];
            foreach (var d in AudioEqualizerBands.CustomBands) c.Add(d[2]);
            string b = string.Join(",", c.ToArray());
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.GraphicEqEnable, AudioFilterStatic.GraphicEqEnable);
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.ParametricEqEnable, AudioFilterStatic.ParametricEqEnable);
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.PassFilterEqEnable, AudioFilterStatic.PassFilterEqEnable);
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.EffectEnable, AudioFilterStatic.EffectEnable);
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.WasapiOnlyEnable, AudioPlayer.WasapiOnly);
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.Latency, AudioPlayer.Latency < 50 ? 50 : AudioPlayer.Latency);
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.AudioEffectDatas, new JArray() { AudioPlayer.Pitch, AudioPlayer.Tempo, AudioPlayer.Rate });
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.GraphicEqString, AudioEqualizerBands.GetNameFromBands(AudioPlayer.EqualizerBand));
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.GraphicEqDatas, b);
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.ParametricEqDatas, AudioFilterStatic.ParametricEqDatas);
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.PassFilterEqDatas, AudioFilterStatic.PassFilterDatas);

            DataFolderBase.JSettingData = settingData;
            DataFolderBase.JAudioEffectData = audioEffectData;
            PluginManager.SavePluginInfoSettings();

            LogManager.Log("App", "设置配置已存储。");
        }

        public async void ShowErrorDialog()
        {
            MessageDialog messageDialog = new("设置文件出现了一些错误，且程序尝试 5 次后也无法恢复默认配置。\n" +
                $"请尝试删除 文档->{AppName}->UserData 里的 Setting 文件。\n" +
                "如果仍然出现问题，请到 GitHub 里向项目提出 Issues。", $"{AppName} - 程序无法正常启动");
            var hwnd = WindowNative.GetWindowHandle(MainWindow);
            InitializeWithWindow.Initialize(messageDialog, hwnd);
            await messageDialog.ShowAsync();
        }
        #endregion

        #region 程序版本设置
        public async Task CheckUpdate(bool addNotify = true)
        {
            var data = await WebHelper.GetStringAsync("https://data.tewi.top/datas/TewiMP/update.json");
            if (string.IsNullOrEmpty(data)) return;
            LogManager.Log("App", $"Update datas: {data}");
            var json = JArray.Parse(data);

            StableVersion = json[0].ToObject<VersionData>();
            PreviewVersion = json[1].ToObject<VersionData>();
            BetaVersion = json[2].ToObject<VersionData>();

            if (AppVersionIsNewest()) return;
            var newestVersion = GetNewVersionByReleaseData(NowVersion.SuffixType);

            if (addNotify)
            {
                MainWindowInstance.AddNotify(
                    "有新版本！",
                    $"可更新到版本 {newestVersion.Version} {newestVersion.SuffixType}，当前版本为 {NowVersion.Version} {NowVersion.SuffixType}。" +
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
        public VersionData GetNewVersionByReleaseData(SuffixType releaseType) => releaseType switch
        {
            SuffixType.Stable => StableVersion,
            SuffixType.Preview => PreviewVersion,
            SuffixType.Beta => BetaVersion,
            _ => NowVersion
        };

        public bool AppVersionIsNewest()
        {
            var newestVersion = GetNewVersionByReleaseData(NowVersion.SuffixType);
            if (!newestVersion.Available) return true;
            return newestVersion.Version <= NowVersion.Version;
        }
        #endregion

        #region Debug
        public void SetFramePerSecondViewer(bool visible = false)
        {
            DebugSettings.EnableFrameRateCounter = visible;
        }

        #endregion

        public static string[] SupportedMediaFormats = [
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
        ];
    }

    /// <summary>
    /// 版本类型
    /// </summary>
    public enum SuffixType
    {
        Stable,
        Preview,
        Beta
    }

    public class VersionData
    {
        public SuffixType SuffixType { get; set; }
        public bool Available { get; set; }
        public Version Version { get; set; }
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

        public static DateTime ToDateTimeFromUnix(this long unix)
        {
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            var time = startTime.AddSeconds(unix);
            return time;
        }

        /// <summary>
        /// 将毫秒级 Unix 时间戳转换为 DateTime (本地时间)
        /// </summary>
        public static DateTime ToDateTimeFromMillisecondsUnix(this long timestamp)
        {
            // Unix 时间戳起点：1970-01-01 00:00:00 UTC
            DateTimeOffset dto = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
            return dto.ToLocalTime().DateTime;
        }

        /// <summary>
        /// 将 DateTime 转换为毫秒级 Unix 时间戳
        /// </summary>
        public static long ToUnixTimeMilliseconds(this DateTime dateTime)
        {
            return new DateTimeOffset(dateTime.ToUniversalTime()).ToUnixTimeMilliseconds();
        }
    }
}
