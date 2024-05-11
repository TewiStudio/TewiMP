using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using FFmpeg.AutoGen;
using Melanchall.DryWetMidi.Common;
using Microsoft.VisualBasic.Devices;
using NVorbis.Ogg;
using TagLib;
using Vanara.PInvoke;

namespace TewiMP.Media.Decoder.FFmpeg
{
    public static class FFmpegBinariesHelper
    {
        public static void InitFFmpeg()
        {
            FFmpegBinariesHelper.RegisterFFmpegBinaries();
        }

        internal static void RegisterFFmpegBinaries()
        {
            var current = Environment.CurrentDirectory;
            var probe = Path.Combine("Media", "Decoder", "FFmpeg", "bin");
            while (current != null)
            {
                var ffmpegBinaryPath = Path.Combine(current, probe);
                if (Directory.Exists(ffmpegBinaryPath))
                {
                    Debug.WriteLine($"FFmpeg binaries found in: {ffmpegBinaryPath}");
                    ffmpeg.RootPath = ffmpegBinaryPath;
                    ffmpeg.avdevice_register_all();
                    return;
                }

                current = Directory.GetParent(current)?.FullName;
            }
        }
    }
}
