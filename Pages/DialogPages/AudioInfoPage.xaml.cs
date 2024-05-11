using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TewiMP.Media;
using TewiMP.Helpers;
using CueSharp;

namespace TewiMP.Pages.DialogPages
{
    public partial class AudioInfoPage : Page
    {
        public AudioInfoPage()
        {
            InitializeComponent();
            Init();
        }

        public void Init()
        {
            SetFileSourceInfoText();
            SetCUEInfoText();
            SetAudioInfoText();
            SetOutInfoText();
        }

        private async void SetFileSourceInfoText()
        {
            string filePath = "";
            string createTime = "";
            await Task.Run(() =>
            {
                FileInfo fileInfo = new(App.audioPlayer.FileReader.FileName);
                createTime = fileInfo.CreationTime.ToString();
                filePath = fileInfo.DirectoryName;
            });

            ((TextBlock)FileInfoSp.Children[2]).Text = App.audioPlayer.FileReader.FileName;
            ((TextBlock)FileInfoSp.Children[4]).Text = filePath;
            ((TextBlock)FileInfoSp.Children[6]).Text = createTime;
            ((TextBlock)FileInfoSp.Children[8]).Text = CodeHelper.GetAutoSizeString(App.audioPlayer.FileSize, 2);
        }

        private async void SetAudioInfoText()
        {
            if (App.audioPlayer.FileReader != null)
            {
                if (App.audioPlayer.FileReader.isMidi)
                {
                    AudioInfoGrid.Visibility = Visibility.Collapsed;
                    return;
                }
            }

            ATL.Track tfile = null;
            if (App.audioPlayer.tfile == null)
            {
                await Task.Run(() =>
                {
                    tfile = new ATL.Track(App.audioPlayer.FileReader.FileName);
                });
            }
            else
            {
                tfile = App.audioPlayer.tfile;
            }

            if (tfile != null)
            {
                string additionalFields = "";
                for (int i = 0; i < tfile.AdditionalFields.Count; i++)
                {
                    var element = tfile.AdditionalFields.ElementAt(i);
                    if (element.Key == "VORBIS-VENDOR") continue;
                    additionalFields += $"● {element.Key}: {(string.IsNullOrEmpty(element.Value) ? "无内容" : element.Value)}{(i == tfile.AdditionalFields.Count - 1 ? "" : "\n")}";
                }

                ((TextBlock)AudioInfoSp.Children[1]).Text = $"{App.audioPlayer.FileType}" +
                    $"{(ConvertCodecFamilyIntToString(tfile.CodecFamily) == null ? "" : $"  {ConvertCodecFamilyIntToString(tfile.CodecFamily)}")}" +
                    $"  {tfile.SampleRate} Hz  {tfile.Bitrate} kbps" +
                    $"  {tfile.ChannelsArrangement.NbChannels} 声道  {App.audioPlayer.FileReader.TotalTime.ToString("hh\\:mm\\:ss\\.ff")}({tfile.Duration}s)";

                if (tfile.BitDepth == -1)
                {
                    AudioInfoSp.Children[2].Visibility = Visibility.Collapsed;
                    AudioInfoSp.Children[3].Visibility = Visibility.Collapsed;
                }
                else
                    ((TextBlock)AudioInfoSp.Children[3]).Text = $"{tfile.BitDepth} 位";

                if (tfile.AdditionalFields.ContainsKey("VORBIS-VENDOR"))
                {
                    ((TextBlock)AudioInfoSp.Children[5]).Text = tfile.AdditionalFields["VORBIS-VENDOR"];
                }
                else
                {
                    AudioInfoSp.Children[4].Visibility = Visibility.Collapsed;
                    AudioInfoSp.Children[5].Visibility = Visibility.Collapsed;
                }

                ((TextBlock)AudioInfoSp.Children[7]).Text = string.IsNullOrEmpty(additionalFields) ? "无内容" : additionalFields;

                //((TextBlock)AudioInfoSp.Children[8]).Text = "";
            }
            else
            {

            }
        }

        private async void SetOutInfoText()
        {
            if (App.audioPlayer.FileReader.isMidi)
            {
                ((TextBlock)OutInfoSp.Children[2]).Text = $"Midi -> {App.audioPlayer.MidiOutputDevice.Name}";
                ((TextBlock)OutInfoSp.Children[3]).Visibility = Visibility.Collapsed;
                ((TextBlock)OutInfoSp.Children[4]).Visibility = Visibility.Collapsed;
                ((TextBlock)OutInfoSp.Children[5]).Visibility = Visibility.Collapsed;
                ((TextBlock)OutInfoSp.Children[6]).Visibility = Visibility.Collapsed;
                ((TextBlock)OutInfoSp.Children[7]).Visibility = Visibility.Collapsed;
                ((TextBlock)OutInfoSp.Children[8]).Visibility = Visibility.Collapsed;
                ((TextBlock)OutInfoSp.Children[9]).Visibility = Visibility.Collapsed;
                ((TextBlock)OutInfoSp.Children[10]).Visibility = Visibility.Collapsed;
            }
            else
            {
                var devices = await OutDevice.GetOutDevicesAsync();
                if (devices.First().DeviceType == AudioPlayer.OutApi.None)
                {
                    ((TextBlock)OutInfoSp.Children[2]).Text = "当前无输出设备";
                    ((TextBlock)OutInfoSp.Children[3]).Visibility = Visibility.Collapsed;
                    ((TextBlock)OutInfoSp.Children[4]).Visibility = Visibility.Collapsed;
                    ((TextBlock)OutInfoSp.Children[5]).Visibility = Visibility.Collapsed;
                    ((TextBlock)OutInfoSp.Children[6]).Visibility = Visibility.Collapsed;
                    ((TextBlock)OutInfoSp.Children[7]).Visibility = Visibility.Collapsed;
                    ((TextBlock)OutInfoSp.Children[8]).Visibility = Visibility.Collapsed;
                    ((TextBlock)OutInfoSp.Children[9]).Visibility = Visibility.Collapsed;
                    ((TextBlock)OutInfoSp.Children[10]).Visibility = Visibility.Collapsed;
                    return;
                }

                string outInfo = $"未知";
                if (App.audioPlayer.WasapiOnly && App.audioPlayer.NowOutDevice.DeviceType == AudioPlayer.OutApi.Wasapi)
                    outInfo = $"{App.audioPlayer.NowOutDevice.DeviceType} -> {App.audioPlayer.NowOutDevice.DeviceName}";
                else
                    outInfo = $"{App.audioPlayer.NowOutDevice.DeviceType} -> SRC -> {App.audioPlayer.NowOutDevice.DeviceName}";

                string sampleRateText = "未知";
                var getd = await OutDevice.GetWasapiDeviceFromOtherAPI(App.audioPlayer.NowOutDevice);
                if (App.audioPlayer.WasapiOnly && App.audioPlayer.NowOutDevice.DeviceType == AudioPlayer.OutApi.Wasapi)
                {
                    if (App.audioPlayer.NowOutObj.OutputWaveFormat.SampleRate != App.audioPlayer.FileReader.WaveFormat.SampleRate)
                        sampleRateText = $"{App.audioPlayer.FileReader.WaveFormat.SampleRate} Hz -> {App.audioPlayer.NowOutObj.OutputWaveFormat.SampleRate} Hz（重采样）";
                    else
                        sampleRateText = $"{App.audioPlayer.NowOutObj.OutputWaveFormat.SampleRate} Hz";
                }
                else
                {
                    if (getd.SampleRate != App.audioPlayer.FileReader.WaveFormat.SampleRate)
                        sampleRateText = $"{App.audioPlayer.FileReader.WaveFormat.SampleRate} Hz -> SRC -> {getd.SampleRate} Hz";
                    else
                        sampleRateText = $"{App.audioPlayer.NowOutObj.OutputWaveFormat.SampleRate} Hz（SRC）";

                }

                string channelsText = null;
                if (App.audioPlayer.FileReader.WaveFormat.Channels != getd.Channels)
                {
                    channelsText = $"{App.audioPlayer.FileReader.WaveFormat.Channels} 声道 -> {getd.Channels} 声道";
                }
                else
                {
                    channelsText = $"{App.audioPlayer.FileReader.WaveFormat.Channels} 声道";
                }

                ((TextBlock)OutInfoSp.Children[2]).Text = outInfo;
                ((TextBlock)OutInfoSp.Children[4]).Text = string.IsNullOrEmpty(App.audioPlayer.FileReader.DecodeName) ? "未知" : App.audioPlayer.FileReader.DecodeName;
                ((TextBlock)OutInfoSp.Children[6]).Text = sampleRateText;
                ((TextBlock)OutInfoSp.Children[8]).Text = channelsText;
                ((TextBlock)OutInfoSp.Children[10]).Text = $"{App.audioPlayer.Latency} ms";
            }
        }

        private async void SetCUEInfoText()
        {
            if (App.audioPlayer.MusicData == null) return;
            if (App.audioPlayer.MusicData.CUETrackData == null)
            {
                CUEInfoGrid.Visibility = Visibility.Collapsed;
                return;
            }

            CueSheet cueSheet = await Task.Run(() =>
            {
                return new CueSheet(App.audioPlayer.MusicData.CUETrackData.Path);
            });
            if (cueSheet == null)
            {
                CUEInfoGrid.Visibility = Visibility.Collapsed;
                return;
            }

            string nowTrackName = $"标题：{App.audioPlayer.MusicData.Title}\n艺术家：{App.audioPlayer.MusicData.ArtistName}\n" +
                $"索引：{App.audioPlayer.MusicData.CUETrackData.Index}\n" +
                $"开始时间：{App.audioPlayer.MusicData.CUETrackData.StartDuration.ToString("hh\\:mm\\:ss\\.ff")}\n" +
                $"结束时间：{App.audioPlayer.MusicData.CUETrackData.EndDuration.ToString("hh\\:mm\\:ss\\.ff")}\n" +
                $"时长：{App.audioPlayer.MusicData.CUETrackData.Duration.ToString("hh\\:mm\\:ss\\.ff")}";

            string tracksName = $"共 {cueSheet.Tracks.Length} 首\n";
            for (int i = 0; i < cueSheet.Tracks.Length; i++)
            {
                var track = cueSheet.Tracks[i];
                string index = "";
                for (int j = 0; j < track.Indices.Length; j++)
                {
                    var index2 = track.Indices[j];
                    index += $"index{index2.Number}->{index2.Minutes}:{index2.Seconds}:{index2.Frames}{(j == track.Indices.Length - 1 ? "" : " || ")}";
                }
                tracksName += $"● {track.TrackNumber}\n  标题：{track.Title}\n  艺术家：{track.Performer}\n  Index：{index}{(i == cueSheet.Tracks.Length - 1 ? "" : "\n")}";
            }

            string commentsName = "";
            if (cueSheet.Comments.Any())
            {
                for (int i = 0; i < cueSheet.Comments.Length; i++)
                {
                    var comment = cueSheet.Comments[i];
                    commentsName += $"● {comment}{(i == cueSheet.Comments.Length - 1 ? "" : "\n")}";
                }
            }

            string garbageName = "";
            if (cueSheet.Garbage.Any())
            {
                for (int i = 0; i < cueSheet.Garbage.Length; i++)
                {
                    var garbage = cueSheet.Garbage[i];
                    garbageName += $"● {garbage}{(i == cueSheet.Garbage.Length - 1 ? "" : "\n")}";
                }
            }

            ((TextBlock)CUEInfoSp.Children[2]).Text = App.audioPlayer.MusicData.CUETrackData.Path;
            ((TextBlock)CUEInfoSp.Children[4]).Text = cueSheet.Title;
            ((TextBlock)CUEInfoSp.Children[6]).Text = string.IsNullOrEmpty(cueSheet.Performer) ? "未知" : cueSheet.Performer;
            ((TextBlock)CUEInfoSp.Children[8]).Text = string.IsNullOrEmpty(nowTrackName) ? "无内容" : nowTrackName;
            ((TextBlock)CUEInfoSp.Children[10]).Text = string.IsNullOrEmpty(tracksName) ? "无内容" : tracksName;
            ((TextBlock)CUEInfoSp.Children[12]).Text = string.IsNullOrEmpty(cueSheet.CDTextFile) ? "无内容" : cueSheet.CDTextFile;
            ((TextBlock)CUEInfoSp.Children[14]).Text = string.IsNullOrEmpty(commentsName) ? "无内容" : commentsName;
            ((TextBlock)CUEInfoSp.Children[16]).Text = string.IsNullOrEmpty(cueSheet.Catalog) ? "无内容" : cueSheet.Catalog;
            ((TextBlock)CUEInfoSp.Children[18]).Text = string.IsNullOrEmpty(cueSheet.CalculateCDDBdiscID()) ? "无内容" : cueSheet.CalculateCDDBdiscID();
            ((TextBlock)CUEInfoSp.Children[20]).Text = string.IsNullOrEmpty(garbageName) ? "无内容" : garbageName; ;
        }

        private string ConvertCodecFamilyIntToString(int codecFamily)
        {
            switch (codecFamily)
            {
                case 0:
                    return "Lossy";
                case 1:
                    return "Lossless";
                default:
                    return null;
            }
        }
    }
}
