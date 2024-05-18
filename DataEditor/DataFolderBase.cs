using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TewiMP.Windowed;
using TewiMP.Background;
using TewiMP.Background.HotKeys;
using Microsoft.UI.Xaml;

namespace TewiMP.DataEditor
{
    public static class DataFolderBase
    {
        /// <summary>
        /// 程序数据文件夹路径
        /// </summary>
        public static string BaseFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{App.AppName}");

        /// <summary>
        /// 数据文件夹路径
        /// </summary>
        public static string UserDataFolder { get; } = Path.Combine(BaseFolder, "UserData");

        /// <summary>
        /// 歌单数据文件路径
        /// </summary>
        public static string PlayListDataPath { get; } = Path.Combine(UserDataFolder, "PlayList");
        
        /// <summary>
        /// 本地音乐数据文件路径
        /// </summary>
        public static string LocalMusicDataPath { get; } = Path.Combine(UserDataFolder, "LocalMusic");
        
        /// <summary>
        /// 设置数据文件路径
        /// </summary>
        public static string SettingDataPath { get; } = Path.Combine(UserDataFolder, "Setting");

        /// <summary>
        /// 历史记录数据文件路径
        /// </summary>
        public static string HistoryDataPath { get; } = Path.Combine(UserDataFolder, "History");
        
        /// <summary>
        /// 日志文件路径
        /// </summary>
        public static string LogDataPath { get; } = Path.Combine(UserDataFolder, "Log");

        /// <summary>
        /// 缓存文件夹路径
        /// </summary>
        public static string CacheFolder { get; set; } = Path.Combine(BaseFolder, "Cache");

        /// <summary>
        /// 歌曲缓存文件夹路径
        /// </summary>
        public static string AudioCacheFolder { get; set; } = Path.Combine(CacheFolder, "Audio");

        /// <summary>
        /// 图片缓存文件夹路径
        /// </summary>
        public static string ImageCacheFolder { get; set; } = Path.Combine(CacheFolder, "Image");

        /// <summary>
        /// 歌词缓存文件夹路径
        /// </summary>
        public static string LyricCacheFolder { get; set; } = Path.Combine(CacheFolder, "Lyric");

        /// <summary>
        /// 下载文件夹路径
        /// </summary>
        public static string DownloadFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

        /// <summary>
        /// 默认播放列表数据
        /// </summary>
        public static MusicListData PlayListDefault = new("default", "默认播放列表", Path.Combine(Environment.CurrentDirectory, "Images", "icon.png"), MusicFrom.localMusic, listDataType: DataType.本地歌单);

        /// <summary>
        /// 默认设置数据
        /// </summary>
        public static JObject SettingDefault = new JObject()
        {
            { SettingParams.Volume.ToString(), 50f },
            { SettingParams.DownloadFolderPath.ToString(), DownloadFolder },
            { SettingParams.AudioCacheFolderPath.ToString(), AudioCacheFolder },
            { SettingParams.ImageCacheFolderPath.ToString(), ImageCacheFolder },
            { SettingParams.LyricCacheFolderPath.ToString(), LyricCacheFolder },
            { SettingParams.DownloadOptions.ToString(), new JArray(){ true, false , true, true } },
            { SettingParams.DownloadNamedMethod.ToString(), (int)DownloadNamedMethod.t_ar_al },
            { SettingParams.DownloadQuality.ToString(), (int)DownloadQuality.lossless },
            { SettingParams.DownloadMaximum.ToString(), 3 },
            { SettingParams.PlayBehavior.ToString(), (int)PlayBehavior.循环播放 },
            { SettingParams.PlayPauseWhenPreviousPause.ToString(), false },
            { SettingParams.PlayNextWhenPlayError.ToString(), true },
            { SettingParams.EqualizerEnable.ToString(), false },
            { SettingParams.EqualizerString.ToString(), nameof(Media.AudioEqualizerBands.CustomBands) },
            { SettingParams.EqualizerCustomData.ToString(), "0,0,0,0,0,0,0,0,0,0" },
            { SettingParams.WasapiOnly.ToString(), false },
            { SettingParams.AudioLatency.ToString(), 120 },
            { SettingParams.MusicPageShowLyricPage.ToString(), true },
            { SettingParams.ThemeColorMode.ToString(), (int)ElementTheme.Default },
            { SettingParams.ThemeMusicPageColorMode.ToString(), (int)ElementTheme.Default },
            { SettingParams.ThemeAccentColor.ToString(), null },
            { SettingParams.ThemeBackdropEffect.ToString(), (int)MainWindow.BackdropType.DesktopAcrylic },
            { SettingParams.ThemeBackdropImagePath.ToString(), null },
            { SettingParams.ThemeBackdropImageMassOpacity.ToString(), 0.5 },
            { SettingParams.DesktopLyricOptions.ToString(),
                new JArray() {
                    true, true, false, true
                }
            },
            { SettingParams.DesktopLyricText.ToString(),
                new JArray() {
                    (int)LyricTextBehavior.Exchange,
                    (int)LyricTextPosition.Default
                }
            },
            { SettingParams.DesktopLyricTranslateText.ToString(),
                new JArray() {
                    (int)LyricTranslateTextBehavior.MainLyric,
                    (int)LyricTranslateTextPosition.Center
                }
            },
            { SettingParams.DesktopLyricOpacity.ToString(), 1 },
            { SettingParams.TaskbarShowIcon.ToString(), true },
            { SettingParams.BackgroundRun.ToString(), false },
            { SettingParams.ImageDarkMass.ToString(), false },
            { SettingParams.LoadLastExitPlayingSongAndSongList.ToString(), true },
            { SettingParams.TopNavigationStyle.ToString(), false },
            { SettingParams.LocalMusicPageItemSortBy.ToString(), 0 },
            { SettingParams.HotKeySettings.ToString(), JArray.FromObject(HotKeyManager.DefaultRegisterHotKeysList) },
        };
        
        /// <summary>
        /// 默认历史记录数据
        /// </summary>
        public static JObject HistoryDefault = new JObject()
        {
            { "Songs", new JObject() },
            { "Search", new JObject() }
        };

        public enum DownloadNamedMethod
        {
            t_ar_al = 0,
            t_ar,
            t_al_ar,
            t_al,
            t
        }
        
        public enum DownloadQuality
        {
            lossless = 960, lossy_high = 320, lossy_mid = 192, lossy_low = 128
        }

        /// <summary>
        /// 设置标志
        /// </summary>
        public enum SettingParams { 
            Volume,
            AudioCacheFolderPath,
            DownloadFolderPath,
            ImageCacheFolderPath,
            LyricCacheFolderPath,
            DownloadOptions,
            DownloadNamedMethod,
            DownloadQuality,
            DownloadMaximum,
            PlayBehavior,
            PlayPauseWhenPreviousPause,
            PlayNextWhenPlayError,
            EqualizerEnable,
            EqualizerString,
            EqualizerCustomData,
            WasapiOnly,
            AudioLatency,
            MusicPageShowLyricPage,
            ThemeColorMode,
            ThemeMusicPageColorMode,
            ThemeAccentColor,
            ThemeBackdropEffect,
            ThemeBackdropImagePath,
            ThemeBackdropImageMassOpacity,
            DesktopLyricOptions,
            DesktopLyricText,
            DesktopLyricTranslateText,
            DesktopLyricOpacity,
            TaskbarShowIcon,
            BackgroundRun,
            ImageDarkMass,
            LoadLastExitPlayingSongAndSongList,
            TopNavigationStyle,
            LocalMusicPageItemSortBy,
            HotKeySettings,
        }

        public enum LocalMusicDataType { LocalMusicFolderPath, AnalyzedDatas }

        /// <summary>
        /// 初始化所有文件夹和文件
        /// </summary>
        public static void InitFiles()
        {
            System.Diagnostics.Debug.WriteLine("[DataFolderBase]: 初始化文件目录中...");
            Directory.CreateDirectory(BaseFolder);
            Directory.CreateDirectory(UserDataFolder);
            Directory.CreateDirectory(CacheFolder);
            Directory.CreateDirectory(AudioCacheFolder);
            Directory.CreateDirectory(ImageCacheFolder);
            Directory.CreateDirectory(LyricCacheFolder);

            if (!File.Exists(PlayListDataPath))
            {
                File.Create(PlayListDataPath).Close();
                File.WriteAllText(PlayListDataPath, "{}");
                PlayListHelper.AddPlayList(PlayListDefault);
            }
            
            if (!File.Exists(LocalMusicDataPath))
            {
                File.Create(LocalMusicDataPath).Close();
                File.WriteAllText(LocalMusicDataPath,
                    new JObject()
                    {
                        { LocalMusicDataType.LocalMusicFolderPath.ToString(), new JArray() { Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) } },
                        { LocalMusicDataType.AnalyzedDatas.ToString(), new JArray() }
                    }.ToString());
            }
            
            if (!File.Exists(SettingDataPath))
            {
                File.Create(SettingDataPath).Close();
                File.WriteAllText(SettingDataPath, SettingDefault.ToString());
            }
            
            if (!File.Exists(HistoryDataPath))
            {
                File.Create(HistoryDataPath).Close();
                File.WriteAllText(HistoryDataPath, HistoryDefault.ToString());
            }

            if (!File.Exists(LogDataPath))
            {
                File.Create(LogDataPath).Close();
            }
            System.Diagnostics.Debug.WriteLine("[DataFolderBase]: 初始化文件目录完成。");
        }

        /// <summary>
        /// <list type="table">
        ///     <item>数据文件的抽象。</item>
        ///     <item>使用时会读取设置文件，设置时会写入数据文件</item>
        /// </list>
        /// </summary>
        public static JObject JSettingData
        {
            get => JObject.Parse(File.ReadAllText(SettingDataPath));
            set
            {
                File.WriteAllText(SettingDataPath, value.ToString());
            }
        }
    }

    public static class JsonNewtonsoft
    {
        /// <summary>
        /// 把对象转换为JSON字符串
        /// </summary>
        /// <param name="o">对象</param>
        /// <returns>JSON字符串</returns>
        public static string ToJSON(this object o)
        {
            if (o == null)
            {
                return null;
            }
            return JsonConvert.SerializeObject(o);
        }
        /// <summary>
        /// 把Json文本转为实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static T FromJSON<T>(this string input)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(input);
            }
            catch (Exception ex)
            {
                return default(T);
            }
        }
    }
}
