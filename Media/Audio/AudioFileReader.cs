namespace TewiMP.Media.Audio;

using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using TewiMP.Helpers;
using TewiMP.Services;
using TewiMP.Services.Storage;
using TewiMP.Core.Models.Audio;
using TewiMP.Media.Audio.AudioEffects;

public class AudioFileReader : WaveStream, ISampleProvider
{
    public static string FFmpegPath = DataFolderBase.FFmpegPath;

    private List<BiQuadFilter[]> _filters = [];
    private List<BiQuadFilter[]> _passFilters = [];
    private WaveStream readerStream;
    private readonly SampleChannel sampleChannel;
    private readonly int destBytesPerSample;
    private readonly int sourceBytesPerSample;
    private readonly long length;
    private readonly object lockObject;

    public bool EqEnabled { get; set; }
    public string FileName { get; }
    public string FileAddr { get; private set; }

    public override WaveFormat WaveFormat => sampleChannel?.WaveFormat;

    public override long Length => length;

    public override long Position
    {
        get
        {
            return SourceToDest(readerStream.Position);
        }
        set
        {
            lock (lockObject)
            {
                readerStream.Position = DestToSource(value);
            }
        }
    }

    public float Volume
    {
        get
        {
            return sampleChannel.Volume;
        }
        set
        {
            sampleChannel.Volume = value;
        }
    }

    private Process _ffmpegProcess;
    private MemoryStream _ffmpegReadMemory;
    public string addr = null;
    public bool isMidi = false;
    public string DecodeName = null;

    public AudioFileReader(string fileName, bool cueFile)
    {
        lockObject = new object();
        FileName = fileName;
        CreateReaderStream(fileName, cueFile);
        if (isMidi) return;
        sourceBytesPerSample = readerStream.WaveFormat.BitsPerSample / 8 * readerStream.WaveFormat.Channels;
        sampleChannel = new SampleChannel(readerStream, forceStereo: false);
        destBytesPerSample = 4 * sampleChannel.WaveFormat.Channels;
        length = SourceToDest(readerStream.Length);
        CreateFilters();
    }

    private void CreateReaderStream(string fileName, bool cueFile = false)
    {
        if (!File.Exists(fileName))
        {
            throw new FileNotFoundException();
        }
        FileStream f = File.OpenRead(fileName);
        if (f.Length <= 10)
        {
            throw new FileLoadException("无法读取此音频文件。");
        }

        DecodeName = null;
        addr = FileHelper.FileTypeGet(f);
        FileAddr = addr;
        /*switch (addr)
        {
            case "10276":
                if (!cueFile)
                {
                    readerStream = new FlakeNAudioAdapter.FlakeFileReader(fileName);
                    LogManager.Log("AudioFileReader", "正在使用 FlakeFlac 解码器");
                }
                else
                {
                    readerStream = new NAudio.Flac.FlacReader(fileName);
                    LogManager.Log("AudioFileReader", "正在使用 NAudio.Flac 解码器（CUE文件兼容性）");
                }
                DecodeName = $"{App.Instance.AppName} built-in FLAC Decoder";
                break;
            case "79103":
                readerStream = new NAudio.Vorbis.VorbisWaveReader(fileName);
                DecodeName = $"{App.Instance.AppName} built-in Vorbis Decoder";
                LogManager.Log("AudioFileReader", "正在使用 Vorbis 解码器");
                break;
            case "7368":
                readerStream = new Mp3FileReader(fileName);
                DecodeName = $"NAudio MP3 Decoder";
                LogManager.Log("AudioFileReader", "正在使用 MP3 解码器");
                break;
            case "8273":
                readerStream = new WaveFileReader(fileName);
                DecodeName = $"NAudio Wave Decoder";
                if (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm && readerStream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                {
                    readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
                    readerStream = new BlockAlignReductionStream(readerStream);
                    LogManager.Log("AudioFileReader", "正在使用 Wave 解码器");
                }
                break;
            case "7079":
                readerStream = new AiffFileReader(fileName);
                DecodeName = $"NAudio Aiff Decoder";
                LogManager.Log("AudioFileReader", "正在使用 Aiff 解码器");
                break;
            case "7784":
                isMidi = true;
                DecodeName = null;
                break;
            default: useMFR = true; break;
        }*/


        if (addr == "7784" || Path.GetExtension(fileName) == ".mid") // MIDI文件处理
        {
            isMidi = true;
            return;
        }

        LogService.Log("AudioFileReader", $"ffmpeg.exe: {FFmpegPath}");
        bool useMF = false;
        if (!File.Exists(FFmpegPath))
        {
            LogService.Error("AudioFileReader", "找不到 ffmpeg.exe，请检查 ffmpeg.exe 是否被删除，或者其路径设置是否正确。");
            useMF = true;
        }
        else
        {
            var tFile = App.Instance.AudioPlayer.tfile;
            string codec = tFile.BitDepth switch
            {
                8 => "u8",
                16 => "s16le",
                24 => "s24le",
                32 => "s32le",
                _ => "s16le"
            };
            var psi = new ProcessStartInfo
            {
                FileName = FFmpegPath,
                Arguments = $"-i \"{fileName}\" -f {codec} -acodec pcm_{codec} -ac 2 -ar {tFile.SampleRate} -",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _ffmpegProcess = Process.Start(psi);
            if (_ffmpegProcess is not null)
            {
                LogService.Log("AudioFileReader", $"正在使用 FFmpeg 解码器，文件标识符为：{addr}");
                _ffmpegProcess.StandardOutput.BaseStream.CopyTo(_ffmpegReadMemory = new());
                _ffmpegReadMemory.Position = 0;
                _ffmpegProcess.Kill();
                _ffmpegProcess.Dispose();
                if (tFile.BitDepth == -1) // 当一些音频数据无位深时
                {
                    readerStream = new RawSourceWaveStream(_ffmpegReadMemory, new WaveFormat((int)tFile.SampleRate, tFile.ChannelsArrangement.NbChannels));
                }
                else
                {
                    readerStream = new RawSourceWaveStream(_ffmpegReadMemory, new WaveFormat((int)tFile.SampleRate, tFile.BitDepth, tFile.ChannelsArrangement.NbChannels));
                }
                DecodeName = $"TewiMP built-in FFmpeg Decoder";
            }
            else
            {
                LogService.Error("AudioFileReader", "ffmpeg 运行失败。");
                useMF = true;
            }
        }

        if (useMF)
        {
            LogService.Log("AudioFileReader", $"正在使用 Microsoft MediaFoundationReader 解码器，文件标识符为：{addr}");
            readerStream = new MediaFoundationReader(fileName);
            DecodeName = $"Microsoft MediaFoundation Decoder";
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        WaveBuffer waveBuffer = new WaveBuffer(buffer);
        int count2 = count / 4;
        return Read(waveBuffer.FloatBuffer, offset / 4, count2) * 4;
    }

    // 均衡器
    public void CreateFilters()
    {
        _filters.Clear();
        _passFilters.Clear();

        int channels = WaveFormat.Channels;

        // 创建 PassFilter 滤波器组
        if (AudioFilterStatic.PassFilterEqEnable)
        {
            foreach (var passData in AudioFilterStatic.PassFilterDatas)
            {
                if (!passData.IsEnable) continue;

                int stagesCount = Math.Max(1, passData.SlopeDbPerOct / 12);
                var filterGroup = new BiQuadFilter[channels * stagesCount];

                // 根据声道类型设置
                for (int ch = 0; ch < channels; ch++)
                {
                    if (!ShouldApplyToChannel(passData.Channel, ch)) continue;

                    for (int s = 0; s < stagesCount; s++)
                        filterGroup[ch * stagesCount + s] = GetPassFilter(passData);
                }

                _passFilters.Add(filterGroup);
            }
        }

        // 创建 Parametric EQ 滤波器组
        if (AudioFilterStatic.ParametricEqEnable)
        {
            foreach (var eq in AudioFilterStatic.ParametricEqDatas)
            {
                if (!eq.IsEnable) continue;

                var filterGroup = new BiQuadFilter[channels];
                for (int ch = 0; ch < channels; ch++)
                {
                    if (!ShouldApplyToChannel(eq.Channel, ch)) continue;
                    filterGroup[ch] = BiQuadFilterPeak(eq.CentreFrequency, eq.Q, eq.Gain);
                }

                _filters.Add(filterGroup);
            }
        }

        // 创建 Graphic EQ 滤波器组
        if (AudioFilterStatic.GraphicEqEnable)
        {
            foreach (float[] band in AudioEqualizerBands.NormalBands)
            {
                var filterGroup = new BiQuadFilter[channels];
                for (int ch = 0; ch < channels; ch++)
                    filterGroup[ch] = BiQuadFilterPeak(band[0], band[1], band[2]);

                _filters.Add(filterGroup);
            }
        }
    }


    /// <summary>
    /// 判断滤波器是否应用于当前声道
    /// </summary>
    private static bool ShouldApplyToChannel(int filterChannel, int currentChannel)
    {
        return filterChannel switch
        {
            0 => currentChannel == 0,  // 左声道
            1 => true,                 // 双声道
            2 => currentChannel == 1,  // 右声道
            _ => true
        };
    }

    public BiQuadFilter BiQuadFilterPeak(float centreFrequency, float q, float dbGain)
    {
        BiQuadFilter filter = BiQuadFilter.PeakingEQ(WaveFormat.SampleRate, centreFrequency, q, dbGain);
        //filter.SetLowPassFilter(WaveFormat.SampleRate, 16000, .03f);
        return filter;
    }

    public BiQuadFilter GetPassFilter(PassFilterData filterData)
    {
        BiQuadFilter filter = null;
        switch (filterData.PassFilterType)
        {
            case PassFilterType.LowPass:
                filter = BiQuadFilter.LowPassFilter(WaveFormat.SampleRate, filterData.CentreFrequency, filterData.Q);
                break;
            case PassFilterType.HighPass:
                filter = BiQuadFilter.HighPassFilter(WaveFormat.SampleRate, filterData.CentreFrequency, filterData.Q);
                break;
            case PassFilterType.AllPass:
                filter = BiQuadFilter.AllPassFilter(WaveFormat.SampleRate, filterData.CentreFrequency, filterData.Q);
                break;
            case PassFilterType.BandPassPeak:
                filter = BiQuadFilter.BandPassFilterConstantPeakGain(WaveFormat.SampleRate, filterData.CentreFrequency, filterData.Q);
                break;
            case PassFilterType.BandPassSkirt:
                filter = BiQuadFilter.BandPassFilterConstantSkirtGain(WaveFormat.SampleRate, filterData.CentreFrequency, filterData.Q);
                break;
            case PassFilterType.LowShelf:
                filter = BiQuadFilter.LowShelf(WaveFormat.SampleRate, filterData.CentreFrequency, filterData.Q, filterData.Gain);
                break;
            case PassFilterType.HighShelf:
                filter = BiQuadFilter.HighShelf(WaveFormat.SampleRate, filterData.CentreFrequency, filterData.Q, filterData.Gain);
                break;
            case PassFilterType.Notch:
                filter = BiQuadFilter.NotchFilter(WaveFormat.SampleRate, filterData.CentreFrequency, filterData.Q);
                break;
        }
        return filter;
    }

    // 在读取音频数据时加入均衡器数据
    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead;

        // 尽可能缩小锁范围（避免音频阻塞）
        lock (lockObject)
        {
            samplesRead = sampleChannel.Read(buffer, offset, count);
            if (samplesRead <= 0 || !EqEnabled)
                return samplesRead;
        }

        // 创建安全副本（防止UI线程修改滤波器列表）
        var localPassFilters = _passFilters?.ToArray();
        var localFilters = _filters?.ToArray();

        int channels = WaveFormat.Channels;
        if (channels <= 0)
            return samplesRead;

        int samplesPerChannel = samplesRead / channels;

        // Pass Filters
        if (localPassFilters != null)
        {
            foreach (var filterArray in localPassFilters)
            {
                if (filterArray == null || filterArray.Length == 0)
                    continue;

                int stagesCount = filterArray.Length / channels;
                if (stagesCount <= 0) continue;

                // 按通道处理
                for (int ch = 0; ch < channels; ch++)
                {
                    for (int n = 0; n < samplesPerChannel; n++)
                    {
                        int idx = offset + n * channels + ch;
                        float sample = buffer[idx];

                        for (int s = 0; s < stagesCount; s++)
                        {
                            int filterIdx = ch * stagesCount + s;
                            if (filterIdx < filterArray.Length)
                            {
                                var filter = filterArray[filterIdx];
                                if (filter != null)
                                    sample = filter.Transform(sample);
                            }
                        }

                        buffer[idx] = sample;
                    }
                }
            }
        }

        // EQ Filters
        if (localFilters != null)
        {
            foreach (var filterArray in localFilters)
            {
                if (filterArray == null || filterArray.Length == 0)
                    continue;

                for (int ch = 0; ch < channels; ch++)
                {
                    var filter = (ch < filterArray.Length) ? filterArray[ch] : null;
                    if (filter == null) continue;

                    for (int n = 0; n < samplesPerChannel; n++)
                    {
                        int idx = offset + n * channels + ch;
                        buffer[idx] = filter.Transform(buffer[idx]);
                    }
                }
            }
        }

        return samplesRead;
    }

    private long SourceToDest(long sourceBytes)
    {
        return destBytesPerSample * (sourceBytes / sourceBytesPerSample);
    }

    private long DestToSource(long destBytes)
    {
        return sourceBytesPerSample * (destBytes / destBytesPerSample);
    }

    public bool IsDisposed { get; set; } = false;
    protected override void Dispose(bool disposing)
    {
        if (disposing && readerStream != null)
        {
            _ffmpegReadMemory?.Dispose();
            _ffmpegReadMemory = null;

            readerStream.Dispose();
            readerStream = null;

        }

        IsDisposed = true;
        base.Dispose(disposing);
    }
}
