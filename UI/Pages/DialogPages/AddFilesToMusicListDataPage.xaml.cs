using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using TewiMP.Helpers;
using TewiMP.UI.Windows;
using TewiMP.Services.Storage;
using TewiMP.Core.Music;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TewiMP.UI.Pages.DialogPages
{
    public sealed partial class AddFilesToMusicListDataPage : UserControl
    {
        public MusicListData musicListData { get; set; }
        public AddFilesToMusicListDataPage()
        {
            InitializeComponent();
        }

        private void AddFilesToPlaylistPage_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void AddFilesToPlaylistPage_Unloaded(object sender, RoutedEventArgs e)
        {
        }

        private async void FilePickerButton_Click(object sender, RoutedEventArgs e)
        {
            var files = await FileHelper.UserSelectFiles(
                PickerViewMode.List, PickerLocationId.MusicLibrary);
            //App.Instance.SupportedMediaFormats);
            if (files.Any())
            {
                App.MainWindowInstance.HideDialog();
                //ItemsList_Header_Info_CommandBar.IsEnabled = false;
                var item = App.MainWindowInstance.AddNotify("添加本地歌曲", "正在准备添加本地歌曲...", NotifySeverity.Loading, TimeSpan.MaxValue);
                var jdata = await PlayListHelper.ReadData();
                int count = 0;
                string listName = musicListData.ListName;
                foreach (var i in files)
                {
                    item.HorizontalAlignment = HorizontalAlignment.Stretch;
                    item.SetNotifyItemData("添加本地歌曲", $"进度：{count}/{files.Count}，{Math.Round(((decimal)count / files.Count) * 100, 1)}%\n正在添加：{i.Name}", NotifySeverity.Loading);
                    item.SetProcess(files.Count, count);
                    FileInfo fi = null;
                    await Task.Run(() => fi = new FileInfo(i.Path));
                    jdata = await PlayListHelper.AddLocalMusicDataToPlayList(listName, fi, jdata);
                    count++;
                }
                item.SetProcess(0, 0);
                item.HorizontalAlignment = HorizontalAlignment.Center;
                item.SetNotifyItemData("添加本地歌曲", "正在保存...", NotifySeverity.Loading);
                await PlayListHelper.SaveData(jdata);
                await App.Instance.PlayListReader.Refresh();
                //InitInfo();
                //InitBindings();
                //ItemsList_Header_Info_CommandBar.IsEnabled = true;
                item.SetNotifyItemData("添加本地歌曲", "添加本地歌曲成功。", NotifySeverity.Complete);
                App.MainWindowInstance.NotifyCountDown(item);
            }
        }

        private async void FolderPickerButton_Click(object sender, RoutedEventArgs e)
        {
            StorageFolder folder = await FileHelper.UserSelectFolder(PickerLocationId.MusicLibrary);
            if (folder != null)
            {
                App.MainWindowInstance.HideDialog();
                var jdata = await PlayListHelper.ReadData();
                DirectoryInfo directory = null;
                await Task.Run(() => directory = Directory.CreateDirectory(folder.Path));
                foreach (var i in directory.GetFiles())
                {
                    if (App.SupportedMediaFormats.Contains(i.Extension))
                    {
                        jdata = await PlayListHelper.AddLocalMusicDataToPlayList(musicListData.ListName, i, jdata);
                    }
                }
                await PlayListHelper.SaveData(jdata);
                await App.Instance.PlayListReader.Refresh();
                //InitInfo();
                //InitBindings();
                App.MainWindowInstance.AddNotify("添加本地歌曲成功。", null, NotifySeverity.Complete);
            }
        }
    }
}
