using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using TewiMP.UI.Pages;
using TewiMP.UI.Windows;
using TewiMP.Core.Music;
using TewiMP.Core.Models;
using TewiMP.Helpers;
using TewiMP.Services.Storage;

namespace TewiMP.UI.Controls
{
    public sealed partial class MusicDataFlyout : UserControl
    {
        public ArrayList arrayList { get; set; }
        MusicDataViewModel songItemBind = null;
        public MusicDataViewModel SongItemBind
        {
            get => songItemBind;
            set
            {
                songItemBind = value;
            }
        }

        public MusicDataFlyout()
        {
            InitializeComponent();
            //arrayList = new ArrayList(10000000);
        }

        void Init()
        {
            TitleTextblock.Text = songItemBind.MusicData.Title;
            AlbumItem.Text = $"专辑：{songItemBind.MusicData.Album.Title}";
        }

        void InitFlyout()
        {
            if (songItemBind.MusicListData?.ListDataType == DataType.Playlist || songItemBind.MusicListData?.ListDataType == DataType.LocalPlaylist)
            {
                DeleteFromPlaylistItem.Visibility = Visibility.Visible;
                if (songItemBind.MusicData.From == MusicFrom.localMusic)
                    DeleteFromPlaylistAndLocalFileItem.Visibility = Visibility.Visible;
                else
                    DeleteFromPlaylistAndLocalFileItem.Visibility = Visibility.Collapsed;
            }
            else
            {
                DeleteFromPlaylistItem.Visibility = Visibility.Collapsed;
                DeleteFromPlaylistAndLocalFileItem.Visibility = Visibility.Collapsed;
            }

            if (songItemBind.MusicData.From == MusicFrom.localMusic)
            {
                LinkItem.Visibility = Visibility.Collapsed;
                ExploreLocalFileItem.Visibility = Visibility.Visible;
            }
            else
            {
                LinkItem.Visibility = Visibility.Visible;
                ExploreLocalFileItem.Visibility = Visibility.Collapsed;
            }
        }

        public void ShowAt(FrameworkElement element)
        {
            if (songItemBind is null) return;
            if (songItemBind.MusicData is null) return;
            root.ShowAt(element);
        }
        
        public void ShowAt(UIElement element, Point point)
        {
            if (songItemBind is null) return;
            if (songItemBind.MusicData is null) return;
            root.ShowAt(element, point);
        }
        
        public void ShowAt(DependencyObject element, FlyoutShowOptions flyoutShowOptions)
        {
            if (songItemBind is null) return;
            if (songItemBind.MusicData is null) return;
            root.ShowAt(element, flyoutShowOptions);
        }

        private void root_Opened(object sender, object e)
        {
            if (songItemBind is null) return;
            if (songItemBind.MusicData is null) return;
            Init();
            InitFlyout();
        }

        private void root_Closed(object sender, object e)
        {

        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            songItemBind = null;
        }

        private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var menuFlyoutItem = sender as MenuFlyoutItem;
            switch (menuFlyoutItem.Tag as string)
            {
                case "play":
                    await App.Instance.PlayingListService.Play(songItemBind.MusicData, true);
                    break;
                case "addToPlayingList":
                    App.Instance.PlayingListService.Add(songItemBind.MusicData);
                    break;
                case "setToNextPlay":
                    App.Instance.PlayingListService.SetNextPlay(App.Instance.AudioService.MusicData, songItemBind.MusicData);
                    break;
                case "deleteFromPlaylist":
                    if (songItemBind.MusicListData.ListDataType == DataType.LocalPlaylist || songItemBind.MusicListData.ListDataType == DataType.Playlist)
                    {
                        await PlayListHelper.DeleteMusicDataFromPlayList(songItemBind.MusicListData.ListName, songItemBind.MusicData);
                        await App.Instance.PlayListReader.Refresh();
                    }
                    break;
                case "deleteFile":
                    var result = await App.MainWindowInstance.ShowDialog("删除音频文件", $"确定要删除 \"{songItemBind.MusicData.Title}\" 吗？此操作不可恢复。", "取消", "确定", null, ContentDialogButton.Close);
                    if (result == ContentDialogResult.Primary)
                    {
                        if (songItemBind.MusicListData.ListDataType == DataType.LocalPlaylist || songItemBind.MusicListData.ListDataType == DataType.Playlist)
                        {
                            string deletePath = songItemBind.MusicData.InLocal;
                            await Task.Run(() => File.Delete(deletePath));
                            await PlayListHelper.DeleteMusicDataFromPlayList(songItemBind.MusicListData.ListName, songItemBind.MusicData);
                            await App.Instance.PlayListReader.Refresh();
                        }
                    }
                    break;
                case "album":
                    Pages.ListViewPages.ListViewPage.SetPageToListViewPage(new() { PageType = Pages.ListViewPages.PageType.Album, Param = songItemBind.MusicData.Album });
                    break;
                case "download":
                    App.Instance.DownloadService.Add(songItemBind.MusicData);
                    break;
                case "search_software":
                    App.MainWindowInstance.SetNavViewContent(typeof(SearchPage), songItemBind.MusicData.Title);
                    break;
                case "search_websiteSearch":
                    await CodeHelper.OpenInBrowser($"https://www.bing.com/search?q={songItemBind.MusicData.Title}-{songItemBind.MusicData.Album}");
                    break;
                case "search_website":
                    string url = null;
                    switch (songItemBind.MusicData.From)
                    {
                        case MusicFrom.pluginMusicSource:
                            url = $"https://music.163.com/#/song?id={songItemBind.MusicData.ID}";
                            break;
                    }

                    if (url != null)
                    {
                        var success = await CodeHelper.OpenInBrowser(url);
                    }
                    break;
                case "search_copy":
                    var dp = new DataPackage();
                    dp.RequestedOperation = DataPackageOperation.Copy;
                    dp.SetText(songItemBind.MusicData.Title);
                    Clipboard.SetContent(dp);
                    break;
                case "link":
                    var link = await songItemBind.MusicData.GetMusicSourcePlugin().GetUrl(songItemBind.MusicData.ID, (int)DataFolderBase.DownloadQuality.lossless);
                    App.MainWindowInstance.HideDialog();
                    await App.MainWindowInstance.ShowDialog("获取到的链接是：", link);
                    break;
                case "exploreLocalFile":
                    await FileHelper.ExploreFile(songItemBind.MusicData.InLocal);
                    break;
                case "openWithOtherSoftware":
                    await FileHelper.OpenInOtherSoftware(new Uri(songItemBind.MusicData.InLocal), new() { DisplayApplicationPicker = true });
                    break;
                case "cache":
                    if (await App.Instance.CacheService.GetCachePath(songItemBind.MusicData) is not null)
                    {
                        App.MainWindowInstance.AddNotify($"此歌曲已缓存！", null, NotifySeverity.Warning);
                        return;
                    }
                    item = App.MainWindowInstance.AddNotify($"正在缓存：{songItemBind.MusicData.Title}", "加载中...", NotifySeverity.Loading, TimeSpan.MaxValue);
                    App.Instance.CacheService.CachingStateChangeMusicData -= CacheManager_CachingStateChangeMusicData;
                    App.Instance.CacheService.CachingStateChangeMusicData += CacheManager_CachingStateChangeMusicData;
                    App.Instance.CacheService.CachedMusicData -= CacheManager_CachedMusicData;
                    App.Instance.CacheService.CachedMusicData += CacheManager_CachedMusicData;
                    await App.Instance.CacheService.StartCacheMusic(songItemBind.MusicData);
                    break;
                case "cacheDelete":
                    var path = await App.Instance.CacheService.GetCachePath(songItemBind.MusicData);
                    if (string.IsNullOrEmpty(path))
                    {
                        App.MainWindowInstance.AddNotify("此歌曲的缓存文件不存在。", null, NotifySeverity.Error);
                        return;
                    }

                    var itema = App.MainWindowInstance.AddNotify($"正在删除：{songItemBind.MusicData.Title}", null, NotifySeverity.Loading, TimeSpan.MaxValue);
                    Exception err = null;
                    try
                    {
                        await Task.Run(() => File.Delete(path));
                    }
                    catch (Exception ex)
                    {
                        err = ex;
                        itema.SetNotifyItemData("删除失败。", null, NotifySeverity.Error);
                    }
                    if (err is null)
                    {
                        itema.SetNotifyItemData("删除成功。", null, NotifySeverity.Complete);
                    }
                    App.MainWindowInstance.NotifyCountDown(itema);
                    break;
                case "info":
                    await App.MainWindowInstance.ShowDialog(
                        $"{songItemBind.MusicData.Title} 的详细信息：",
                        $"标题：{songItemBind.MusicData.Title}\n" +
                            $"艺术家&专辑：{songItemBind.MusicData.ButtonName}\n" +
                            $"ID：{songItemBind.MusicData.ID}\n" +
                            $"来源：{songItemBind.MusicData.From}" +
                                $"{(songItemBind.MusicData.GetMusicSourcePlugin() is not null ? $" {songItemBind.MusicData.GetMusicSourcePlugin().PluginInfo.Name}" : "")}" +
                            $"\n图片地址：{songItemBind.MusicData.Album.PicturePath}");
                    break;
            }
        }

        NotifyItem item = null;
        private void CacheManager_CachingStateChangeMusicData(MusicData musicData, object value)
        {
            if (musicData != songItemBind?.MusicData) return;
            item.SetProcess(100, (int)value);
            item.SetNotifyItemData(item.GetNotifyItemData().Title, $"{value}%", NotifySeverity.Loading);
        }

        private void CacheManager_CachedMusicData(MusicData musicData, object value)
        {
            if (musicData != songItemBind?.MusicData) return;
            App.Instance.CacheService.CachingStateChangeMusicData -= CacheManager_CachingStateChangeMusicData;
            App.Instance.CacheService.CachedMusicData -= CacheManager_CachedMusicData;
            item.SetNotifyItemData(item.GetNotifyItemData().Title, "缓存完成。", NotifySeverity.Complete);
            App.MainWindowInstance.NotifyCountDown(item);
            item = null;
        }

        private async void AddToPlayListSubItems_Loaded(object sender, RoutedEventArgs e)
        {
            AddToPlayListSubItems.Items.Clear();
            var mls = await PlayListHelper.ReadAllPlayList();
            foreach (var item in mls)
            {
                var menuItem = new MenuFlyoutItem()
                {
                    Text = item.ListShowName,
                    Tag = item
                };
                menuItem.Click += MenuItem_Click;
                AddToPlayListSubItems.Items.Add(menuItem);
            }
        }

        private void AddToPlayListSubItems_Unloaded(object sender, RoutedEventArgs e)
        {
            foreach (MenuFlyoutItem item in AddToPlayListSubItems.Items)
            {
                item.Tag = null;
                item.Click -= MenuItem_Click;
            }
            AddToPlayListSubItems.Items.Clear();
        }

        private async void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            await PlayListHelper.AddMusicDataToPlayList(((sender as FrameworkElement).Tag as MusicListData).ListName, songItemBind.MusicData);
        }

        private void ArtistItem_Loaded(object sender, RoutedEventArgs e)
        {
            ArtistItem.Items.Clear();
            foreach (var artist in songItemBind.MusicData.Artists)
            {
                var mfi = new MenuFlyoutItem()
                {
                    Text = artist.Name,
                    Tag = artist
                };
                mfi.Click += Mfi_Click;
                ArtistItem.Items.Add(mfi);
            }

        }

        private void ArtistItem_Unloaded(object sender, RoutedEventArgs e)
        {
            foreach (MenuFlyoutItem item in ArtistItem.Items)
            {
                item.Tag = null;
                item.Click -= Mfi_Click;
            }
            ArtistItem.Items.Clear();
        }

        private void Mfi_Click(object sender, RoutedEventArgs e)
        {
            Pages.ListViewPages.ListViewPage.SetPageToListViewPage(new() { PageType = Pages.ListViewPages.PageType.Artist, Param = (sender as FrameworkElement).Tag });
        }
    }
}
