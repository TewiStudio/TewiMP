using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Composition;
using TewiMP.Helpers;
using TewiMP.Controls;
using TewiMP.DataEditor;
using TewiMP.Pages.ListViewPages;
using CommunityToolkit.WinUI;
using System.Collections.Generic;

namespace TewiMP.Pages
{
    public partial class ItemListViewSearch : Page, IPage
    {
        public bool IsNavigatedOutFromPage { get; set; } = false;
        private ScrollViewer scrollViewer { get; set; }
        public object NavToObj { get; set; }
        public SearchDataType NowSearchMode { get; set; } = SearchDataType.歌曲;
        public MusicFrom NowMusicFrom { get; set; } = MusicFrom.neteaseMusic;
        MusicListData musicListData;

        public ItemListViewSearch()
        {
            InitializeComponent();
            DataContext = this;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //PlayAllButton.Foreground = new SolidColorBrush(CodeHelper.IsAccentColorDark() ? Colors.White : Colors.Black);
            base.OnNavigatedTo(e);
            IsNavigatedOutFromPage = false;
            var a =  ((PageData)e.Parameter).Param as SearchData;
            NavToObj = a.Key;
            NowMusicFrom = a.From;
            NowSearchMode = a.SearchDataType;
            musicListData = new() { ListDataType = DataType.歌曲 };
            UpdateShyHeader();
            InitData();
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            IsNavigatedOutFromPage = true;
            await Task.Delay(500);
            scrollViewer?.ScrollToVerticalOffset(0);
            MusicDataList.Clear();
            Children.ItemsSource = null;
            Children.Items.Clear();
            UnloadObject(this);
        }

        public ObservableCollection<SongItemBindBase> MusicDataList = new();
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

            SearchResult_BaseGrid.Visibility = Visibility.Visible;
            SearchPageSelector.Visibility = Visibility.Visible;
            SearchPageSelectorCustom.Visibility = Visibility.Visible;
            SearchHomeButton.Visibility = Visibility.Visible;
            var searchData = NavToObj as string;
            Result_Search_Header.Text = $"\"{searchData}\"的搜索结果";
            NowPage.Text = pageNumber.ToString();

            MusicDataList.Clear();

            bool isComplete = false;
            while (!isComplete)
            {
                try
                {
                    searchDatas = await WebHelper.SearchData(searchData, pageNumber, pageSize, NowMusicFrom, NowSearchMode);
                    isComplete = true;
                }
                catch (ArgumentOutOfRangeException)
                {
                    MainWindow.AddNotify("不支持的平台", "当前不支持此平台搜索。", NotifySeverity.Error);
                    searchDatas = null;
                    break;
                }
                catch (NullReferenceException)
                {
                    MainWindow.AddNotify("搜索失败", "无相关结果。", NotifySeverity.Error);
                    searchDatas = null;
                    break;
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLog("SearchError", ex.ToString(), false);
                    string errString = $"搜索时出现错误：\n{ex.Message}";
                    var d = await MainWindow.ShowDialog("搜索失败", errString, "重试", "确定", defaultButton: ContentDialogButton.Primary);
                    if (d == ContentDialogResult.Primary)
                    {
                        searchDatas = null;
                        break;
                    }
                }
            }

            if (IsNavigatedOutFromPage) return;

            if (searchDatas != null)
            {
                MusicDataList.Clear();

                var dpi = CodeHelper.GetScaleAdjustment(App.WindowLocal);

                var count = 0;
                switch (NowSearchMode)
                {
                    case SearchDataType.歌曲:
                        Children.ItemsSource = MusicDataList;
                        Children.ItemTemplate = this.Resources["SongItemTemplate"] as DataTemplate;

                        MusicData[] array = (searchDatas as MusicListData).Songs.ToArray();
                        count = pageNumber * pageSize - pageSize;
                        foreach (var i in array)
                        {
                            count++;
                            i.Count = count;
                            MusicDataList.Add(new() { MusicData = i, MusicListData = musicListData, ImageScaleDPI = dpi });
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
                MainWindow.AddNotify("搜索失败", "无相关结果。", NotifySeverity.Error);
            }

            LoadingTipControl.UnShowLoading();
            UpdateShyHeader();
        }

        CompositionPropertySet scrollerPropertySet;
        Compositor compositor;
        Visual headerVisual;
        Visual backgroundVisual;
        Visual logoVisual;
        Visual stackVisual;
        Visual headerFootRootVisual;
        public void UpdateShyHeader()
        {
            if (scrollViewer is null) return;

            double anotherHeight = HeaderBaseGrid.ActualHeight;
            String progress = $"Clamp(-scroller.Translation.Y / {anotherHeight}, 0, 1.0)";

            if (scrollerPropertySet is null)
            {
                scrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
                compositor = scrollerPropertySet.Compositor;
                headerVisual = ElementCompositionPreview.GetElementVisual(menu_border);
                backgroundVisual = ElementCompositionPreview.GetElementVisual(BackColorBaseRectangle);
                headerFootRootVisual = ElementCompositionPreview.GetElementVisual(ItemsList_Header_Foot_Root);
            }

            var offsetExpression = compositor.CreateExpressionAnimation($"-scroller.Translation.Y - {progress} * {anotherHeight}");
            offsetExpression.SetReferenceParameter("scroller", scrollerPropertySet);
            headerVisual.StartAnimation("Offset.Y", offsetExpression);

            var backgroundVisualOpacityAnimation = compositor.CreateExpressionAnimation($"Lerp(0, 1, {progress})");
            backgroundVisualOpacityAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            backgroundVisual.StartAnimation("Opacity", backgroundVisualOpacityAnimation);

            var headerFootRootVisualOffsetAnimation = compositor.CreateExpressionAnimation(
                $"Lerp(" +
                    $"Vector3(" +
                        $"-16," +
                        $"{ActualHeight} - {headerFootRootVisual.Size.Y} - 8," +
                        $"0)," +
                    $"Vector3(" +
                        $"-16," +
                        $"{anotherHeight} + {ActualHeight} - {headerFootRootVisual.Size.Y} - 8," +
                        $"0)," +
                    $"{progress})");
            headerFootRootVisualOffsetAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            headerFootRootVisual.StartAnimation("Offset", headerFootRootVisualOffsetAnimation);
        }

        private async void UpdateCommandToolBarWidth()
        {
            ToolsCommandBar.Width = 0;
            await Task.Delay(1);
            ToolsCommandBar.Width = double.NaN;
        }

        private async void PositionToButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            switch ((ScrollFootButton.ButtonType)btn.Tag)
            {
                case ScrollFootButton.ButtonType.NowPlaying:
                    foreach (var i in MusicDataList)
                    {
                        if (i.MusicData != App.audioPlayer.MusicData) continue;
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
            ItemsList_Header_Foot_Buttons.PositionToBottom_Button.Click -= PositionToButton_Click;
            ItemsList_Header_Foot_Buttons.PositionToBottom_Button.Click += PositionToButton_Click;
            ItemsList_Header_Foot_Buttons.PositionToNowPlaying_Button.Click -= PositionToButton_Click;
            ItemsList_Header_Foot_Buttons.PositionToNowPlaying_Button.Click += PositionToButton_Click;
            ItemsList_Header_Foot_Buttons.PositionToTop_Button.Click -= PositionToButton_Click;
            ItemsList_Header_Foot_Buttons.PositionToTop_Button.Click += PositionToButton_Click;

            if (scrollViewer is null)
            {
                scrollViewer = (VisualTreeHelper.GetChild(Children, 0) as Border).Child as ScrollViewer;
                scrollViewer.CanContentRenderOutsideBounds = true;
                scrollViewer.ViewChanging += ScrollViewer_ViewChanging;

                // 设置header为顶层
                var headerPresenter = (UIElement)VisualTreeHelper.GetParent((UIElement)Children.Header);
                var headerContainer = (UIElement)VisualTreeHelper.GetParent(headerPresenter);
                Canvas.SetZIndex(headerContainer, 1);
            }

            UpdateShyHeader();
            UpdateCommandToolBarWidth();
        }

        private void Artist_Image_Unloaded(object sender, RoutedEventArgs e)
        {
            ItemsList_Header_Foot_Buttons.PositionToBottom_Button.Click -= PositionToButton_Click;
            ItemsList_Header_Foot_Buttons.PositionToNowPlaying_Button.Click -= PositionToButton_Click;
            ItemsList_Header_Foot_Buttons.PositionToTop_Button.Click -= PositionToButton_Click;
        }

        private void ScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            headerVisual.IsPixelSnappingEnabled = true;
        }

        private void Result_BaseGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateShyHeader();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!Children.Items.Any()) return;
            if (App.playingList.PlayBehavior == TewiMP.Background.PlayBehavior.随机播放)
            {
                App.playingList.ClearAll();
            }
            foreach (var songItem in MusicDataList)
            {
                App.playingList.Add(songItem.MusicData, false);
            }
            await App.playingList.Play(MusicDataList.First().MusicData, true);
            App.playingList.SetRandomPlay(App.playingList.PlayBehavior);
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
            MainWindow.AllowDragEvents = SelectItemButton.IsChecked == false;
            UpdateCommandToolBarWidth();
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
                foreach (SongItemBindBase item in Children.SelectedItems)
                {
                    App.playingList.Add(item.MusicData);
                }
            }
        }

        private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (Children.SelectedItems.Any())
            {
                foreach (SongItemBindBase item in Children.SelectedItems)
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
            foreach (SongItemBindBase item in Children.Items)
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
                foreach (SongItemBindBase songItem in Children.Items)
                {
                    App.downloadManager.Add(songItem.MusicData);
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
            MainWindow.ShowLoadingDialog();
            var text = await PlayListHelper.ReadData();
            foreach (SongItemBindBase item in Children.SelectedItems)
            {
                MainWindow.SetLoadingText($"正在添加：{item.MusicData.Title} - {item.MusicData.ButtonName}");
                
                text = PlayListHelper.AddMusicDataToPlayList(
                    ((sender as MenuFlyoutItem).Tag as MusicListData).ListName,
                    item.MusicData, text);
            }
            await PlayListHelper.SaveData(text);
            MainWindow.HideDialog();
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
                        if (i.MusicData == App.audioPlayer.MusicData)
                        {
                            await Children.SmoothScrollIntoViewWithItemAsync(i, ScrollItemPlacement.Center);
                            await Children.SmoothScrollIntoViewWithItemAsync(i, ScrollItemPlacement.Center, true);
                            foreach (var j in SongItem.StaticSongItems)
                            {
                                if (j != null)
                                    if (j.MusicData == App.audioPlayer.MusicData)
                                        j.AnimateStroke();
                            }
                        }
                    }
                    break;
            }
        }
    }
}
