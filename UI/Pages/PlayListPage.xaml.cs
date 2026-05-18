using System.Collections;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Composition;
using TewiMP.Core.Music;
using TewiMP.UI.Controls;

namespace TewiMP.UI.Pages
{
    public partial class PlayListPage : Page
    {
        private static double verticalOffset = 0;
        ArrayList arrayList;
        public PlayListPage()
        {
            InitializeComponent();
            //arrayList = new ArrayList(100000000);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (scrollViewer is not null)
                verticalOffset = scrollViewer.VerticalOffset;
        }

        ObservableCollection<MusicListData> playListCards = new();
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ItemsViewer.ItemsSource = playListCards;
            Init();
            UpdatePlayList();
            await Task.Delay(10);
            InitShyHeader();
            scrollViewer.ScrollToVerticalOffset(verticalOffset);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            playListCards.Clear();
            ItemsViewer.ItemsSource = null;
            BaseGridView.Items.Clear();
            RemoveEvent();
            DisposeVisuals();
        }

        private void MainWindow_MainViewStateChanged(bool isView)
        {/*
            if (isView)
                ItemsViewer.ItemsSource = playListCards;
            else
            {
                ItemsViewer.ItemsSource = null;
            }*/
        }

        bool isInUpdate = false;
        public async void UpdatePlayList()
        {
            if (isInUpdate) return;
            isInUpdate = true;
            playListCards.Clear();

            if (App.Instance.PlayListReader.NowMusicListData is null)
                await App.Instance.PlayListReader.Refresh();

            int count = 0;
            foreach (var item in App.Instance.PlayListReader.NowMusicListData)
            {
                count++;
                playListCards.Add(item);
            }
            isInUpdate = false;
        }

        void Init()
        {
            InitEvent();
            InitVisual();
            InitShyHeader();
        }

        ScrollViewer scrollViewer;
        CompositionPropertySet scrollerPropertySet;
        Compositor compositor;
        Visual headerVisual;
        Visual backgroundVisual;
        Visual logoVisual;
        Visual headerFootRootVisual;
        void InitVisual()
        {
            // 设置header为顶层
            var headerPresenter = (UIElement)VisualTreeHelper.GetParent((UIElement)BaseGridView.Header);
            var headerContainer = (UIElement)VisualTreeHelper.GetParent(headerPresenter);
            Canvas.SetZIndex(headerContainer, 1);

            ItemsViewer.ScrollView.HorizontalScrollBarVisibility = ScrollingScrollBarVisibility.Hidden;
            ItemsViewer.ScrollView.HorizontalScrollMode = ScrollingScrollMode.Disabled;

            scrollViewer = (VisualTreeHelper.GetChild(BaseGridView, 0) as Border).Child as ScrollViewer;
            scrollViewer.CanContentRenderOutsideBounds = true;
            scrollViewer.ViewChanging -= ScrollViewer_ViewChanging;
            scrollViewer.ViewChanging += ScrollViewer_ViewChanging;

            scrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
            compositor = scrollerPropertySet.Compositor;
            headerVisual = ElementCompositionPreview.GetElementVisual(BaseGridView_HeaderGrid);
            logoVisual = ElementCompositionPreview.GetElementVisual(BaseGridView_HeaderTextBlock);
            backgroundVisual = ElementCompositionPreview.GetElementVisual(BaseGridView_HeaderRectangle);
            headerFootRootVisual = ElementCompositionPreview.GetElementVisual(BaseGridView_HeaderGrid_Foot_Root);
        }

        private ExpressionAnimation _offsetAnim;
        private ExpressionAnimation _logoScaleAnim;
        private ExpressionAnimation _logoOffsetAnim;
        private ExpressionAnimation _bgOpacityAnim;
        private ExpressionAnimation _footerOffsetAnim;

        // Progress = Clamp(-scroller.Translation.Y / Padding, 0, 1.0)
        private const string ProgressExp = "Clamp(-scroller.Translation.Y / Padding, 0, 1.0)";

        void InitShyHeader()
        {
            // 基础检查
            if (!IsLoaded || scrollViewer is null) return;

            // 准备参数
            float paddingSize = 40f;

            // 动画 1: Header Sticky Offset
            if (_offsetAnim is null)
            {
                // -scroller.Y - (Progress * Padding)
                string exp = $"-scroller.Translation.Y - ({ProgressExp} * Padding)";
                _offsetAnim = compositor.CreateExpressionAnimation(exp);
                _offsetAnim.SetReferenceParameter("scroller", scrollerPropertySet);
            }
            // 更新参数
            _offsetAnim.SetScalarParameter("Padding", paddingSize);
            headerVisual.StartAnimation("Offset.Y", _offsetAnim);

            // 动画 2: Logo Scale
            if (_logoScaleAnim is null)
            {
                string exp = $"Lerp(Vector2(1,1), Vector2(0.7, 0.7), {ProgressExp})";
                _logoScaleAnim = compositor.CreateExpressionAnimation(exp);
                _logoScaleAnim.SetReferenceParameter("scroller", scrollerPropertySet);
            }
            _logoScaleAnim.SetScalarParameter("Padding", paddingSize);
            logoVisual.StartAnimation("Scale.xy", _logoScaleAnim);

            // X: 0 -> -12, Y: 0 -> 24
            if (_logoOffsetAnim is null)
            {
                string exp = $"Lerp(Vector3(0,0,0), Vector3(-12, 24, 0), {ProgressExp})";
                _logoOffsetAnim = compositor.CreateExpressionAnimation(exp);
                _logoOffsetAnim.SetReferenceParameter("scroller", scrollerPropertySet);
            }
            _logoOffsetAnim.SetScalarParameter("Padding", paddingSize);
            logoVisual.StartAnimation(nameof(logoVisual.Offset), _logoOffsetAnim);

            // 动画 4: Background Opacity
            if (_bgOpacityAnim is null)
            {
                string exp = $"Lerp(0, 1, {ProgressExp})";
                _bgOpacityAnim = compositor.CreateExpressionAnimation(exp);
                _bgOpacityAnim.SetReferenceParameter("scroller", scrollerPropertySet);
            }
            _bgOpacityAnim.SetScalarParameter("Padding", paddingSize);
            backgroundVisual.StartAnimation("Opacity", _bgOpacityAnim);

            // 动画 5: Footer Offset (复杂参数)
            if (_footerOffsetAnim is null)
            {
                // 表达式逻辑：Lerp(StartPos, EndPos, Progress)
                // StartPos = Vector3(-16, StartY, 0)
                // EndPos   = Vector3(-16, EndY,   0)
                string exp = $"Lerp(Vector3(-16, StartY, 0), Vector3(-16, EndY, 0), {ProgressExp})";
                _footerOffsetAnim = compositor.CreateExpressionAnimation(exp);
                _footerOffsetAnim.SetReferenceParameter("scroller", scrollerPropertySet);
            }

            // 计算动态数值
            float footerVisualH = headerFootRootVisual.Size.Y;
            float actualH = (float)ActualHeight;

            // 参数化更新 (这是性能最高的做法)
            _footerOffsetAnim.SetScalarParameter("Padding", paddingSize);
            _footerOffsetAnim.SetScalarParameter("StartY", actualH - footerVisualH - 8f);
            _footerOffsetAnim.SetScalarParameter("EndY", paddingSize + actualH - footerVisualH - 8f);

            headerFootRootVisual.StartAnimation("Offset", _footerOffsetAnim);
        }
        void DisposeVisuals()
        {
            _offsetAnim?.Dispose();
            _logoScaleAnim?.Dispose();
            _logoOffsetAnim?.Dispose();
            _bgOpacityAnim?.Dispose();
            _footerOffsetAnim?.Dispose();

            scrollViewer = null;
            scrollerPropertySet = null;
            compositor = null;
            headerVisual = null;
            backgroundVisual = null;
            logoVisual = null;
            headerFootRootVisual = null;
            _offsetAnim = null;
            _logoScaleAnim = null;
            _logoOffsetAnim = null;
            _bgOpacityAnim = null;
            _footerOffsetAnim = null;
        }

        void InitEvent()
        {
            if (!IsLoaded) return;
            App.Instance.PlayListReader.Updated -= PlayListReader_Updated;
            App.Instance.PlayListReader.Updated += PlayListReader_Updated;
            App.MainWindowInstance.MainViewStateChanged -= MainWindow_MainViewStateChanged;
            App.MainWindowInstance.MainViewStateChanged += MainWindow_MainViewStateChanged;
            BaseGridView_HeaderGrid_Foot_Buttons.PositionToBottom_Button.Click -= PositionToButton_Click;
            BaseGridView_HeaderGrid_Foot_Buttons.PositionToBottom_Button.Click += PositionToButton_Click;
            BaseGridView_HeaderGrid_Foot_Buttons.PositionToTop_Button.Click -= PositionToButton_Click;
            BaseGridView_HeaderGrid_Foot_Buttons.PositionToTop_Button.Click += PositionToButton_Click;
        }

        void RemoveEvent()
        {
            if (scrollViewer != null) scrollViewer.ViewChanging -= ScrollViewer_ViewChanging;
            App.Instance.PlayListReader.Updated -= PlayListReader_Updated;
            App.MainWindowInstance.MainViewStateChanged -= MainWindow_MainViewStateChanged;
            BaseGridView_HeaderGrid_Foot_Buttons.PositionToBottom_Button.Click -= PositionToButton_Click;
            BaseGridView_HeaderGrid_Foot_Buttons.PositionToTop_Button.Click -= PositionToButton_Click;
        }

        private void PositionToButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            switch ((ScrollFootButton.ButtonType)btn.Tag)
            {
                case ScrollFootButton.ButtonType.Top:
                    scrollViewer.ChangeView(null, 0, null);
                    break;
                case ScrollFootButton.ButtonType.Bottom:
                    scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null);
                    break;
            }
        }

        private void PlayListReader_Updated()
        {
            UpdatePlayList();
        }

        private void ScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            headerVisual.IsPixelSnappingEnabled = true;
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            await App.Instance.PlayListReader.Refresh();
        }

        private async void AppBarButton_Click_1(object sender, RoutedEventArgs e)
        {
            await DialogPages.AddPlayListPage.ShowDialog();
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //UpdateShyH
            //
            //eader();
            InitShyHeader();
        }

        private async void AppBarButton_Click_2(object sender, RoutedEventArgs e)
        {
            await DialogPages.InsertPlayListPage.ShowDialog();
        }
    }
}
