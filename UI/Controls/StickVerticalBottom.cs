using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using TewiMP.Helpers;

namespace TewiMP.UI.Controls;

public partial class StickVerticalBottom : ContentControl
{
    public ScrollViewer CachedScrollviewer => GetScrollViewer(this);

    public StickVerticalBottom()
    {
        RegisterPropertyChangedCallback(VerticalAlignmentProperty, VerticalAlignmentChanged);
        DefaultStyleKey = typeof(StickVerticalBottom);
    }

    #region Stick Header
    private ScrollViewer _cachedScrollViewer;
    private ItemsStackPanel _cachedItemsStackPanel;
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

    private ItemsStackPanel GetItemsStackPanel(DependencyObject root)
    {
        if (_cachedItemsStackPanel != null) return _cachedItemsStackPanel;
        if (CodeHelper.FindParent<ItemsPresenter>(root) is ItemsPresenter itemsPresenter)
        {
            _cachedItemsStackPanel = CodeHelper.FindDescendant<ItemsStackPanel>(itemsPresenter);
            return _cachedItemsStackPanel;
        }
        return null;
    }

    public void UpdateStickHeader()
    {
        var scrollViewer = GetScrollViewer(this);
        if (scrollViewer is null) return;

        if (_scrollerPropSet is null)
        {
            _scrollerPropSet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
            _compositor = _scrollerPropSet.Compositor;

            Canvas.SetZIndex(CodeHelper.FindParent<ContentControl>(this), 1);
        }

        // Visuals
        var headerRootVisual = ElementCompositionPreview.GetElementVisual(_PART_Root);

        // Offset Y
        if (true || _offsetAnim is null)
        {
            string exp = $"-scroller.Translation.Y + pageHeight - height";
            _offsetAnim = _compositor.CreateExpressionAnimation(exp);
            _offsetAnim.SetReferenceParameter("scroller", _scrollerPropSet);
        }
        _offsetAnim.SetScalarParameter("pageHeight", scrollViewer.ActualSize.Y);
        _offsetAnim.SetScalarParameter("height", ActualSize.Y);
        headerRootVisual.StartAnimation("Offset.Y", _offsetAnim);
    }
    #endregion

    #region Events
    private Border _PART_Root;
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _PART_Root = GetTemplateChild("PART_Root") as Border;

        Loaded -= StickHeaderListView_Loaded;
        Loaded += StickHeaderListView_Loaded;
        Unloaded -= StickHeaderListView_Unloaded;
        Unloaded += StickHeaderListView_Unloaded;
    }

    private void StickHeaderListView_Loaded(object sender, RoutedEventArgs e)
    {
        SizeChanged += StickVerticalBottom_SizeChanged;
        GetScrollViewer(this).SizeChanged += StickVertical_SizeChanged;
        OnSizeChanged();
    }

    private void StickHeaderListView_Unloaded(object sender, RoutedEventArgs e)
    {
        SizeChanged -= StickVerticalBottom_SizeChanged;
        GetScrollViewer(this).SizeChanged -= StickVertical_SizeChanged;
        Loaded -= StickHeaderListView_Loaded;
        Unloaded -= StickHeaderListView_Unloaded;
    }

    private void StickVerticalBottom_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        OnSizeChanged();
    }

    private void StickVertical_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        OnSizeChanged();
    }

    private void VerticalAlignmentChanged(DependencyObject sender, DependencyProperty dp)
    {
        //OnSizeChanged();
    }

    private void OnSizeChanged()
    {
        GetItemsStackPanel(this)?.Margin = new(0, 0, 0, _PART_Root.ActualHeight);
        UpdateStickHeader();
    }
    #endregion
}

