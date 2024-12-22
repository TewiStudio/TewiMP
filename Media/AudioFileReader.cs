using System.IO;
using System.Collections.Generic;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
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

/*
            foreach (var file in Directory.GetFiles(DataEditor.DataFolderBase.RemovedIDv3CacheFolder))
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }

            string newPath = Path.Combine(DataEditor.DataFolderBase.RemovedIDv3CacheFolder, Path.GetFileName(fileName));
            File.Copy(fileName, newPath);
            TagLib.File tagFile = TagLib.File.Create(newPath);
            tagFile.RemoveTags(TagLib.TagTypes.AllTags);
            tagFile.Save();
            tagFile.Dispose();
            fileName = newPath;
*/
            FileStream f = File.OpenRead(fileName);
            if (f.Length <= 10)
            {
                throw new FileLoadException("无法读取此音频文件。");
            }

            try
            {
                addr = FileHelper.FileTypeGet(f);
            }
            catch
            {
                addr = "-1";
            }

            bool useMFR = false;
            //addr = "-1";
            FileAddr = addr;
            switch (addr)
            {
                case "10276":
                    if (!cueFile)
                    {
                        readerStream = new FlakeNAudioAdapter.FlakeFileReader(fileName);
                        System.Diagnostics.Debug.WriteLine("[AudioFileReader]: 正在使用 FlakeFlac 解码器");
                    }
                    else
                    {
                        readerStream = new NAudio.Flac.FlacReader(fileName);
                        System.Diagnostics.Debug.WriteLine("[AudioFileReader]: 正在使用 NAudio.Flac 解码器（CUE文件兼容性）");
                    }
                    DecodeName = $"{App.AppName} built-in FLAC Decoder";
                    break;
                case "79103":
                    readerStream = new NAudio.Vorbis.VorbisWaveReader(fileName);
                    DecodeName = $"{App.AppName} built-in Vorbis Decoder";
                    System.Diagnostics.Debug.WriteLine("[AudioFileReader]: 正在使用 Vorbis 解码器");
                    break;
                case "7368":
                    readerStream = new Mp3FileReader(fileName);
                    DecodeName = $"NAudio MP3 Decoder";
                    System.Diagnostics.Debug.WriteLine("[AudioFileReader]: 正在使用 MP3 解码器");
                    break;
                case "8273":
                    readerStream = new WaveFileReader(fileName);
                    DecodeName = $"NAudio Wave Decoder";
                    if (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm && readerStream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                    {
                        readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
                        readerStream = new BlockAlignReductionStream(readerStream);
                        System.Diagnostics.Debug.WriteLine("[AudioFileReader]: 正在使用 Wave 解码器");
                    }
                    break;
                case "7079":
                    readerStream = new AiffFileReader(fileName);
                    DecodeName = $"NAudio Aiff Decoder";
                    System.Diagnostics.Debug.WriteLine("[AudioFileReader]: 正在使用 Aiff 解码器");
                    break;
                case "7784":
                    isMidi = true;
                    DecodeName = null;
                    break;
                default: useMFR = true; break;
            }

            if (useMFR)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioFileReader]: 正在使用 Microsoft MediaFoundationReader 解码器，文件标识符为：{addr}");
                DecodeName = $"Microsoft MediaFoundation Decoder";
                readerStream = new MediaFoundationReader(fileName);
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

            foreach (var eqData in AudioFilterStatic.EQDatas)
            {
                if (!eqData.IsEnable) continue;
                var filter = new BiQuadFilter[WaveFormat.Channels];
                switch (eqData.Channel)
                {
                    case 0:
                        filter[0] = BiQuadFilterPeak(eqData.CentreFrequency, eqData.Q / 10f, eqData.Decibels / 10f);
                        break;
                    case 1:
                        for (int n = 0; n < WaveFormat.Channels; n++)
                        {
                            filter[n] = BiQuadFilterPeak(eqData.CentreFrequency, eqData.Q / 10f, eqData.Decibels / 10f);
                        }
                        break;
                    case 2:
                        if (filter.Length >= 2)
                        {
                            filter[1] = BiQuadFilterPeak(eqData.CentreFrequency, eqData.Q / 10f, eqData.Decibels / 10f);
                        }
                        break;
                }
                _filters.Add(filter);
            }
        }

        public BiQuadFilter BiQuadFilterPeak(float centreFrequency, float q, float dbGain)
        {
            BiQuadFilter filter = BiQuadFilter.PeakingEQ(WaveFormat.SampleRate, centreFrequency, q, dbGain);
            //filter.SetLowPassFilter(WaveFormat.SampleRate, 16000, .03f);
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
                readerStream.Dispose();
                readerStream = null;
            }

            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
