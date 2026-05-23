using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using TewiMP.Helpers;

namespace TewiMP.UI.Controls;

public partial class StickVertical : ContentControl
{
    public ScrollViewer CachedScrollviewer => GetScrollViewer(this);

    public StickVertical()
    {
        DefaultStyleKey = typeof(StickVertical);
    }

    #region Stick Header
    private ScrollViewer _cachedScrollViewer;
    private CompositionPropertySet _scrollerPropSet;
    private Compositor _compositor;
    private ExpressionAnimation _offsetAnim;

    private ScrollViewer GetScrollViewer(DependencyObject root)
    {
        if (_cachedScrollViewer != null) return _cachedScrollViewer;
        if (CodeHelper.FindParent<ScrollViewer>(root) is ScrollViewer sv)
        {
            _cachedScrollViewer = sv;
            _cachedScrollViewer.CanContentRenderOutsideBounds = true;
            return sv;
        }
        return null;
    }

    private ContentControl _parent;
    public void UpdateStickHeader()
    {
        var scrollViewer = GetScrollViewer(this);
        if (scrollViewer is null) return;

        if (_scrollerPropSet is null)
        {
            _parent = CodeHelper.FindParent<ContentControl>(this);
            _scrollerPropSet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
            _compositor = _scrollerPropSet.Compositor;
        }

        // Visuals
        var headerRootVisual = ElementCompositionPreview.GetElementVisual(_parent);

        // Offset Y
        if (_offsetAnim is null)
        {
            string exp = $"-scroller.Translation.Y";
            _offsetAnim = _compositor.CreateExpressionAnimation(exp);
            _offsetAnim.SetReferenceParameter("scroller", _scrollerPropSet);
        }
        headerRootVisual.StartAnimation("Offset.Y", _offsetAnim);
    }
    #endregion

    #region Events
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        Loaded -= StickHeaderListView_Loaded;
        Loaded += StickHeaderListView_Loaded;
        Unloaded -= StickHeaderListView_Unloaded;
        Unloaded += StickHeaderListView_Unloaded;
    }

    private void StickHeaderListView_Loaded(object sender, RoutedEventArgs e)
    {
        VerticalAlignment = VerticalAlignment.Top;
        GetScrollViewer(this).SizeChanged += StickVertical_SizeChanged;
        Height = GetScrollViewer(this).ActualHeight;
        UpdateStickHeader();
    }

    private void StickHeaderListView_Unloaded(object sender, RoutedEventArgs e)
    {
        GetScrollViewer(this).SizeChanged -= StickVertical_SizeChanged;
        Loaded -= StickHeaderListView_Loaded;
        Unloaded -= StickHeaderListView_Unloaded;
    }

    private void StickVertical_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        Height = GetScrollViewer(this).ActualHeight;
        UpdateStickHeader();
    }
    #endregion
}

