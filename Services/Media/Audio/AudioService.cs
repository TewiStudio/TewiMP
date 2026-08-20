using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using SoundTouch;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TewiMP.Core;
using TewiMP.Core.Music;
using TewiMP.Services.Media.Audio.AudioEffects;
using TewiMP.UI.Windows;

namespace TewiMP.Services.Media.Audio;

public class AudioService
{
    public delegate void AudioServiceDelegate(AudioService AudioService);
    public delegate void AudioServiceDataDelegate(AudioService AudioService, object data);
    public delegate void AudioServiceVolumeMeterDelegate(AudioService AudioService, float[] sample);
    public event AudioServiceDelegate PlayEnd;
    public event AudioServiceDelegate SourceChanged;
    public event AudioServiceDelegate PreviewSourceChanged;
    public event AudioServiceDelegate TimingChanged;
    public event AudioServiceDelegate PlayStateChanged;
    public event AudioServiceDataDelegate VolumeChanged;
    public event AudioServiceDataDelegate CacheLoadingChanged;
    public event AudioServiceDelegate CacheLoadedChanged;
    public event AudioServiceDelegate EqEnableChanged;
    public event AudioServiceDelegate EqBandChanged;
    public event AudioServiceVolumeMeterDelegate VolumeMeter;

    private DispatcherTimer _timer;
    private List<float[]> _equalizerBand = AudioEqualizerBands.NormalBands;
    private OutDevice _nowOutDevice = new(OutApi.None);
    private MMDevice _wasapiMMDevice;
    private SemaphoreSlim _loadingLock = new(1, 1);
    private string _audioFilePath = null;
    private bool _eqEnalbed = false;
    private bool _wasapiOnly = false;
    private int _latency = 50;
    private float _volume = 0f;
    private double _pitch = 1f;
    private double _tempo = 1f;
    private double _rate = 1f;

    public MusicData pointMusicData = null;
    public string FileType = null;
    public int FileSize = 0;
    public string AudioBitrate = null;
    public bool InLoadingAudioFile = false;
    public ATL.Track tfile = null;

    public AudioThread AudioThread { get; private set; } = new();
    public ClientDeviceEvents ClientDeviceEvents { get; private set; } = new();
    public AudioFileReader FileReader { get; set; } = null;
    public SoundTouchWaveProvider FileProvider { get; set; } = null;
    public VolumeSampleProvider VolumeSampleProvider { get; set; } = null;
    public SpectrumAnalyzer AudioAnalyzer { get; set; } = null;
    public IWavePlayer NowOutObj { get; set; } = null;
    public MidiFile MidiFile { get; set; } = null;
    public OutputDevice MidiOutputDevice { get; set; } = null;
    public Playback MidiPlayback { get; set; } = null;
    public MusicData MusicData { get; private set; }
    public bool IsReloadErrorFile { get; set; }
    public string NameOfBand { get; set; }
    public string NameOfBandCH { get; set; }

    public List<float[]> EqualizerBand
    {
        get
        {
            return _equalizerBand;
        }
        set
        {
            if (value != null)
            {
                _equalizerBand = value;
                if (FileReader != null)
                {
                    for (int i = 0; i < value.Count; i++)
                    {
                        AudioEqualizerBands.NormalBands[i][2] = value[i][2];
                    }
                    FileReader.CreateFilters();
                }
                EqBandChanged?.Invoke(this);
            }
        }
    }

    public bool EqEnabled
    {
        get
        {
            return _eqEnalbed;
        }
        set
        {
            _eqEnalbed = value;
            EqEnableChanged?.Invoke(this);
            if (FileReader != null)
            {
                EqualizerBand = EqualizerBand;
                FileReader.EqEnabled = value;
            }
        }
    }

    public bool WasapiOnly
    {
        get
        {
            return _wasapiOnly;
        }
        set
        {
            _wasapiOnly = value;
            if (NowOutObj is null) return;
            if (NowOutObj.GetType() == typeof(WasapiPlayer))
            {
                SetReloadAsync();
            }
        }
    }

    public int Latency
    {
        get { return _latency; }
        set
        {
            _latency = value;
            SetReloadAsync();
        }
    }

    public TimeSpan CurrentTime
    {
        get
        {
            if (InLoadingAudioFile) return TimeSpan.Zero;
            if (FileReader != null)
            {
                if (FileReader.isMidi)
                {
                    if (MidiPlayback is null) return TimeSpan.Zero;
                    return TimeSpan.FromMilliseconds((MidiPlayback.GetCurrentTime(TimeSpanType.Metric) as MetricTimeSpan).TotalMilliseconds);
                }
                else
                {
                    if (MusicData.CUETrackData != null)
                    {
                        //LogManager.Log($"{FileReader.CurrentTime}  --  {MusicData.CUETrackData.EndDuration}");
                        return FileReader.CurrentTime - MusicData.CUETrackData.StartDuration - TimeSpan.FromMilliseconds(Latency);
                    }
                    else
                    {
                        return FileReader.CurrentTime - (NowOutDevice.DeviceType != OutApi.Wasapi ? TimeSpan.FromMilliseconds(Latency) : TimeSpan.Zero);
                    }
                }
            }
            return TimeSpan.Zero;
        }
        set
        {
            if (InLoadingAudioFile) return;
            if (FileReader != null)
            {
                if (FileReader.isMidi)
                {
                    if (MidiPlayback != null)
                        MidiPlayback.MoveToTime(new MetricTimeSpan(value.Hours, value.Minutes, value.Seconds, value.Milliseconds));
                }
                else
                {
                    if (MusicData.CUETrackData != null)
                    {
                        FileReader.CurrentTime = MusicData.CUETrackData.StartDuration + value;
                    }
                    else
                    {
                        FileReader.CurrentTime = value;
                    }
                }
                TimingChanged?.Invoke(this);
            }
        }
    }

    public TimeSpan TotalTime
    {
        get
        {
            if (InLoadingAudioFile) return TimeSpan.MaxValue;
            if (FileReader != null)
            {
                if (FileReader.isMidi)
                {
                    if (MidiPlayback is null) return TimeSpan.Zero;
                    return TimeSpan.FromMilliseconds((MidiPlayback.GetDuration(TimeSpanType.Metric) as MetricTimeSpan).TotalMilliseconds);
                }
                else
                {
                    if (MusicData.CUETrackData != null && MusicData.CUETrackData.Duration > TimeSpan.Zero)
                    {
                        return MusicData.CUETrackData.Duration;
                    }
                    else
                    {
                        return FileReader.TotalTime;// - TimeSpan.FromMilliseconds(Latency);
                    }
                }
            }
            return TimeSpan.Zero;
        }
    }

    public PlaybackState PlaybackState
    {
        get
        {
            if (FileReader != null)
            {
                if (FileReader.isMidi)
                {
                    if (MidiPlayback is null) return PlaybackState.Stopped;
                    if (MidiPlayback.IsRunning)
                        return PlaybackState.Playing;
                    else return PlaybackState.Paused;
                }
                else
                {
                    if (NowOutObj != null)
                        return NowOutObj.PlaybackState;
                    else return PlaybackState.Stopped;
                }
            }
            return PlaybackState.Stopped;
        }
    }

    public OutDevice NowOutDevice
    {
        get => _nowOutDevice;
        set
        {
            _nowOutDevice = value;
        }
    }

    public float Volume
    {
        get
        {
            return _volume;
        }
        set
        {
            _volume = float.Clamp(value, 0, 100f);
            VolumeChanged?.Invoke(this, _volume);
            if (VolumeSampleProvider != null)
            {
                if (!FileReader.isMidi)
                {
                    VolumeSampleProvider.Volume = _volume / 100f;
                }
            }
        }
    }

    public double Pitch
    {
        get => _pitch;
        set
        {
            _pitch = value;
            if (FileProvider != null) FileProvider.Pitch = value;
        }
    }

    public double Tempo
    {
        get => _tempo;
        set
        {
            _tempo = value;
            if (FileProvider != null) FileProvider.Tempo = value;
            if (FileReader?.isMidi == true)
            {
                MidiPlayback.Speed = value;
            }
        }
    }

    public double Rate
    {
        get => _rate;
        set
        {
            _rate = value;
            if (FileProvider != null) FileProvider.Rate = value;
        }
    }

    public string WaveInfo { get; set; } = "";

    public AudioService()
    {
        _timer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(200) };
        _timer.Tick += (_, __) => ReCallTiming();
        CompositionTarget.Rendering += CompositionTarget_Rendering;

        App.Instance.CacheService.AddingCacheMusicData += CacheManager_AddingCacheMusicData;
        App.Instance.CacheService.CachedMusicData += CacheManager_CachedMusicData;
        App.Instance.CacheService.CachingStateChangeMusicData += CacheManager_CachingStateChangeMusicData;

        ClientDeviceEvents.DeviceNotificationClient.DefaultDeviceChanged += DeviceNotificationClient_DefaultDeviceChanged;
        ClientDeviceEvents.DeviceNotificationClient.DeviceStateChanged += DeviceNotificationClient_DeviceStateChanged;
        ClientDeviceEvents.DeviceNotificationClient.DeviceRemoved += DeviceNotificationClient_DeviceRemoved;
    }

    private readonly object _analyzerLock = new();
    public float[] spectrum = null;

    private readonly Queue<double> _frameTimes = new();
    private double _deltaTime = 0;
    private double _lastDrawTime = 0; 
    private const double SampleDuration = 1.0;
    public double AvgFps = 0;
    public double AvgFrameMs = 0;
    private void CompositionTarget_Rendering(object sender, object e)
    {
        if (VolumeMeter is null) return;

        spectrum = null;
        lock (_analyzerLock)
        {
            var analyzer = AudioAnalyzer;
            if (analyzer is null) return;

            analyzer.Analyze();
            spectrum = analyzer.Spectrum;
        }

        double now = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        if (_lastDrawTime > 0)
        {
            _deltaTime = now - _lastDrawTime;

            _frameTimes.Enqueue(_deltaTime);
            // 移除超出统计窗口的旧数据
            while (_frameTimes.Sum() > SampleDuration)
                _frameTimes.Dequeue();

            // 计算平均帧时间和FPS
            if (_frameTimes.Count > 0)
            {
                double avgDelta = _frameTimes.Average();
                AvgFps = 1.0 / avgDelta;
                AvgFrameMs = avgDelta * 1000.0;
            }
        }
        _lastDrawTime = now;

        if (spectrum != null)
            VolumeMeter.Invoke(this, spectrum);
    }

    private void CacheManager_AddingCacheMusicData(MusicData musicData, object value)
    {
        if (musicData != pointMusicData) return;
        CacheLoadingChanged?.Invoke(this, null);
    }

    private void CacheManager_CachedMusicData(MusicData musicData, object value)
    {
        if (musicData != pointMusicData) return;
        CacheLoadedChanged?.Invoke(this);
    }

    private void CacheManager_CachingStateChangeMusicData(MusicData musicData, object value)
    {
        if (musicData != pointMusicData) return;
        CacheLoadingChanged?.Invoke(this, value);
    }

    int defaultDeviceChangedCounter = 0;
    private async void DeviceNotificationClient_DefaultDeviceChanged(object sender, DefaultDeviceChangedEventArgs _)
    {
        defaultDeviceChangedCounter++;
        await Task.Delay(100); // 不加会导致 集合被修改 的错误，DirectSound 导致的 >:(
        defaultDeviceChangedCounter--;
        if (defaultDeviceChangedCounter != 0) return;

        var devices = await OutDevice.GetOutDevicesAsync();
        if (NowOutObj is null)
        {
            NowOutDevice = devices.First();
            return;
        }
        if (devices.First().DeviceType == OutApi.None)
        {
            App.MainWindowInstance.Invoke(() =>
            {
                App.MainWindowInstance.AddNotify("无音频输出设备", "似乎所有音频输出设备都已被拔出，程序找不到音频输出设备。\n" +
                    "请检查音频驱动是否正常工作，或检查音频输出设备的接口是否松动或拔出。\n" +
                    "如果检查完毕后仍然无法正常播放，请到 GitHub 里向项目提出 Issues。",
                    NotifySeverity.Error, TimeSpan.FromSeconds(10));
            });
            return;
        }
        if (!devices.Contains(NowOutDevice))
        {
            if (NowOutDevice.DeviceType == OutApi.DirectSound) NowOutDevice = OutDevice.GetDirectSoundOutDefaultDevice();
            else if (NowOutDevice.DeviceType == OutApi.Wasapi) NowOutDevice = OutDevice.GetWasapiDefaultDevice();
            else NowOutDevice = devices.First();
        }

        App.MainWindowInstance.Invoke(() =>
        {
            if (isPlaying) SetPlay();
            else SetPause();
        });
        if (NowOutObj.GetType() != typeof(DirectSoundOut) && NowOutObj.GetType() != typeof(WasapiPlayer)) return;
        if (!NowOutDevice.IsDefaultDevice) return;

        if (NowOutObj.GetType() == typeof(WasapiPlayer)) NowOutDevice = OutDevice.GetWasapiDefaultDevice();
        else if (NowOutObj.GetType() == typeof(DirectSoundOut)) NowOutDevice = OutDevice.GetDirectSoundOutDefaultDevice();
        App.MainWindowInstance.Invoke(() =>
        {
            SetReloadAsync(isPlaying);
        });
    }

    private void DeviceNotificationClient_DeviceStateChanged(object sender, DeviceStateChangedEventArgs e)
    {
        if (e.NewState== DeviceState.Disabled)
            DeviceNotificationClient_DefaultDeviceChanged(sender, null);
    }

    private void DeviceNotificationClient_DeviceRemoved(object sender, DeviceNotificationEventArgs e)
    {
        LogService.Log("DeviceManager", $"Device {e.DeviceId} Removed.");
    }

    private CancellationTokenSource _loadCts;
    public async Task SetSourceAsync(MusicData musicData)
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;
        bool lockTaken = false;

        pointMusicData = musicData;

        await Task.Delay(200);
        if (token.IsCancellationRequested)
            return;

        //LogManager.Log(nameof(AudioService), $"Loading：\"{musicData}\"");
        try
        {
            //await SetSource(musicData);
            if (isCUEEndCalled)
            {
                isCUEEndCalled = false;
                CurrentTime = TimeSpan.Zero;
            }

            // 如果是同一首歌，重置时间即可。
            if (musicData == MusicData)
            {
                if (FileReader != null)
                {
                    CurrentTime = TimeSpan.Zero;
                }
                return;
            }

            // 如果是本地文件，直接使用本地路径。否则先缓存音乐文件在返回本地路径。
            string resultPath = null;
            if (musicData.From == MusicFrom.localMusic) resultPath = musicData.InLocal;
            else
            {
                // 获取音频缓存文件
                resultPath = await App.Instance.CacheService.StartCacheMusic(musicData);
            }
            if (await Task.Run(() => !File.Exists(resultPath)))
            {
                throw new FileNotFoundException($"找不到位于 \"{resultPath}\" 的音频文件。");
            }

            // 如果在缓存期间切换了歌曲，取消执行
            if (token.IsCancellationRequested)
                return;

            await _loadingLock.WaitAsync(token);
            lockTaken = true;
            CacheLoadingChanged?.Invoke(this, null);

            // 获取输出设备
            var devices = await OutDevice.GetOutDevicesAsync();
            if (devices.First().DeviceType == OutApi.None)
            {
                throw new Exception("当前没有音频输出设备。\n请检查音频设置是否正确、输出设备是否插入和音频设备驱动是否正常工作。");
            }
            if (NowOutDevice.DeviceType == OutApi.None)
            {
                NowOutDevice = devices.First();
            }

            // 初始化音频读取
            await AudioThread.InvokeAsync(() =>
            {
                // 获取音频信息
                UpdateInfo(resultPath);
                FileSize = (int)tfile.AudioDataSize;

                // ffmpeg
                var fileReader = new AudioFileReader(resultPath, musicData.CUETrackData != null);

                // 释放非托管对象
                DisposeAll();

                FileReader = fileReader;
                if (FileReader.isMidi)
                {
                    WaveInfo = "midi";
                    MidiOutputDevice = OutputDevice.GetByIndex(0);
                    MidiFile = MidiFile.Read(resultPath, new ReadingSettings()
                    {
                        NotEnoughBytesPolicy = NotEnoughBytesPolicy.Ignore,
                        InvalidChunkSizePolicy = InvalidChunkSizePolicy.Ignore,
                        InvalidMetaEventParameterValuePolicy = InvalidMetaEventParameterValuePolicy.SnapToLimits,
                    });
                    MidiPlayback = MidiFile.GetPlayback(MidiOutputDevice);
                    MidiPlayback.Finished += (_, __) => App.MainWindowInstance.Invoke(() => AudioService_PlaybackStopped(null, null));
                    MidiPlayback.Speed = Tempo;
                }
                else
                {
                    // 频谱分析
                    AudioAnalyzer = new SpectrumAnalyzer(FileReader, 8192);
                    // 变速效果
                    FileProvider = new SoundTouchWaveProvider(AudioAnalyzer.ToWaveProvider());
                    // 音量效果
                    VolumeSampleProvider = new VolumeSampleProvider(FileProvider.ToSampleProvider());

                    // 设置参数
                    FileReader.EqEnabled = EqEnabled;
                    FileProvider.Pitch = Pitch;
                    FileProvider.Tempo = Tempo;
                    FileProvider.Rate = Rate;
                    VolumeSampleProvider.Volume = Volume / 100f;

                    // 应用EQ
                    App.MainWindowInstance.Invoke(() => { EqualizerBand = _equalizerBand; });

                    // 如果播放的是 cue 表单，指定播放位置为当前播放的歌曲的开始时间
                    if (musicData.CUETrackData != null)
                    {
                        FileReader.CurrentTime = musicData.CUETrackData.StartDuration;
                        TimingChanged += AudioService_TimingChanged; // 用于每次时间更新时检查此歌曲播放位置是否大于 cue 表单表示的位置，是则切换下一首
                    }

                    App.MainWindowInstance.Invoke(() => PreviewSourceChanged?.Invoke(this));

                    // 初始化音频输出
                    CreateOutputDevice(NowOutDevice, Latency);
                    try
                    {
                        NowOutObj.Init(VolumeSampleProvider);
                    }
                    catch (COMException err)
                    {
                        if (NowOutDevice.DeviceType == OutApi.Wasapi && WasapiOnly)
                            throw new Exception("当前输出设备似乎不支持独占模式。\n" +
                                $"请尝试到音频输出设备的 属性 页面的 高级 选项卡打开 允许应用程序独占控制该设备。\n错误信息：{err.Message}", err);
                        throw new Exception($"无法初始化音频输出，可能是其它应用程序独占了此音频输出设备，请尝试重新播放。\n错误信息：{err.Message}", err);
                    }
                    NowOutObj.PlaybackStopped += AudioService_PlaybackStopped;
                }
                MusicData = musicData;
            });
        }
        catch (OperationCanceledException)
        {
            LogService.Log(nameof(AudioService), $"Canceled：\"{musicData}\"");
        }
        finally
        {
            if (lockTaken)
            {
                _loadingLock.Release();
            }
            if (!token.IsCancellationRequested && lockTaken)
            {
                CacheLoadedChanged?.Invoke(this);
                SourceChanged?.Invoke(this);
                LogService.Log(nameof(AudioService), $"Loaded：\"{musicData}\"");
            }
        }
    }

    private void CreateOutputDevice(OutDevice device, int latency)
    {
        switch (NowOutDevice.DeviceType)
        {
            case OutApi.WaveOut:
                LogService.Log(nameof(AudioService), "Using WaveOut.");
                var outApi = new WaveOut();
                NowOutObj = outApi;
                outApi.DeviceNumber = NowOutDevice.Device is null ? -1 : (int)NowOutDevice.Device;
                outApi.BufferMilliseconds = Latency;
                break;
            case OutApi.DirectSound:
                LogService.Log(nameof(AudioService), "Using DirectSound.");
                if (NowOutDevice.Device is null)
                {
                    NowOutObj = new DirectSoundOut(Latency);
                }
                else
                {
                    NowOutObj = new DirectSoundOut((NowOutDevice.Device as DirectSoundDeviceInfo).Guid, Latency);
                }
                break;
            case OutApi.Wasapi:
                LogService.Log(nameof(AudioService), "Using Wasapi.");
                _wasapiMMDevice = new MMDeviceEnumerator().GetDevice(NowOutDevice.Device as string);
                var builder = new WasapiPlayerBuilder()
                    .WithLatency(Latency)
                    .WithDevice(_wasapiMMDevice);

                builder = WasapiOnly
                    ? builder.WithExclusiveMode()
                    : builder.WithSharedMode();

                NowOutObj = builder.Build();
                break;
            case OutApi.Asio:
                LogService.Log(nameof(AudioService), "Using Asio.");
                var asioOut = new AsioOut((int)NowOutDevice.Device);
                asioOut.AutoStop = false;
                NowOutObj = asioOut;
                TimingChanged += AudioService_TimingChanged;
                break;
        }

    }

    private void AudioService_TimingChanged(AudioService AudioService)
    {
        App.MainWindowInstance.Invoke(() =>
        {
            if (NowOutDevice.DeviceType == OutApi.Asio)
            {
                if ((NowOutObj as AsioOut).HasReachedEnd)
                {
                    AudioService_PlaybackStopped(null, null);
                    TimingChanged -= AudioService_TimingChanged;
                }
            }
        });
    }

    public async Task Reload(TimeSpan? reloadedStreamPosition = null)
    {
        //if (IsInPlaybackStopped) return;
        if (FileReader is null) return;
        if (FileReader.isMidi) return;

        TimeSpan nowPosition = reloadedStreamPosition is null ? FileReader.CurrentTime : (TimeSpan)reloadedStreamPosition;
        var nowPlayState = NowOutObj?.PlaybackState;
        string filePath = FileReader.FileName;

        await Task.Run(DisposeAll);
        var musicData = MusicData;
        MusicData = null;
        await SetSourceAsync(musicData);

        if (FileReader != null)
        {
            FileReader.CurrentTime = nowPosition;
        }
        if (nowPlayState == PlaybackState.Playing) SetPlay();
        else SetPause();
    }

    public async void SetReloadAsync(bool autoPlay = false)
    {
        try
        {
            await Reload();
            if (autoPlay)
            {
                await Task.Delay(10);
                SetPlay();
            }
        }
        catch (Exception err) { LogService.Error(nameof(AudioService), err.ToString()); }
    }

    public void UpdateInfo(string path)
    {
        try
        {
            tfile = new ATL.Track(path);
        }
        catch { }
        if (tfile != null)
        {
            FileType = tfile.AudioFormat.MimeList.First().Split('/')[1];
            try
            {
                WaveInfo = $"{tfile.SampleRate / 1000d}kHz-{tfile.Bitrate}kbps-{FileType}";
            }
            catch
            {
                WaveInfo = "未知";
            }
        }
    }

    public void UpdateEqualizer()
    {
        EqBandChanged?.Invoke(this);
        FileReader?.CreateFilters();
    }

    bool isCUEEndCalled = false;
    private void AudioService_PlaybackStopped(object sender, StoppedEventArgs e)
    {
        App.MainWindowInstance.Invoke(() =>
        {
            if (FileReader != null)
            {
                if (FileReader.IsDisposed)
                    PlayEnd?.Invoke(this);
                else
                {
                    var a = CurrentTime + TimeSpan.FromSeconds(1.5);
                    if (a >= TotalTime)
                    {
                        if (!isCUEEndCalled)
                        {
                            if (MusicData.CUETrackData != null) isCUEEndCalled = true;
                            PlayEnd?.Invoke(this);
                        }
                    }
                }
            }
        });
    }

    bool isPlaying = false;
    public async void SetPlay(bool ifErrorReload = true)
    {
        if (InLoadingAudioFile) return;

        try
        {
            NowOutObj?.Play();
        }
        catch
        {
            if (ifErrorReload)
            {
                await Reload();
                NowOutObj?.Play();
            }
        }
        MidiPlayback?.Start();
        isPlaying = true;
        PlayStateChanged?.Invoke(this);
        ReCallTiming();
    }

    public async void SetPause()
    {
        if (InLoadingAudioFile) return;
        try
        {
            NowOutObj?.Pause();
        }
        catch
        {
            await Reload();
            NowOutObj?.Pause();
        }
        MidiPlayback?.Stop();
        isPlaying = false;
        PlayStateChanged?.Invoke(this);
    }

    public void SetStop()
    {
        if (InLoadingAudioFile) return;
        NowOutObj?.Stop();
        MidiPlayback?.Stop();
        isPlaying = false;
        PlayStateChanged?.Invoke(this);
    }

    public void ReCallTiming()
    {
        //.WriteLine($"ReCall Audio Player Timing Count {TimingChanged?.GetInvocationList()?.Length}.");
        _timer.Start();
        if (PlaybackState != PlaybackState.Playing) _timer.Stop();
        if (TimingChanged is null) _timer.Stop();
        if (!_timer.IsEnabled) return;

        TimingChanged?.Invoke(this);
        if (MusicData is not null && MusicData.CUETrackData != null) AudioService_PlaybackStopped(null, null);
    }

    bool isDisposing = false;
    public void DisposeAll()
    {
        isDisposing = true;
        TimingChanged -= AudioService_TimingChanged;

        try
        {
            _wasapiMMDevice?.Dispose();
        }
        finally
        {
            _wasapiMMDevice = null;
        }

        try
        {
            (NowOutObj as IDisposable)?.Dispose();
        }
        finally
        {
            NowOutObj = null;
        }

        try
        {
            MidiFile = null;
            MidiPlayback?.Dispose();
        }
        finally
        {
            MidiPlayback = null;
        }

        try
        {
            MidiOutputDevice?.Dispose();
        }
        finally
        {
            MidiOutputDevice = null;
        }

        try
        {
            FileReader?.Dispose();
        }
        finally
        {
            FileReader = null;
        }

        AudioAnalyzer = null;
        VolumeSampleProvider = null;

        try
        {
            FileProvider?.Clear();
        }
        finally
        {
            FileProvider = null;
        }

        isDisposing = false;
    }
}
