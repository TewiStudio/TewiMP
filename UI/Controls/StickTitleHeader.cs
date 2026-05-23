using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using TewiMP.Helpers;
using Windows.Foundation;
using TewiMP.Services;

namespace TewiMP.UI.Controls;

public partial class StickTitleHeader : Control
{
    #region Dependency Propertys
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(StickTitleHeader),
        new PropertyMetadata("Title")
    );
    
    public static readonly DependencyProperty CommandBarProperty =
        DependencyProperty.Register(
            nameof(CommandBar),
            typeof(object),
            typeof(StickTitleHeader),
            new PropertyMetadata(null));
    #endregion

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public object CommandBar
    {
        get => GetValue(CommandBarProperty);
        set => SetValue(CommandBarProperty, value);
    }

    public ScrollViewer CachedScrollviewer => GetScrollViewer(this);

    public StickTitleHeader()
    {
        DefaultStyleKey = typeof(StickTitleHeader);
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
    private ExpressionAnimation _titleScaleAnim;
    private ExpressionAnimation _titleOffsetAnim;
    private ExpressionAnimation _infoOffsetAnim;
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

        float stickScale = .74f;
        float paddingSize = (int)(_PART_Root.ActualHeight - _PART_TitleTextBlock.ActualSize.Y * stickScale);

        // Visuals
        var headerRootVisual = ElementCompositionPreview.GetElementVisual(_PART_Root);
        var headerBackgroundVisual = ElementCompositionPreview.GetElementVisual(_PART_Background);
        var titleVisual = ElementCompositionPreview.GetElementVisual(_PART_TitleTextBlock);

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

        // Title Scale
        if (_titleScaleAnim is null)
        {
            string exp = $"Lerp(Vector2(1,1), Vector2(stickScale, stickScale), {ProgressExp})";
            _titleScaleAnim = _compositor.CreateExpressionAnimation(exp);
            _titleScaleAnim.SetReferenceParameter("scroller", _scrollerPropSet);
        }
        _titleScaleAnim.SetScalarParameter("stickScale", stickScale);
        _titleScaleAnim.SetScalarParameter("Padding", paddingSize);
        titleVisual.StartAnimation("Scale.xy", _titleScaleAnim);

        // Title Offset
        // X: 0 -> -12, Y: 0 -> 24
        if (true || _titleOffsetAnim is null)
        {
            string exp = $"Lerp(Vector3(0,0,0), Vector3(-12, Padding - 12, 0), {ProgressExp})";
            _titleOffsetAnim = _compositor.CreateExpressionAnimation(exp);
            _titleOffsetAnim.SetReferenceParameter("scroller", _scrollerPropSet);
        }
        _titleOffsetAnim.SetScalarParameter("Padding", paddingSize);
        titleVisual.StartAnimation(nameof(titleVisual.Offset), _titleOffsetAnim);

        // --------------
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
    #endregion

    #region Events
    private Grid _PART_Root;
    private Border _PART_Background;
    private StackPanel _PART_Info_Root;
    private TextBlock _PART_TitleTextBlock;
    private ContentPresenter _PART_CommandBar;
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _PART_Root = GetTemplateChild("PART_Root") as Grid;
        _PART_Background = GetTemplateChild("PART_Background") as Border;
        _PART_Info_Root = GetTemplateChild("PART_Info_Root") as StackPanel;
        _PART_TitleTextBlock = GetTemplateChild("PART_TitleTextBlock") as TextBlock;
        _PART_CommandBar = GetTemplateChild("PART_CommandBar") as ContentPresenter;

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

