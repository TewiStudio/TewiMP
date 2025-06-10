using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TewiMP.Helpers;

namespace TewiMP.Media
{
    public class AudioFileReader : WaveStream, ISampleProvider
    {
        private List<BiQuadFilter[]> _filters = new();
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
                        App.logManager.Log("AudioFileReader", "正在使用 FlakeFlac 解码器");
                    }
                    else
                    {
                        readerStream = new NAudio.Flac.FlacReader(fileName);
                        App.logManager.Log("AudioFileReader", "正在使用 NAudio.Flac 解码器（CUE文件兼容性）");
                    }
                    DecodeName = $"{App.AppName} built-in FLAC Decoder";
                    break;
                case "79103":
                    readerStream = new NAudio.Vorbis.VorbisWaveReader(fileName);
                    DecodeName = $"{App.AppName} built-in Vorbis Decoder";
                    App.logManager.Log("AudioFileReader", "正在使用 Vorbis 解码器");
                    break;
                case "7368":
                    readerStream = new Mp3FileReader(fileName);
                    DecodeName = $"NAudio MP3 Decoder";
                    App.logManager.Log("AudioFileReader", "正在使用 MP3 解码器");
                    break;
                case "8273":
                    readerStream = new WaveFileReader(fileName);
                    DecodeName = $"NAudio Wave Decoder";
                    if (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm && readerStream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                    {
                        readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
                        readerStream = new BlockAlignReductionStream(readerStream);
                        App.logManager.Log("AudioFileReader", "正在使用 Wave 解码器");
                    }
                    break;
                case "7079":
                    readerStream = new AiffFileReader(fileName);
                    DecodeName = $"NAudio Aiff Decoder";
                    App.logManager.Log("AudioFileReader", "正在使用 Aiff 解码器");
                    break;
                case "7784":
                    isMidi = true;
                    DecodeName = null;
                    break;
                default: useMFR = true; break;
            }*/


            if (addr == "7784") // MIDI文件处理
            {
                isMidi = true;
                return;
            }

            var tFile = App.audioPlayer.tfile;
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
                FileName = Path.Combine(Environment.CurrentDirectory, "ffmpeg.exe"),
                Arguments = $"-i \"{fileName}\" -f {codec} -acodec pcm_{codec} -ac 2 -ar {tFile.SampleRate} -",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _ffmpegProcess = Process.Start(psi);
            if (_ffmpegProcess is not null)
            {
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
                App.logManager.Log("AudioFileReader", $"正在使用 Microsoft MediaFoundationReader 解码器，文件标识符为：{addr}");
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

            if (AudioFilterStatic.GraphicEqEnable)
            {
                foreach (float[] floats in AudioEqualizerBands.NormalBands)
                {
                    var filter = new BiQuadFilter[WaveFormat.Channels];
                    for (int n = 0; n < WaveFormat.Channels; n++)
                    {
                        filter[n] = BiQuadFilterPeak(floats[0], floats[1], floats[2]);
                    }
                    _filters.Add(filter);
                }
            }

            if (AudioFilterStatic.ParametricEqEnable)
            {
                foreach (var eqData in AudioFilterStatic.ParametricEqDatas)
                {
                    if (!eqData.IsEnable) continue;
                    var filter = new BiQuadFilter[WaveFormat.Channels];
                    switch (eqData.Channel)
                    {
                        case 0:
                            filter[0] = BiQuadFilterPeak(eqData.CentreFrequency, eqData.Q, eqData.Gain);
                            break;
                        case 1:
                            for (int n = 0; n < WaveFormat.Channels; n++)
                            {
                                filter[n] = BiQuadFilterPeak(eqData.CentreFrequency, eqData.Q, eqData.Gain);
                            }
                            break;
                        case 2:
                            if (filter.Length >= 2)
                            {
                                filter[1] = BiQuadFilterPeak(eqData.CentreFrequency, eqData.Q, eqData.Gain);
                            }
                            break;
                    }
                    _filters.Add(filter);
                }
            }

            if (AudioFilterStatic.PassFilterEqEnable)
            {
                foreach (var passFilterData in AudioFilterStatic.PassFilterDatas)
                {
                    if (!passFilterData.IsEnable) continue;
                    var filter = new BiQuadFilter[WaveFormat.Channels];
                    switch (passFilterData.Channel)
                    {
                        case 0:
                            filter[0] = GetPassFilter(passFilterData);
                            break;
                        case 1:
                            for (int n = 0; n < WaveFormat.Channels; n++)
                            {
                                filter[n] = GetPassFilter(passFilterData);
                            }
                            break;
                        case 2:
                            if (filter.Length >= 2)
                            {
                                filter[1] = GetPassFilter(passFilterData);
                            }
                            break;
                    }
                    _filters.Add(filter);
                }
            }
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
            lock (lockObject)
            {
                int samplesRead = sampleChannel.Read(buffer, offset, count);
                if (!EqEnabled) return samplesRead;

                try
                {
                    for (var a = 0; a < _filters.Count; a++)
                    {
                        for (int i = 0; i < samplesRead; i++)
                        {
                            var ch = i % WaveFormat.Channels;
                            var filterArray = _filters[a];
                            if (ch <= filterArray.Length - 1)
                            {
                                var filter = filterArray[ch];
                                if (filter is not null)
                                {
                                    buffer[offset + i] = filter.Transform(buffer[offset + i]);
                                }
                            }
                        }
                    }
                }
                catch { }
                return samplesRead;
            }
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
}
