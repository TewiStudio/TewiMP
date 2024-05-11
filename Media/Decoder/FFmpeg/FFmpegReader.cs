/*using Microsoft.VisualBasic.Devices;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TewiMP.Media.Decoder.FFmpeg
{
    public class FFmpegReader : WaveStream
    {
        public string FileName { get; set; }
        public FFmpegDecoder Decoder { get; set; }

        WaveFormat _waveFormat;
        public override WaveFormat WaveFormat => _waveFormat;

        public override long Length => 1000000;
        public override long Position
        {
            get
            {
                return 0;
            }
            set
            {

            }
        }

        public override TimeSpan CurrentTime
        {
            get
            {
                return TimeSpan.Zero;
            }
            set
            {
                base.CurrentTime = value;
            }
        }

        public override TimeSpan TotalTime
        {
            get
            {
                return TimeSpan.MaxValue;
            }
        }

        BufferedWaveProvider _provider;
        public FFmpegReader(string filename)
        {
            FileName = filename;
            Decoder = new FFmpegDecoder();
            Decoder.InitDecodecAudio(filename);
            //_waveFormat = new(Decoder.SampleRate, Decoder.Channels);
            _provider = new(WaveFormat);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return 0;
        }

        public void Dispose()
        {

        }
    }
}
*/