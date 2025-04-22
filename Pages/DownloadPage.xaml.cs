using System;
using System.Linq;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Composition;
using TewiMP.Background;

namespace TewiMP.Pages
{
    public partial class DownloadPage : Page
    {
        public ObservableCollection<DownloadData> DownloadDatas = new();
        public DownloadPage()
        {
            InitializeComponent();
            Loaded += DownloadPage_Loaded;
            Unloaded += DownloadPage_Unloaded;
        }

        private void UpdateTextTB()
        {
            PausePlayBtn.Visibility = DownloadDatas.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            HeaderBaseTextBlock.Text = $"下载（{App.downloadManager.DownloadedData.Count}/{App.downloadManager.AllDownloadData.Count} - {App.downloadManager.DownloadingData.Count} 下载中，{App.downloadManager.DownloadErrorData.Count} 错误）";
        }
        private void DownloadPage_Loaded(object sender, RoutedEventArgs e)
        {
            App.downloadManager.NowDownloadPage = this;
            UpdateTextTB();
            ListViewBase.ItemsSource = DownloadDatas;

            App.downloadManager.AddDownload += DownloadManager_AddDownload;
            App.downloadManager.OnDownloading += DownloadManager_OnDownloading;
            App.downloadManager.OnDownloadedSaving += DownloadManager_OnDownloadedSaving;
            App.downloadManager.OnDownloadedPreview += DownloadManager_OnDownloading;
            App.downloadManager.OnDownloaded += DownloadManager_OnDownloading;
            App.downloadManager.OnDownloadError += DownloadManager_OnDownloading;

            // 当第一次初始化时加载
            foreach (var dm in App.downloadManager.AllDownloadData)
            {
                DownloadDatas.Add(dm);
            }
            foreach (var dm in App.downloadManager.DownloadingData)
            {
                App.downloadManager.CallOnDownloadingEvent(dm);
            }
            foreach (var dm in App.downloadManager.DownloadedData)
            {
                App.downloadManager.CallOnDownloadedEvent(dm);
            }
            foreach (var dm in App.downloadManager.DownloadErrorData)
            {
                App.downloadManager.CallOnDownloadErrorEvent(dm);
            }

            if (!DownloadDatas.Any())
            {
                ListEmptyPopup.Visibility = Visibility.Visible;
                AtListBottomTb.Visibility = Visibility.Collapsed;
            }
            else
            {
                ListEmptyPopup.Visibility = Visibility.Collapsed;
                AtListBottomTb.Visibility = Visibility.Visible;
            }
        }

        private void DownloadManager_OnDownloadedSaving(DownloadData data)
        {
            UpdateTextTB();
        }

        private void DownloadManager_OnDownloading(DownloadData data)
        {
            UpdateTextTB();
        }

        private void DownloadManager_AddDownload(DownloadData data)
        {
            DownloadDatas.Add(data);
            UpdateTextTB();
        }

        private void DownloadPage_Unloaded(object sender, RoutedEventArgs e)
        {
            ListViewBase.ItemsSource = null;
            App.downloadManager.AddDownload -= DownloadManager_AddDownload;
            App.downloadManager.OnDownloading -= DownloadManager_OnDownloading;
            App.downloadManager.OnDownloadedPreview -= DownloadManager_OnDownloading;
            App.downloadManager.OnDownloadedSaving -= DownloadManager_OnDownloadedSaving;
            App.downloadManager.OnDownloaded -= DownloadManager_OnDownloading;
            App.downloadManager.OnDownloadError -= DownloadManager_OnDownloading;
        }

        ScrollViewer scrollViewer;
        Visual headerVisual;
        public void UpdateShyHeader()
        {
            // 设置header为顶层
            var headerPresenter = (UIElement)VisualTreeHelper.GetParent((UIElement)ListViewBase.Header);
            var headerContainer = (UIElement)VisualTreeHelper.GetParent(headerPresenter);
            Canvas.SetZIndex(headerContainer, 1);

            if (scrollViewer is null)
            {
                headerVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseGrid);
                scrollViewer = (VisualTreeHelper.GetChild(ListViewBase, 0) as Border).Child as ScrollViewer;
                scrollViewer.CanContentRenderOutsideBounds = true;
                scrollViewer.ViewChanging += ScrollViewer_ViewChanging;
            }

            CompositionPropertySet scrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
            Compositor compositor = scrollerPropertySet.Compositor;

            var padingSize = 40;
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
            var logoHeaderScaleAnimation = compositor.CreateExpressionAnimation("Lerp(Vector2(1,1), Vector2(0.7, 0.7), " + progress + ")");
            logoHeaderScaleAnimation.SetReferenceParameter("scroller", scrollerPropertySet);

            var logoVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseTextBlock);
            logoVisual.StartAnimation("Scale.xy", logoHeaderScaleAnimation);

            var logoVisualOffsetYAnimation = compositor.CreateExpressionAnimation($"Lerp(0, 24, {progress})");
            logoVisualOffsetYAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            logoVisual.StartAnimation("Offset.Y", logoVisualOffsetYAnimation);

            var logoVisualOffsetXAnimation = compositor.CreateExpressionAnimation($"Lerp(0, -12, {progress})");
            logoVisualOffsetXAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            logoVisual.StartAnimation("Offset.X", logoVisualOffsetXAnimation);

            var backgroundVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseRectangle);
            var backgroundVisualOpacityAnimation = compositor.CreateExpressionAnimation($"Lerp(0, 1, {progress})");
            backgroundVisualOpacityAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            backgroundVisual.StartAnimation("Opacity", backgroundVisualOpacityAnimation);
        }

        private void ScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            headerVisual.IsPixelSnappingEnabled = true;
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateShyHeader();
        }

        private void ToSettingBtn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.SetNavViewContent(
                typeof(SettingPage),
                "open download");
        }

        private void PausePlayBtn_Click(object sender, RoutedEventArgs e)
        {
            if (App.downloadManager.PauseDownload)
            {
                App.downloadManager.PauseDownload = false;
                PausePlayBtn.Label = "暂停下载";
                PausePlayIcon.Glyph = "\uE769";
            }
            else
            {
                App.downloadManager.PauseDownload = true;
                PausePlayBtn.Label = "继续下载";
                PausePlayIcon.Glyph = "\uE768";
            }
        }
    }
}
