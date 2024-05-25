using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Composition;
using TewiMP.DataEditor;
using TewiMP.Controls;
using System.Collections;

namespace TewiMP.Pages
{
    public partial class PlayListPage : Page
    {
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
        }

        ObservableCollection<MusicListData> playListCards = new();
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ItemsViewer.ItemsSource = playListCards;
            Init();
            UpdatePlayList();
            await Task.Delay(10);
            InitShyHeader();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            RemoveEvent();
            DisposeVisuals();
            playListCards.Clear();
            ItemsViewer.ItemsSource = null;
            BaseGridView.Items.Clear();
        }

        private void MainWindow_MainViewStateChanged(bool isView)
        {
            if (isView)
                ItemsViewer.ItemsSource = playListCards;
            else
            {
                ItemsViewer.ItemsSource = null;
            }
        }

        bool isInUpdate = false;
        public async void UpdatePlayList()
        {
            if (isInUpdate) return;
            isInUpdate = true;
            playListCards.Clear();

            if (App.playListReader.NowMusicListData == null)
                await App.playListReader.Refresh();

            int count = 0;
            foreach (var item in App.playListReader.NowMusicListData)
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

        ExpressionAnimation offsetExpression;
        ExpressionAnimation logoHeaderScaleAnimation;
        ExpressionAnimation logoVisualOffsetYAnimation;
        ExpressionAnimation logoVisualOffsetXAnimation;
        ExpressionAnimation backgroundVisualOpacityAnimation;
        ExpressionAnimation headerFootRootVisualOffsetAnimation;
        void InitShyHeader()
        {
            if (!IsLoaded) return;
            if (scrollViewer == null) return;

            var paddingSize = 40;
            var progress = $"Clamp(-scroller.Translation.Y / {paddingSize}, 0, 1.0)";

            offsetExpression?.Dispose();
            offsetExpression = compositor.CreateExpressionAnimation($"-scroller.Translation.Y - Round({progress} * {paddingSize})");
            offsetExpression.SetReferenceParameter("scroller", scrollerPropertySet);
            headerVisual.StartAnimation("Offset.Y", offsetExpression);

            logoHeaderScaleAnimation?.Dispose();
            logoHeaderScaleAnimation = compositor.CreateExpressionAnimation("Lerp(Vector2(1,1), Vector2(0.7, 0.7), " + progress + ")");
            logoHeaderScaleAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            logoVisual.StartAnimation("Scale.xy", logoHeaderScaleAnimation);

            logoVisualOffsetYAnimation?.Dispose();
            logoVisualOffsetYAnimation = compositor.CreateExpressionAnimation($"Lerp(0, 24, {progress})");
            logoVisualOffsetYAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            logoVisual.StartAnimation("Offset.Y", logoVisualOffsetYAnimation);

            logoVisualOffsetXAnimation?.Dispose();
            logoVisualOffsetXAnimation = compositor.CreateExpressionAnimation($"Lerp(0, -12, {progress})");
            logoVisualOffsetXAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            logoVisual.StartAnimation("Offset.X", logoVisualOffsetXAnimation);

            backgroundVisualOpacityAnimation?.Dispose();
            backgroundVisualOpacityAnimation = compositor.CreateExpressionAnimation($"Lerp(0, 1, {progress})");
            backgroundVisualOpacityAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            backgroundVisual.StartAnimation("Opacity", backgroundVisualOpacityAnimation);

            headerFootRootVisualOffsetAnimation?.Dispose();
            headerFootRootVisualOffsetAnimation = compositor.CreateExpressionAnimation(
                $"Lerp(" +
                    $"Vector3(" +
                        $"-16," +
                        $"{ActualHeight} - {headerFootRootVisual.Size.Y} - 8," +
                        $"0)," +
                    $"Vector3(" +
                        $"-16," +
                        $"{paddingSize} + {ActualHeight} - {headerFootRootVisual.Size.Y} - 8," +
                        $"0)," +
                    $"{progress})");
            headerFootRootVisualOffsetAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            headerFootRootVisual.StartAnimation("Offset", headerFootRootVisualOffsetAnimation);
        }
        void DisposeVisuals()
        {
            offsetExpression?.Dispose();
            logoHeaderScaleAnimation?.Dispose();
            logoVisualOffsetYAnimation?.Dispose();
            logoVisualOffsetXAnimation?.Dispose();
            backgroundVisualOpacityAnimation?.Dispose();
            headerFootRootVisualOffsetAnimation?.Dispose();

            scrollViewer = null;
            scrollerPropertySet = null;
            compositor = null;
            headerVisual = null;
            backgroundVisual = null;
            logoVisual = null;
            headerFootRootVisual = null;
            offsetExpression = null;
            logoHeaderScaleAnimation = null;
            logoVisualOffsetYAnimation = null;
            logoVisualOffsetXAnimation = null;
            backgroundVisualOpacityAnimation = null;
            headerFootRootVisualOffsetAnimation = null;
        }

        void InitEvent()
        {
            if (!IsLoaded) return;
            App.playListReader.Updated -= PlayListReader_Updated;
            App.playListReader.Updated += PlayListReader_Updated;
            MainWindow.MainViewStateChanged -= MainWindow_MainViewStateChanged;
            MainWindow.MainViewStateChanged += MainWindow_MainViewStateChanged;
            BaseGridView_HeaderGrid_Foot_Buttons.PositionToBottom_Button.Click -= PositionToButton_Click;
            BaseGridView_HeaderGrid_Foot_Buttons.PositionToBottom_Button.Click += PositionToButton_Click;
            BaseGridView_HeaderGrid_Foot_Buttons.PositionToTop_Button.Click -= PositionToButton_Click;
            BaseGridView_HeaderGrid_Foot_Buttons.PositionToTop_Button.Click += PositionToButton_Click;
        }

        void RemoveEvent()
        {
            if (scrollViewer != null) scrollViewer.ViewChanging -= ScrollViewer_ViewChanging;
            App.playListReader.Updated -= PlayListReader_Updated;
            MainWindow.MainViewStateChanged -= MainWindow_MainViewStateChanged;
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
            await App.playListReader.Refresh();
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
