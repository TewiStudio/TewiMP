using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TewiMP.Media;
using TewiMP.Helpers;
using TewiMP.Controls;
using TewiMP.DataEditor;
using Newtonsoft.Json.Linq;

namespace TewiMP.Pages.DialogPages
{
    public partial class InsertPlayListPage : Page
    {
        delegate void ResultDelegate(ContentDialogResult contentDialogResult);
        static event ResultDelegate ResultEvent;
        ObservableCollection<MusicListData> bindingMusicListData;

        public InsertPlayListPage()
        {
            InitializeComponent();
            ResultEvent += InsertPlayListPage_ResultEvent;
            bindingMusicListData = new();
            ListBaseViewer.ItemsSource = bindingMusicListData;

            foreach (var l in App.playListReader.NowMusicListData) bindingMusicListData.Add(l);
        }

        private async void InsertPlayListPage_ResultEvent(ContentDialogResult contentDialogResult)
        {
            ResultEvent -= InsertPlayListPage_ResultEvent;
            if (contentDialogResult != ContentDialogResult.Primary) return;
            MainWindow.AddNotify("正在更改歌单排序...", null);

            JObject data = await Task.Run(() =>
            {
                JObject data = new();
                foreach (var l in bindingMusicListData)
                {
                    data.Add(l.ListName, JObject.FromObject(l));
                }
                return data;
            });

            await PlayListHelper.SaveData(data);
            await App.playListReader.Refresh();
            MainWindow.AddNotify("歌单排序更改成功。", null, NotifySeverity.Complete);
        }

        public static async Task ShowDialog()
        {
            var a = await MainWindow.ShowDialog($"排序播放列表", new InsertPlayListPage(), "取消", "确定", defaultButton: ContentDialogButton.Primary);
            ResultEvent?.Invoke(a);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private async void Grid_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var ml = sender.DataContext as MusicListData;
            ImageEx image = (sender as Grid).Children[0] as ImageEx;
            if (ml is null) return;
            if (image is null) return;

            int size = 0;
            if (ml.ListDataType == DataType.本地歌单)
            {
                image.Source = await FileHelper.GetImageSource(ml.PicturePath, size, size, true);
            }
            else if (ml.ListDataType == DataType.歌单)
            {
                var imageSources = await ImageManage.GetImageSource(ml, size, size, true);
                image.Source = imageSources.Item1;
            }
        }
    }
}
