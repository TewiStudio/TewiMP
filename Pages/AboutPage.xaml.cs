using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.WinUI.Controls;
using Windows.System;
using NAudio.Wave;
using TewiMP.Media;

namespace TewiMP.Pages
{
    public partial class AboutPage : Page
    {
        private WaveOut waveOut;
        private BufferedWaveProvider bufferedWaveProvider;
        public AboutPage()
        {
            InitializeComponent();
            VersionRun.Text = $"v{App.AppVersion}";
            waveOut = new WaveOut();
            bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat());
            waveOut.Init(bufferedWaveProvider);
            waveOut.Play();
        }

        unsafe void Play(string file)
        {
/*
            var audio = new Media.Decoder.FFmpeg.FFmpegDecoder();
            audio.InitDecodecAudio(file);
            audio.Play();

            var PlayTask = new Task(() =>
            {
                while (true)
                {
                    //播放中
                    if (audio.IsPlaying)
                    {
                        //获取下一帧视频
                        if (audio.TryReadNextFrame(out var frame))
                        {
                            var bytes = audio.FrameConvertBytes(&frame);
                            if (bytes is null)
                                continue;
                            if (bufferedWaveProvider.BufferLength <= bufferedWaveProvider.BufferedBytes + bytes.Length)
                            {
                                bufferedWaveProvider.ClearBuffer();
                            }
                            bufferedWaveProvider.AddSamples(bytes, 0, bytes.Length);//向缓存中添加音频样本
                        }
                    }
                }
            });
            PlayTask.Start();*/

        }

        //int a = 0;
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            //await App.playingList.Play(App.playListReader.NowMusicListDatas[0].Songs[0]);

            //CueSharp.CueSheet cueSheet = new CueSharp.CueSheet("E:\\vedio\\anime\\[170816] TVアニメ「Fate／Apocrypha」OPテーマ「英雄 運命の詩」／EGOIST [通常盤] [FLAC+CUE]\\VVCL-1080.cue");

            try
            {
                if (App.playingList.NowPlayingList.Any())
                    abcd.Source = (await ImageManage.GetImageSource(App.playingList.NowPlayingList[new Random().Next(0, App.playingList.NowPlayingList.Count - 1)])).Item1;
            }
            catch { }
            GC.Collect();/*
            var f = await FileHelper.UserSelectFile();
            Play(f.Path);
            try
            {
                if (App.playingList.NowPlayingList.Any())
                    abcd.Source = App.playingList.NowPlayingList[new Random().Next(0, App.playingList.NowPlayingList.Count - 1)].Album.PicturePath;
            }
            catch { }
*/
            return;
            //App.audioPlayerBass.LoadAudio();
            //System.Diagnostics.App.logManager.Log(a[0].ListName);
            //MainWindow.SetBackdrop(MainWindow.BackdropType.DesktopAcrylic);
            //await App.audioPlayer.Reload();
            /*
            if (a == 0)
            {
                MainWindow.SetBackdrop(MainWindow.BackdropType.Mica);
                a++;
            }
            else if (a == 1)
            {
                MainWindow.SetBackdrop(MainWindow.BackdropType.DesktopAcrylic);
                a++;
            }
            else if (a == 2)
            {
                MainWindow.SetBackdrop(MainWindow.BackdropType.DefaultColor);
                a = 0;
            }
            */
        }

        private void Image_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private async void Hyperlink_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            await Launcher.LaunchUriAsync(new Uri($"https://github.com/dotnet/sdk"));
        }

        private async void Hyperlink_Click_1(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            await Launcher.LaunchUriAsync(new Uri($"https://github.com/microsoft/WindowsAppSDK"));
        }

        private async void SettingsCard_Click(object sender, RoutedEventArgs e)
        {

            await Launcher.LaunchUriAsync(new Uri((sender as SettingsCard).Tag as string));
        }

        private async void Hyperlink_Click_2(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            await Launcher.LaunchUriAsync(new Uri($"https://www.pixiv.net/artworks/117179092"));
        }
    }
}
