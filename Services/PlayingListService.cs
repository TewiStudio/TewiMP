using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using NAudio;
using Melanchall.DryWetMidi.Core;
using System.Threading.Tasks;
using TewiMP.Helpers;
using TewiMP.UI.Windows;
using TewiMP.Core.Music;
using TewiMP.Services.Media;
using TewiMP.Services.Media.Audio;
using TewiMP.Services.Storage;

namespace TewiMP.Services;

public enum PlayBehavior { 循环播放, 顺序播放, 单曲循环, 随机播放, 播放完成后停止 }
public enum SetPlayInfo { Normal, Next, Previous }

public static class PlayBehaviorStatic
{
    public static string GetIcon(this PlayBehavior playBehavior)
    {
        return playBehavior switch
        {
            PlayBehavior.循环播放 => "\uE895",
            PlayBehavior.顺序播放 => "\uE8AB",
            PlayBehavior.单曲循环 => "\uE777",
            PlayBehavior.随机播放 => "\uE8B1",
            PlayBehavior.播放完成后停止 => "\uE71A",
            _ => ""
        };
    }
}

public class PlayingListService
{
    public delegate void PlayingListItemChangeDelegate(ObservableCollection<MusicData> nowPlayingList);
    public event PlayingListItemChangeDelegate PlayingListItemChange;

    public delegate void NowPlayingImageChangeDelegate(Uri imageSource, string path);
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

    Uri _nowPlayingImage;
    public Uri NowPlayingImage
    {
        get => _nowPlayingImage;
        set
        {
            _nowPlayingImage = value;
        }
    }

    public PlayingListService()
    {
        LogService.Log("Starting", "初始化 PlayingList.");

        App.Instance.AudioService.SourceChanged += AudioService_SourceChanged;
        App.Instance.AudioService.PlayEnd += AudioService_PlayEnd;
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
    private async void AudioService_PlayEnd(AudioService AudioService)
    {
        isPlayEndCallPlay = true;
        switch (PlayBehavior)
        {
            case PlayBehavior.循环播放:
            case PlayBehavior.顺序播放:
            case PlayBehavior.随机播放:
                await App.Instance.PlayingListService.PlayNext(true);
                break;/*
            case PlayBehavior.随机播放:
                await Play(NowPlayingList[new Random().Next(NowPlayingList.Count - 1)], true);
                break;*/
            case PlayBehavior.单曲循环:
                await Play(App.Instance.AudioService.MusicData, true);
                break;
            case PlayBehavior.播放完成后停止:
                App.Instance.AudioService.CurrentTime = TimeSpan.Zero;
                App.Instance.AudioService.SetStop();
                break;
        }
        isPlayEndCallPlay = true;
    }

    MusicData lastMusicData = null;
    private CancellationTokenSource _imageLoadingCts;
    private async void AudioService_SourceChanged(AudioService AudioService)
    {
        if (AudioService?.FileReader is null) return;

        // 检查是否为无需加载封面的情况 (MIDI 或 数据为空)
        if (AudioService.FileReader.isMidi || AudioService.MusicData is null)
        {
            lastMusicData = null;
            NowPlayingImage = null;
            NowPlayingImagePath = null;
            NowPlayingImageLoaded?.Invoke(null, null);
            return;
        }

        // 如果是同一张专辑，则不重新加载图片
        if (IsSameAlbum(AudioService.MusicData, lastMusicData))
        {
            return;
        }

        // 如果上一次加载还在进行中，取消它
        _imageLoadingCts?.Cancel();
        _imageLoadingCts = new CancellationTokenSource();
        var currentToken = _imageLoadingCts.Token;

        try
        {
            // 更新 lastMusicData
            lastMusicData = AudioService.MusicData;

            // 通知 UI 开始加载
            NowPlayingImageLoading?.Invoke(null, null);

            // 异步获取图片
            var uri = await ImageService.GetImageUri(AudioService.MusicData);

            // 如果 await 期间又切歌了，直接退出，不要覆盖新歌的数据
            if (currentToken.IsCancellationRequested) return;

            NowPlayingImage = uri;
            NowPlayingImagePath = uri.LocalPath;

            // 处理空图片情况 TODO: FIX THIS
            if (uri is null) lastMusicData = null;

            // 提取颜色。确保此时 NowPlayingImagePath 已经是新的
            await GetImageColor();

            // 再次检查取消
            if (currentToken.IsCancellationRequested) return;

            // 加载完成
            NowPlayingImageLoaded?.Invoke(NowPlayingImage, NowPlayingImagePath);
        }
        catch (OperationCanceledException)
        {
            // 任务被取消是预期行为，忽略
        }
        catch (Exception ex)
        {
            LogService.Error(nameof(PlayingListService), $"加载封面失败: {ex.Message}");
            // 发生错误时，最好重置图片或显示默认图
            NowPlayingImage = null;
            NowPlayingImageLoaded?.Invoke(null, null);
        }
    }

    /// <summary>
    /// 辅助方法：判断两个 MusicData 是否属于同一张专辑
    /// </summary>
    private bool IsSameAlbum(MusicData current, MusicData last)
    {
        if (last is null) return false;
        if (current.Album.IsNull()) return false; // 如果当前没有专辑信息，视为不同，强制刷新

        // 如果是本地音乐
        if (current.InLocal != null)
        {
            // 比较专辑对象是否相同 (或者你可以比较 current.Album.Name)
            return current.Album == last.Album;
        }
        else
        {
            // 在线音乐通常比较 ID
            return current.Album.ID == last.Album.ID;
        }
    }

    public string NowPlayingImagePath = null;

    public void Add(MusicData musicData, bool invoke = true, bool insert = false)
    {
        bool isFind = Find(musicData);
        if (!isFind)
        {
            if (insert)
            {
                int index = 0;
                if (App.Instance.AudioService.MusicData != null) index = NowPlayingList.IndexOf(App.Instance.AudioService.MusicData) + 1;
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
                    if (App.Instance.AudioService.MusicData is not null) index = RandomSavePlayingList.IndexOf(App.Instance.AudioService.MusicData) + 1;
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

    public SetPlayInfo IsNextPlay = default;
    int nextErrorCount = 0;
    public async Task<bool> Play(MusicData musicData, bool isAutoPlay = true, SetPlayInfo isNextPlay = default)
    {
        IsNextPlay = isNextPlay;
        var time = DateTime.Now;
        Add(musicData, true, true);

        LogService.Log(nameof(PlayingListService), $"Playing：\"{musicData}\"");
        NAudio.Wave.PlaybackState playState;
        if (PauseWhenPreviousPause)
        {
            if (App.Instance.AudioService.NowOutObj != null)
                playState = App.Instance.AudioService.NowOutObj.PlaybackState;
            else
                playState = NAudio.Wave.PlaybackState.Playing; 
        }
        else
        {
            playState = NAudio.Wave.PlaybackState.Paused;
        }

        if (isAutoPlay)
        {
            playState = NAudio.Wave.PlaybackState.Playing;
        }

        var clear = false;
        //System.Diagnostics.LogManager.Log(musicData.Title);
        try
        {
            await App.Instance.AudioService.SetSourceAsync(musicData);
            if (playState == NAudio.Wave.PlaybackState.Playing)
                App.Instance.AudioService.SetPlay(false);
            clear = true;
        }
        catch (DivideByZeroException err)
        {
            var data = DataFolderBase.JSettingData;
            data[DataFolderBase.SettingParams.AudioLatency.ToString()] =
                DataFolderBase.SettingDefault[DataFolderBase.SettingParams.AudioLatency.ToString()];
            App.Instance.AudioService.Latency = (int)data[DataFolderBase.SettingParams.AudioLatency.ToString()];
            DataFolderBase.JSettingData = data;

            App.MainWindowInstance.AddNotify(
                "播放失败",
                $"播放音频时出现错误，可能是播放延迟设置不正确导致的。\n" +
                    $"已将播放延迟设置到默认值，请尝试重新播放。",
                NotifySeverity.Error);
            LogService.Error(nameof(PlayingListService), $"播放音频时出现错误，可能是播放延迟设置不正确导致的。\n错误信息：{err}");
        }
        catch (NotEnoughBytesException err)
        {
            LogService.Error(nameof(PlayingListService), $"播放Midi音频时出现错误，似乎不支持此Midi音频文件。\n错误信息：{err}");
            App.MainWindowInstance.AddNotify("播放Midi音频时出现错误", $"似乎不支持此Midi音频文件。\n错误信息：{err.Message}", NotifySeverity.Error);
        }
        catch (MmException err)
        {
            LogService.Error(nameof(PlayingListService), $"无法初始化音频输出。请尝试重新播放音频，如果仍然无法初始化，请检查是否有其它应用程序独占此音频设备。\n错误信息：{err}");
            App.MainWindowInstance.AddNotify("无法初始化音频输出", $"请尝试重新播放音频，如果仍然无法初始化，请检查是否有其它应用程序独占此音频设备。\n错误信息：{err.Message}", NotifySeverity.Error);
        }
        catch (Exception e)
        {
            LogService.Error(nameof(PlayingListService), $"播放音频时出现错误。\n错误信息：{e}");
#if DEBUG
            App.MainWindowInstance.AddNotify("播放音频时出现错误", e.ToString(), NotifySeverity.Error);
#else
            App.MainWindowInstance.AddNotify("播放音频时出现错误", e.Message, NotifySeverity.Error);
#endif

        }
        if (!clear)
        {
            if (NextWhenPlayError && nextErrorCount <= 10)
            {
                nextErrorCount++;
                if (isNextPlay == SetPlayInfo.Next)
                {
                    var index = NowPlayingList.IndexOf(musicData) + 1;
                    if (index > NowPlayingList.Count - 1) index = 0;
                    await Play(NowPlayingList[index], true, isNextPlay);
                }
                else if (isNextPlay == SetPlayInfo.Previous)
                {
                    var index = NowPlayingList.IndexOf(musicData) - 1;
                    if (index < 0) index = NowPlayingList.Count - 1;
                    await Play(NowPlayingList[index], true, isNextPlay);
                }
            }
            else
            {
                if (nextErrorCount > 10)
                {
                    App.MainWindowInstance.AddNotify("无法继续播放", "因为错误次数太多，自动播放下一首歌曲的功能已在此次禁用。", NotifySeverity.Error);
                    nextErrorCount = 0;
                }
            }
        }
        else
        {
            nextErrorCount = 0;
        }
        LogService.Elapsed("PlayingList.Play", "Setting play in {0}.", time);
        return clear;
    }

    private async void AddHistory(MusicData musicData)
    {
        await SongHistoryHelper.AddHistory(new() { MusicData = musicData, Time = DateTime.Now });
    }

    public async Task<bool> PlayNext(bool isAutoPlay = true)
    {
        if (NowPlayingList.Any())
        {
            var a = NowPlayingList.IndexOf(App.Instance.AudioService.pointMusicData) + 1;
            if (a > NowPlayingList.Count - 1)
            {
                a = 0;
            }

            var succes = await Play(NowPlayingList[a], isAutoPlay, SetPlayInfo.Next);
            return succes;
        }

        return true;
    }

    public async Task<bool> PlayPrevious(bool isAutoPlay = true)
    {
        if (NowPlayingList.Any())
        {
            var a = NowPlayingList.IndexOf(App.Instance.AudioService.pointMusicData) - 1;
            if (a < 0)
            {
                a = NowPlayingList.Count - 1;
            }

            var succes = await Play(NowPlayingList[a], isAutoPlay, SetPlayInfo.Previous);
            return succes;
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

    public async Task UpdateImageColor(bool forceChange = false)
    {
        await GetImageColor(forceChange);
        NowPlayingImageLoaded?.Invoke(NowPlayingImage, NowPlayingImagePath);
    }

    public Windows.UI.Color AlbumAccentColor { get; set; }
    public Windows.UI.Color AlbumAccentColorReverse { get; set; }
    public Windows.UI.Color TextOnAlbumAccentColor { get; set; }
    public Windows.UI.Color TextColor { get; set; }

    // 记录上一次处理成功的图片路径，避免重复计算
    private string _lastProcessedImagePath;

    public async Task GetImageColor(bool forceChange = false)
    {
        // 获取当前路径
        var nowImagePath = NowPlayingImagePath;

        if (string.Equals(nowImagePath, _lastProcessedImagePath, StringComparison.OrdinalIgnoreCase)
            && _lastProcessedImagePath is not null
            && !forceChange)
            return;

        LogService.Info(nameof(PlayingListService), $"Album accent color source：From \"{_lastProcessedImagePath}\" To \"{nowImagePath}\"");
        _lastProcessedImagePath = nowImagePath;

        Windows.UI.Color albumColor, albumColorReverse, textColorOnAlbum;

        // 是重置默认还是提取新颜色
        if (string.IsNullOrEmpty(nowImagePath))
        {
            // 路径为空，重置为系统主题色
            var systemAccent = (Windows.UI.Color)App.Current.Resources["SystemAccentColor"];
            albumColor = systemAccent;
            albumColorReverse = systemAccent;
            textColorOnAlbum = CodeHelper.IsAccentColorDark(albumColor) ? Colors.White : Windows.UI.Color.FromArgb(228, 0, 0, 0);
        }
        else
        {
            try
            {
                // 异步提取颜色
                var themeColor = await CodeHelper.GetThemeColorAsync(nowImagePath);

                // 并发保护：await 回来后，检查当前播放图片是否已经又变了
                // 如果变了，说明这次计算已经过时，直接丢弃
                if (nowImagePath != NowPlayingImagePath)
                {
                    LogService.Info(nameof(PlayingListService), "图片路径已变更，放弃应用旧的颜色计算结果。");
                    return;
                }

                albumColor = themeColor.Item1;
                albumColorReverse = themeColor.Item2;
                textColorOnAlbum = themeColor.Item3;

                LogService.Info(nameof(PlayingListService), $"Current album color：{albumColor}");
            }
            catch (Exception ex)
            {
                LogService.Error(nameof(PlayingListService), $"提取颜色失败，回退到默认颜色: {ex.Message}");
                // 发生异常时的回退逻辑
                var systemAccent = (Windows.UI.Color)App.Current.Resources["SystemAccentColor"];
                albumColor = systemAccent;
                albumColorReverse = systemAccent;
                textColorOnAlbum = Colors.White;
            }
        }

        // 更新属性
        AlbumAccentColor = albumColor;
        AlbumAccentColorReverse = albumColorReverse;
        TextOnAlbumAccentColor = textColorOnAlbum;

        TextColor = App.MainWindowInstance.WindowGridBase.ActualTheme == ElementTheme.Dark
            ? Colors.White
            : Windows.UI.Color.FromArgb(228, 0, 0, 0);

        // 更新资源字典
        UpdateColorResource("MusicAlbumAccentBrush", albumColor);
        UpdateColorResource("MusicAlbumAccentBrushDark1", albumColor.Darken(.1f));
        UpdateColorResource("MusicAlbumAccentBrushDark2", albumColor.Darken(.2f));
        UpdateColorResource("MusicAlbumAccentBrushReverse", albumColorReverse);

        UpdateColorResource("TextOnMusicAlbumAccentForegroundBrush", textColorOnAlbum);
        UpdateColorResource("TextOnMusicAlbumAccentForegroundBrushDark1", textColorOnAlbum.Darken(.1f));
        UpdateColorResource("TextOnMusicAlbumAccentForegroundBrushDark2", textColorOnAlbum.Darken(.2f));

        // 更新 GradientStop (特殊处理)
        if (App.Current.Resources.TryGetValue("TextControlElevationBorderMusicAlbumAccentColorFocusedBrush", out var brushObj)
            && brushObj is LinearGradientBrush lgb
            && lgb.GradientStops.Count > 0)
        {
            lgb.GradientStops[0].Color = albumColor;
        }
    }

    // 安全更新 SolidColorBrush
    private void UpdateColorResource(string resourceKey, Windows.UI.Color newColor)
    {
        if (App.Current.Resources.TryGetValue(resourceKey, out var obj) && obj is SolidColorBrush brush)
        {
            brush.Color = newColor;
        }
    }
}
