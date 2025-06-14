﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using TewiMP.Helpers;
using TewiMP.DataEditor;
using System.IO;

namespace TewiMP.Background
{
    public class LyricManager
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
            //App.logManager.Log("LyricManager", $"当前歌词已设置为：\"{lyricData?.Lyric?.FirstOrDefault()}\"");
        }

        public LyricManager()
        {
            timer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(UpdateInterval) };
            timer.Tick += (_, __) =>
            {
                ReCallUpdate();
                LyricTimingChanged?.Invoke(NowLyricsData);
            };

            //MainWindow.WindowViewStateChanged += MainWindow_WindowViewStateChanged;
            App.audioPlayer.SourceChanged += AudioPlayer_SourceChanged;
            App.audioPlayer.PlayStateChanged += AudioPlayer_PlayStateChanged;
            App.audioPlayer.TimingChanged += AudioPlayer_TimingChanged;
        }

        private void AudioPlayer_PlayStateChanged(Media.AudioPlayer audioPlayer)
        {
            if (App.audioPlayer.PlaybackState == NAudio.Wave.PlaybackState.Playing)
            {
                StartTimer();
            }
            else
            {
                StopTimer();
            }
        }

        private void AudioPlayer_TimingChanged(Media.AudioPlayer audioPlayer)
        {
            // 使暂停时更改播放进度可以改变歌词
            if (audioPlayer.PlaybackState != NAudio.Wave.PlaybackState.Playing) ReCallUpdate();
        }

        public async Task InitLyricList(MusicData musicData)
        {
            App.logManager.Log("LyricManager", $"初始化歌词：\"{musicData.Title}\"");
            if (musicData is null) return;
            NowPlayingLyrics.Clear();

            string cachePath = await FileHelper.GetLyricCache(musicData);
            string resultPath = null;

            if (cachePath != null)
            {
                resultPath = cachePath;
                App.logManager.Log("LyricManager", $"找到歌词缓存：\"{cachePath}\"");
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

                App.logManager.Log("LyricManager", "从网络中下载歌词");
                Tuple<string, string> lyricTuple;
                if (musicData.From == MusicFrom.pluginMusicSource)
                {
                    lyricTuple = await musicData.PluginInfo.GetMusicSourcePlugin().GetLyric(musicData.ID);
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
                    string path = Path.Combine(DataFolderBase.LyricCacheFolder, $"{musicData.PluginInfo}{musicData.ID}");
                    await Task.Run(() =>
                    {
                        if (!File.Exists(path))
                        {
                            File.Create(path).Close();
                        }
                        File.WriteAllText(path, $"{lyricTuple.Item1}\n{lyricTuple.Item2}");
                    });
                    resultPath = path;
                    App.logManager.Log("LyricManager", "下载网络歌词完成");
                }
            }

            await InitLyricList(resultPath);
            App.logManager.Log("LyricManager", $"初始化歌词成功： \"{musicData.Title}\"");
        }

        public async Task InitLyricList(TagLib.File file)
        {
            App.logManager.Log("LyricManager", "从 IDv3 标签中获取歌词");
            if (file is null)
            {
                await InitLyricList("");
                return;
            }
            if (string.IsNullOrEmpty(file.Tag.Lyrics))
            {
                App.logManager.Log("LyricManager", "IDv3 标签中找不到歌词。", LogLevel.Warning);
                await InitLyricList("");
                return;
            }
            InitLyricList(await LyricHelper.LyricToLrcData(file.Tag.Lyrics));
        }

        public async Task InitLyricList(string lyricPath)
        {
            if (string.IsNullOrEmpty(lyricPath))
            {
                NowPlayingLyrics.Clear();
                NowLyricsData = null;
                App.logManager.Log("LyricManager", "无法获取有效歌词。", LogLevel.Warning);
                return;
            }

            App.logManager.Log("LyricManager", $"读取歌词文件：\"{lyricPath}\"");
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
                App.logManager.Log("LyricManager", "歌词文件大小未超过 10 字节，不会使用此歌词文件", LogLevel.Warning);
                //System.IO.File.Delete(lyricPath);
                return;
            }

            InitLyricList(await LyricHelper.LyricToLrcData(f, UseRomajiLyric));
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
            //App.logManager.Log($"[LyricManager]: 歌词循环已开始");
            ReCallUpdate();
        }
        
        private void StopTimer()
        {
            //App.logManager.Log($"[LyricManager]: 歌词循环已停止");
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
            if (App.audioPlayer.PlaybackState != NAudio.Wave.PlaybackState.Playing) StopTimer();

            foreach (var data in NowPlayingLyrics)
            {
                if (data.LyricTimeSpan < App.audioPlayer.CurrentTime)
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
        

        private async void AudioPlayer_SourceChanged(Media.AudioPlayer audioPlayer)
        {
            if (MusicData != audioPlayer.MusicData)
            {
                MusicData = audioPlayer.MusicData;
                await InitLyricList(audioPlayer.MusicData);
                PlayingLyricSourceChanged?.Invoke(NowPlayingLyrics);

                //if (audioPlayer.NowOutDevice.DeviceType == Media.AudioPlayer.OutApi.Wasapi) timer.Interval = TimeSpan.FromMilliseconds(audioPlayer.Latency);
                //else timer.Interval = TimeSpan.FromMilliseconds(100);
                StartTimer();
            }
        }
    }
}
