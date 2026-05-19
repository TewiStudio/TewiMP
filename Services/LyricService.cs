using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TewiMP.Core;
using TewiMP.Core.Music;
using TewiMP.Helpers;
using TewiMP.Services.Media.Audio;
using TewiMP.Services.Storage;

namespace TewiMP.Services;

public class LyricService
{
    public delegate void PlayingLyricDelegate(ObservableCollection<LyricData> nowPlayingLyrics);
    public delegate void PlayingLyricData(LyricData nowLyricsData);
    public event PlayingLyricDelegate PlayingLyricSourceChanged;
    public event PlayingLyricData PlayingLyricSelectedChanged;
    public event PlayingLyricData LyricTimingChanged;
    MusicData MusicData;
    DispatcherTimer timer;

    public ObservableCollection<LyricData> NowPlayingLyrics = new();

    public static bool UseRomajiLyric = true;
    public bool FastUpdateMode = false;
    public double DefaultUpdateInterval = 100;
    public double FastUpdateInterval = 100;
    public double UpdateInterval
    { 
        get
        {
            return FastUpdateMode ? FastUpdateInterval : DefaultUpdateInterval;
        }
    }
    private LyricData _nowLyricsData = null;
    public LyricData NowLyricsData
    {
        get => _nowLyricsData;
        set
        {
            //if (_nowLyricsData is null || value is null) return;
            if (value == _nowLyricsData) return;
            if (value is null)
            {
                _nowLyricsData = value;
                InvokeLyricChangeEvent(value);
            }
            else if (_nowLyricsData != value)
            {
                _nowLyricsData = value;
                InvokeLyricChangeEvent(value);
            }
        }
    }

    private void InvokeLyricChangeEvent(LyricData lyricData)
    {
        PlayingLyricSelectedChanged?.Invoke(lyricData);
        //LogManager.Log(nameof(LyricService), $"当前歌词已设置为：\"{lyricData?.Lyric?.FirstOrDefault()}\"");
    }

    public LyricService()
    {
        LogService.Log("Starting", "初始化 LyricManager.");

        timer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(UpdateInterval) };
        timer.Tick += (_, __) =>
        {
            ReCallUpdate();
            LyricTimingChanged?.Invoke(NowLyricsData);
        };

        //App.MainWindowInstance.WindowViewStateChanged += MainWindow_WindowViewStateChanged;
        App.Instance.AudioService.SourceChanged += AudioService_SourceChanged;
        App.Instance.AudioService.PlayStateChanged += AudioService_PlayStateChanged;
        App.Instance.AudioService.TimingChanged += AudioService_TimingChanged;
    }

    private void AudioService_PlayStateChanged(AudioService AudioService)
    {
        if (App.Instance.AudioService.PlaybackState == NAudio.Wave.PlaybackState.Playing)
        {
            StartTimer();
        }
        else
        {
            StopTimer();
        }
    }

    private void AudioService_TimingChanged(AudioService AudioService)
    {
        // 使暂停时更改播放进度可以改变歌词
        if (AudioService.PlaybackState != NAudio.Wave.PlaybackState.Playing) ReCallUpdate();
    }

    public async Task InitLyricList(MusicData musicData)
    {
        //LogManager.Log(nameof(LyricService), $"初始化歌词：\"{musicData.Title}\"");
        if (musicData is null) return;
        var startTime = DateTime.Now;
        NowPlayingLyrics.Clear();

        string cachePath = await CacheFileHelpers.GetLyricCache(musicData);
        string resultPath = null;

        if (cachePath != null)
        {
            resultPath = cachePath;
            //LogManager.Log(nameof(LyricService), $"找到歌词缓存：\"{cachePath}\"");
        }
        else
        {
            if (musicData.From == MusicFrom.localMusic)
            {
                TagLib.File tagfile = null;
                tagfile = await Task.Run(() =>
                {
                    try
                    {
                        return TagLib.File.Create(musicData.InLocal);
                    }
                    catch
                    {
                        return null;
                    }
                });
                await InitLyricList(tagfile);
                
                return;
            }

            //LogManager.Log(nameof(LyricService), "从网络中下载歌词");
            Tuple<string, string> lyricTuple;
            if (musicData.From == MusicFrom.pluginMusicSource)
            {
                lyricTuple = await musicData.GetMusicSourcePlugin().GetLyric(musicData.ID);
            }
            else
            {
                lyricTuple = null;
            }

            if (lyricTuple is null)
            {
                resultPath = null;
            }
            else
            {
                string path = Path.Combine(DataFolderBase.LyricCacheFolder, $"{musicData.PluginInfoGUID}{musicData.ID}");
                await Task.Run(() =>
                {
                    if (!File.Exists(path))
                    {
                        File.Create(path).Close();
                    }
                    File.WriteAllText(path, $"{lyricTuple.Item1}\n{lyricTuple.Item2}");
                });
                resultPath = path;
                LogService.Log(nameof(LyricService), "下载网络歌词完成");
            }
        }

        await InitLyricList(resultPath);
        LogService.Log(nameof(LyricService), $"初始化歌词成功： \"{musicData.Title}\"。Elapsed {DateTime.Now - startTime}");
    }

    public async Task InitLyricList(TagLib.File file)
    {
        //LogManager.Log(nameof(LyricService), "从 IDv3 标签中获取歌词");
        if (file is null)
        {
            await InitLyricList("");
            return;
        }
        if (string.IsNullOrEmpty(file.Tag.Lyrics))
        {
            LogService.Log(nameof(LyricService), "IDv3 标签中找不到歌词。", LogLevel.Warning);
            await InitLyricList("");
            return;
        }
        InitLyricList(await Task.Run(async () =>  await LyricHelper.LyricToLrcData(file.Tag.Lyrics, UseRomajiLyric)));
    }

    public async Task InitLyricList(string lyricPath)
    {
        if (string.IsNullOrEmpty(lyricPath))
        {
            NowPlayingLyrics.Clear();
            NowLyricsData = null;
            LogService.Log(nameof(LyricService), "无法获取有效歌词。", LogLevel.Warning);
            return;
        }

        //LogManager.Log(nameof(LyricService), $"读取歌词文件：\"{lyricPath}\"");
        string f = null;
        var lrcEncode = FileHelper.GetEncodingType(lyricPath);
        if (lrcEncode == Encoding.Default)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            f = await File.ReadAllTextAsync(lyricPath, Encoding.GetEncoding("GB2312"));
        }
        else
        {
            f = await File.ReadAllTextAsync(lyricPath, lrcEncode);
        }

        if (f.Length < 10)
        {
            NowPlayingLyrics.Clear();
            NowLyricsData = null;
            LogService.Log(nameof(LyricService), "歌词文件大小未超过 10 字节，不会使用此歌词文件", LogLevel.Warning);
            //System.IO.File.Delete(lyricPath);
            return;
        }

        InitLyricList(await Task.Run(async () => await LyricHelper.LyricToLrcData(f, UseRomajiLyric)));
    }

    public void InitLyricList(LyricData[] lyricDatas)
    {
        if (lyricDatas.Length > 1)
        {
            foreach (var i in lyricDatas)
            {
                NowPlayingLyrics.Add(i);
            }
            //NowLyricsData = lyricDatas[0];
        }
        else
        {
            NowLyricsData = null;
        }
    }

    public void StartTimer()
    {
        //LogManager.Log($"[LyricManager]: 歌词循环已开始");
        ReCallUpdate();
    }
    
    private void StopTimer()
    {
        //LogManager.Log($"[LyricManager]: 歌词循环已停止");
        timer.Stop();
    }

    LyricData lastLyricData = null;
    public void ReCallUpdate()
    {
        timer.Interval = TimeSpan.FromMilliseconds(UpdateInterval);
        timer.Start();
        if (PlayingLyricSelectedChanged is null) StopTimer();
        if (!NowPlayingLyrics.Any()) StopTimer();
        if (NowPlayingLyrics.Count <= 3) StopTimer();
        if (App.Instance.AudioService.PlaybackState != NAudio.Wave.PlaybackState.Playing) StopTimer();

        foreach (var data in NowPlayingLyrics)
        {
            if (data.LyricTimeSpan < App.Instance.AudioService.CurrentTime)
            {
                lastLyricData = data;
            }
            else
            {
                NowLyricsData = lastLyricData;
                break;
            }
        }
    }
    

    private async void AudioService_SourceChanged(AudioService AudioService)
    {
        if (MusicData != AudioService.MusicData)
        {
            MusicData = AudioService.MusicData;
            await InitLyricList(AudioService.MusicData);
            PlayingLyricSourceChanged?.Invoke(NowPlayingLyrics);

            //if (AudioService.NowOutDevice.DeviceType == Media.AudioService.OutApi.Wasapi) timer.Interval = TimeSpan.FromMilliseconds(AudioService.Latency);
            //else timer.Interval = TimeSpan.FromMilliseconds(100);
            StartTimer();
        }
    }
}
