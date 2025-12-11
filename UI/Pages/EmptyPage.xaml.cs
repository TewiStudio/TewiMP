using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Composition;

namespace TewiMP.UI.Pages
{
    public partial class EmptyPage : Page
    {
        public EmptyPage()
        {
            InitializeComponent();
        }

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

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateShyHeader();
        }
    }
}
