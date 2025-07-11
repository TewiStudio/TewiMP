using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.WinUI.Controls;
using Windows.System;
using NAudio.Wave;
using TewiMP.Media;
using TewiMP.Helpers;
using System.IO;

namespace TewiMP.Pages
{
    public partial class AboutPage : Page
    {
        private WaveOut waveOut;
        private BufferedWaveProvider bufferedWaveProvider;
        public AboutPage()
        {
            InitializeComponent();
            VersionRun.Text = $"v{App.Instance.AppVersion} {App.Instance.NowVersion.SuffixType}";
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
            //await App.Instance.playingList.Play(App.Instance.playListReader.NowMusicListDatas[0].Songs[0]);

            //CueSharp.CueSheet cueSheet = new CueSharp.CueSheet("E:\\vedio\\anime\\[170816] TVアニメ「Fate／Apocrypha」OPテーマ「英雄 運命の詩」／EGOIST [通常盤] [FLAC+CUE]\\VVCL-1080.cue");

            try
            {
                if (App.Instance.playingList.NowPlayingList.Any())
                    abcd.Source = (await ImageManage.GetImageSource(App.Instance.playingList.NowPlayingList[new Random().Next(0, App.Instance.playingList.NowPlayingList.Count - 1)])).Item1;
            }
            catch { }
            GC.Collect();/*
            var f = await FileHelper.UserSelectFile();
            Play(f.Path);
            try
            {
                if (App.Instance.playingList.NowPlayingList.Any())
                    abcd.Source = App.Instance.playingList.NowPlayingList[new Random().Next(0, App.Instance.playingList.NowPlayingList.Count - 1)].Album.PicturePath;
            }
            catch { }
*/
            return;
            //App.Instance.audioPlayerBass.LoadAudio();
            //System.Diagnostics.LogManager.Log(a[0].ListName);
            //App.MainWindowInstance.SetBackdrop(BackdropType.DesktopAcrylic);
            //await App.Instance.audioPlayer.Reload();
            /*
            if (a == 0)
            {
                App.MainWindowInstance.SetBackdrop(BackdropType.Mica);
                a++;
            }
            else if (a == 1)
            {
                App.MainWindowInstance.SetBackdrop(BackdropType.DesktopAcrylic);
                a++;
            }
            else if (a == 2)
            {
                App.MainWindowInstance.SetBackdrop(BackdropType.DefaultColor);
                a = 0;
            }
            */
        }

        private void Image_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private async void Hyperlink_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            await CodeHelper.OpenInBrowser("https://github.com/dotnet/sdk");
        }

        private async void Hyperlink_Click_1(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            await CodeHelper.OpenInBrowser("https://github.com/microsoft/WindowsAppSDK");
        }

        private async void SettingsCard_Click(object sender, RoutedEventArgs e)
        {

            await CodeHelper.OpenInBrowser(new Uri((sender as SettingsCard).Tag as string));
        }

        private async void Hyperlink_Click_2(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            await CodeHelper.OpenInBrowser("https://www.pixiv.net/artworks/117179092");
        }
        
        private async void Hyperlink_Click_3(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            await CodeHelper.OpenInBrowser("https://github.com/zilongcn23/TewiMP-Release/issues");
        }

        private async void Hyperlink_Click_4(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            await CodeHelper.OpenInBrowser("https://music.163.com/#/user/home?id=7916651285");
        }

        private async void Hyperlink_Click_5(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            await CodeHelper.OpenInBrowser("https://github.com/TewiStudio/TewiMP");
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            CheckUpdate();
        }

        bool checkingUpdate = false;
        private void CheckUpdate()
        {
            if (checkingUpdate) return;
            checkingUpdate = true;

            var newestVersion = App.Instance.GetNewVersionByReleaseData(App.Instance.NowVersion.SuffixType);
            if (App.Instance.AppVersionIsNewest())
            {
                UpdateExpander.Description = "当前版本是最新版本";
                NewestVersion.Visibility = Visibility.Collapsed;
            }
            else
            {
                UpdateExpander.Description = "发现新版本";
                NewestVersion.Visibility = Visibility.Visible;
            }

            NewestVersionRun.Text = $"{newestVersion.Version} {newestVersion.SuffixType}";
            NewestVersion.Description = $"时间：{newestVersion.ReleaseTime}";

            NowVersionRun.Text = $"{App.Instance.AppVersion} {App.Instance.NowVersion.SuffixType}";
            NowVersion.Description = $"时间：{App.Instance.AppVersionReleaseDate}";

            checkingUpdate = false;
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            UpdateExpander.Description = "检查更新中......";
            try
            {
                await App.Instance.CheckUpdate(false);
            }
            catch
            {
                App.MainWindowInstance.AddNotify("无法获取到更新信息", "请检查网络设置。", NotifySeverity.Warning);
            }
            CheckUpdate();
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var newestVersion = App.Instance.GetNewVersionByReleaseData(App.Instance.NowVersion.SuffixType);
            await CodeHelper.OpenInBrowser(newestVersion.Url);
        }

        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            await CodeHelper.OpenInBrowser("https://github.com/TewiStudio/TewiMP/issues");
        }

        private async void Button_Click_4(object sender, RoutedEventArgs e)
        {
            await CodeHelper.OpenInBrowser("https://afdian.com/a/TewiStudio");
        }

        private void SettingsCard_Loading(FrameworkElement sender, object args)
        {
            var licensePath = Path.Combine(Environment.CurrentDirectory, "LICENSE");
            if (File.Exists(licensePath))
            {
                var licence = File.ReadAllText(licensePath);
                LicenseTextBlock.Text = licence;
            }
            else
            {
                LicenseExpander.IsEnabled = false;
                LicenseExpander.Description = "找不到许可证文件。";
            }
        }
    }
}
