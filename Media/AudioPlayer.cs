using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using TewiMP.Background;
using TewiMP.DataEditor;

namespace TewiMP.Media
{
    public class AudioPlayer
    {
        public delegate void AudioPlayerDelegate(AudioPlayer audioPlayer);
        public delegate void AudioPlayerDataDelegate(AudioPlayer audioPlayer, object data);
        public delegate void AudioPlayerVolumeMeterDelegate(AudioPlayer audioPlayer, float[] sample);
        public event AudioPlayerDelegate PlayEnd;
        public event AudioPlayerDelegate SourceChanged;
        public event AudioPlayerDelegate PreviewSourceChanged;
        public event AudioPlayerDelegate TimingChanged;
        public event AudioPlayerDelegate PlayStateChanged;
        public event AudioPlayerDataDelegate VolumeChanged;
        public event AudioPlayerDataDelegate CacheLoadingChanged;
        public event AudioPlayerDelegate CacheLoadedChanged;
        public event AudioPlayerDelegate EqEnableChanged;
        public event AudioPlayerDelegate EqBandChanged;
        public event AudioPlayerVolumeMeterDelegate VolumeMeter;

        private DispatcherTimer timer;
        private List<float[]> _equalizerBand = AudioEqualizerBands.NormalBands;
        private bool _eqEnalbed = false;
        private bool _wasapiOnly = false;
        private int _latency = 50;
        private TimeSpan ct = TimeSpan.Zero;
        private OutDevice _nowOutDevice = new(OutApi.None);
        private float _volume = 0f;
        private double _pitch = 1f;
        private double _tempo = 1f;
        private double _rate = 1f;

        public ClientDeviceEvents ClientDeviceEvents { get; private set; } = new();
        public Media.AudioFileReader FileReader { get; set; } = null;
        public AudioEffects.SoundTouchWaveProvider FileProvider { get; set; } = null;
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
                        for (int i = 0; i < value.Count - 1; i++)
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
                if (NowOutObj.GetType() == typeof(WasapiOut))
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
                if (localFileIniting) return TimeSpan.Zero;
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
                if (localFileIniting) return;
                if (FileReader != null)
                {
                    if (FileReader.isMidi)
                    {
                        if (MidiPlayback != null)
                            MidiPlayback.MoveToTime(new MetricTimeSpan(value.Hours, value.Minutes, value.Seconds, value.Milliseconds));
                    }
                    else
                    {
                        ct = value;
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
                if (localFileIniting) return TimeSpan.MaxValue;
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

        public AudioPlayer()
        {
            LogManager.Log("Starting", "初始化 AudioPlayer.");

            timer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(200) };
            timer.Tick += (_, __) => ReCallTiming();
            CompositionTarget.Rendering += CompositionTarget_Rendering;

            App.Instance.CacheManager.AddingCacheMusicData += CacheManager_AddingCacheMusicData;
            App.Instance.CacheManager.CachedMusicData += CacheManager_CachedMusicData;
            App.Instance.CacheManager.CachingStateChangeMusicData += CacheManager_CachingStateChangeMusicData;
            ClientDeviceEvents.notificationClient.OnDefaultDeviceChangedEvent += NotificationClient_OnDefaultDeviceChangedEvent;
            ClientDeviceEvents.notificationClient.OnDeviceStateChangedEvent += NotificationClient_OnDeviceStateChangedEvent;
            ClientDeviceEvents.notificationClient.OnDeviceRemovedEvent += NotificationClient_OnDeviceRemovedEvent;
        }

        private readonly object _analyzerLock = new();
        private void CompositionTarget_Rendering(object sender, object e)
        {
            if (VolumeMeter is null) return;

            float[] spectrum = null;
            lock (_analyzerLock)
            {
                var analyzer = AudioAnalyzer;
                if (analyzer is null) return;

                analyzer.Analyze();
                spectrum = analyzer.Spectrum;
            }

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
        
        int loadCounter = 0;
        bool isInErrorDialog = false;
        private async void NotificationClient_OnDefaultDeviceChangedEvent(DataFlow dataFlow, Role deviceRole, string defaultDeviceId)
        {
            //loadCounter++;
            //await Task.Delay(100); // 不加会导致 集合被修改 的错误，DirectSound 导致的 >:(
            //loadCounter--;
            //if (loadCounter != 0) return;

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
            if (NowOutObj.GetType() != typeof(DirectSoundOut) && NowOutObj.GetType() != typeof(WasapiOut)) return;
            if (!NowOutDevice.IsDefaultDevice) return;

            if (NowOutObj.GetType() == typeof(WasapiOut)) NowOutDevice = OutDevice.GetWasapiDefaultDevice();
            else if (NowOutObj.GetType() == typeof(DirectSoundOut)) NowOutDevice = OutDevice.GetDirectSoundOutDefaultDevice();
            App.MainWindowInstance.Invoke(() =>
            {
                SetReloadAsync(isPlaying);
            });
        }

        private void NotificationClient_OnDeviceStateChangedEvent(string deviceId, DeviceState newState)
        {
            if (newState == DeviceState.Disabled)
                NotificationClient_OnDefaultDeviceChangedEvent(DataFlow.All, Role.Multimedia, deviceId);
        }


        private void NotificationClient_OnDeviceRemovedEvent(string deviceId)
        {
            LogManager.Log("DeviceManager", "Device Removed.");
        }

        bool isInSetSource = false;
        public async Task SetSourceAsync(MusicData musicData)
        {
            isInSetSource = true;
            LogManager.Log("AudioPlayer", $"正在加载：\"{musicData}\"");
            await SetSource(musicData);
            LogManager.Log("AudioPlayer", $"加载完成：\"{musicData}\"");
            isInSetSource = false;
        }

        public MusicData pointMusicData = null;
        int freezeSetSourceCount = 0;
        private async Task SetSource(MusicData musicData)
        {
            pointMusicData = musicData;
            freezeSetSourceCount++;
            await Task.Delay(200);
            freezeSetSourceCount--;
            if (freezeSetSourceCount > 0) return;

            if (isCUEEndCalled)
            {
                isCUEEndCalled = false;
                CurrentTime = TimeSpan.Zero;
            }

            if (MusicData is not null)
                LogManager.Log("AudioPlayer", $"当前播放：{MusicData.Title}, Time: {CurrentTime}/{TotalTime}, IsMIDI: {FileReader.isMidi}, IsCUE: {MusicData.CUETrackData != null}");

            if (musicData == MusicData)
            {
                if (FileReader != null)
                {
                    CurrentTime = TimeSpan.Zero;
                }
                return;
            }

            string resultPath = null;
            if (musicData.From == MusicFrom.localMusic) resultPath = musicData.InLocal;
            else
            {
                try
                {
                    resultPath = await App.Instance.CacheManager.StartCacheMusic(musicData);
                }
                catch (Exception e) { throw; }
            }

            if (await Task.Run(() => !File.Exists(resultPath)))
            {
                throw new FileNotFoundException($"找不到位于 \"{resultPath}\" 的音频文件。");
            }

            if (pointMusicData == musicData)
            {
                var m = MusicData;
                MusicData = musicData;
                Exception exception = null;
                _filePath = resultPath;
                LogManager.Log("AudioPlayer", $"正在加载 \"{resultPath}\".");
                try
                {
                    CacheLoadingChanged?.Invoke(this, null);
                    await SetSource(resultPath, musicData.CUETrackData != null);
                }
                catch (Exception err)
                {
                    MusicData = m;
                    exception = err;
                    LogManager.Log("AudioPlayer", $"{err}", Background.LogLevel.Error);
                }
                finally
                {
                    localFileIniting = false;
                    CacheLoadedChanged?.Invoke(this);
                    PlayStateChanged?.Invoke(this);
                    TimingChanged?.Invoke(this);
                }

                if (exception != null)
                {
                    throw exception;
                }
            }
        }

        List<IWavePlayer> WavePlayers { get; set; } = new();
        int setSourceCallCounter = 0;
        string _filePath = null;
        public string FileType = null;
        public int FileSize = 0;
        public string AudioBitrate = null;
        public bool localFileIniting = false;
        public ATL.Track tfile = null;
        private async Task SetSource(string filePath, bool cueFile = false)
        {
            //if (MusicData != pointMusicData) return;
            MusicData musicData = pointMusicData;
            if (localFileIniting) return;
            if (filePath != _filePath) return;
            if (FileReader != null)
            {
                if (filePath == FileReader.FileName)
                {/*
                    if (MusicData.CUETrackData != null)
                        CurrentTime = MusicData.CUETrackData.StartDuration;
                    else*/
                    CurrentTime = TimeSpan.Zero;
                    PreviewSourceChanged?.Invoke(this);
                    SourceChanged?.Invoke(this);
                    localFileIniting = false;
                    return;
                }
            }

            var devices = await OutDevice.GetOutDevicesAsync();
            if (devices.First().DeviceType == OutApi.None)
            {
                throw new Exception("当前没有音频输出设备。\n请检查音频设置是否正确、输出设备是否插入和音频设备驱动是否正常工作。");
            }
            if (NowOutDevice.DeviceType == OutApi.None)
            {
                NowOutDevice = devices.First();
            }

            localFileIniting = true;
            AudioFileReader fileReader = null;
            SpectrumAnalyzer audioAnalyzer = null;
            AudioEffects.SoundTouchWaveProvider fileProvider = null;
            VolumeSampleProvider volumeSampleProvider = null;

            await Task.Run(() =>
            {
                UpdateInfo();
                FileSize = (int)tfile.AudioDataSize;
                fileReader = new AudioFileReader(filePath, cueFile);

                if (fileReader.isMidi)
                {
                    WaveInfo = "midi";
                    return;
                }
                audioAnalyzer = new SpectrumAnalyzer(fileReader, 8192);
                fileProvider = new AudioEffects.SoundTouchWaveProvider(audioAnalyzer.ToWaveProvider());
                volumeSampleProvider = new VolumeSampleProvider(fileProvider.ToSampleProvider());
                fileReader.EqEnabled = EqEnabled;
                fileProvider.Pitch = Pitch;
                fileProvider.Tempo = Tempo;
                fileProvider.Rate = Rate;
                volumeSampleProvider.Volume = Volume / 100f;
            });
            await Task.Run(DisposeAll);
            FileReader = fileReader;
            AudioAnalyzer = audioAnalyzer;
            FileProvider = fileProvider;
            VolumeSampleProvider = volumeSampleProvider;
            LogManager.Log("AudioPlayer", $"FileReader filePath \"{fileReader.FileName}\".");
            if (EqEnabled && !fileReader.isMidi)
            {
                EqualizerBand = EqualizerBand;
            }

            if (MusicData.CUETrackData != null)
            {
                FileReader.CurrentTime = musicData.CUETrackData.StartDuration;
                TimingChanged += AudioPlayer_TimingChanged;
            }

            PreviewSourceChanged?.Invoke(this);

            if (FileReader.isMidi)
            {
                MidiOutputDevice = OutputDevice.GetByIndex(0);
                MidiFile = MidiFile.Read(filePath, new()
                {
                    NotEnoughBytesPolicy = NotEnoughBytesPolicy.Ignore,
                    InvalidChunkSizePolicy = InvalidChunkSizePolicy.Ignore,
                    InvalidMetaEventParameterValuePolicy = InvalidMetaEventParameterValuePolicy.SnapToLimits,
                });
                MidiPlayback = MidiFile.GetPlayback(MidiOutputDevice);
                MidiPlayback.Finished += (_, __) => App.MainWindowInstance.Invoke(() => AudioPlayer_PlaybackStopped(null, null));
                MidiPlayback.Speed = Tempo;
            }
            else
            {/*
                bool notDefaultLatency = false;
                if (Latency != (int)SettingDefault[SettingParams.AudioLatency.ToString()])
                {
                    notDefaultLatency = true;
                }*/

                switch (NowOutDevice.DeviceType)
                {
                    case OutApi.WaveOut:
                        LogManager.Log("AudioPlayer", "Using WaveOut.");
                        await Task.Run(() => NowOutObj = new WaveOutEvent());
                        (NowOutObj as WaveOutEvent).DeviceNumber = NowOutDevice.Device is null ? -1 : (int)NowOutDevice.Device;
                        (NowOutObj as WaveOutEvent).NumberOfBuffers = Latency;
                        NowOutObj.Init(VolumeSampleProvider);
                        NowOutObj.PlaybackStopped += AudioPlayer_PlaybackStopped;
                        break;
                    case OutApi.DirectSound:
                        LogManager.Log("AudioPlayer", "Using DirectSound.");
                        if (NowOutDevice.Device is null)
                        {
                            await Task.Run(() => NowOutObj = new DirectSoundOut(Latency));
                        }
                        else
                        {
                            await Task.Run(() => NowOutObj = new DirectSoundOut((NowOutDevice.Device as DirectSoundDeviceInfo).Guid, Latency));
                        }
                        NowOutObj.Init(VolumeSampleProvider);
                        NowOutObj.PlaybackStopped += AudioPlayer_PlaybackStopped;
                        break;
                    case OutApi.Wasapi:
                        LogManager.Log("AudioPlayer", "Using Wasapi.");
                        MMDevice device = null;
                        await Task.Run(() =>
                        {
                            device = new MMDeviceEnumerator().GetDevice(NowOutDevice.Device as string);
                            NowOutObj = new WasapiOut(
                                device,
                                WasapiOnly ? AudioClientShareMode.Exclusive : AudioClientShareMode.Shared, false,
                                Latency);
                        });
                        try
                        {
                            NowOutObj.Init(VolumeSampleProvider);
                        }
                        catch (COMException err)
                        {
                            if (WasapiOnly)
                                throw new Exception("当前输出设备似乎不支持独占模式。\n" +
                                    $"请尝试到音频输出设备的 属性 页面的 高级 选项卡打开 允许应用程序独占控制该设备。\n错误信息：{err.Message}");
                            throw new Exception($"无法初始化音频输出，可能是其它应用程序独占了此音频输出设备，请尝试重新播放。\n错误信息：{err.Message}");
                        }
                        NowOutObj.PlaybackStopped += AudioPlayer_PlaybackStopped;
                        device.Dispose();
                        break;
                    case OutApi.Asio:
                        LogManager.Log("AudioPlayer", "Using Asio.");
                        var asioOut = new AsioOut((int)NowOutDevice.Device);
                        asioOut.AutoStop = false;
                        NowOutObj = asioOut;
                        NowOutObj.Init(VolumeSampleProvider);
                        TimingChanged += AudioPlayer_TimingChanged;
                        NowOutObj.PlaybackStopped += AudioPlayer_PlaybackStopped;
                        break;
                }

                LogManager.Log("AudioPlayer", $"Inited FileReader filePath \"{fileReader.FileName}\".");
                LogManager.Log("AudioPlayer", $"Inited MusicData \"{MusicData}\".");
            }

            SourceChanged?.Invoke(this);
            localFileIniting = false;
        }

        private void AudioPlayer_TimingChanged(AudioPlayer audioPlayer)
        {
            if (NowOutDevice.DeviceType == OutApi.Asio)
            {
                if ((NowOutObj as AsioOut).HasReachedEnd)
                {
                    AudioPlayer_PlaybackStopped(null, null);
                    TimingChanged -= AudioPlayer_TimingChanged;
                }
            }
        }

        public async Task Reload(TimeSpan? reloadedStreamPosition = null)
        {
            //if (IsInPlaybackStopped) return;
            if (isInSetSource) return;
            if (FileReader is null) return;
            if (FileReader.isMidi) return;

            TimeSpan nowPosition = reloadedStreamPosition is null ? FileReader.CurrentTime : (TimeSpan)reloadedStreamPosition;
            var nowPlayState = NowOutObj?.PlaybackState;
            string filePath = FileReader.FileName;

            await Task.Run(DisposeAll);
            await SetSource(filePath);

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
            catch (Exception err) { LogManager.Log("AudioPlayer", err.ToString(), Background.LogLevel.Error); }
        }

        public void UpdateInfo()
        {
            try
            {
                tfile = new ATL.Track(_filePath);
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
        private void AudioPlayer_PlaybackStopped(object sender, StoppedEventArgs e)
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
            if (localFileIniting) return;

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
            if (localFileIniting) return;
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
            if (localFileIniting) return;
            NowOutObj?.Stop();
            MidiPlayback?.Stop();
            isPlaying = false;
            PlayStateChanged?.Invoke(this);
        }

        public void ReCallTiming()
        {
            //.WriteLine($"ReCall Audio Player Timing Count {TimingChanged?.GetInvocationList()?.Length}.");
            timer.Start();
            if (PlaybackState != PlaybackState.Playing) timer.Stop();
            if (TimingChanged is null) timer.Stop();
            if (!timer.IsEnabled) return;

            TimingChanged?.Invoke(this);
            if (MusicData.CUETrackData != null) AudioPlayer_PlaybackStopped(null, null);
        }

        bool isDisposing = false;
        public void DisposeAll()
        {
            isDisposing = true;
            TimingChanged -= AudioPlayer_TimingChanged;

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
}
