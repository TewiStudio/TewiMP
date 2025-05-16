using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TewiMP.Helpers;
using TewiMP.DataEditor;
using TewiMP.Plugin;

namespace TewiMP.Pages.DialogPages
{
    public partial class AddPlayListPage : Page
    {
        delegate void ResultDelegate(ContentDialogResult contentDialogResult);
        static event ResultDelegate ResultEvent;

        public AddPlayListPage()
        {
            InitializeComponent();

            AddOutSidePage_PlatfromCb.ItemsSource = PluginManager.MusicSourcePlugins;
            AddOutSidePage_PlatfromCb.SelectedIndex = 0;

            ResultEvent += AddPlayListPage_ResultEvent;
        }

        private async void AddPlayListPage_ResultEvent(ContentDialogResult contentDialogResult)
        {
            if (contentDialogResult == ContentDialogResult.Primary)
            {
                MainWindow.AddNotify("正在添加列表...", null);
                MusicListData musicListData = null;

                if (PivotList.SelectedIndex == 0)
                {
                    musicListData = new MusicListData(null, AddLocalPage_ListNameTB.Text, AddLocalPage_ListImageTB.Text, MusicFrom.localMusic);
                    musicListData.ListName = musicListData.MD5;
                    musicListData.ListFrom = MusicFrom.localMusic;
                    musicListData.ListDataType = DataType.本地歌单;
                }
                else
                {
                    try
                    {
                        var platform = (MusicSourcePlugin)AddOutSidePage_PlatfromCb.SelectedItem;
                        MusicListData pl = await platform.GetPlayList(AddOutSidePage_IDTb.Text);
                        if (pl != null)
                        {
                            musicListData = pl;
                        }
                    }
                    catch
                    {
                    }
                }

                if (musicListData != null)
                {
                    try
                    {
                        await PlayListHelper.AddPlayList(musicListData);
                        await App.playListReader.Refresh();
                        MainWindow.AddNotify("添加列表成功", null, NotifySeverity.Complete);
                    }
                    catch (ArgumentException)
                    {
                        MainWindow.AddNotify(
                            "已存在一个同名的列表",
                            "无法添加这个播放列表，因为填写的属性已被其它播放列表占用。\n请尝试换一个播放列表名称或图片地址试试。",
                            NotifySeverity.Error);
                    }
                    catch (Exception err)
                    {
                        LogHelper.WriteLog("AddPlayListPage", err.ToString(), false);
                        var b = await MainWindow.ShowDialog("添加播放列表时出现错误", err.Message, "确定", "重试", defaultButton: ContentDialogButton.Primary);
                        if (b == ContentDialogResult.Primary)
                        {
                            AddPlayListPage_ResultEvent(contentDialogResult);
                        }
                    }
                }
            }
            ResultEvent -= AddPlayListPage_ResultEvent;
        }

        public static async Task ShowDialog()
        {
            var a = await MainWindow.ShowDialog("", new AddPlayListPage(), "取消", "创建", defaultButton: ContentDialogButton.Primary);
            ResultEvent?.Invoke(a);
        }

        private async void AddLocalPage_ListImageSelectBtn_Click(object sender, RoutedEventArgs e)
        {
            var a = await FileHelper.UserSelectFile(Windows.Storage.Pickers.PickerViewMode.List, Windows.Storage.Pickers.PickerLocationId.PicturesLibrary);
            if (a != null)
                AddLocalPage_ListImageTB.Text = a.Path;
        }

        private void PivotList_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
        }

        private void PivotList_Loaded(object sender, RoutedEventArgs e)
        {
            PivotList.SelectedItem = PivotList.Items[0];
        }

        private void PivotList_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {/*
            if ((PivotList.SelectedItem as NavigationViewItem).Content as string == "添加播放列表")
            {
                AddLocalPage.Visibility = Visibility.Visible;
                AddOutSidePage.Visibility = Visibility.Collapsed;
            }
            else
            {
                AddLocalPage.Visibility = Visibility.Collapsed;
                AddOutSidePage.Visibility = Visibility.Visible;
            }*/
        }
    }
}
