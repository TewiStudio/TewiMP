using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Composition;
using CommunityToolkit.WinUI;
using TewiMP.UI.Controls;
using TewiMP.UI.Windows;
using TewiMP.UI.Pages.ListViewPages;
using TewiMP.Core;
using TewiMP.Core.Music;
using TewiMP.Core.Models;
using TewiMP.Services;
using TewiMP.Services.Plugin;
using TewiMP.Services.Storage;

namespace TewiMP.UI.Pages
{
    public partial class ItemListViewSearch : Page
    {
        private ScrollViewer scrollViewer => StickContentHeader.CachedScrollViewer;
        public object NavToObj { get; set; }
        public SearchDataType NowSearchMode { get; set; } = SearchDataType.歌曲;
        public MusicSourcePlugin NowMusicFrom { get; set; }
        MusicListData musicListData;

        public ItemListViewSearch()
        {
            InitializeComponent();
        }

        SearchData searchData { get; set; }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //PlayAllButton.Foreground = new SolidColorBrush(CodeHelper.IsAccentColorDark() ? Colors.White : Colors.Black);
            base.OnNavigatedTo(e);
            searchData =  ((PageData)e.Parameter).Param as SearchData;
            pageNumber = searchData.PageNumber;
            pageSize = searchData.PageSize;
            NavToObj = searchData.Key;
            NowMusicFrom = searchData.SourcePlugin;
            NowSearchMode = searchData.SearchDataType;
            musicListData = new() { ListDataType = DataType.Song };
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        public ObservableCollection<MusicDataViewModel> MusicDataList = new();
        public ObservableCollection<SearchItemBindBase> SearchList = new();
        object searchDatas = null;
        static bool firstInit = false;
        int pageNumber = 1;
        int pageSize = 30;
        public async void InitData()
        {
            LoadingTipControl.ShowLoading();
            SelectorSeparator.Visibility = Visibility.Collapsed;
            AddSelectedToPlayingListButton.Visibility = Visibility.Collapsed;
            AddSelectedToPlayListButton.Visibility = Visibility.Collapsed;
            DownloadSelectedButton.Visibility = Visibility.Collapsed;
            DeleteSelectedButton.Visibility = Visibility.Collapsed;
            SelectReverseButton.Visibility = Visibility.Collapsed;
            SelectAllButton.Visibility = Visibility.Collapsed;
            SearchHomeButton.Visibility = Visibility.Collapsed;
            SearchPageSelectorSeparator.Visibility = Visibility.Collapsed;

            SearchPageSelector.Visibility = Visibility.Collapsed;
            SearchPageSelectorCustom.Visibility = Visibility.Collapsed;

            SearchPageSelector.Visibility = Visibility.Visible;
            SearchPageSelectorCustom.Visibility = Visibility.Visible;
            SearchHomeButton.Visibility = Visibility.Visible;
            var searchData = NavToObj as string;
            //StickContentHeader.Title = $"\"{searchData}\" {NowSearchMode}的搜索结果";
            NowPage.Text = pageNumber.ToString();

            MusicDataList.Clear();
            SearchList.Clear();

            try
            {
                searchDatas = await NowMusicFrom.GetSearch(searchData, pageNumber, pageSize, (int)NowSearchMode);
            }
            catch (NullReferenceException)
            {
                App.MainWindowInstance.AddNotify("搜索失败", "无相关结果。", NotifySeverity.Error);
                searchDatas = null;
            }
            catch (Exception ex)
            {
                LogService.Error("SearchError", ex.ToString());
                string errString = $"搜索时出现错误：\n{ex.Message}";
                var d = await App.MainWindowInstance.ShowDialog("搜索失败", errString, "重试", "确定", defaultButton: ContentDialogButton.Primary);
                if (d == ContentDialogResult.Primary)
                {
                    searchDatas = null;
                }
            }

            if (!IsLoaded) return;

            if (searchDatas != null)
            {
                MusicDataList.Clear();
                SearchList.Clear();

                var count = pageNumber * pageSize - pageSize;
                switch (NowSearchMode)
                {
                    case SearchDataType.歌曲:
                        Children.ItemsSource = MusicDataList;
                        Children.ItemTemplate = this.Resources["SongItemTemplate"] as DataTemplate;

                        var musicListData = searchDatas as MusicListData;
                        foreach (var i in musicListData.Songs)
                        {
                            MusicDataList.Add(new(i, musicListData, ++count));
                        }
                        break;
                    default:
                        Children.ItemsSource = SearchList;
                        Children.ItemTemplate = this.Resources["SearchItemTemplate"] as DataTemplate;

                        if (NowSearchMode == SearchDataType.艺术家)
                        {
                            foreach (var i in searchDatas as List<Artist>)
                            {
                                count++;
                                SearchList.Add(new()
                                {
                                    DataType = SearchBindDataType.Artist,
                                    Artist = i,
                                    Count = count
                                });
                            }
                        }
                        else if (NowSearchMode == SearchDataType.专辑)
                        {
                            foreach (var i in searchDatas as List<Album>)
                            {
                                count++;
                                SearchList.Add(new()
                                {
                                    DataType = SearchBindDataType.Album,
                                    Album = i,
                                    Count = count
                                });
                            }
                        }
                        else if (NowSearchMode == SearchDataType.歌单)
                        {
                            foreach (var i in searchDatas as List<object[]>)
                            {
                                count++;
                                SearchList.Add(new()
                                {
                                    DataType = SearchBindDataType.PlayList,
                                    PlayList = i[0] as MusicListData,
                                    PlayList_Count = (int)i[1],
                                    Count = count
                                });
                            }
                        }
                        break;
                }
            }
            else
            {
                App.MainWindowInstance.AddNotify("搜索失败", "无相关结果。", NotifySeverity.Error);
            }

            LoadingTipControl.UnShowLoading();
        }

        private async void ItemsList_Header_Foot_Buttons_PositionButtonClick(object sender, RoutedEventArgs e)
        {
            switch ((ScrollFootButton.ButtonType)sender)
            {
                case ScrollFootButton.ButtonType.NowPlaying:
                    foreach (var i in MusicDataList)
                    {
                        if (i.MusicData != App.Instance.AudioService.MusicData) continue;
                        await Children.SmoothScrollIntoViewWithItemAsync(i, ScrollItemPlacement.Center);
                        await Children.SmoothScrollIntoViewWithItemAsync(i, ScrollItemPlacement.Center, disableAnimation: true);
                        MusicDataItem.TryHighlightPlayingItem();
                    }
                    break;
                case ScrollFootButton.ButtonType.Top:
                    scrollViewer.ChangeView(null, 0, null);
                    break;
                case ScrollFootButton.ButtonType.Bottom:
                    scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null);
                    break;
            }
        }

        private void menu_border_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Artist_Image_Unloaded(object sender, RoutedEventArgs e)
        {
        }

        private void Result_BaseGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!Children.Items.Any()) return;
            if (App.Instance.PlayingListService.PlayBehavior == TewiMP.Services.PlayBehavior.随机播放)
            {
                App.Instance.PlayingListService.ClearAll();
            }
            foreach (var songItem in MusicDataList)
            {
                App.Instance.PlayingListService.Add(songItem.MusicData, false);
            }
            await App.Instance.PlayingListService.Play(MusicDataList.First().MusicData, true);
            App.Instance.PlayingListService.SetRandomPlay(App.Instance.PlayingListService.PlayBehavior);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            InitData();
        }

        DropShadow dropShadow;
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (SelectItemButton.IsChecked == true)
            {
                PlayAllButton.Visibility = Visibility.Collapsed;
                RefreshButton.Visibility = Visibility.Collapsed;
                SearchHomeButton.Visibility = Visibility.Collapsed;
                SearchPageSelectorCustom.Visibility = Visibility.Collapsed;
                SearchPageSelector.Visibility = Visibility.Collapsed;

                SelectorSeparator.Visibility = Visibility.Visible;
                AddSelectedToPlayingListButton.Visibility = Visibility.Visible;
                AddSelectedToPlayListButton.Visibility = Visibility.Visible;
                DeleteSelectedButton.Visibility = Visibility.Visible;
                DownloadSelectedButton.Visibility = Visibility.Visible;
                SelectReverseButton.Visibility = Visibility.Visible;
                SelectAllButton.Visibility = Visibility.Visible;

                Children.SelectionMode = ListViewSelectionMode.Multiple;
                Children.AllowDrop = true;
                Children.CanReorderItems = true;
            }
            else
            {
                PlayAllButton.Visibility = Visibility.Visible;
                RefreshButton.Visibility = Visibility.Visible;
                SearchHomeButton.Visibility = Visibility.Visible;
                SearchPageSelectorCustom.Visibility = Visibility.Visible;
                SearchPageSelector.Visibility = Visibility.Visible;

                SelectorSeparator.Visibility = Visibility.Collapsed;
                AddSelectedToPlayingListButton.Visibility = Visibility.Collapsed;
                AddSelectedToPlayListButton.Visibility = Visibility.Collapsed;
                DeleteSelectedButton.Visibility = Visibility.Collapsed;
                DownloadSelectedButton.Visibility = Visibility.Collapsed;
                SelectReverseButton.Visibility = Visibility.Collapsed;
                SelectAllButton.Visibility = Visibility.Collapsed;

                Children.SelectionMode = ListViewSelectionMode.None;
                Children.AllowDrop = false;
                Children.CanReorderItems = false;
            }
            MusicDataItem.SetIsCloseMouseEvent(SelectItemButton.IsChecked == true);
            App.MainWindowInstance.AllowDragEvents = SelectItemButton.IsChecked == false;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            pageNumber = 1;
            InitData();
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            if (pageNumber - 1 > 0)
            {
                pageNumber--;
                InitData();
            }
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            pageNumber++;
            InitData();
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            if (PageNumberTextBox.Text != String.Empty)
                pageNumber = int.Parse(PageNumberTextBox.Text);
            else pageNumber = 1;

            if (PageSizeTextBox.Text != String.Empty)
                pageSize = int.Parse(PageSizeTextBox.Text);
            else pageSize = 30;

            InitData();
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            SearchPageSelectorCustomFlyout.Hide();
        }

        private void AddSelectedToPlayingListButton_Click(object sender, RoutedEventArgs e)
        {
            if (Children.SelectedItems.Any())
            {
                foreach (MusicDataViewModel item in Children.SelectedItems)
                {
                    App.Instance.PlayingListService.Add(item.MusicData);
                }
            }
        }

        private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (Children.SelectedItems.Any())
            {
                foreach (MusicDataViewModel item in Children.SelectedItems)
                {
                    MusicDataList.Remove(item);
                }
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            /*
            foreach (SongItem item in Children.Items)
            {
                (Children.ContainerFromIndex(Children.Items.IndexOf(item)) as ListViewItem).IsSelected = true;
            }*/
            Children.SelectAll();
        }

        private void SelectReverseButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (MusicDataViewModel item in Children.Items)
            {
                if (Children.SelectedItems.Contains(item))
                {
                    Children.SelectedItems.Remove(item);
                }
                else
                {
                    Children.SelectedItems.Add(item);
                }
            }
        }

        private void AppBarButton_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private void DownloadSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (Children.SelectedItems.Any())
            {
                foreach (MusicDataViewModel songItem in Children.Items)
                {
                    App.Instance.DownloadService.Add(songItem.MusicData);
                }
            }
        }

        private async void AddToPlayListFlyout_Opened(object sender, object e)
        {
            AddToPlayListFlyout.Items.Clear();
            foreach (var m in await PlayListHelper.ReadAllPlayList())
            {
                var a = new MenuFlyoutItem()
                {
                    Text = m.ListShowName,
                    Tag = m
                };
                a.Click += A_Click;

                AddToPlayListFlyout.Items.Add(a);
            }
        }

        private async void A_Click(object sender, RoutedEventArgs e)
        {
            App.MainWindowInstance.ShowLoadingDialog();
            var text = await PlayListHelper.ReadData();
            foreach (MusicDataViewModel item in Children.SelectedItems)
            {
                App.MainWindowInstance.SetLoadingText($"正在添加：{item.MusicData.Title} - {item.MusicData.ButtonName}");
                
                text = PlayListHelper.AddMusicDataToPlayList(
                    ((sender as MenuFlyoutItem).Tag as MusicListData).ListName,
                    item.MusicData, text);
            }
            await PlayListHelper.SaveData(text);
            App.MainWindowInstance.HideDialog();
        }

        private void AddToPlayListFlyout_Closed(object sender, object e)
        {
            //AddToPlayListFlyout.Items.Clear();
        }
        private async void Button_Click_8(object sender, RoutedEventArgs e)
        {
            switch ((sender as Button).Tag)
            {
                case "0":
                    scrollViewer.ChangeView(null, 0, null);
                    break;
                case "1":
                    scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null);
                    break;
                case "2":
                    foreach (var i in MusicDataList)
                    {
                        if (i.MusicData == App.Instance.AudioService.MusicData)
                        {
                            await Children.SmoothScrollIntoViewWithItemAsync(i, ScrollItemPlacement.Center);
                            await Children.SmoothScrollIntoViewWithItemAsync(i, ScrollItemPlacement.Center, true);
                            foreach (var j in SongItem.StaticSongItems)
                            {
                                if (j != null)
                                    if (j.MusicData == App.Instance.AudioService.MusicData)
                                        j.AnimateStroke();
                            }
                        }
                    }
                    break;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = this;
            InitData();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            searchData.PageNumber = pageNumber;
            searchData.PageSize = pageSize;
            searchData = null;

            scrollViewer?.ScrollToVerticalOffset(0);
            MusicDataList.Clear();
            Children.ItemsSource = null;
            Children.Items.Clear();
            UnloadObject(this);
        }
    }
}
