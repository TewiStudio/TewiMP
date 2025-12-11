using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Composition;
using Windows.System;
using CommunityToolkit.WinUI;
using TewiMP.Services.Media;
using TewiMP.Helpers;
using TewiMP.UI.Controls;
using TewiMP.UI.Windows;
using TewiMP.Services;
using TewiMP.Services.Storage;
using TewiMP.Core.Music;
using TewiMP.UI.Pages.ListViewPages;

namespace TewiMP.UI.Pages
{
    public partial class ItemListViewAlbum : Page
    {
        private ScrollViewer scrollViewer { get; set; }
        public Album NavToObj { get; set; }
        public MusicFrom NowMusicFrom { get; set; } = MusicFrom.pluginMusicSource;

        public ItemListViewAlbum()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //PlayAllButton.Foreground = new SolidColorBrush(CodeHelper.IsAccentColorDark() ? Colors.White : Colors.Black);
            base.OnNavigatedTo(e);
            Album a = ((PageData)e.Parameter).Param as Album;
            NavToObj = a;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        private async void LeavingPageDo()
        {
            ItemsList_Header_Foot_Buttons.PositionToTop_Button.Click -= PositionToButton_Click;
            ItemsList_Header_Foot_Buttons.PositionToBottom_Button.Click -= PositionToButton_Click;
            ItemsList_Header_Foot_Buttons.PositionToNowPlaying_Button.Click -= PositionToButton_Click;
            SearchBox.SearchingAItem -= SearchBox_SearchingAItem;
            SearchBox.IsOpenChanged -= SearchBox_IsOpenChanged;
            App.MainWindowInstance.InKeyDownEvent -= MainWindow_InKeyDownEvent;
            App.MainWindowInstance.MainViewStateChanged -= MainWindow_MainViewStateChanged;

            SongItemBindBase.RecycleBindItems(MusicDataList);
            MusicDataList?.Clear();

            if (Children != null)
            {
                Children.ItemsSource = null;
            }

            musicListData = null;
            MusicDataList = null;

            if (Album_Image != null) Album_Image.Source = null;
            if (AlbumLogo != null) AlbumLogo.Source = null;

            NavToObj = null;

            UnloadObject(this);
        }

        private void MainWindow_MainViewStateChanged(bool isView)
        {
            AutoScrollViewerControl.Pause = !isView;
        }

        private void CrateShadow()
        {
            var visual = ElementCompositionPreview.GetElementVisual(AlbumLogoRoot);
            compositor = visual.Compositor;

            var basicRectVisual = compositor.CreateSpriteVisual();
            basicRectVisual.Size = AlbumLogoRoot.RenderSize.ToVector2();

            dropShadow = compositor.CreateDropShadow();
            dropShadow.BlurRadius = 45f;
            dropShadow.Color = Colors.Black;
            dropShadow.Opacity = 0.3f;
            dropShadow.Offset = new Vector3(0, 4, 0);

            basicRectVisual.Shadow = dropShadow;
            ElementCompositionPreview.SetElementChildVisual(AlbumLogo_DropShadowBase, basicRectVisual);
        }

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
            if (NavToObj is null)
            {
                LoadingTipControl.UnShowLoading();
                return;
            }
            if (NavToObj.PluginInfo is null)
            {
                LoadingTipControl.UnShowLoading();
                App.MainWindowInstance.AddNotify("找不到此专辑的信息", "无插件源查询专辑信息。", NotifySeverity.Error);
                return;
            }
            var obj = await NavToObj.PluginInfo.GetMusicSourcePlugin().GetAlbum(NavToObj.ID);
            if (!IsLoaded) return;
            if (obj is null)
            {
                LoadingTipControl.UnShowLoading();
                App.MainWindowInstance.AddNotify("加载专辑信息时出现错误", "无法获取专辑信息，请重试。",
                    NotifySeverity.Error);
                return;
            }
            NavToObj = obj;
            musicListData = NavToObj.Songs;
            if (string.IsNullOrEmpty(obj.Title2) && obj.ReleaseTime == DateTime.MinValue)
            {
                Title2_Text.Visibility = Visibility.Collapsed;
            }
            else
            {
                Title2_Text.Visibility = Visibility.Visible;
                Title2.Text = obj.Title2;
                ReleaseTime.Text = string.IsNullOrEmpty(obj.Title2) ? $"发布日期：{obj.ReleaseTime}" : $"\n发布日期：{obj.ReleaseTime}";
            }

            if (musicListData != null)
            {
                LoadImage();
                DescribeeText.Text = obj.Describe;
                SongItemBindBase.RecycleBindItems(MusicDataList);
                MusicDataList.Clear();
                int count = 1;
                foreach (var i in musicListData.Songs)
                {
                    MusicDataList.Add(SongItemBindBase.GetBindItem(i, musicListData, count++));
                }
            }
            LoadingTipControl.UnShowLoading();
            LogService.Log(nameof(ItemListViewAlbum), "Loaded.");
            await Task.Delay(1000);
            UpdateShyHeader();
        }

        private async void LoadImage()
        {
            Album_Image.Source = null;
            AlbumLogo.Source = null;
            AlbumLogo.BorderThickness = new(0);
            if (NavToObj is null) return;
            if (musicListData.ListDataType == DataType.本地歌单)
            {
                Album_Image.Source = musicListData.PicturePath.ToImageUri();
            }
            else if (musicListData.ListDataType == DataType.歌单)
            {
                Album_Image.Source =(await ImageService.GetImageUri(musicListData)).Item1;
            }
            else if (musicListData.ListDataType == DataType.专辑)
            {
                var art = NavToObj;
                Album_Image.Source = new Uri(art.PicturePath);
            }
            AlbumLogo.Source = Album_Image.Source;
            AlbumLogo.SaveName = NavToObj.Title;
            AlbumLogo.BorderThickness = new(1);
            LogService.Log(nameof(ItemListViewAlbum), "Image loaded.");
            UpdateShyHeader();
            await Task.Delay(10);
            UpdateShyHeader();
            await Task.Delay(100);
            UpdateShyHeader();
            await Task.Delay(200);
            UpdateShyHeader();
            if (!IsLoaded) LeavingPageDo();
        }

        CompositionPropertySet scrollerPropertySet;
        Compositor compositor;
        Visual headerVisual;
        Visual massAlbumRootVisual;
        Visual blurAlbumRootVisual;
        Visual ImageScrollVisual;
        Visual logoVisual;
        Visual logoShadowVisual;
        Visual infoTextsRootVisual;
        Visual commandbarVisual;
        Visual describeeRootVisual;
        Visual searchBaseVisual;
        Visual headerFootRootVisual;

        private ExpressionAnimation _headerOffsetAnim;
        private ExpressionAnimation _blurOpacityAnim;
        private ExpressionAnimation _massOpacityAnim;
        private ExpressionAnimation _describeeOpacityAnim;
        private ExpressionAnimation _imgScrollOffsetAnim;
        private ExpressionAnimation _logoScaleAnim; // Logo 和 Shadow 共用这一个
        private ExpressionAnimation _cmdBarOffsetAnim;
        private ExpressionAnimation _infoTextOffsetAnim;
        private ExpressionAnimation _searchBaseOffsetAnim;
        private ExpressionAnimation _footerOffsetAnim;

        // 预定义常量表达式字符串 (方便阅读和复用)
        // HeightParam 是动态传入的高度 (168)
        private const string ProgressExp = "Clamp(-scroller.Translation.Y / HeightParam, 0, 1.0)";
        // Describee 也就是固定除以 80 的进度
        private const string DescProgressExp = "Clamp(-scroller.Translation.Y / 80.0, 0, 1.0)";

        public void UpdateShyHeader()
        {
            if (scrollViewer is null) return;

            // 1. 初始化 Visuals (只执行一次)
            if (scrollerPropertySet is null)
            {
                scrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
                compositor = scrollerPropertySet.Compositor;

                // 获取所有 Visual
                headerVisual = ElementCompositionPreview.GetElementVisual(menu_border);
                massAlbumRootVisual = ElementCompositionPreview.GetElementVisual(MassAlbumRoot);
                blurAlbumRootVisual = ElementCompositionPreview.GetElementVisual(BlurAlbumRoot);
                ImageScrollVisual = ElementCompositionPreview.GetElementVisual(Album_ImageBaseBorder);
                infoTextsRootVisual = ElementCompositionPreview.GetElementVisual(InfoTextsRoot);
                logoVisual = ElementCompositionPreview.GetElementVisual(AlbumLogoRoot);
                logoShadowVisual = ElementCompositionPreview.GetElementVisual(AlbumLogo_DropShadowBase);
                commandbarVisual = ElementCompositionPreview.GetElementVisual(ToolsCommandBar);
                describeeRootVisual = ElementCompositionPreview.GetElementVisual(DescribeeTextRoot);
                searchBaseVisual = ElementCompositionPreview.GetElementVisual(SearchBase);
                headerFootRootVisual = ElementCompositionPreview.GetElementVisual(ItemsList_Header_Foot_Root);

                CrateShadow();
            }

            // 2. 准备参数 (提取变量，避免在表达式里重复计算)
            float anotherHeight = 168f;
            float sizeDouble = 0.391f;

            // 更新中心点 (CenterPoint 依赖 Size，如果 Size 会变，这行必须保留)
            // 注意：Vector3 的 Z 轴设为 1 没问题，但在 2D 变换中通常用不到
            logoVisual.CenterPoint = new System.Numerics.Vector3(0, logoVisual.Size.Y, 1);
            logoShadowVisual.CenterPoint = new System.Numerics.Vector3(0, logoVisual.Size.Y, 1);

            // --------------------------------------------------------------------------
            // 动画 1: Header Offset
            // --------------------------------------------------------------------------
            if (_headerOffsetAnim is null)
            {
                string exp = $"-scroller.Translation.Y - ({ProgressExp} * HeightParam)";
                _headerOffsetAnim = compositor.CreateExpressionAnimation(exp);
                _headerOffsetAnim.SetReferenceParameter("scroller", scrollerPropertySet);
            }
            _headerOffsetAnim.SetScalarParameter("HeightParam", anotherHeight);
            headerVisual.StartAnimation("Offset.Y", _headerOffsetAnim);

            // --------------------------------------------------------------------------
            // 动画 2 & 3: Opacity (Blur & Mass)
            // --------------------------------------------------------------------------
            // Blur: Lerp(1, 0, P)
            if (_blurOpacityAnim is null)
            {
                string exp = $"Lerp(1, 0, {ProgressExp})";
                _blurOpacityAnim = compositor.CreateExpressionAnimation(exp);
                _blurOpacityAnim.SetReferenceParameter("scroller", scrollerPropertySet);
            }
            _blurOpacityAnim.SetScalarParameter("HeightParam", anotherHeight);
            blurAlbumRootVisual.StartAnimation("Opacity", _blurOpacityAnim);

            // Mass: Lerp(0, 1, P)
            if (_massOpacityAnim is null)
            {
                string exp = $"Lerp(0, 1, {ProgressExp})";
                _massOpacityAnim = compositor.CreateExpressionAnimation(exp);
                _massOpacityAnim.SetReferenceParameter("scroller", scrollerPropertySet);
            }
            _massOpacityAnim.SetScalarParameter("HeightParam", anotherHeight);
            massAlbumRootVisual.StartAnimation("Opacity", _massOpacityAnim);

            // --------------------------------------------------------------------------
            // 动画 4: Describee Opacity (使用独立的进度 /80)
            // --------------------------------------------------------------------------
            if (_describeeOpacityAnim is null)
            {
                string exp = $"Lerp(1, 0, {DescProgressExp})";
                _describeeOpacityAnim = compositor.CreateExpressionAnimation(exp);
                _describeeOpacityAnim.SetReferenceParameter("scroller", scrollerPropertySet);
            }
            describeeRootVisual.StartAnimation("Opacity", _describeeOpacityAnim);

            // --------------------------------------------------------------------------
            // 动画 5: Image Scroll Offset
            // --------------------------------------------------------------------------
            if (_imgScrollOffsetAnim is null)
            {
                // Lerp(0, TargetY, P)
                string exp = $"Lerp(Vector3(0,0,0), Vector3(0, TargetY, 0), {ProgressExp})";
                _imgScrollOffsetAnim = compositor.CreateExpressionAnimation(exp);
                _imgScrollOffsetAnim.SetReferenceParameter("scroller", scrollerPropertySet);
            }
            _imgScrollOffsetAnim.SetScalarParameter("HeightParam", anotherHeight);
            _imgScrollOffsetAnim.SetScalarParameter("TargetY", anotherHeight / 1.2f);
            ImageScrollVisual.StartAnimation("Offset", _imgScrollOffsetAnim);

            // --------------------------------------------------------------------------
            // 动画 6 & 7: Logo & Shadow Scale (共用一个动画对象！)
            // --------------------------------------------------------------------------
            if (_logoScaleAnim is null)
            {
                string exp = $"Lerp(Vector3(1, 1, 1), Vector3(TargetScale, TargetScale, 1), {ProgressExp})";
                _logoScaleAnim = compositor.CreateExpressionAnimation(exp);
                _logoScaleAnim.SetReferenceParameter("scroller", scrollerPropertySet);
            }
            _logoScaleAnim.SetScalarParameter("HeightParam", anotherHeight);
            _logoScaleAnim.SetScalarParameter("TargetScale", sizeDouble);

            // 同时应用给两个 Visual
            logoVisual.StartAnimation("Scale", _logoScaleAnim);
            logoShadowVisual.StartAnimation("Scale", _logoScaleAnim);

            // --------------------------------------------------------------------------
            // 动画 8: CommandBar Offset Y
            // --------------------------------------------------------------------------
            if (_cmdBarOffsetAnim is null)
            {
                string exp = $"Lerp(StartY, EndY, {ProgressExp})";
                _cmdBarOffsetAnim = compositor.CreateExpressionAnimation(exp);
                _cmdBarOffsetAnim.SetReferenceParameter("scroller", scrollerPropertySet);
            }
            float cmdBarH = commandbarVisual.Size.Y;
            _cmdBarOffsetAnim.SetScalarParameter("HeightParam", anotherHeight);
            _cmdBarOffsetAnim.SetScalarParameter("StartY", 282f - cmdBarH);
            _cmdBarOffsetAnim.SetScalarParameter("EndY", 114f - cmdBarH);
            commandbarVisual.StartAnimation("Offset.Y", _cmdBarOffsetAnim);

            // --------------------------------------------------------------------------
            // 动画 9: Info Text Root Offset
            // --------------------------------------------------------------------------
            if (_infoTextOffsetAnim is null)
            {
                // Start: (StartX, 0, 0) -> End: (EndX, HeightParam, 0)
                string exp = $"Lerp(Vector3(StartX, 0, 0), Vector3(EndX, HeightParam, 0), {ProgressExp})";
                _infoTextOffsetAnim = compositor.CreateExpressionAnimation(exp);
                _infoTextOffsetAnim.SetReferenceParameter("scroller", scrollerPropertySet);
            }
            float logoW = logoVisual.Size.X;
            _infoTextOffsetAnim.SetScalarParameter("HeightParam", anotherHeight);
            _infoTextOffsetAnim.SetScalarParameter("StartX", logoW + 12f);
            _infoTextOffsetAnim.SetScalarParameter("EndX", logoW * sizeDouble + 12f);
            infoTextsRootVisual.StartAnimation("Offset", _infoTextOffsetAnim);

            // --------------------------------------------------------------------------
            // 动画 10: Search Base Offset
            // --------------------------------------------------------------------------
            if (_searchBaseOffsetAnim is null)
            {
                // StartY -> EndY
                string exp = $"Lerp(Vector3(16, StartY, 0), Vector3(16, EndY, 0), {ProgressExp})";
                _searchBaseOffsetAnim = compositor.CreateExpressionAnimation(exp);
                _searchBaseOffsetAnim.SetReferenceParameter("scroller", scrollerPropertySet);
            }
            _searchBaseOffsetAnim.SetScalarParameter("HeightParam", anotherHeight);
            _searchBaseOffsetAnim.SetScalarParameter("StartY", headerVisual.Size.Y + 12f);
            _searchBaseOffsetAnim.SetScalarParameter("EndY", anotherHeight + 132f + 12f);
            searchBaseVisual.StartAnimation("Offset", _searchBaseOffsetAnim);

            // --------------------------------------------------------------------------
            // 动画 11: Header Foot Root Offset
            // --------------------------------------------------------------------------
            if (_footerOffsetAnim is null)
            {
                string exp = $"Lerp(Vector3(-16, StartY, 0), Vector3(-16, EndY, 0), {ProgressExp})";
                _footerOffsetAnim = compositor.CreateExpressionAnimation(exp);
                _footerOffsetAnim.SetReferenceParameter("scroller", scrollerPropertySet);
            }
            float footerH = headerFootRootVisual.Size.Y;
            float actualH = (float)ActualHeight;

            _footerOffsetAnim.SetScalarParameter("HeightParam", anotherHeight);
            _footerOffsetAnim.SetScalarParameter("StartY", actualH - footerH - 8f);
            _footerOffsetAnim.SetScalarParameter("EndY", anotherHeight + actualH - footerH - 8f);
            headerFootRootVisual.StartAnimation("Offset", _footerOffsetAnim);
        }

        private async void UpdateCommandToolBarWidth()
        {
            ToolsCommandBar.Width = 0;
            await Task.Delay(1);
            ToolsCommandBar.Width = double.NaN;
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
            CrateShadow();
            SearchBox.SongItemBinds = MusicDataList;
            SearchBox.IsOpenChanged -= SearchBox_IsOpenChanged;
            SearchBox.IsOpenChanged += SearchBox_IsOpenChanged;
            SearchBox.SearchingAItem -= SearchBox_SearchingAItem;
            SearchBox.SearchingAItem += SearchBox_SearchingAItem;
            ItemsList_Header_Foot_Buttons.PositionToBottom_Button.Click -= PositionToButton_Click;
            ItemsList_Header_Foot_Buttons.PositionToBottom_Button.Click += PositionToButton_Click;
            ItemsList_Header_Foot_Buttons.PositionToNowPlaying_Button.Click -= PositionToButton_Click;
            ItemsList_Header_Foot_Buttons.PositionToNowPlaying_Button.Click += PositionToButton_Click;
            ItemsList_Header_Foot_Buttons.PositionToTop_Button.Click -= PositionToButton_Click;
            ItemsList_Header_Foot_Buttons.PositionToTop_Button.Click += PositionToButton_Click;
        }

        private void ScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            UpdateShyHeader();
            if (scrollViewer != null)
                AlbumLogoRoot.CornerRadius = new(Math.Min(Math.Max(scrollViewer.VerticalOffset / 8, 8), 15));
            if (logoVisual != null)
            {
                var a = ActualWidth - (logoVisual.Scale.X * AlbumLogoRoot.ActualWidth + 44);
                if (a > 0)
                {
                    InfoTextsRoot.Width = a;
                    ToolsCommandBar.MaxWidth = a;
                }
            }
            if (headerVisual != null) headerVisual.IsPixelSnappingEnabled = true;
            //BackColorBaseRectangle.Margin = new(0, Math.Min(scrollViewer.VerticalOffset, 180), 0, 0);
        }

        private void Result_BaseGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //menu_border.MinHeight = LittleBarGrid.ActualHeight;
            //try { menu_border.Height = ActualHeight; }
            //catch { }
            //ImageClip.Rect = new(0, 0, ActualWidth, ActualHeight);
            ScrollViewer_ViewChanging(null, null);
            CrateShadow();
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
                foreach (SongItemBindBase item in Children.SelectedItems)
                {
                    App.Instance.PlayingListService.Add(item.MusicData);
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
            foreach (SongItemBindBase item in Children.SelectedItems)
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

        }

        private void SearchBox_IsOpenChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                SearchBox.FocusToSearchBox();
                menu_border.Margin = new(0, 0, 0, searchBaseVisual.Size.Y + 12 + 12);
            }
            else
            {
                menu_border.Margin = new(0, 0, 0, 12);
            }
        }

        SongItemBindBase searchPointSongItemBindBase = null;
        private async void SearchBox_SearchingAItem(SongItemBindBase songItemBind)
        {
            searchPointSongItemBindBase = songItemBind;
            var scrollPlacement = ScrollItemPlacement.Top;
            int additionalVerticalOffset = -210;
            bool tryHighlight = MusicDataItem.TryHighlight(songItemBind);
            await Children.SmoothScrollIntoViewWithItemAsync(songItemBind, scrollPlacement, additionalVerticalOffset: additionalVerticalOffset);
            while (!tryHighlight)
            {
                if (!IsLoaded) break;
                if (searchPointSongItemBindBase != songItemBind) break;
                await Children.SmoothScrollIntoViewWithItemAsync(songItemBind, scrollPlacement, true, additionalVerticalOffset: additionalVerticalOffset);
                await Children.SmoothScrollIntoViewWithItemAsync(songItemBind, scrollPlacement, true, additionalVerticalOffset: additionalVerticalOffset);
                tryHighlight = MusicDataItem.TryHighlight(songItemBind);
                await Task.Delay(80);
            }
            searchPointSongItemBindBase = null;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.IsOpen = !SearchBox.IsOpen;
        }

        private void MainWindow_InKeyDownEvent(VirtualKey key)
        {
            if (App.MainWindowInstance.isControlDown)
            {
                if (key == VirtualKey.F)
                {
                    SearchBox.IsOpen = !SearchBox.IsOpen;
                    if (!SearchBox.IsOpen)
                        ToolsCommandBar.Focus(FocusState.Programmatic);
                }
            }
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

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = this;
            musicListData = new() { ListDataType = DataType.专辑 };
            App.MainWindowInstance.InKeyDownEvent += MainWindow_InKeyDownEvent;
            App.MainWindowInstance.MainViewStateChanged += MainWindow_MainViewStateChanged;
            InitData();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            LeavingPageDo();
        }
    }
}
