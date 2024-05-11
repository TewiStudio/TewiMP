using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Composition;
using TewiMP.Helpers;
using TewiMP.DataEditor;

namespace TewiMP.Pages
{
    public partial class PlayListPage : Page
    {
        public PlayListPage()
        {
            InitializeComponent();
            App.playListReader.Updated += PlayListReader_Updated;
        }

        private void PlayListReader_Updated()
        {
            UpdatePlayList();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ItemsViewer.ItemsSource = playListCards;
            MainWindow.MainViewStateChanged += MainWindow_MainViewStateChanged;
            UpdatePlayList();
        }

        ObservableCollection<MusicListData> playListCards = new();
        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            await Task.Delay(500);
            App.playListReader.Updated -= PlayListReader_Updated;
            MainWindow.MainViewStateChanged -= MainWindow_MainViewStateChanged;
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
            var dpi = CodeHelper.GetScaleAdjustment(App.WindowLocal);

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

        CompositionPropertySet scrollerPropertySet;
        Compositor compositor;
        Visual headerVisual;
        Visual backgroundVisual;
        Visual logoVisual;
        ScrollViewer scrollViewer;
        private void UpdateShyHeader()
        {
            if (scrollViewer == null)
            {
                scrollViewer = (VisualTreeHelper.GetChild(BaseGridView, 0) as Border).Child as ScrollViewer;
                scrollViewer.CanContentRenderOutsideBounds = true;
                scrollViewer.ViewChanging += ScrollViewer_ViewChanging;
            }

            var padingSize = 40;
            // Get the visual that represents our HeaderTextBlock 
            // And define the progress animation string
            var progress = $"Clamp(-scroller.Translation.Y / {padingSize}, 0, 1.0)";

            if (scrollerPropertySet == null)
            {
                scrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
                compositor = scrollerPropertySet.Compositor;
                headerVisual = ElementCompositionPreview.GetElementVisual(BaseGridView_HeaderGrid);
                logoVisual = ElementCompositionPreview.GetElementVisual(BaseGridView_HeaderTextBlock);
                backgroundVisual = ElementCompositionPreview.GetElementVisual(BaseGridView_HeaderRectangle);
            }

            // Shift the header by 50 pixels when scrolling down
            var offsetExpression = compositor.CreateExpressionAnimation($"-scroller.Translation.Y - Round({progress} * {padingSize})");
            offsetExpression.SetReferenceParameter("scroller", scrollerPropertySet);
            headerVisual.StartAnimation("Offset.Y", offsetExpression);

            /*
            Visual textVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseTextBlock);
            Vector3 finalOffset = new Vector3(0, 10, 0);
            var headerOffsetAnimation = compositor.CreateExpressionAnimation($"Lerp(Vector3(0,0,0), finalOffset, {progress})");
            headerOffsetAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            headerOffsetAnimation.SetVector3Parameter("finalOffset", finalOffset);
            textVisual.StartAnimation(nameof(Visual.Offset), headerOffsetAnimation);
            */

            // Logo scale and transform                                          from               to
            var logoHeaderScaleAnimation = compositor.CreateExpressionAnimation("Lerp(Vector2(1,1), Vector2(0.7, 0.7), " + progress + ")");
            logoHeaderScaleAnimation.SetReferenceParameter("scroller", scrollerPropertySet);

            logoVisual.StartAnimation("Scale.xy", logoHeaderScaleAnimation);

            var logoVisualOffsetYAnimation = compositor.CreateExpressionAnimation($"Lerp(0, 24, {progress})");
            logoVisualOffsetYAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            logoVisual.StartAnimation("Offset.Y", logoVisualOffsetYAnimation);

            var logoVisualOffsetXAnimation = compositor.CreateExpressionAnimation($"Lerp(0, -12, {progress})");
            logoVisualOffsetXAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            logoVisual.StartAnimation("Offset.X", logoVisualOffsetXAnimation);

            var backgroundVisualOpacityAnimation = compositor.CreateExpressionAnimation($"Lerp(0, 1, {progress})");
            backgroundVisualOpacityAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            backgroundVisual.StartAnimation("Opacity", backgroundVisualOpacityAnimation);
        }

        private void ScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            headerVisual.IsPixelSnappingEnabled = true;
        }

        private void BaseGridView_Loaded(object sender, RoutedEventArgs e)
        {
            // 设置header为顶层
            var headerPresenter = (UIElement)VisualTreeHelper.GetParent((UIElement)BaseGridView.Header);
            var headerContainer = (UIElement)VisualTreeHelper.GetParent(headerPresenter);
            Canvas.SetZIndex(headerContainer, 1);

            UpdateShyHeader();
            ItemsViewer.ScrollView.HorizontalScrollBarVisibility = ScrollingScrollBarVisibility.Hidden;
            ItemsViewer.ScrollView.HorizontalScrollMode = ScrollingScrollMode.Disabled;
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
            UpdateShyHeader();
        }

        private async void AppBarButton_Click_2(object sender, RoutedEventArgs e)
        {
            await DialogPages.InsertPlayListPage.ShowDialog();
        }

        private void ItemContainer_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
