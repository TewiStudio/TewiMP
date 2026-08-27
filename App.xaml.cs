using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NAudio.Wave;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TewiMP.Core;
using TewiMP.Core.Audio;
using TewiMP.Core.Music;
using TewiMP.Helpers;
using TewiMP.Services;
using TewiMP.Services.Media.Audio;
using TewiMP.Services.Media.Audio.AudioEffects;
using TewiMP.Services.Plugin;
using TewiMP.Services.Storage;
using TewiMP.UI.Pages;
using TewiMP.UI.Windows;
using Windows.ApplicationModel.Core;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using WinRT.Interop;


namespace TewiMP;

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
    /// 从 <see cref="App.Instance"/> 获取 <see cref=nameof(MainWindow)/> 的实例。"
    /// </summary>
    public static MainWindow MainWindowInstance => (MainWindow)Instance.MainWindow;
    #endregion

    #region STMC
    public MediaPlayer BMP { get; private set; } = null;
    public SystemMediaTransportControls SMTC { get; private set; } = null;
    #endregion

    #region Services
    public AudioService AudioService { get; private set; } = null;
    public CacheService CacheService { get; private set; } = null;
    public PlayingListService PlayingListService { get; private set; } = null;
    public LyricService LyricService { get; private set; } = null;
    public DownloadService DownloadService { get; private set; } = null;
    public LocalMusicManagerService LocalMusicManagerService { get; private set; } = null;
    public HotKeyService HotKeyService { get; private set; } = null;
    public LogService LogService { get; private set; } = null;
    public PlayListReader PlayListReader { get; private set; } = null;
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
        ReleaseTime = new(2026, 8, 20, 18, 40, 00),
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

    public UISettings UISettings { get; set; } = new();

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
#if DEBUG
        // 开启布局循环追踪
        if (Current.DebugSettings != null)
        {
            Current.DebugSettings.LayoutCycleTracingLevel = LayoutCycleTracingLevel.High;
        }
#endif
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
        LogService.Error("App:UnhandledError", $"{e.Exception}");
    }

    private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        LogService.Error("App:UnobservedTaskError", $"{e.Exception}");
    }

    private void CoreApplication_UnhandledErrorDetected(object sender, UnhandledErrorDetectedEventArgs e)
    {
        // useless
        //LogManager.Error("App", $"CoreApplication UnhandledErrorDetected: {e.UnhandledError}");
    }

    private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        LogService.Error("App:AppDomainFatalError", $"{e.ExceptionObject}");
    }
    #endregion

    #region 启动和退出
    public List<string> LaunchArgs = null;
    /// <summary>
    /// Invoked when the application is launched normally by the end user.  Other entry points
    /// will be used such as when the application is launched to open settingData specific file.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        var time = DateTime.Now;
        LogService = new();
        LogService.Log("App", "Preparing init...");

        MainWindow = new MainWindow();

        DataFolderBase.InitFiles();
        LogService.InitNowLog();
        CacheService = new();
        AudioService = new();
        PlayingListService = new();
        LocalMusicManagerService = new();
        LyricService = new();
        DownloadService = new();
        PlayListReader = new();
        HotKeyService = new();

        InitSMTC();

        UISettings.ColorValuesChanged += async (uiSettings, obj) =>
        {
            MainWindowInstance.Invoke(async () =>
                await PlayingListService.UpdateImageColor(true));
        };
        /*
                StartingSettings = DataFolderBase.JSettingData;
                var accentColor = StartingSettings[DataFolderBase.SettingParams.ThemeAccentColor.ToString()];
                if (accentColor != null)
                {
                    Current.Resources["SystemAccentColor"] = Windows.UI.Color.FromArgb(255, 2,255,2);
                    Current.Resources["SystemAccentColorLight2"] = Windows.UI.Color.FromArgb(255, 2, 255, 2);
                    Current.Resources["SystemAccentColorDark1"] = Windows.UI.Color.FromArgb(255, 2, 255, 2);

                    //LogManager.Log(Current.Resources["SystemAccentColorLight2"].GetType());
                }*/
        LoadSettings();

        // WinUI Bug: 获取不到启动参数
        //LAE = args;=
        LaunchArgs = [.. Environment.GetCommandLineArgs()];
        LaunchArgs.Remove(LaunchArgs.First());
        if (LaunchArgs.Count != 0)
            LogService.Log("App", $"Starting Args: {string.Join(", ", LaunchArgs)}");
        else
            LogService.Log("App", $"Starting Args: None");

        HotKeyService.Init(MainWindow);
        if (loadFailed)
        {
            ShowErrorDialog();
            return;
        }
        NotifyIconWindow = new();
        taskBarInfoWindow = new();
        LaunchAsync();
        PluginService.Init();
        LoadLastPlaying();
        LogService.Elapsed("App", "App inited in {0}", time);
    }

    public async void LaunchAsync()
    {
        //await PlayListReader.Refresh();
#if DEBUG
        SetFramePerSecondViewer(true);
#endif
        await PlayingListService.UpdateImageColor();
        await LocalMusicManagerService.Refresh();
        await CheckUpdate();
    }

    public static bool IsExited = false;
    public async Task ExitApp()
    {
        try
        {
            IsExited = true;
            LogService.Log("App", "Exiting application...");
            MainWindowInstance.AppWindow.Hide();
            MainWindowInstance.SetBackdrop(BackdropType.DefaultColor); // 在App.Instance.Exit前将MainWindow的Backdrop释放，否则会报错
            AudioService.SetPause();
            SaveSettings();
            await SaveNowPlaying();
            MainWindowInstance.DesktopLyricWindow?.Close();
            NotifyIconWindow.HideIcon();
            NotifyIconWindow.Close();
            LogWindow.CloseWindow();
            taskBarInfoWindow.Close();
            SMTC.PlaybackStatus = MediaPlaybackStatus.Closed;
            SMTC.DisplayUpdater.ClearAll();
            SMTC.DisplayUpdater.Update();
            AudioService.DisposeAll();
            HotKeyService.UnregisterHotKeys([.. HotKeyService.RegisteredHotKeys]);
            LogService.DisposeNowLogStream();
        }
        catch (Exception ex)
        {
            MainWindowInstance.AddNotify("退出时出现错误", "退出程序时发生错误，请检查日志文件。", NotifySeverity.Error);
            LogService.Error("App", $"ExitApp Error: {ex}");
        }
        finally
        {
            IsExited = false;
        }
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

    #region SMTC
    private void InitSMTC()
    {
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
        SMTC.DisplayUpdater.MusicProperties.Artist = "TewiStudio";
        SMTC.DisplayUpdater.Update();

        bool mediaChanging = false;
        int saveSettingsWhenSourceChangedCount = 0;

        AudioService.SourceChanged += async (AudioService) =>
        {
            AudioService.AudioThread.Invoke(async () =>
                await SongHistoryHelper.AddHistory(new() { MusicData = AudioService.MusicData, Time = DateTime.Now })
            );
            if (saveSettingsWhenSourceChangedCount > 4)
            {
                saveSettingsWhenSourceChangedCount = 0;
                await SaveNowPlaying();
                SaveSettings();
            }
            saveSettingsWhenSourceChangedCount++;
        };
        AudioService.CacheLoadingChanged += (s, __) =>
        {/*
            try
            {
                SMTC.DisplayUpdater.Thumbnail = null;
                SMTC.DisplayUpdater.Update();
                mediaChanging = true;
            }
            catch { }*/
        };
        AudioService.CacheLoadedChanged += (s) =>
        {
            try
            {
                if (s.MusicData is null)
                {
                    SMTC.DisplayUpdater.MusicProperties.Title = s.FileReader?.FileName;
                    MainWindowInstance.AppWindow.Title = AppName;
                }
                else
                {
                    SMTC.DisplayUpdater.MusicProperties.Title = s.MusicData.Title;
                    SMTC.DisplayUpdater.MusicProperties.Artist = s.MusicData.ArtistName;
                    MainWindowInstance.AppWindow.Title = $"{s.MusicData.Title} - {s.MusicData.ArtistName} · {AppName}";
                }
                SMTC.DisplayUpdater.Update();
            }
            catch { }
        };
        AudioService.PlayStateChanged += (s) =>
        {
            try
            {
                if (s.PlaybackState == PlaybackState.Playing)
                {
                    SMTC.PlaybackStatus = MediaPlaybackStatus.Playing;
                }
                else
                {
                    SMTC.PlaybackStatus = MediaPlaybackStatus.Paused;
                }
            }
            catch { }
        };
        AudioService.TimingChanged += (audioService) =>
        {
            var timeline = new SystemMediaTransportControlsTimelineProperties
            {
                StartTime = TimeSpan.Zero,
                EndTime = audioService.TotalTime,
                Position = audioService.CurrentTime,
                MinSeekTime = TimeSpan.Zero,
                MaxSeekTime = audioService.TotalTime
            };
            SMTC.UpdateTimelineProperties(timeline);
        };
        PlayingListService.NowPlayingImageLoading += (_, __) =>
        {
            try
            {
                SMTC.DisplayUpdater.Thumbnail = null;
                SMTC.DisplayUpdater.Update();
            }
            catch { }
        };
        PlayingListService.PlayBehaviorChanged += (playBehavior) =>
        {
            SMTC.ShuffleEnabled = playBehavior == PlayBehavior.随机播放;

            SMTC.AutoRepeatMode = playBehavior switch
            {
                PlayBehavior.播放完成后停止 => MediaPlaybackAutoRepeatMode.None,
                PlayBehavior.单曲循环 => MediaPlaybackAutoRepeatMode.Track,
                PlayBehavior.循环播放 => MediaPlaybackAutoRepeatMode.List,
                _ => MediaPlaybackAutoRepeatMode.None
            };
        };
        SMTC.ButtonPressed += (_, e) =>
        {
            MainWindowInstance.Invoke(() =>
            {
                switch (e.Button)
                {
                    case SystemMediaTransportControlsButton.Play:
                        AudioService.SetPlay();
                        break;
                    case SystemMediaTransportControlsButton.Pause:
                        AudioService.SetPause();
                        break;
                    case SystemMediaTransportControlsButton.Previous:
                        PlayingListService.PlayPrevious();
                        break;
                    case SystemMediaTransportControlsButton.Next:
                        PlayingListService.PlayNext();
                        break;
                    case SystemMediaTransportControlsButton.Stop:
                        AudioService.SetStop();
                        break;
                }
            });
        };
        SMTC.PlaybackPositionChangeRequested += (s, e) =>
        {
            MainWindowInstance.Invoke(() =>
            {
                AudioService.CurrentTime = e.RequestedPlaybackPosition;
            });
        };
        SMTC.ShuffleEnabledChangeRequested += (_, __) =>
        {
            MainWindowInstance.Invoke(() =>
            {
                PlayingListService.PlayBehavior =
                    PlayingListService.PlayBehavior == PlayBehavior.随机播放 ? PlayBehavior.循环播放 : PlayBehavior.随机播放;
            });
        };
        SMTC.AutoRepeatModeChangeRequested += (_, e) =>
        {
            MainWindowInstance.Invoke(() =>
            {
                PlayingListService.PlayBehavior = e.RequestedAutoRepeatMode switch
                {
                    MediaPlaybackAutoRepeatMode.None => PlayBehavior.播放完成后停止,
                    MediaPlaybackAutoRepeatMode.Track => PlayBehavior.单曲循环,
                    MediaPlaybackAutoRepeatMode.List => PlayBehavior.循环播放,
                    _ => PlayBehavior.播放完成后停止
                };
            });
        };
        PlayingListService.NowPlayingImageLoaded += async (_, e) =>
        {
            try
            {
                if (string.IsNullOrEmpty(e))
                {
                    SMTC.DisplayUpdater.Thumbnail = null;
                }
                else
                {
                    try
                    {
                        SMTC.DisplayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromFile(await StorageFile.GetFileFromPathAsync(e));
                    }
                    catch { }
                }

                SMTC.DisplayUpdater.Update();
            }
            catch { }
        };
    }
    #endregion

    #region 加载和保存上次播放设置
    public async void LoadLastPlaying()
    {
        if (!LoadLastExitPlayingSongAndSongList) return;
        //if (isOpeningMusicLoaded) return;

        var path = DataFolderBase.LastPlayedDataPath;
        if (!File.Exists(path)) return;

        MusicData musicData = null;
        JObject jObject = null;
        await Task.Run(() =>
        {
            var texts = File.ReadAllText(path);
            jObject = JObject.Parse(texts);
            musicData = JsonConvert.DeserializeObject<MusicData>(jObject["music"].ToString());
        });
        foreach (var m in jObject["list"])
        {
            var md = JsonConvert.DeserializeObject<MusicData>(m.ToString());
            PlayingListService.NowPlayingList.Add(md);
        }

        if (musicData is null) return;
        if (PlayingListService.PlayBehavior == PlayBehavior.随机播放)
        {
            PlayingListService.SetRandomPlay(PlayBehavior.随机播放);
        }
        await PlayingListService.Play(musicData, false);
    }

    bool savingNowPlaying = false;
    public async Task SaveNowPlaying()
    {
        if (savingNowPlaying) return;
        if (AudioService.MusicData is null) return;
        savingNowPlaying = true;

        var path = DataFolderBase.LastPlayedDataPath;
        if (!LoadLastExitPlayingSongAndSongList)
        {
            await Task.Run(() => File.Delete(path));
            return;
        }

        if (!await Task.Run(() => File.Exists(path))) await Task.Run(() => File.Create(path).Close());

        var startTime = DateTime.Now;
        JObject jObject = null;
        await Task.Run(() =>
        {
            JArray array = [];
            foreach (var a in PlayingListService.PlayBehavior == PlayBehavior.随机播放 ? PlayingListService.RandomSavePlayingList : PlayingListService.NowPlayingList)
                array.Add(JObject.FromObject(a));
            jObject = new JObject() {
                    { "music", JObject.FromObject(AudioService.MusicData) },
                    { "list", array }
            };
        });
        if (jObject is null) return;
        await File.WriteAllTextAsync(path, jObject.ToString());
        LogService.Elapsed("SaveNowPlaying", "Now playing list saved in {0}", startTime);

        savingNowPlaying = false;
    }
    #endregion

    #region 加载和保存设置
    bool loadFailed = false;
    int retryCount = 0;
    public global::Windows.UI.Color AccentColor = global::Windows.UI.Color.FromArgb(0, 0, 0, 0);

    public void LoadSettings(bool loadDefaultSettings = false)
    {
        DateTime elapsdTime = DateTime.Now;
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

            AudioService.Volume = SettingEditHelper.GetSetting<float>(settingData, DataFolderBase.SettingParams.Volume);
            AudioService.EqEnabled = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.EqualizerEnable);
            //MainWindowInstance.SMusicPage.ShowLrcPage = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.MusicPageShowLyricPage);

            DownloadService.DownloadQuality = (DataFolderBase.DownloadQuality)SettingEditHelper.GetSetting<int>(settingData, DataFolderBase.SettingParams.DownloadQuality);
            DownloadService.DownloadingMaximum = SettingEditHelper.GetSetting<int>(settingData, DataFolderBase.SettingParams.DownloadMaximum);
            DownloadService.DownloadNamedMethod = (DataFolderBase.DownloadNamedMethod)SettingEditHelper.GetSetting<int>(settingData, DataFolderBase.SettingParams.DownloadNamedMethod);
            DownloadService.IDv3WriteImage = (bool)SettingEditHelper.GetSetting<JArray>(settingData, DataFolderBase.SettingParams.DownloadOptions)[0];
            DownloadService.IDv3WriteArtistImage = (bool)SettingEditHelper.GetSetting<JArray>(settingData, DataFolderBase.SettingParams.DownloadOptions)[1];
            DownloadService.IDv3WriteLyric = (bool)SettingEditHelper.GetSetting<JArray>(settingData, DataFolderBase.SettingParams.DownloadOptions)[2];
            DownloadService.SaveLyricToLrcFile = (bool)SettingEditHelper.GetSetting<JArray>(settingData, DataFolderBase.SettingParams.DownloadOptions)[3];
            PlayingListService.PlayBehavior = (PlayBehavior)SettingEditHelper.GetSetting<int>(settingData, DataFolderBase.SettingParams.PlayBehavior);
            PlayingListService.PauseWhenPreviousPause = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.PlayPauseWhenPreviousPause);
            PlayingListService.NextWhenPlayError = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.PlayNextWhenPlayError);
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
            UI.Controls.ImageEx.ImageDarkMass = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.ImageDarkMass);
            LoadLastExitPlayingSongAndSongList = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.LoadLastExitPlayingSongAndSongList);
            MainWindowInstance.NavView.PaneDisplayMode = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.TopNavigationStyle) ? NavigationViewPaneDisplayMode.Top : NavigationViewPaneDisplayMode.Auto;
            LocalAudioPage.ItemSortBy = SettingEditHelper.GetSetting<int>(settingData, DataFolderBase.SettingParams.LocalMusicPageItemSortBy);
            JArray hkd = SettingEditHelper.GetSetting<JArray>(settingData, DataFolderBase.SettingParams.HotKeySettings);
            HotKeyService.WillRegisterHotKeysList = hkd.ToObject<List<HotKey>>();
            HotKeyService.EnableHotKey = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.HotKeyEnable);
            LyricService.UseRomajiLyric = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.UseRomajiLyric);
            PlayingListService.UseSystemAccentColor = SettingEditHelper.GetSetting<bool>(settingData, DataFolderBase.SettingParams.UseSystemAccentColor);

            var audioEffects = SettingEditHelper.GetSetting<JArray>(audioEffectData, DataFolderBase.AudioEffectFlag.AudioEffectDatas);

            AudioFilterStatic.GraphicEqEnable = SettingEditHelper.GetSetting<bool>(audioEffectData, DataFolderBase.AudioEffectFlag.GraphicEqEnable);
            AudioFilterStatic.ParametricEqEnable = SettingEditHelper.GetSetting<bool>(audioEffectData, DataFolderBase.AudioEffectFlag.ParametricEqEnable);
            AudioFilterStatic.PassFilterEqEnable = SettingEditHelper.GetSetting<bool>(audioEffectData, DataFolderBase.AudioEffectFlag.PassFilterEqEnable);
            AudioFilterStatic.EffectEnable = SettingEditHelper.GetSetting<bool>(audioEffectData, DataFolderBase.AudioEffectFlag.EffectEnable);
            AudioService.WasapiOnly = SettingEditHelper.GetSetting<bool>(audioEffectData, DataFolderBase.AudioEffectFlag.WasapiOnlyEnable);
            AudioService.Latency = SettingEditHelper.GetSetting<int>(audioEffectData, DataFolderBase.AudioEffectFlag.Latency);
            AudioService.Pitch = (double)audioEffects[0];
            AudioService.Tempo = (double)audioEffects[1];
            AudioService.Rate = (double)audioEffects[2];
            AudioService.EqualizerBand = AudioEqualizerBands.GetBandFromString(SettingEditHelper.GetSetting<string>(audioEffectData, DataFolderBase.AudioEffectFlag.GraphicEqString));
            var bData = SettingEditHelper.GetSetting<string>(audioEffectData, DataFolderBase.AudioEffectFlag.GraphicEqDatas).Split(',');
            for (int i = 0; i < 10; i++) AudioEqualizerBands.CustomBands[i][2] = float.Parse(bData[i]);
            AudioFilterStatic.ParametricEqDatas = SettingEditHelper.GetSetting<JArray>(audioEffectData, DataFolderBase.AudioEffectFlag.ParametricEqDatas).ToObject<ObservableCollection<EQData>>();
            AudioFilterStatic.PassFilterDatas = SettingEditHelper.GetSetting<JArray>(audioEffectData, DataFolderBase.AudioEffectFlag.PassFilterEqDatas).ToObject<ObservableCollection<PassFilterData>>();
        }
        catch (Exception e)
        {
            LogService.Log("SettingError", e.ToString());
            if (retryCount >= 5)
            {
                loadFailed = true;
                return;
            }
            retryCount++;
            DataFolderBase.JSettingData = DataFolderBase.SettingDefault;
            LoadSettings(true);
        }
        LogService.Elapsed("App", "Settings loaded in {0}", elapsdTime);
    }

    private readonly Lock _saveSettingsLock = new();
    public void SaveSettings()
    {
        lock (_saveSettingsLock)
        {
            var startTime = DateTime.Now;
            var settingData = DataFolderBase.JSettingData;
            var audioEffectData = DataFolderBase.JAudioEffectData;
            if (DataFolderBase.CacheFolder != DataFolderBase.DefaultCacheFolder)
            {
                SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.CacheFolderPath, DataFolderBase.CacheFolder);
            }
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.Volume, AudioService.Volume == 0 ? MainWindowInstance.NoVolumeValue : AudioService.Volume);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.DownloadFolderPath, DataFolderBase.DownloadFolder);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.AudioCacheFolderPath, DataFolderBase.AudioCacheFolder);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.ImageCacheFolderPath, DataFolderBase.ImageCacheFolder);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.LyricCacheFolderPath, DataFolderBase.LyricCacheFolder);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.DownloadOptions,
                new JArray() {
                    DownloadService.IDv3WriteImage,
                    DownloadService.IDv3WriteArtistImage,
                    DownloadService.IDv3WriteLyric,
                    DownloadService.SaveLyricToLrcFile
                    });
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.DownloadNamedMethod, (int)DownloadService.DownloadNamedMethod);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.DownloadQuality, (int)DownloadService.DownloadQuality);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.DownloadMaximum, DownloadService.DownloadingMaximum);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.PlayBehavior, (int)PlayingListService.PlayBehavior);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.PlayPauseWhenPreviousPause, PlayingListService.PauseWhenPreviousPause);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.PlayNextWhenPlayError, PlayingListService.NextWhenPlayError);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.DownloadMaximum, DownloadService.DownloadingMaximum);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.EqualizerEnable, AudioService.EqEnabled);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.EqualizerString, AudioEqualizerBands.GetNameFromBands(AudioService.EqualizerBand));
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.WasapiOnly, AudioService.WasapiOnly);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.AudioLatency, AudioService.Latency < 50 ? 50 : AudioService.Latency);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.MusicPageShowLyricPage, MainWindowInstance.SMusicPage.ShowLrcPage);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.ThemeColorMode, (int)MainWindowInstance.WindowGridBase.RequestedTheme);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.ThemeMusicPageColorMode, (int)MainWindowInstance.SMusicPage.pageRoot.RequestedTheme);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.ThemeBackdropEffect, (int)MainWindowInstance.CurrentBackdrop);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.ThemeBackdropImagePath, MainWindowInstance.ImagePath);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.ThemeBackdropImageMassOpacity, MainWindowInstance.BackgroundMass.Opacity);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.ThemeAccentColor, AccentColor == global::Windows.UI.Color.FromArgb(0, 0, 0, 0) ? null : AccentColor.ToString());
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
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.ImageDarkMass, UI.Controls.ImageEx.ImageDarkMass);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.LoadLastExitPlayingSongAndSongList, LoadLastExitPlayingSongAndSongList);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.HotKeyEnable, HotKeyService.EnableHotKey);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.HotKeySettings, JArray.FromObject(App.Instance.HotKeyService.RegisteredHotKeys));
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.TopNavigationStyle, MainWindowInstance.NavView.PaneDisplayMode == NavigationViewPaneDisplayMode.Top);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.LocalMusicPageItemSortBy, LocalAudioPage.ItemSortBy);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.UseRomajiLyric, LyricService.UseRomajiLyric);
            SettingEditHelper.EditSetting(settingData, DataFolderBase.SettingParams.UseSystemAccentColor, PlayingListService.UseSystemAccentColor);

            List<float> c = [];
            foreach (var d in AudioEqualizerBands.CustomBands) c.Add(d[2]);
            string b = string.Join(",", c.ToArray());
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.GraphicEqEnable, AudioFilterStatic.GraphicEqEnable);
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.ParametricEqEnable, AudioFilterStatic.ParametricEqEnable);
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.PassFilterEqEnable, AudioFilterStatic.PassFilterEqEnable);
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.EffectEnable, AudioFilterStatic.EffectEnable);
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.WasapiOnlyEnable, AudioService.WasapiOnly);
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.Latency, AudioService.Latency < 50 ? 50 : AudioService.Latency);
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.AudioEffectDatas, new JArray() { AudioService.Pitch, AudioService.Tempo, AudioService.Rate });
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.GraphicEqString, AudioEqualizerBands.GetNameFromBands(AudioService.EqualizerBand));
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.GraphicEqDatas, b);
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.ParametricEqDatas, AudioFilterStatic.ParametricEqDatas);
            SettingEditHelper.EditSetting(audioEffectData, DataFolderBase.AudioEffectFlag.PassFilterEqDatas, AudioFilterStatic.PassFilterDatas);

            DataFolderBase.JSettingData = settingData;
            DataFolderBase.JAudioEffectData = audioEffectData;
            PluginService.SavePluginInfoSettings();

            LogService.Elapsed("App", "Settings saved in {0}", startTime);
        }
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
        LogService.Log("App", $"Update datas: {data}");
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
                "更新", () =>
                {
                    //var success = await CodeHelper.OpenInBrowser(newestVersion.Url);
                    MainWindowInstance.SetNavViewContent(typeof(AboutPage));
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
    public string InstallUrl { get; set; }
    public DateTime ReleaseTime { get; set; }
    public string ExtendMessage { get; set; }

    public override string ToString()
    {
        return $"{Version} {SuffixType}";
    }
}

public static class DateConverter
{
    public static DateTime ToDateTimeFromMillisecondsUnix(this long timestamp)
    {
        // Unix 时间戳起点：1970-01-01 00:00:00 UTC
        DateTimeOffset dto = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
        return dto.ToLocalTime().DateTime;
    }

    public static long ToUnixTimeMilliseconds(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime.ToUniversalTime()).ToUnixTimeMilliseconds();
    }
}

