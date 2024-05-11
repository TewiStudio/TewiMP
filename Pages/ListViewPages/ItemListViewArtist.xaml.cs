using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Composition;
using TewiMP.Media;
using TewiMP.Helpers;
using TewiMP.Controls;
using TewiMP.DataEditor;
using CommunityToolkit.WinUI.UI;
using TewiMP.Pages.ListViewPages;

namespace TewiMP.Pages
{
    public partial class ItemListViewArtist : Page, IPage
    {
        public bool IsNavigatedOutFromPage { get; set; } = false;
        private ScrollViewer scrollViewer { get; set; }
        public Artist NavToObj { get; set; }
        public MusicFrom NowMusicFrom { get; set; } = MusicFrom.neteaseMusic;

        public ItemListViewArtist()
        {
            InitializeComponent();
            DataContext = this;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //PlayAllButton.Foreground = new SolidColorBrush(CodeHelper.IsAccentColorDark() ? Colors.White : Colors.Black);
            base.OnNavigatedTo(e);
            IsNavigatedOutFromPage = false;
            Artist a = ((PageData)e.Parameter).Param as Artist;
            NavToObj = a;
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
            Artist_Image.Source = null;
            musicListData = null;
            NavToObj = null;
            UnloadObject(this);
        }
/*
        private void CrateShadow()
        {
            var visual = ElementCompositionPreview.GetElementVisual(Artist_Image);
            compositor = visual.Compositor;

            var basicRectVisual = compositor.CreateSpriteVisual();
            basicRectVisual.Size = Artist_Image.RenderSize.ToVector2();

            dropShadow = compositor.CreateDropShadow();
            dropShadow.BlurRadius = 45f;
            dropShadow.Color = Colors.Black;
            dropShadow.Opacity = 0.3f;
            dropShadow.Offset = new Vector3(0, 4, 0);

            basicRectVisual.Shadow = dropShadow;
            ElementCompositionPreview.SetElementChildVisual(Artist_Image_DropShadowBase, basicRectVisual);
        }
*/
        public ObservableCollection<SongItemBindBase> MusicDataList = new();
        MusicListData musicListData = null;
        static bool firstInit = false;
        int pageNumber = 1;
        int pageSize = 30;
        public async void InitData()
        {
            SelectorSeparator.Visibility = Visibility.Collapsed;
            AddSelectedToPlayingListButton.Visibility = Visibility.Collapsed;
            AddSelectedToPlayListButton.Visibility = Visibility.Collapsed;
            DeleteSelectedButton.Visibility = Visibility.Collapsed;
            DownloadSelectedButton.Visibility = Visibility.Collapsed;
            SelectReverseButton.Visibility = Visibility.Collapsed;
            SelectAllButton.Visibility = Visibility.Collapsed;
            LoadingTipControl.ShowLoading();
            var obj = await App.metingServices.NeteaseServices.GetArtist(NavToObj.ID);
            if (obj == null)
            {
                MainWindow.AddNotify("加载艺术家信息时出现错误", "无法加载艺术家信息，请重试。", NotifySeverity.Error);
                return;
            }
            if (IsNavigatedOutFromPage) return;
            NavToObj = obj;
            musicListData = NavToObj.HotSongs;
            Artist_SmallName.Text = string.IsNullOrEmpty(NavToObj.Name2) ? NavToObj.Name : $"{NavToObj.Name}（{NavToObj.Name2}）";
            //ToolTipService.SetToolTip(Artist_Info, NavToObj.Describee);

            if (musicListData != null)
            {
                LoadImage();
                await Task.Delay(100);
                var dpi = CodeHelper.GetScaleAdjustment(App.WindowLocal);

                MusicDataList.Clear();
                int count = 0;
                foreach (var i in musicListData.Songs)
                {
                    count++;
                    i.Count = count;
                    MusicDataList.Add(new() { MusicData = i, MusicListData = musicListData, ImageScaleDPI = dpi });
                }
            }
            LoadingTipControl.UnShowLoading();
        }

        private async void LoadImage()
        {
            if (musicListData.ListDataType == DataType.本地歌单)
            {
                Artist_Image.Source = await FileHelper.GetImageSource(musicListData.PicturePath);
            }
            else if (musicListData.ListDataType == DataType.歌单)
            {
                Artist_Image.Source = (await ImageManage.GetImageSource(musicListData)).Item1;
            }
            else if (musicListData.ListDataType == DataType.艺术家)
            {
                var art = NavToObj;
                Artist_Image.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(art.PicturePath));
                System.Diagnostics.Debug.WriteLine(art.PicturePath);
            }
            Artist_Image1.Source = Artist_Image.Source;
            Artist_Image1.SaveName = NavToObj.Name;
        }

        CompositionPropertySet scrollerPropertySet;
        Compositor compositor;
        Visual headerVisual;
        Visual backgroundVisual;
        Visual tbVisual;
        Visual ImageScrollVisual;
        public void UpdateShyHeader()
        {
            if (scrollViewer == null) return;

            double anotherHeight = menu_border.ActualHeight - LittleBarGrid.ActualHeight;
            String progress = $"Clamp(-scroller.Translation.Y / {anotherHeight}, 0, 1.0)";

            if (scrollerPropertySet == null)
            {
                scrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
                compositor = scrollerPropertySet.Compositor;
                headerVisual = ElementCompositionPreview.GetElementVisual(menu_border);
                backgroundVisual = ElementCompositionPreview.GetElementVisual(BackColorBaseRectangle);
                tbVisual = ElementCompositionPreview.GetElementVisual(ArtistTb);
                ImageScrollVisual = ElementCompositionPreview.GetElementVisual(Artist_ImageBaseBorder);
            }

            var offsetExpression = compositor.CreateExpressionAnimation($"-scroller.Translation.Y - {progress} * {anotherHeight}");
            offsetExpression.SetReferenceParameter("scroller", scrollerPropertySet);
            headerVisual.StartAnimation("Offset.Y", offsetExpression);

            var backgroundVisualOpacityAnimation = compositor.CreateExpressionAnimation($"Lerp(1, 0, {progress})");
            backgroundVisualOpacityAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            ImageScrollVisual.StartAnimation("Opacity", backgroundVisualOpacityAnimation);

            var ImageVisualOffsetAnimation = compositor.CreateExpressionAnimation($"Lerp(Vector3(0,0,0), Vector3(0,{menu_border.ActualHeight / 2},0), {progress})");
            ImageVisualOffsetAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            ImageScrollVisual.StartAnimation(nameof(ImageScrollVisual.Offset), ImageVisualOffsetAnimation);
        }

        private async void UpdateCommandToolBarWidth()
        {
            ToolsCommandBar.Width = 0;
            await Task.Delay(1);
            ToolsCommandBar.Width = double.NaN;
        }

        Vector3 ATBOffset = default;
        private async void menu_border_Loaded(object sender, RoutedEventArgs e)
        {
            if (scrollViewer == null)
            {
                scrollViewer = (VisualTreeHelper.GetChild(Children, 0) as Border).Child as ScrollViewer;
                scrollViewer.CanContentRenderOutsideBounds = true;
                scrollViewer.ViewChanging += ScrollViewer_ViewChanging;

                // 设置header为顶层
                var headerPresenter = (UIElement)VisualTreeHelper.GetParent((UIElement)Children.Header);
                var headerContainer = (UIElement)VisualTreeHelper.GetParent(headerPresenter);
                Canvas.SetZIndex(headerContainer, 1);
            }

            UpdateCommandToolBarWidth();
            Result_BaseGrid_SizeChanged(null, null);
        }

        private void ScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            headerVisual.IsPixelSnappingEnabled = true;
        }

        private void Result_BaseGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            menu_border.MinHeight = LittleBarGrid.ActualHeight;
            try { menu_border.Height = ActualHeight - 54; }
            catch { }
            ImageClip.Rect = new(0, 0, Artist_ImageBaseGrid.ActualWidth, Artist_ImageBaseGrid.ActualHeight);
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
                foreach (SongItemBindBase songItem in Children.SelectedItems)
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

        private async void Button_Click_6(object sender, RoutedEventArgs e)
        {
            switch ((sender as Button).Tag)
            {
                case "1":
                    await MainWindow.ShowDialog($"{NavToObj.Name}的信息", NavToObj.Describee);
                    break;
                case "2":
                    scrollViewer.ChangeView(null, menu_border.ActualHeight - LittleBarGrid.ActualHeight, null);
                    break;
            }
        }
        private async void Button_Click_7(object sender, RoutedEventArgs e)
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
