using System;
using System.IO;
using System.Collections;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.System;
using Windows.Storage;
using Windows.Foundation;
using TewiMP.Pages;
using TewiMP.Helpers;
using TewiMP.DataEditor;

namespace TewiMP.Controls
{
    public sealed partial class MusicDataFlyout : UserControl
    {
        public ArrayList arrayList { get; set; }
        SongItemBindBase songItemBind = null;
        public SongItemBindBase SongItemBind
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
            if (songItemBind.MusicListData?.ListDataType == DataType.歌单 || songItemBind.MusicListData?.ListDataType == DataType.本地歌单)
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
                    await App.playingList.Play(songItemBind.MusicData, true);
                    break;
                case "addToPlayingList":
                    App.playingList.Add(songItemBind.MusicData);
                    break;
                case "setToNextPlay":
                    App.playingList.SetNextPlay(App.audioPlayer.MusicData, songItemBind.MusicData);
                    break;
                case "deleteFromPlaylist":
                    if (songItemBind.MusicListData.ListDataType == DataType.本地歌单 || songItemBind.MusicListData.ListDataType == DataType.歌单)
                    {
                        await PlayListHelper.DeleteMusicDataFromPlayList(songItemBind.MusicListData.ListName, songItemBind.MusicData);
                        await App.playListReader.Refresh();
                    }
                    break;
                case "deleteFile":
                    if (songItemBind.MusicListData.ListDataType == DataType.本地歌单 || songItemBind.MusicListData.ListDataType == DataType.歌单)
                    {
                        string deletePath = songItemBind.MusicData.InLocal;
                        await Task.Run(() => File.Delete(deletePath));
                        await PlayListHelper.DeleteMusicDataFromPlayList(songItemBind.MusicListData.ListName, songItemBind.MusicData);
                        await App.playListReader.Refresh();
                    }
                    break;
                case "album":
                    Pages.ListViewPages.ListViewPage.SetPageToListViewPage(new() { PageType = Pages.ListViewPages.PageType.Album, Param = songItemBind.MusicData.Album });
                    break;
                case "download":
                    App.downloadManager.Add(songItemBind.MusicData);
                    break;
                case "search_software":
                    MainWindow.SetNavViewContent(typeof(SearchPage), songItemBind.MusicData.Title);
                    break;
                case "search_websiteSearch":
                    await CodeHelper.OpenInBrowser($"https://www.bing.com/search?q={songItemBind.MusicData.Title}-{songItemBind.MusicData.Album}");
                    break;
                case "search_website":
                    string url = null;
                    switch (songItemBind.MusicData.From)
                    {
                        case MusicFrom.neteaseMusic:
                            url = $"https://music.163.com/#/song?id={songItemBind.MusicData.ID}";
                            break;
                    }

                    if (url != null)
                    {
                        var success = await CodeHelper.OpenInBrowser(url);
                    }
                    break;
                case "search_copy":
                    var dp = new Windows.ApplicationModel.DataTransfer.DataPackage();
                    dp.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
                    dp.SetText(songItemBind.MusicData.Title);
                    Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);
                    break;
                case "link":
                    var link = await App.metingServices.NeteaseServices.GetUrl(songItemBind.MusicData.ID, (int)DataFolderBase.DownloadQuality.lossless);
                    MainWindow.HideDialog();
                    await MainWindow.ShowDialog("获取到的链接是：", link);
                    break;
                case "exploreLocalFile":
                    await FileHelper.ExploreFile(songItemBind.MusicData.InLocal);
                    break;
                case "openWithOtherSoftware":
                    await FileHelper.OpenInOtherSoftware(new Uri(songItemBind.MusicData.InLocal), new() { DisplayApplicationPicker = true });
                    break;
                case "cache":
                    if (await App.cacheManager.GetCachePath(songItemBind.MusicData) is not null)
                    {
                        MainWindow.AddNotify($"此歌曲已缓存！", null, NotifySeverity.Warning);
                        return;
                    }
                    item = MainWindow.AddNotify($"正在缓存：{songItemBind.MusicData.Title}", "加载中...", NotifySeverity.Loading, TimeSpan.MaxValue);
                    App.cacheManager.CachingStateChangeMusicData -= CacheManager_CachingStateChangeMusicData;
                    App.cacheManager.CachingStateChangeMusicData += CacheManager_CachingStateChangeMusicData;
                    App.cacheManager.CachedMusicData -= CacheManager_CachedMusicData;
                    App.cacheManager.CachedMusicData += CacheManager_CachedMusicData;
                    await App.cacheManager.StartCacheMusic(songItemBind.MusicData);
                    break;
                case "cacheDelete":
                    var path = await App.cacheManager.GetCachePath(songItemBind.MusicData);
                    if (string.IsNullOrEmpty(path))
                    {
                        MainWindow.AddNotify("此歌曲的缓存文件不存在。", null, NotifySeverity.Error);
                        return;
                    }

                    var itema = MainWindow.AddNotify($"正在删除：{songItemBind.MusicData.Title}", null, NotifySeverity.Loading, TimeSpan.MaxValue);
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
                    MainWindow.NotifyCountDown(itema);
                    break;
                case "info":
                    await MainWindow.ShowDialog($"{songItemBind.MusicData.Title} 的详细信息：", $"标题：{songItemBind.MusicData.Title}\n艺术家&专辑：{songItemBind.MusicData.ButtonName}\nID：{songItemBind.MusicData.ID}\n来源：{songItemBind.MusicData.From}\n图片地址：{songItemBind.MusicData.Album.PicturePath}");
                    break;
            }
        }

        NotifyItem item = null;
        private void CacheManager_CachingStateChangeMusicData(MusicData musicData, object value)
        {
            if (musicData != songItemBind.MusicData) return;
            item.SetProcess(100, (int)value);
            item.SetNotifyItemData(item.GetNotifyItemData().Title, $"{value}%", NotifySeverity.Loading);
        }

        private void CacheManager_CachedMusicData(MusicData musicData, object value)
        {
            if (musicData != songItemBind.MusicData) return;
            App.cacheManager.CachingStateChangeMusicData -= CacheManager_CachingStateChangeMusicData;
            App.cacheManager.CachedMusicData -= CacheManager_CachedMusicData;
            item.SetNotifyItemData(item.GetNotifyItemData().Title, "缓存完成。", NotifySeverity.Complete);
            MainWindow.NotifyCountDown(item);
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
