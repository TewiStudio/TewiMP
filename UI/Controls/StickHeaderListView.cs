using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Composition;

namespace TewiMP.UI.Controls;

public partial class StickHeaderListView : ListView
{
    public StickHeaderListView()
    {
        DefaultStyleKey = typeof(StickHeaderListView);
    }

    private ExpressionAnimation _headerOffsetAnim;
    private ExpressionAnimation _logoScaleAnim;
    private ExpressionAnimation _logoOffsetAnim;
    private ExpressionAnimation _bgOpacityAnim;

    private ScrollViewer _cachedScrollViewer;
    private CompositionPropertySet _scrollerPropSet;
    private Compositor _compositor;

    private const string ProgressExp = "Clamp(-scroller.Translation.Y / Padding, 0, 1.0)";
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

    public void UpdateStickHeader()
    {
        var scrollViewer = GetScrollViewer(this);
        if (scrollViewer is null) return;

        if (Header != null)
        {
            var headerPresenter = VisualTreeHelper.GetParent((UIElement)Header) as UIElement;
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

        if (_scrollerPropSet is null)
        {
            _scrollerPropSet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
            _compositor = _scrollerPropSet.Compositor;
        }

        float paddingSize = 40f;

        // 获取 Visuals
        var headerRootVisual = ElementCompositionPreview.GetElementVisual(Header_Root);
        var headerTextBlockVisual = ElementCompositionPreview.GetElementVisual(Header_TextBlock);
        var headerBackgroundVisual = ElementCompositionPreview.GetElementVisual(Header_Background);

        // Header Offset Y
        if (_headerOffsetAnim is null)
        {
            string exp = $"-scroller.Translation.Y - ({ProgressExp} * Padding)";
            _headerOffsetAnim = _compositor.CreateExpressionAnimation(exp);
            _headerOffsetAnim.SetReferenceParameter("scroller", _scrollerPropSet);
        }
        _headerOffsetAnim.SetScalarParameter("Padding", paddingSize);
        headerRootVisual.StartAnimation("Offset.Y", _headerOffsetAnim);

        // Logo Scale
        if (_logoScaleAnim is null)
        {
            string exp = $"Lerp(Vector2(1,1), Vector2(0.7, 0.7), {ProgressExp})";
            _logoScaleAnim = _compositor.CreateExpressionAnimation(exp);
            _logoScaleAnim.SetReferenceParameter("scroller", _scrollerPropSet);
        }
        _logoScaleAnim.SetScalarParameter("Padding", paddingSize);
        headerTextBlockVisual.StartAnimation("Scale.xy", _logoScaleAnim);

        // Logo Offset (合并 X 和 Y)
        // X: 0 -> -12, Y: 0 -> 24
        if (_logoOffsetAnim is null)
        {
            string exp = $"Lerp(Vector3(0,0,0), Vector3(-12, 24, 0), {ProgressExp})";
            _logoOffsetAnim = _compositor.CreateExpressionAnimation(exp);
            _logoOffsetAnim.SetReferenceParameter("scroller", _scrollerPropSet);
        }
        _logoOffsetAnim.SetScalarParameter("Padding", paddingSize);
        headerTextBlockVisual.StartAnimation(nameof(headerTextBlockVisual.Offset), _logoOffsetAnim);

        // Background Opacity
        if (_bgOpacityAnim is null)
        {
            string exp = $"Lerp(0, 1, {ProgressExp})";
            _bgOpacityAnim = _compositor.CreateExpressionAnimation(exp);
            _bgOpacityAnim.SetReferenceParameter("scroller", _scrollerPropSet);
        }
        _bgOpacityAnim.SetScalarParameter("Padding", paddingSize);
        headerBackgroundVisual.StartAnimation("Opacity", _bgOpacityAnim);
    }

    private Grid Header_Root;
    private Border Header_Background;
    private TextBlock Header_TextBlock;
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        Loaded += StickHeaderListView_Loaded;
        Unloaded += StickHeaderListView_Unloaded;
    }

    private void StickHeaderListView_Loaded(object sender, RoutedEventArgs e)
    {
        Header_Root = Header as Grid;
        Header_Background = Header_Root.FindName("Header_Background") as Border;
        Header_TextBlock = Header_Root.FindName("Header_TextBlock") as TextBlock;

        SizeChanged += StickHeaderListView_SizeChanged;
    }

    private void StickHeaderListView_Unloaded(object sender, RoutedEventArgs e)
    {
        Loaded -= StickHeaderListView_Loaded;
        Unloaded -= StickHeaderListView_Unloaded;
        SizeChanged -= StickHeaderListView_SizeChanged;
    }

    private void StickHeaderListView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateStickHeader();
    }
}

