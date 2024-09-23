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
using TewiMP.Helpers;
using TewiMP.Controls;
using TewiMP.DataEditor;
using CommunityToolkit.WinUI.UI;
using TewiMP.Pages.ListViewPages;

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

                switch (NowSearchMode)
                {
                    case SearchDataType.歌曲:
                        MusicData[] array = (searchDatas as MusicListData).Songs.ToArray();
                        int count = pageNumber * pageSize - pageSize;
                        foreach (var i in array)
                        {
                            count++;
                            i.Count = count;
                            MusicDataList.Add(new() { MusicData = i, MusicListData = musicListData, ImageScaleDPI = dpi });
                        }
                        ItemPresenterControlBridge.Margin = new(14, 0, 16, 0);
                        break;
                    case SearchDataType.艺术家:
                        /*foreach (var i in searchDatas as List<Artist>)
                        {
                            var a = new Controls.ArtistCard() { Artist = i, ImageScaleDPI = dpi };
                            Children.Items.Add(a);
                        }
                        ItemPresenterControlBridge.Margin = new(14, 14, 16, 14);*/
                        break;
                }
            }
            else
            {
                MainWindow.AddNotify("搜索失败", "无相关结果。", NotifySeverity.Error);
            }

            System.Diagnostics.Debug.WriteLine("加载完成。");
            LoadingTipControl.UnShowLoading();
        }

        CompositionPropertySet scrollerPropertySet;
        Compositor compositor;
        Visual headerVisual;
        Visual backgroundVisual;
        Visual logoVisual;
        Visual stackVisual;
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
            }

            var offsetExpression = compositor.CreateExpressionAnimation($"-scroller.Translation.Y - {progress} * {anotherHeight}");
            offsetExpression.SetReferenceParameter("scroller", scrollerPropertySet);
            headerVisual.StartAnimation("Offset.Y", offsetExpression);

            var backgroundVisualOpacityAnimation = compositor.CreateExpressionAnimation($"Lerp(0, 1, {progress})");
            backgroundVisualOpacityAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            backgroundVisual.StartAnimation("Opacity", backgroundVisualOpacityAnimation);
        }

        private async void UpdateCommandToolBarWidth()
        {
            ToolsCommandBar.Width = 0;
            await Task.Delay(1);
            ToolsCommandBar.Width = double.NaN;
        }

        private void menu_border_Loaded(object sender, RoutedEventArgs e)
        {
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
