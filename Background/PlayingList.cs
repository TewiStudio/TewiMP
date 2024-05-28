using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using TewiMP.Media;
using TewiMP.DataEditor;
using NAudio;
using Microsoft.UI.Xaml.Media;
using Melanchall.DryWetMidi.Core;

namespace TewiMP.Background
{
    public enum PlayBehavior { 循环播放, 顺序播放, 单曲循环, 随机播放, 播放完成后停止 }
    public enum SetPlayInfo { Normal, Next, Previous }
    public class PlayingList
    {
        public delegate void PlayingListItemChangeDelegate(ObservableCollection<MusicData> nowPlayingList);
        public event PlayingListItemChangeDelegate PlayingListItemChange;

        public delegate void NowPlayingImageChangeDelegate(ImageSource imageSource, string path);
        public event NowPlayingImageChangeDelegate NowPlayingImageLoading;
        public event NowPlayingImageChangeDelegate NowPlayingImageLoaded;

        public delegate void PlayBehaviorDelegate(PlayBehavior playBehavior);
        public event PlayBehaviorDelegate PlayBehaviorChanged;

        public ObservableCollection<MusicData> NowPlayingList = new();
        public ObservableCollection<MusicData> RandomSavePlayingList = new();

        public bool PauseWhenPreviousPause { get; set; } = true;
        public bool NextWhenPlayError { get; set; } = true;

        bool lastIsRandomPlay = false;
        private PlayBehavior _playBehavior = PlayBehavior.循环播放;
        public PlayBehavior PlayBehavior
        {
            get => _playBehavior;
            set
            {
                _playBehavior = value;
                SetRandomPlay(value);
                PlayBehaviorChanged?.Invoke(value);
            }
        }

        ImageSource _nowPlayingImage;
        public ImageSource NowPlayingImage
        {
            get => _nowPlayingImage;
            set
            {
                _nowPlayingImage = value;
            }
        }

        public PlayingList()
        {
            App.audioPlayer.SourceChanged += AudioPlayer_SourceChanged;
            App.audioPlayer.PlayEnd += AudioPlayer_PlayEnd;
        }

        public void SetRandomPlay(PlayBehavior value)
        {
            if (value == PlayBehavior.随机播放)
            {
                lastIsRandomPlay = true;
                RandomSavePlayingList.Clear();
                foreach (var item in NowPlayingList) RandomSavePlayingList.Add(item);
                var arr = NowPlayingList.ToList();
                for (int i = 0; i < NowPlayingList.Count; i++)
                {
                    int index = new Random().Next(i, NowPlayingList.Count);
                    var temp = arr[i];
                    var random = arr[index];
                    arr[i] = random;
                    arr[index] = temp;
                }
                NowPlayingList.Clear();
                foreach (var item in arr) NowPlayingList.Add(item);
            }
            else
            {
                if (lastIsRandomPlay)
                {
                    ClearAll();
                    NowPlayingList.Clear();
                    foreach (var item in RandomSavePlayingList) NowPlayingList.Add(item);
                    RandomSavePlayingList.Clear();
                }
                lastIsRandomPlay = false;
            }
            PlayingListItemChange?.Invoke(NowPlayingList);
            /*
            if (playFirst)
                if (NowPlayingList.Any())
                    await Play(NowPlayingList.First());*/
        }

        bool isPlayEndCallPlay = false;
        private async void AudioPlayer_PlayEnd(Media.AudioPlayer audioPlayer)
        {
            isPlayEndCallPlay = true;
            AddHistory(audioPlayer.MusicData);
            switch (PlayBehavior)
            {
                case PlayBehavior.循环播放:
                case PlayBehavior.顺序播放:
                case PlayBehavior.随机播放:
                    await App.playingList.PlayNext(true);
                    break;/*
                case PlayBehavior.随机播放:
                    await Play(NowPlayingList[new Random().Next(NowPlayingList.Count - 1)], true);
                    break;*/
                case PlayBehavior.单曲循环:
                    await Play(App.audioPlayer.MusicData, true);
                    break;
                case PlayBehavior.播放完成后停止:
                    App.audioPlayer.CurrentTime = TimeSpan.Zero;
                    App.audioPlayer.SetStop();
                    break;
            }
            isPlayEndCallPlay = true;
        }

        MusicData lastMusicData = null;
        private async void AudioPlayer_SourceChanged(AudioPlayer audioPlayer)
        {
            //System.Diagnostics.Debug.WriteLine(NowPlayingImageLoaded.GetInvocationList().Length);
            if (audioPlayer.FileReader.isMidi ||
                audioPlayer.MusicData == null)
            {
                lastMusicData = null;
                NowPlayingImage = null;
                NowPlayingImageLoaded?.Invoke(null, null);
                return;
            }
            if (audioPlayer.MusicData.InLocal != null)
            {
                if (audioPlayer.MusicData.Album == lastMusicData?.Album)
                {
                    if (!audioPlayer.MusicData.Album.IsNull())
                        return;
                }
            }
            else
            {
                if (!audioPlayer.MusicData.Album.IsNull())
                    if (audioPlayer.MusicData.Album.ID == lastMusicData?.Album.ID) return;
            }
            lastMusicData = audioPlayer.MusicData;

            NowPlayingImageLoading?.Invoke(null, null);
            string path;
            ImageSource a = null;

            var _ = await ImageManage.GetImageSource(audioPlayer.MusicData);
            a = _.Item1;
            path = _.Item2;

            if (a == null) { lastMusicData = null; }
            NowPlayingImage = a;
            NowPlayingImagePath = path;
            NowPlayingImageLoaded?.Invoke(NowPlayingImage, path);
            //System.Diagnostics.Debug.WriteLine(NowPlayingImageLoaded.GetInvocationList().Length);
        }
        public string NowPlayingImagePath = null;

        public void Add(MusicData musicData, bool invoke = true, bool insert = false)
        {
            Debug.WriteLine($"[PlayingList]: 播放列表已添加：\"{musicData.Title}\"");
            bool isFind = Find(musicData);
            if (!isFind)
            {
                if (insert)
                {
                    int index = 0;
                    if (App.audioPlayer.MusicData != null) index = NowPlayingList.IndexOf(App.audioPlayer.MusicData) + 1;
                    NowPlayingList.Insert(index, musicData);
                }
                else
                {
                    NowPlayingList.Add(musicData);
                }
                if (PlayBehavior == PlayBehavior.随机播放)
                {
                    if (insert)
                    {
                        int index = 0;
                        if (App.audioPlayer.MusicData is not null) index = RandomSavePlayingList.IndexOf(App.audioPlayer.MusicData) + 1;
                        RandomSavePlayingList.Insert(index, musicData);
                    }
                    else
                    {
                        RandomSavePlayingList.Add(musicData);
                    }
                }
            }
            if (invoke)
                PlayingListItemChange?.Invoke(NowPlayingList);
        }

        public async Task<bool> Play(MusicData musicData, bool isAutoPlay = false, SetPlayInfo isNextPlay = default)
        {
            Add(musicData, true, true);

            Debug.WriteLine($"[PlayingList]: 正在设置播放：\"{musicData.Title}\"");
            NAudio.Wave.PlaybackState playState;
            if (PauseWhenPreviousPause)
            {
                if (App.audioPlayer.NowOutObj != null)
                    playState = App.audioPlayer.NowOutObj.PlaybackState;
                else
                    playState = NAudio.Wave.PlaybackState.Playing; 
            }
            else
            {
                playState = NAudio.Wave.PlaybackState.Playing;
            }

            if (isAutoPlay)
            {
                playState = NAudio.Wave.PlaybackState.Playing;
            }

            var a = true;
            //System.Diagnostics.Debug.WriteLine(musicData.Title);
            try
            {
                await App.audioPlayer.SetSourceAsync(musicData);
                if (playState == NAudio.Wave.PlaybackState.Playing)
                    App.audioPlayer.SetPlay(false);
                Debug.WriteLine($"[PlayingList]: 设置播放完成：\"{musicData.Title}\"");
            }
            catch (DivideByZeroException)
            {
                a = false;
                var data = DataFolderBase.JSettingData;
                data[DataFolderBase.SettingParams.AudioLatency.ToString()] =
                    DataFolderBase.SettingDefault[DataFolderBase.SettingParams.AudioLatency.ToString()];
                App.audioPlayer.Latency = (int)data[DataFolderBase.SettingParams.AudioLatency.ToString()];
                DataFolderBase.JSettingData = data;

                MainWindow.AddNotify(
                    "播放失败",
                    $"播放音频时出现错误，可能是播放延迟设置不正确导致的。\n" +
                        $"已将播放延迟设置到默认值，请尝试重新播放。",
                    NotifySeverity.Error);
            }
            catch (NotEnoughBytesException err)
            {
                LogHelper.WriteLog("PlayingList Play Midi Error", err.ToString(), false);
                MainWindow.AddNotify("播放Midi音频时出现错误", $"似乎不支持此Midi音频文件。\n错误信息：{err.Message}", NotifySeverity.Error);
            }
            catch (MmException err)
            {
                MainWindow.AddNotify("无法初始化音频输出", $"请尝试重新播放音频，如果仍然无法初始化，请检查是否有其它应用程序独占此音频设备。\n错误信息：{err.Message}", NotifySeverity.Error);
            }
            catch (Exception e)
            {
                LogHelper.WriteLog("PlayingList Play Error", e.ToString(), false);
                a = false;

                if (NextWhenPlayError)
                {
                    if (isNextPlay == SetPlayInfo.Next)
                    {
                        var index = NowPlayingList.IndexOf(musicData) + 1;
                        if (index > NowPlayingList.Count - 1) index = 0;
                        Play(NowPlayingList[index], true, isNextPlay);
                    }
                    else if (isNextPlay == SetPlayInfo.Previous)
                    {
                        var index = NowPlayingList.IndexOf(musicData) - 1;
                        if (index < 0) index = NowPlayingList.Count - 1;
                        Play(NowPlayingList[index], true, isNextPlay);
                    }
                }
#if DEBUG
                MainWindow.AddNotify("播放音频时出现错误", e.ToString(), NotifySeverity.Error);
#else
                MainWindow.AddNotify("播放音频时出现错误", e.Message, NotifySeverity.Error);
#endif
            }

            return a;
        }

        private async void AddHistory(MusicData musicData)
        {
            await SongHistoryHelper.AddHistory(new() { MusicData = musicData, Time = DateTime.Now });
        }

        public async Task<bool> PlayNext(bool isAutoPlay = false)
        {
            if (NowPlayingList.Any())
            {
                var a = NowPlayingList.IndexOf(App.audioPlayer.pointMusicData) + 1;
                if (a > NowPlayingList.Count - 1)
                {
                    a = 0;
                }

                return await Play(NowPlayingList[a], isAutoPlay, SetPlayInfo.Next);
            }

            return true;
        }

        public async Task<bool> PlayPrevious()
        {
            if (NowPlayingList.Any())
            {
                var a = NowPlayingList.IndexOf(App.audioPlayer.pointMusicData) - 1;
                if (a < 0)
                {
                    a = NowPlayingList.Count - 1;
                }

                return await Play(NowPlayingList[a], false, SetPlayInfo.Previous);
            }

            return true;
        }

        public void SetNextPlay(MusicData currentData, MusicData insertData)
        {
            if (!NowPlayingList.Any()) return;
            if (Find(insertData)) NowPlayingList.Remove(insertData);

            NowPlayingList.Insert(NowPlayingList.IndexOf(currentData) + 1, insertData);
        }

        public bool Find(MusicData musicData)
        {
            return NowPlayingList.Contains(musicData);
        }

        public void ClearAll()
        {
            NowPlayingList.Clear();
        }
    }
}
