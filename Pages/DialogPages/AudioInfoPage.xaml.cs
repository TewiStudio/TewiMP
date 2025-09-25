using CueSharp;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using NAudio.Wave;
using NAudio.Wave.Asio;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TewiMP.Helpers;
using TewiMP.Media;

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
                FileInfo fileInfo = new(App.Instance.AudioPlayer.FileReader.FileName);
                createTime = fileInfo.CreationTime.ToString();
                filePath = fileInfo.DirectoryName;
            });

            ((Run)((Hyperlink)((TextBlock)FileInfoSp.Children[2]).Inlines[0]).Inlines[0]).Text = App.Instance.AudioPlayer.FileReader.FileName;
            ((Run)((Hyperlink)((TextBlock)FileInfoSp.Children[4]).Inlines[0]).Inlines[0]).Text = filePath;
            ((TextBlock)FileInfoSp.Children[6]).Text = createTime;
            ((TextBlock)FileInfoSp.Children[8]).Text = CodeHelper.GetAutoSizeString(App.Instance.AudioPlayer.FileSize, 2);
        }

        private async void SetAudioInfoText()
        {
            if (App.Instance.AudioPlayer.FileReader != null)
            {
                if (App.Instance.AudioPlayer.FileReader.isMidi)
                {
                    AudioInfoGrid.Visibility = Visibility.Collapsed;
                    return;
                }
            }

            ATL.Track tfile = null;
            if (App.Instance.AudioPlayer.tfile is null)
            {
                await Task.Run(() =>
                {
                    tfile = new ATL.Track(App.Instance.AudioPlayer.FileReader.FileName);
                });
            }
            else
            {
                tfile = App.Instance.AudioPlayer.tfile;
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

                ((TextBlock)AudioInfoSp.Children[1]).Text = $"{App.Instance.AudioPlayer.FileType}" +
                    $"{(ConvertCodecFamilyIntToString(tfile.CodecFamily) is null ? "" : $"  {ConvertCodecFamilyIntToString(tfile.CodecFamily)}")}" +
                    $"  {tfile.SampleRate} Hz  {tfile.Bitrate} kbps" +
                    $"  {tfile.ChannelsArrangement.NbChannels} 声道  {App.Instance.AudioPlayer.FileReader.TotalTime.ToString("hh\\:mm\\:ss\\.ff")}({tfile.Duration}s)";

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
            if (App.Instance.AudioPlayer.FileReader.isMidi)
            {
                ((TextBlock)OutInfoSp.Children[2]).Text = $"Midi -> {App.Instance.AudioPlayer.MidiOutputDevice.Name}";
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
                if ((App.Instance.AudioPlayer.WasapiOnly && App.Instance.AudioPlayer.NowOutDevice.DeviceType == AudioPlayer.OutApi.Wasapi) || App.Instance.AudioPlayer.NowOutDevice.DeviceType == AudioPlayer.OutApi.Asio)
                {
                    outInfo = $"{App.Instance.AudioPlayer.NowOutDevice.DeviceType} -> {App.Instance.AudioPlayer.NowOutDevice.DeviceName}";
                }
                else
                {
                    outInfo = $"{App.Instance.AudioPlayer.NowOutDevice.DeviceType} -> SRC -> {App.Instance.AudioPlayer.NowOutDevice.DeviceName}";
                }

                string sampleRateText = "未知";
                string channelsText = "未知";

                if (App.Instance.AudioPlayer.NowOutDevice.DeviceType != AudioPlayer.OutApi.Asio)
                {
                    //var getd = await OutDevice.GetWasapiDeviceFromOtherAPI(App.Instance.AudioPlayer.NowOutDevice);
                    var outputFormat = await OutDevice.GetWasapiDeviceFromOtherAPI(App.Instance.AudioPlayer.NowOutDevice);
                    //var outputFormat = App.Instance.AudioPlayer.NowOutDevice;
                    if (App.Instance.AudioPlayer.WasapiOnly && App.Instance.AudioPlayer.NowOutDevice.DeviceType == AudioPlayer.OutApi.Wasapi)
                    {
                        if (App.Instance.AudioPlayer.NowOutObj.OutputWaveFormat.SampleRate != App.Instance.AudioPlayer.FileReader.WaveFormat.SampleRate)
                            sampleRateText = $"{App.Instance.AudioPlayer.FileReader.WaveFormat.SampleRate} Hz -> {App.Instance.AudioPlayer.NowOutObj.OutputWaveFormat.SampleRate} Hz（重采样）";
                        else
                            sampleRateText = $"{App.Instance.AudioPlayer.NowOutObj.OutputWaveFormat.SampleRate} Hz";
                    }
                    else
                    {
                        if (outputFormat.SampleRate != App.Instance.AudioPlayer.FileReader.WaveFormat.SampleRate)
                            sampleRateText = $"{App.Instance.AudioPlayer.FileReader.WaveFormat.SampleRate} Hz -> SRC -> {outputFormat.SampleRate} Hz";
                        else
                            sampleRateText = $"{App.Instance.AudioPlayer.NowOutObj.OutputWaveFormat.SampleRate} Hz（SRC）";

                    }

                    if (App.Instance.AudioPlayer.FileReader.WaveFormat.Channels != outputFormat.Channels)
                    {
                        channelsText = $"{App.Instance.AudioPlayer.FileReader.WaveFormat.Channels} 声道 -> {outputFormat.Channels} 声道";
                    }
                    else
                    {
                        channelsText = $"{App.Instance.AudioPlayer.FileReader.WaveFormat.Channels} 声道";
                    }
                    ((TextBlock)OutInfoSp.Children[10]).Text = $"{App.Instance.AudioPlayer.Latency} ms";
                }
                else
                {
                    var asioOut = App.Instance.AudioPlayer.NowOutObj as AsioOut;
                    if (asioOut.OutputWaveFormat.SampleRate != App.Instance.AudioPlayer.FileReader.WaveFormat.SampleRate)
                        sampleRateText = $"{App.Instance.AudioPlayer.FileReader.WaveFormat.SampleRate} Hz -> {asioOut.OutputWaveFormat.SampleRate} Hz（重采样）";
                    else
                        sampleRateText = $"{asioOut.OutputWaveFormat.SampleRate} Hz";

                    if (App.Instance.AudioPlayer.FileReader.WaveFormat.Channels != asioOut.OutputWaveFormat.Channels)
                    {
                        channelsText = $"{App.Instance.AudioPlayer.FileReader.WaveFormat.Channels} 声道 -> {asioOut.OutputWaveFormat.Channels} 声道";
                    }
                    else
                    {
                        channelsText = $"{asioOut.OutputWaveFormat.Channels} 声道";
                    }
                    ((TextBlock)OutInfoSp.Children[10]).Text = $"{(App.Instance.AudioPlayer.NowOutObj as AsioOut).PlaybackLatency} ms";
                }

                ((TextBlock)OutInfoSp.Children[2]).Text = outInfo;
                ((TextBlock)OutInfoSp.Children[4]).Text = string.IsNullOrEmpty(App.Instance.AudioPlayer.FileReader.DecodeName) ? "未知" : App.Instance.AudioPlayer.FileReader.DecodeName;
                ((TextBlock)OutInfoSp.Children[6]).Text = sampleRateText;
                ((TextBlock)OutInfoSp.Children[8]).Text = channelsText;
            }
        }

        private async void SetCUEInfoText()
        {
            if (App.Instance.AudioPlayer.MusicData is null) return;
            if (App.Instance.AudioPlayer.MusicData.CUETrackData is null)
            {
                CUEInfoGrid.Visibility = Visibility.Collapsed;
                return;
            }

            CueSheet cueSheet = await Task.Run(() =>
            {
                return new CueSheet(App.Instance.AudioPlayer.MusicData.CUETrackData.Path);
            });
            if (cueSheet is null)
            {
                CUEInfoGrid.Visibility = Visibility.Collapsed;
                return;
            }

            string nowTrackName = $"标题：{App.Instance.AudioPlayer.MusicData.Title}\n艺术家：{App.Instance.AudioPlayer.MusicData.ArtistName}\n" +
                $"索引：{App.Instance.AudioPlayer.MusicData.CUETrackData.Index}\n" +
                $"开始时间：{App.Instance.AudioPlayer.MusicData.CUETrackData.StartDuration.ToString("hh\\:mm\\:ss\\.ff")}\n" +
                $"结束时间：{App.Instance.AudioPlayer.MusicData.CUETrackData.EndDuration.ToString("hh\\:mm\\:ss\\.ff")}\n" +
                $"时长：{App.Instance.AudioPlayer.MusicData.CUETrackData.Duration.ToString("hh\\:mm\\:ss\\.ff")}";

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
            if (cueSheet.Comments.Length == 0)
            {
                for (int i = 0; i < cueSheet.Comments.Length; i++)
                {
                    var comment = cueSheet.Comments[i];
                    commentsName += $"● {comment}{(i == cueSheet.Comments.Length - 1 ? "" : "\n")}";
                }
            }

            string garbageName = "";
            if (cueSheet.Garbage.Length == 0)
            {
                for (int i = 0; i < cueSheet.Garbage.Length; i++)
                {
                    var garbage = cueSheet.Garbage[i];
                    garbageName += $"● {garbage}{(i == cueSheet.Garbage.Length - 1 ? "" : "\n")}";
                }
            }

            ((TextBlock)CUEInfoSp.Children[2]).Text = App.Instance.AudioPlayer.MusicData.CUETrackData.Path;
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

        private async void Hyperlink_Click(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            if (sender.Inlines[0] is Run run)
            {
                await FileHelper.ExploreFile(run.Text);
            }
        }

        private async void Hyperlink_Click_1(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            if (sender.Inlines[0] is Run run)
            {
                await FileHelper.ExploreFolder(run.Text);
            }
        }
    }
}
