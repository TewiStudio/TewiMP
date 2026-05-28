using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using TewiMP.Helpers;
using Microsoft.UI.Xaml.Markup;

namespace TewiMP.UI.Controls;

public partial class StickContentHeader : ContentControl
{
    #region Dependency Propertys
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(StickContentHeader),
        new PropertyMetadata("Title")
    );

    public static readonly DependencyProperty BackgroundStartOpacityProperty = DependencyProperty.Register(
        nameof(BackgroundStartOpacity),
        typeof(double),
        typeof(StickContentHeader),
        new PropertyMetadata(0.0)
    );
    
    public static readonly DependencyProperty CommandBarProperty =
        DependencyProperty.Register(
            nameof(CommandBar),
            typeof(object),
            typeof(StickContentHeader),
            new PropertyMetadata(null));
    #endregion

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public double BackgroundStartOpacity
    {
        get => (double)GetValue(BackgroundStartOpacityProperty);
        set => SetValue(BackgroundStartOpacityProperty, value);
    }

    public object CommandBar
    {
        get => GetValue(CommandBarProperty);
        set => SetValue(CommandBarProperty, value);
    }

    public ScrollViewer CachedScrollViewer => GetScrollViewer(this);

    public StickContentHeader()
    {
        DefaultStyleKey = typeof(StickContentHeader);
    }

    #region Stick Header
    private ScrollViewer _cachedScrollViewer;
    private ItemsStackPanel _cachedItemsStackPanel;
    private ContentControl _cachedContentControl;
    private CompositionPropertySet _scrollerPropSet;
    private Compositor _compositor;
    private InsetClip _itemsStackPanelClip;

    private ExpressionAnimation _offsetAnim;
    private ExpressionAnimation _itemsStackClipAnim;
    private ExpressionAnimation _bgOpacityAnim;

    private const string ProgressExp = "Clamp(-scroller.Translation.Y / Padding, 0, 1.0)";
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

    private ContentControl GetContentControl(DependencyObject root)
    {
        if (_cachedContentControl != null) return _cachedContentControl;
        if (CodeHelper.FindParent<ContentControl>(root) is ContentControl contentControl)
        {
            _cachedContentControl = contentControl;
            return _cachedContentControl;
        }
        return null;
    }

    public void UpdateStickHeader()
    {
        var scrollViewer = GetScrollViewer(this);
        if (scrollViewer is null) return;

        var itemsStackPanel = GetItemsStackPanel(this);
        if (itemsStackPanel is not null)
        {
            var itemsStackPanelVisual = ElementCompositionPreview.GetElementVisual(itemsStackPanel);
            _itemsStackPanelClip = itemsStackPanelVisual.Compositor.CreateInsetClip();
            itemsStackPanelVisual.Clip = _itemsStackPanelClip;
        }

        var contentControl = GetContentControl(this);
        if (_scrollerPropSet is null)
        {
            _scrollerPropSet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
            _compositor = _scrollerPropSet.Compositor;
        }

        float paddingSize = _PART_Root.ActualSize.Y - _PART_Content.ActualSize.Y;

        // Visuals
        var headerRootVisual = ElementCompositionPreview.GetElementVisual(_PART_Root);
        var headerBackgroundVisual = ElementCompositionPreview.GetElementVisual(_PART_Background);

        // Header Offset Y
        if (_offsetAnim is null)
        {
            string exp = $"-scroller.Translation.Y - ({ProgressExp} * Padding)";
            _offsetAnim = _compositor.CreateExpressionAnimation(exp);
            _offsetAnim.SetReferenceParameter("scroller", _scrollerPropSet);
        }
        _offsetAnim.SetScalarParameter("Padding", paddingSize == 0 ? 1 : paddingSize);
        headerRootVisual.StartAnimation("Offset.Y", _offsetAnim);

        // ItemsStack Clip
        if (_itemsStackPanelClip is not null)
        {
            if (_itemsStackClipAnim is null)
            {
                string exp = $"-scroller.Translation.Y - ({ProgressExp} * Padding) - headerHeight + height";
                _itemsStackClipAnim = _compositor.CreateExpressionAnimation(exp);
                _itemsStackClipAnim.SetReferenceParameter("scroller", _scrollerPropSet);
            }
            _itemsStackClipAnim.SetScalarParameter("Padding", paddingSize);
            _itemsStackClipAnim.SetScalarParameter("headerHeight", contentControl.ActualSize.Y);
            _itemsStackClipAnim.SetScalarParameter("height", ActualSize.Y);
            _itemsStackPanelClip.StartAnimation(nameof(_itemsStackPanelClip.TopInset), _itemsStackClipAnim);
        }

        // --------------
        // Background Opacity
        if (_bgOpacityAnim is null)
        {
            string exp = $"Lerp(opacity, 1, {ProgressExp})";
            _bgOpacityAnim = _compositor.CreateExpressionAnimation(exp);
            _bgOpacityAnim.SetReferenceParameter("scroller", _scrollerPropSet);
        }
        _bgOpacityAnim.SetScalarParameter("Padding", paddingSize);
        _bgOpacityAnim.SetScalarParameter("opacity", (float)BackgroundStartOpacity);
        headerBackgroundVisual.StartAnimation("Opacity", _bgOpacityAnim);
    }
    #endregion

    #region Events
    private Grid _PART_Root;
    private Border _PART_Background;
    private Grid _PART_Info_Root;
    private TextBlock _PART_TitleTextBlock;
    private ContentPresenter _PART_CommandBar;
    private ContentPresenter _PART_Content;
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _PART_Root = GetTemplateChild("PART_Root") as Grid;
        _PART_Background = GetTemplateChild("PART_Background") as Border;
        _PART_Info_Root = GetTemplateChild("PART_Info_Root") as Grid;
        _PART_TitleTextBlock = GetTemplateChild("PART_TitleTextBlock") as TextBlock;
        _PART_CommandBar = GetTemplateChild("PART_CommandBar") as ContentPresenter;
        _PART_Content = GetTemplateChild("PART_Content") as ContentPresenter;

        Loaded -= StickHeaderListView_Loaded;
        Loaded += StickHeaderListView_Loaded;
        Unloaded -= StickHeaderListView_Unloaded;
        Unloaded += StickHeaderListView_Unloaded;
    }

    private void StickHeaderListView_Loaded(object sender, RoutedEventArgs e)
    {
        SizeChanged += StickHeaderListView_SizeChanged;
        GetContentControl(this).SizeChanged += StickHeaderListView_SizeChanged;
        UpdateStickHeader();
    }

    private void StickHeaderListView_Unloaded(object sender, RoutedEventArgs e)
    {
        SizeChanged -= StickHeaderListView_SizeChanged;
        GetContentControl(this).SizeChanged -= StickHeaderListView_SizeChanged;
        Loaded -= StickHeaderListView_Loaded;
        Unloaded -= StickHeaderListView_Unloaded;
    }

    private void StickHeaderListView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateStickHeader();
    }
    #endregion
}

