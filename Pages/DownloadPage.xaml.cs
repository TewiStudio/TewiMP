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
            HeaderBaseTextBlock.Text = $"下载（{App.Instance.DownloadManager.DownloadedData.Count}/{App.Instance.DownloadManager.AllDownloadData.Count} - {App.Instance.DownloadManager.DownloadingData.Count} 下载中，{App.Instance.DownloadManager.DownloadErrorData.Count} 错误）";
        }
        private void DownloadPage_Loaded(object sender, RoutedEventArgs e)
        {
            App.Instance.DownloadManager.NowDownloadPage = this;
            UpdateTextTB();
            ListViewBase.ItemsSource = DownloadDatas;

            App.Instance.DownloadManager.AddDownload += DownloadManager_AddDownload;
            App.Instance.DownloadManager.OnDownloading += DownloadManager_OnDownloading;
            App.Instance.DownloadManager.OnDownloadedSaving += DownloadManager_OnDownloadedSaving;
            App.Instance.DownloadManager.OnDownloadedPreview += DownloadManager_OnDownloading;
            App.Instance.DownloadManager.OnDownloaded += DownloadManager_OnDownloading;
            App.Instance.DownloadManager.OnDownloadError += DownloadManager_OnDownloading;

            // 当第一次初始化时加载
            foreach (var dm in App.Instance.DownloadManager.AllDownloadData)
            {
                DownloadDatas.Add(dm);
            }
            foreach (var dm in App.Instance.DownloadManager.DownloadingData)
            {
                App.Instance.DownloadManager.CallOnDownloadingEvent(dm);
            }
            foreach (var dm in App.Instance.DownloadManager.DownloadedData)
            {
                App.Instance.DownloadManager.CallOnDownloadedEvent(dm);
            }
            foreach (var dm in App.Instance.DownloadManager.DownloadErrorData)
            {
                App.Instance.DownloadManager.CallOnDownloadErrorEvent(dm);
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
            App.Instance.DownloadManager.AddDownload -= DownloadManager_AddDownload;
            App.Instance.DownloadManager.OnDownloading -= DownloadManager_OnDownloading;
            App.Instance.DownloadManager.OnDownloadedPreview -= DownloadManager_OnDownloading;
            App.Instance.DownloadManager.OnDownloadedSaving -= DownloadManager_OnDownloadedSaving;
            App.Instance.DownloadManager.OnDownloaded -= DownloadManager_OnDownloading;
            App.Instance.DownloadManager.OnDownloadError -= DownloadManager_OnDownloading;
        }

        Visual headerVisual;
        private ExpressionAnimation _headerOffsetAnim;
        private ExpressionAnimation _logoScaleAnim;
        private ExpressionAnimation _logoOffsetAnim;
        private ExpressionAnimation _bgOpacityAnim;

        private ScrollViewer _cachedScrollViewer;
        private CompositionPropertySet _scrollerPropSet;
        private Compositor _compositor;

        // 预定义常量表达式
        private const string ProgressExp = "Clamp(-scroller.Translation.Y / Padding, 0, 1.0)";

        // 安全获取 ScrollViewer
        private ScrollViewer GetScrollViewer(DependencyObject root)
        {
            // 如果已经缓存且有效，直接返回
            if (_cachedScrollViewer != null) return _cachedScrollViewer;

            // 尝试查找
            if (VisualTreeHelper.GetChildrenCount(root) > 0)
            {
                var child = VisualTreeHelper.GetChild(root, 0) as Border;
                if (child?.Child is ScrollViewer sv)
                {
                    _cachedScrollViewer = sv;
                    _cachedScrollViewer.CanContentRenderOutsideBounds = true;
                    return sv;
                }
            }
            return null;
        }

        public void UpdateShyHeader()
        {
            // 1. 获取 ScrollViewer
            var scrollViewer = GetScrollViewer(ListViewBase);
            if (scrollViewer is null) return;

            // 2. 处理 ZIndex (仅当需要时处理)
            // 注意：修改 ListView 内部容器的 ZIndex 是为了让 Header 浮在 Item 上面
            if (ListViewBase.Header != null)
            {
                var headerPresenter = VisualTreeHelper.GetParent((UIElement)ListViewBase.Header) as UIElement;
                if (headerPresenter != null)
                {
                    var headerContainer = VisualTreeHelper.GetParent(headerPresenter) as UIElement;
                    // 只有当 ZIndex 不对时才设置，避免重复调用
                    if (headerContainer != null && Canvas.GetZIndex(headerContainer) != 1)
                    {
                        Canvas.SetZIndex(headerContainer, 1);
                    }
                }
            }

            // 3. 初始化 Compositor 和 PropertySet
            if (_scrollerPropSet is null)
            {
                _scrollerPropSet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
                _compositor = _scrollerPropSet.Compositor;
            }

            // 4. 准备参数
            float paddingSize = 40f;

            // 获取 Visuals
            var headerVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseGrid);
            var logoVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseTextBlock);
            var backgroundVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseRectangle);

            // 动画 1: Header Offset Y (Sticky Effect)
            if (_headerOffsetAnim is null)
            {
                // 逻辑: -scroller.Y - (Progress * Padding)
                string exp = $"-scroller.Translation.Y - ({ProgressExp} * Padding)";
                _headerOffsetAnim = _compositor.CreateExpressionAnimation(exp);
                _headerOffsetAnim.SetReferenceParameter("scroller", _scrollerPropSet);
            }
            _headerOffsetAnim.SetScalarParameter("Padding", paddingSize);
            headerVisual.StartAnimation("Offset.Y", _headerOffsetAnim);

            // 动画 2: Logo Scale
            if (_logoScaleAnim is null)
            {
                string exp = $"Lerp(Vector2(1,1), Vector2(0.7, 0.7), {ProgressExp})";
                _logoScaleAnim = _compositor.CreateExpressionAnimation(exp);
                _logoScaleAnim.SetReferenceParameter("scroller", _scrollerPropSet);
            }
            _logoScaleAnim.SetScalarParameter("Padding", paddingSize);
            logoVisual.StartAnimation("Scale.xy", _logoScaleAnim);

            // 动画 3: Logo Offset (合并 X 和 Y)
            // X: 0 -> -12, Y: 0 -> 24
            if (_logoOffsetAnim is null)
            {
                string exp = $"Lerp(Vector3(0,0,0), Vector3(-12, 24, 0), {ProgressExp})";
                _logoOffsetAnim = _compositor.CreateExpressionAnimation(exp);
                _logoOffsetAnim.SetReferenceParameter("scroller", _scrollerPropSet);
            }
            _logoOffsetAnim.SetScalarParameter("Padding", paddingSize);
            logoVisual.StartAnimation(nameof(logoVisual.Offset), _logoOffsetAnim);

            // 动画 4: Background Opacity
            if (_bgOpacityAnim is null)
            {
                string exp = $"Lerp(0, 1, {ProgressExp})";
                _bgOpacityAnim = _compositor.CreateExpressionAnimation(exp);
                _bgOpacityAnim.SetReferenceParameter("scroller", _scrollerPropSet);
            }
            _bgOpacityAnim.SetScalarParameter("Padding", paddingSize);
            backgroundVisual.StartAnimation("Opacity", _bgOpacityAnim);
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
            App.MainWindowInstance.SetNavViewContent(
                typeof(SettingPage),
                "open download");
        }

        private void PausePlayBtn_Click(object sender, RoutedEventArgs e)
        {
            if (App.Instance.DownloadManager.PauseDownload)
            {
                App.Instance.DownloadManager.PauseDownload = false;
                PausePlayBtn.Label = "暂停下载";
                PausePlayIcon.Glyph = "\uE769";
            }
            else
            {
                App.Instance.DownloadManager.PauseDownload = true;
                PausePlayBtn.Label = "继续下载";
                PausePlayIcon.Glyph = "\uE768";
            }
        }
    }
}
