using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NAudio.Wave;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TewiMP.Services.Media;
using TewiMP.Helpers;
using System.Threading;
using TewiMP.UI.Windows;
using System.Threading.Tasks;
using TewiMP.Services.Storage;

namespace TewiMP.UI.Pages
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
                if (App.Instance.PlayingListService.NowPlayingList.Any())
                    abcd.Source = (await ImageService.GetImageUri(App.Instance.PlayingListService.NowPlayingList[new Random().Next(0, App.Instance.PlayingListService.NowPlayingList.Count - 1)])).Item1;
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
            //App.Instance.AudioServiceBass.LoadAudio();
            //System.Diagnostics.LogManager.Log(a[0].ListName);
            //App.MainWindowInstance.SetBackdrop(BackdropType.DesktopAcrylic);
            //await App.Instance.AudioService.Reload();
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

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            CheckUpdate();
            installButton = InstallButton;
            await DownloadInstallExeAsync(true);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            installButton = null;
        }

        bool checkingUpdate = false;
        static VersionData newestVersion;
        private void CheckUpdate()
        {
            if (checkingUpdate) return;
            checkingUpdate = true;

            newestVersion = App.Instance.GetNewVersionByReleaseData(App.Instance.NowVersion.SuffixType);
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

        static Button installButton; static CancellationTokenSource? cts = null;
        static async Task DownloadInstallExeAsync(bool dontDownload = false)
        {
            if (newestVersion is null || newestVersion.Version == new Version(0, 0, 0, 0)) return;
            if (string.IsNullOrEmpty(newestVersion.InstallUrl))
            {
                App.MainWindowInstance.AddNotify("下载更新程序失败", "没有可用的更新程序，请前往发布详情页下载。", NotifySeverity.Warning,
                    buttonMessage: "详情页", buttonAction: async () => await CodeHelper.OpenInBrowser(newestVersion.Url));
                return;
            }

            var installExeFilePath = Path.Combine(DataFolderBase.UpdateFolder, $"Update_{newestVersion.Version}_{newestVersion.SuffixType}.exe");
            bool fileExists = File.Exists(installExeFilePath);

            // Helper: 更新按钮文本
            void UpdateButton(string text) => ChangeInstallButtonContent(text);

            // 如果正在下载
            if (cts != null)
            {
                if (dontDownload)
                    return;

                // 取消下载
                cts.Cancel();
                cts = null;
                UpdateButton("下载安装程序");
                return;
            }

            // 文件已经存在
            if (fileExists)
            {
                if (dontDownload)
                {
                    UpdateButton("开始安装");
                }
                else
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo { FileName = installExeFilePath });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"启动安装程序失败: {ex.Message}");
                    }
                    UpdateButton("重新安装");
                }
                return;
            }

            // 文件不存在，需要下载
            if (dontDownload)
            {
                UpdateButton("下载安装程序");
                return;
            }

            cts = new CancellationTokenSource();
            UpdateButton("取消（正在加载...）");

            try
            {
                await WebHelper.DownloadFileAsync(newestVersion.InstallUrl, installExeFilePath,
                    new Progress<double>(p =>
                    {
                        if (p < 0.99)
                            UpdateButton($"取消（{p * 100:F0}%）");
                    }), cts.Token);

                UpdateButton("开始安装");
            }
            catch (OperationCanceledException)
            {
                // 用户取消下载
                UpdateButton("下载安装程序");
                if (File.Exists(installExeFilePath))
                    File.Delete(installExeFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"下载失败: {ex.Message}");
                UpdateButton("下载安装程序");
            }
            finally
            {
                cts = null;
            }
        }

        static void ChangeInstallButtonContent(object content)
        {
            if (installButton is null) return;
            App.MainWindowInstance.Invoke(() => installButton.Content = content);
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe)
            {
                if (fe.Tag.Equals("Card"))
                {
                    await CodeHelper.OpenInBrowser(newestVersion.Url);
                }
                else
                {
                    await DownloadInstallExeAsync();
                }
            }
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
