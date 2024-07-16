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
using Microsoft.UI.Composition;
using TewiMP.Controls;
using TewiMP.DataEditor;

namespace TewiMP.Pages
{
    public partial class HistoryPage : Page
    {
        bool isLeavedPage = false;
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            isLeavedPage = true;
            LeavingPageDo();
        }

        async void LeavingPageDo()
        {
            await Task.Delay(500);
            HistoryHelper.HistoryDataChanged -= HistoryHelper_HistoryDataChanged;
            songHistories.Clear();
            ListViewBase.ItemsSource = null;
            ListViewBase.Items.Clear();
        }

        ObservableCollection<SongHistoryData> songHistories = new();
        public HistoryPage()
        {
            InitializeComponent();
            ListViewBase.ItemsSource = songHistories;
            HistoryHelper.HistoryDataChanged += HistoryHelper_HistoryDataChanged;
        }

        private void HistoryHelper_HistoryDataChanged()
        {
            if (HeaderSelectBase.SelectedIndex == 0)
                Init();
        }

        private async void Init()
        {
            if (isLeavedPage) return;
            var scrollOffset = scrollViewer.VerticalOffset;
            var datas = await SongHistoryHelper.GetHistories();
            List<SongHistoryData> d = [.. datas];
            d = d.OrderByDescending(m => m.Time).ToList();
            if (isLeavedPage) return;
            songHistories.Clear();
            foreach (var data in d)
            {
                songHistories.Add(data);
            }
            await Task.Delay(10);
            scrollViewer.ScrollToVerticalOffset(scrollOffset);
            if (isLeavedPage) LeavingPageDo();
        }

        Visual headerVisual;
        Visual headerSelectVisual;
        public void UpdateShyHeader()
        {
            if (scrollViewer == null) return;
            CompositionPropertySet scrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
            Compositor compositor = scrollerPropertySet.Compositor;

            var padingSize = HeaderBaseTextBlock_Base.ActualHeight - 8;
            // Get the visual that represents our HeaderTextBlock 
            // And define the progress animation string
            String progress = $"Clamp(-scroller.Translation.Y / {padingSize}, 0, 1.0)";

            // Shift the header by 50 pixels when scrolling down
            var offsetExpression = compositor.CreateExpressionAnimation($"-scroller.Translation.Y - {progress} * {padingSize}");
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
            /*var logoHeaderScaleAnimation = compositor.CreateExpressionAnimation("Lerp(Vector2(1,1), Vector2(0.7, 0.7), " + progress + ")");
            logoHeaderScaleAnimation.SetReferenceParameter("scroller", scrollerPropertySet);

            var logoVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseTextBlock);
            logoVisual.StartAnimation("Scale.xy", logoHeaderScaleAnimation);

            var logoVisualOffsetXAnimation = compositor.CreateExpressionAnimation($"Lerp(0, -12, {progress})");
            logoVisualOffsetXAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            logoVisual.StartAnimation("Offset.X", logoVisualOffsetXAnimation);

            var logoVisualOffsetYAnimation = compositor.CreateExpressionAnimation($"Lerp(0, 22, {progress})");
            logoVisualOffsetYAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            logoVisual.StartAnimation("Offset.Y", logoVisualOffsetYAnimation);

            var backgroundVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseRectangle);
            var backgroundVisualOpacityAnimation = compositor.CreateExpressionAnimation($"Lerp(0, 1, {progress})");
            backgroundVisualOpacityAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            backgroundVisual.StartAnimation("Opacity", backgroundVisualOpacityAnimation);

*//*
            var selectVisualOffsetYAnimation = compositor.CreateExpressionAnimation($"Lerp(34, {padingSize} + 2, {progress})");
            selectVisualOffsetYAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            headerSelectVisual.StartAnimation("Offset.Y", selectVisualOffsetYAnimation);

            var selectVisualOffsetXAnimation = compositor.CreateExpressionAnimation($"Lerp(100, 60, {progress})");
            selectVisualOffsetXAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            headerSelectVisual.StartAnimation("Offset.X", selectVisualOffsetXAnimation);*/

            var selectVisualOffsetYAnimation = compositor.CreateExpressionAnimation($"Lerp(34, 34, {progress})");
            selectVisualOffsetYAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            headerSelectVisual.StartAnimation("Offset.Y", selectVisualOffsetYAnimation);

            var selectVisualOffsetXAnimation = compositor.CreateExpressionAnimation($"Lerp(100, 100, {progress})");
            selectVisualOffsetXAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            headerSelectVisual.StartAnimation("Offset.X", selectVisualOffsetXAnimation);
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateShyHeader();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await App.playingList.Play(((sender as Button).DataContext as SongHistoryData).MusicData);
        }

        ScrollViewer scrollViewer;
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // 设置header为顶层
            var headerPresenter = (UIElement)VisualTreeHelper.GetParent((UIElement)ListViewBase.Header);
            var headerContainer = (UIElement)VisualTreeHelper.GetParent(headerPresenter);
            Canvas.SetZIndex(headerContainer, 1);

            headerVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseGrid);
            headerSelectVisual = ElementCompositionPreview.GetElementVisual(HeaderSelectBase);
            scrollViewer = (VisualTreeHelper.GetChild(ListViewBase, 0) as Border).Child as ScrollViewer;
            scrollViewer.CanContentRenderOutsideBounds = true;
            scrollViewer.ViewChanging += ScrollViewer_ViewChanging;

            UpdateShyHeader();
            await Task.Delay(1);
            UpdateShyHeader();
        }

        private void ScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            headerVisual.IsPixelSnappingEnabled = true;
        }

        private void Segmented_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isLoaded) return;
            if (HeaderSelectBase.SelectedIndex == 0)
            {
                HeaderText.Visibility = Visibility.Visible;
                ListViewBase.Items.Clear();
                ListViewBase.ItemsSource = songHistories;
                ListViewBase.ItemTemplate = this.Resources["HistoryDataTemplate"] as DataTemplate;
                Init();
            }
            else
            {
                HeaderText.Visibility = Visibility.Collapsed;
                songHistories.Clear();
                ListViewBase.ItemsSource = null;
                ListViewBase.ItemTemplate = null;
                ListViewBase.Items.Add(new SongHistoryInfo() { Margin = new(0, 12, 0, 0) });
            }
        }

        bool isLoaded = false;
        private void HeaderSelectBase_Loaded(object sender, RoutedEventArgs e)
        {
            ListViewBase.ItemTemplate = this.Resources["HistoryDataTemplate"] as DataTemplate;
            Init();
            isLoaded = true;
        }
    }
}
