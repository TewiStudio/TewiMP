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
using CommunityToolkit.WinUI;
using TewiMP.UI.Controls;
using TewiMP.UI.Windows;
using TewiMP.UI.Pages.ListViewPages;
using TewiMP.Core.Music;
using TewiMP.Core.Models;
using TewiMP.Services.Storage;

namespace TewiMP.UI.Pages
{
    public partial class ItemListViewArtist : Page
    {
        private ScrollViewer scrollViewer { get; set; }
        public Artist NavToObj { get; set; }
        public MusicFrom NowMusicFrom { get; set; } = MusicFrom.pluginMusicSource;

        public ItemListViewArtist()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //PlayAllButton.Foreground = new SolidColorBrush(CodeHelper.IsAccentColorDark() ? Colors.White : Colors.Black);
            base.OnNavigatedTo(e);
            Artist a = ((PageData)e.Parameter).Param as Artist;
            NavToObj = a;
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
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
        public ObservableCollection<MusicDataViewModel> MusicDataList = new();
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
            var obj = await NavToObj.GetMusicSourcePlugin().GetArtist(NavToObj.ID);
            if (obj is null)
            {
                App.MainWindowInstance.AddNotify("加载艺术家信息时出现错误", "无法加载艺术家信息，请重试。", NotifySeverity.Error);
                return;
            }
            if (!IsLoaded) return;
            NavToObj = obj;
            musicListData = NavToObj.HotSongs;
            Artist_SmallName.Text = string.IsNullOrEmpty(NavToObj.Name2) ? NavToObj.Name : $"{NavToObj.Name}（{NavToObj.Name2}）";
            //ToolTipService.SetToolTip(Artist_Info, NavToObj.Describe);

            if (musicListData != null)
            {
                LoadImage();

                MusicDataList.Clear();
                int count = 1;
                foreach (var i in musicListData.Songs)
                {
                    MusicDataList.Add(new(i, musicListData, count++));
                }
            }
            LoadingTipControl.UnShowLoading();
        }

        private async void LoadImage()
        {
            Artist_Image.Source = null;
            Artist_Image1.Source = null;

            var art = NavToObj;
            Artist_Image.Source = new Uri(art.PicturePath);

            Artist_Image1.Source = Artist_Image.Source;
            Artist_Image1.SaveName = NavToObj.Name;
        }

        CompositionPropertySet scrollerPropertySet;
        Compositor compositor;
        Visual headerVisual;
        Visual backgroundVisual;
        Visual tbVisual;
        Visual ImageScrollVisual;
        Visual headerFootRootVisual; private ExpressionAnimation _offsetAnimation;
        ExpressionAnimation _opacityAnimation;
        ExpressionAnimation _imageOffsetAnimation;
        ExpressionAnimation _footOffsetAnimation;
        public void UpdateShyHeader(bool footBarUpdate = true)
        {
            if (scrollViewer is null) return;

            if (scrollerPropertySet is null)
            {
                scrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
                compositor = scrollerPropertySet.Compositor;
                headerVisual = ElementCompositionPreview.GetElementVisual(menu_border);
                backgroundVisual = ElementCompositionPreview.GetElementVisual(BackColorBaseRectangle);
                tbVisual = ElementCompositionPreview.GetElementVisual(ArtistTb);
                ImageScrollVisual = ElementCompositionPreview.GetElementVisual(Artist_ImageBaseBorder);
                headerFootRootVisual = ElementCompositionPreview.GetElementVisual(ItemsList_Header_Foot_Root);
            }

            // 计算动态数值
            double anotherHeight = menu_border.ActualHeight - LittleBarGrid.ActualHeight + 2;

            // 创建或更新 Offset 动画
            if (_offsetAnimation is null)
            {
                // Logic: -scroller.Y - (Progress * HeightParam)
                string exp = "-scroller.Translation.Y - (Clamp(-scroller.Translation.Y / HeightParam, 0, 1.0) * HeightParam)";
                _offsetAnimation = compositor.CreateExpressionAnimation(exp);
                _offsetAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            }
            _offsetAnimation.SetScalarParameter("HeightParam", (float)anotherHeight);
            headerVisual.StartAnimation("Offset.Y", _offsetAnimation);


            // 创建或更新 Opacity 动画
            if (_opacityAnimation is null)
            {
                string exp = "Lerp(1, 0, Clamp(-scroller.Translation.Y / HeightParam, 0, 1.0))";
                _opacityAnimation = compositor.CreateExpressionAnimation(exp);
                _opacityAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            }
            _opacityAnimation.SetScalarParameter("HeightParam", (float)anotherHeight);
            ImageScrollVisual.StartAnimation("Opacity", _opacityAnimation);


            // Image Offset 动画
            if (_imageOffsetAnimation is null)
            {
                // TargetY 也是动态的，所以做成参数
                string exp = "Lerp(Vector3(0,0,0), Vector3(0, TargetY, 0), Clamp(-scroller.Translation.Y / HeightParam, 0, 1.0))";
                _imageOffsetAnimation = compositor.CreateExpressionAnimation(exp);
                _imageOffsetAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            }
            _imageOffsetAnimation.SetScalarParameter("HeightParam", (float)anotherHeight);
            _imageOffsetAnimation.SetScalarParameter("TargetY", (float)(menu_border.ActualHeight / 2));
            ImageScrollVisual.StartAnimation(nameof(ImageScrollVisual.Offset), _imageOffsetAnimation);


            // Footer 动画
            if (footBarUpdate)
            {
                if (_footOffsetAnimation is null)
                {
                    string exp = "Lerp(Vector3(-16, StartY, 0), Vector3(-16, EndY, 0), Clamp(-scroller.Translation.Y / HeightParam, 0, 1.0))";
                    _footOffsetAnimation = compositor.CreateExpressionAnimation(exp);
                    _footOffsetAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
                }

                float visualH = headerFootRootVisual.Size.Y;
                float actualH = (float)ActualHeight;

                _footOffsetAnimation.SetScalarParameter("HeightParam", (float)anotherHeight);
                _footOffsetAnimation.SetScalarParameter("StartY", actualH - visualH - 8);
                _footOffsetAnimation.SetScalarParameter("EndY", (float)anotherHeight + actualH - visualH - 8);

                headerFootRootVisual.StartAnimation("Offset", _footOffsetAnimation);
            }
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

        Vector3 ATBOffset = default;
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

            UpdateCommandToolBarWidth();
            Result_BaseGrid_SizeChanged(null, null);
        }

        private void Artist_Image_Unloaded(object sender, RoutedEventArgs e)
        {
        }

        private void ScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            headerVisual!.IsPixelSnappingEnabled = true;
        }

        int resizeCounter = 0;
        private async void Result_BaseGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            menu_border.MinHeight = LittleBarGrid.ActualHeight;
            try { menu_border.Height = ActualHeight - 58; }
            catch { }
            ImageClip.Rect = new(0, 0, Artist_ImageBaseGrid.ActualWidth, Artist_ImageBaseGrid.ActualHeight);

            UpdateShyHeader(false); // headerFootRoot 会因为 menu_border 改变高度而闪烁
            if (resizeCounter > 1) ItemsList_Header_Foot_Root.Opacity = 0;
            resizeCounter++;
            await Task.Delay(200);
            resizeCounter--;
            if (resizeCounter != 0) return;
            ItemsList_Header_Foot_Root.Opacity = 1;
            UpdateShyHeader();
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
            MusicDataItem.SetIsCloseMouseEvent(SelectItemButton.IsChecked == true);
            App.MainWindowInstance.AllowDragEvents = SelectItemButton.IsChecked == false;
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
                foreach (MusicDataViewModel songItem in Children.SelectedItems)
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

        private async void Button_Click_6(object sender, RoutedEventArgs e)
        {
            switch ((sender as Button).Tag)
            {
                case "1":
                    await App.MainWindowInstance.ShowDialog($"{NavToObj.Name}的信息", NavToObj.Describee);
                    break;
                case "2":
                    scrollViewer.ChangeView(null, menu_border.ActualHeight - LittleBarGrid.ActualHeight, null);
                    break;
            }
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

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = this;
            InitData();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            scrollViewer?.ScrollToVerticalOffset(0);

            MusicDataList.Clear();
            Children.ItemsSource = null;
            Children.Items.Clear();
            Artist_Image.Source = null;
            musicListData = null;
            NavToObj = null;
            UnloadObject(this);
        }
    }
}
