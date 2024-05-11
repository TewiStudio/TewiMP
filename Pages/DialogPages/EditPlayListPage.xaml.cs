using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TewiMP.Helpers;
using TewiMP.DataEditor;

namespace TewiMP.Pages.DialogPages
{
    public partial class EditPlayListPage : Page
    {
        delegate void ResultDelegate(ContentDialogResult contentDialogResult);
        static event ResultDelegate ResultEvent;

        public MusicListData MusicListData { get; set; }
        public EditPlayListPage()
        {
            InitializeComponent();
            ResultEvent += AddPlayListPage_ResultEvent;
        }

        private async void AddPlayListPage_ResultEvent(ContentDialogResult contentDialogResult)
        {
            if (contentDialogResult == ContentDialogResult.Primary)
            {
                var data = await PlayListHelper.ReadData();
                data[MusicListData.ListName]["ListShowName"] = Name_TB.Text;
                data[MusicListData.ListName]["PicturePath"] = ImagePath_TB.Text;
                await PlayListHelper.SaveData(data);
                await App.playListReader.Refresh();
                MainWindow.AddNotify("编辑列表成功。", null, NotifySeverity.Complete);
            }
            ResultEvent -= AddPlayListPage_ResultEvent;
        }

        public static async Task ShowDialog(MusicListData musicListData)
        {
            var a = await MainWindow.ShowDialog($"编辑 \"{musicListData.ListShowName}\"", new EditPlayListPage() { MusicListData = musicListData }, "取消", "更改", defaultButton: ContentDialogButton.Primary);
            ResultEvent?.Invoke(a);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var a = await FileHelper.UserSelectFile(Windows.Storage.Pickers.PickerViewMode.List, Windows.Storage.Pickers.PickerLocationId.PicturesLibrary);
            if (a != null)
                ImagePath_TB.Text = a.Path;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Name_TB.Text = MusicListData.ListShowName;
            ImagePath_TB.Text = MusicListData.PicturePath;
        }
    }
}
