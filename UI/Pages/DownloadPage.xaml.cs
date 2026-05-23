using System.Linq;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Composition;
using TewiMP.Core;

namespace TewiMP.UI.Pages
{
    public partial class DownloadPage : Page
    {
        public ObservableCollection<DownloadData> DownloadDatas = new();
        public DownloadPage()
        {
            InitializeComponent();
            Loaded += DownloadPage_Loaded;
            Unloaded += DownloadPage_Unloaded;
        }

        private void UpdateTextTB()
        {
            PausePlayBtn.Visibility = DownloadDatas.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            StickTitleHeader.Title = $"下载（{App.Instance.DownloadService.DownloadedData.Count}/{App.Instance.DownloadService.AllDownloadData.Count} - {App.Instance.DownloadService.DownloadingData.Count} 下载中，{App.Instance.DownloadService.DownloadErrorData.Count} 错误）";
        }
        private void DownloadPage_Loaded(object sender, RoutedEventArgs e)
        {
            App.Instance.DownloadService.NowDownloadPage = this;
            UpdateTextTB();
            ListViewBase.ItemsSource = DownloadDatas;

            App.Instance.DownloadService.AddDownload += DownloadManager_AddDownload;
            App.Instance.DownloadService.OnDownloading += DownloadManager_OnDownloading;
            App.Instance.DownloadService.OnDownloadedSaving += DownloadManager_OnDownloadedSaving;
            App.Instance.DownloadService.OnDownloadedPreview += DownloadManager_OnDownloading;
            App.Instance.DownloadService.OnDownloaded += DownloadManager_OnDownloading;
            App.Instance.DownloadService.OnDownloadError += DownloadManager_OnDownloading;

            // 当第一次初始化时加载
            foreach (var dm in App.Instance.DownloadService.AllDownloadData)
            {
                DownloadDatas.Add(dm);
            }
            foreach (var dm in App.Instance.DownloadService.DownloadingData)
            {
                App.Instance.DownloadService.CallOnDownloadingEvent(dm);
            }
            foreach (var dm in App.Instance.DownloadService.DownloadedData)
            {
                App.Instance.DownloadService.CallOnDownloadedEvent(dm);
            }
            foreach (var dm in App.Instance.DownloadService.DownloadErrorData)
            {
                App.Instance.DownloadService.CallOnDownloadErrorEvent(dm);
            }

            if (!DownloadDatas.Any())
            {
                ListEmptyPopup.Visibility = Visibility.Visible;
                AtListBottomTb.Visibility = Visibility.Collapsed;
            }
            else
            {
                ListEmptyPopup.Visibility = Visibility.Collapsed;
                AtListBottomTb.Visibility = Visibility.Visible;
            }
        }

        private void DownloadManager_OnDownloadedSaving(DownloadData data)
        {
            UpdateTextTB();
        }

        private void DownloadManager_OnDownloading(DownloadData data)
        {
            UpdateTextTB();
        }

        private void DownloadManager_AddDownload(DownloadData data)
        {
            DownloadDatas.Add(data);
            UpdateTextTB();
        }

        private void DownloadPage_Unloaded(object sender, RoutedEventArgs e)
        {
            ListViewBase.ItemsSource = null;
            App.Instance.DownloadService.AddDownload -= DownloadManager_AddDownload;
            App.Instance.DownloadService.OnDownloading -= DownloadManager_OnDownloading;
            App.Instance.DownloadService.OnDownloadedPreview -= DownloadManager_OnDownloading;
            App.Instance.DownloadService.OnDownloadedSaving -= DownloadManager_OnDownloadedSaving;
            App.Instance.DownloadService.OnDownloaded -= DownloadManager_OnDownloading;
            App.Instance.DownloadService.OnDownloadError -= DownloadManager_OnDownloading;
        }

        private void ToSettingBtn_Click(object sender, RoutedEventArgs e)
        {
            App.MainWindowInstance.SetNavViewContent(
                typeof(SettingPage),
                "open download");
        }

        private void PausePlayBtn_Click(object sender, RoutedEventArgs e)
        {
            if (App.Instance.DownloadService.PauseDownload)
            {
                App.Instance.DownloadService.PauseDownload = false;
                PausePlayBtn.Label = "暂停下载";
                PausePlayIcon.Glyph = "\uE769";
            }
            else
            {
                App.Instance.DownloadService.PauseDownload = true;
                PausePlayBtn.Label = "继续下载";
                PausePlayIcon.Glyph = "\uE768";
            }
        }
    }
}
