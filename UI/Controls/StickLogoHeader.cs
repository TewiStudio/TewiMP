using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using TewiMP.Helpers;

namespace TewiMP.UI.Controls;

public partial class StickLogoHeader : Control
{
    #region Dependency Propertys
    public static readonly DependencyProperty LogoProperty =
        DependencyProperty.Register(
            nameof(Logo),
            typeof(object),
            typeof(StickLogoHeader),
            new PropertyMetadata(null));
    
    public static readonly DependencyProperty LogoMarginProperty =
        DependencyProperty.Register(
            nameof(LogoMargin),
            typeof(Thickness),
            typeof(StickLogoHeader),
            new PropertyMetadata(new Thickness(16)));

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(StickLogoHeader),
        new PropertyMetadata("Title")
    );

    public static readonly DependencyProperty InfoProperty = DependencyProperty.Register(
        nameof(Info),
        typeof(string),
        typeof(StickLogoHeader),
        new PropertyMetadata("Info")
    );

    public static readonly DependencyProperty PaddingHeightProperty = DependencyProperty.Register(
        nameof(PaddingHeight),
        typeof(double),
        typeof(StickLogoHeader),
        new PropertyMetadata(0d, new((_, __) =>
        {
            (_ as StickLogoHeader)?.UpdateStickHeader();
        })
    ));

    public static readonly DependencyProperty LogoStickyScaleProperty = DependencyProperty.Register(
        nameof(LogoStickyScale),
        typeof(double),
        typeof(StickLogoHeader),
        new PropertyMetadata(.5d, new((_, __) =>
        {
            (_ as StickLogoHeader)?.UpdateStickHeader();
        })
    ));
    #endregion

    public object Logo
    {
        get => GetValue(LogoProperty);
        set => SetValue(LogoProperty, value);
    }

    public Thickness LogoMargin
    {
        get => (Thickness)GetValue(LogoMarginProperty);
        set => SetValue(LogoMarginProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Info
    {
        get => (string)GetValue(InfoProperty);
        set => SetValue(InfoProperty, value);
    }

    public double LogoStickyScale
    {
        get => (double)GetValue(LogoStickyScaleProperty);
        set => SetValue(LogoStickyScaleProperty, value);
    }

    public double PaddingHeight
    {
        get => (double)GetValue(PaddingHeightProperty);
        set => SetValue(PaddingHeightProperty, value);
    }

    public StickLogoHeader()
    {
        DefaultStyleKey = typeof(StickLogoHeader);
    }

    #region Stick Header
    private ScrollViewer _cachedScrollViewer;
    private CompositionPropertySet _scrollerPropSet;
    private Compositor _compositor;
    private InsetClip _itemsStackPanelClip;

    private ExpressionAnimation _offsetAnim;
    private ExpressionAnimation _itemsStackClipAnim;
    private ExpressionAnimation _logoScaleAnim;
    private ExpressionAnimation _logoOffsetAnim;
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

            var itemsStackPanel = CodeHelper.FindDescendant<ItemsStackPanel>(sv);
            var itemsStackPanelVisual = ElementCompositionPreview.GetElementVisual(itemsStackPanel);
            _itemsStackPanelClip = itemsStackPanelVisual.Compositor.CreateInsetClip();
            itemsStackPanelVisual.Clip = _itemsStackPanelClip;
            return sv;
        }
        return null;
    }

    public void UpdateStickHeader()
    {
        var scrollViewer = GetScrollViewer(this);
        if (scrollViewer is null) return;

        if (_PART_Root != null)
        {
            var headerPresenter = VisualTreeHelper.GetParent(_PART_Root) as UIElement;
            if (headerPresenter != null)
            {
                var headerContainer = VisualTreeHelper.GetParent(headerPresenter) as UIElement;
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

        float paddingSize = (float)(_PART_Root.ActualHeight - _PART_Logo.ActualSize.Y * LogoStickyScale - LogoMargin.Top - LogoMargin.Bottom + PaddingHeight);

        // Visuals
        var headerRootVisual = ElementCompositionPreview.GetElementVisual(_PART_Root);
        var logoVisual = ElementCompositionPreview.GetElementVisual(_PART_Logo);
        var infoVisual = ElementCompositionPreview.GetElementVisual(_PART_InfoGrid);
        var headerBackgroundVisual = ElementCompositionPreview.GetElementVisual(_PART_Background);

        // Header Offset Y
        if (_offsetAnim is null)
        {
            string exp = $"-scroller.Translation.Y - ({ProgressExp} * Padding)";
            _offsetAnim = _compositor.CreateExpressionAnimation(exp);
            _offsetAnim.SetReferenceParameter("scroller", _scrollerPropSet);
        }
        _offsetAnim.SetScalarParameter("Padding", paddingSize);
        headerRootVisual.StartAnimation("Offset.Y", _offsetAnim);

        // ItemsStack Clip
        if (_itemsStackClipAnim is null)
        {
            string exp = $"-scroller.Translation.Y - ({ProgressExp} * Padding)";
            _itemsStackClipAnim = _compositor.CreateExpressionAnimation(exp);
            _itemsStackClipAnim.SetReferenceParameter("scroller", _scrollerPropSet);
        }
        _itemsStackClipAnim.SetScalarParameter("Padding", paddingSize);
        _itemsStackPanelClip.StartAnimation(nameof(_itemsStackPanelClip.TopInset), _itemsStackClipAnim);

        // --------------
        // Logo
        // Logo Scale
        if (_logoScaleAnim is null)
        {
            string exp = $"Lerp(Vector2(1, 1), Vector2(TargetScale, TargetScale), {ProgressExp})";
            _logoScaleAnim = _compositor.CreateExpressionAnimation(exp);
            _logoScaleAnim.SetReferenceParameter("scroller", _scrollerPropSet);
        }
        _logoScaleAnim.SetScalarParameter("Padding", paddingSize);
        _logoScaleAnim.SetScalarParameter("TargetScale", (float)LogoStickyScale);
        logoVisual.StartAnimation("Scale.xy", _logoScaleAnim);

        // Logo Offset
        if (_logoOffsetAnim is null)
        {
            string exp = $"Lerp(Vector3(0,0,0), Vector3(0, Padding, 0), {ProgressExp})";
            _logoOffsetAnim = _compositor.CreateExpressionAnimation(exp);
            _logoOffsetAnim.SetReferenceParameter("scroller", _scrollerPropSet);
        }
        _logoOffsetAnim.SetScalarParameter("Padding", paddingSize);
        logoVisual.StartAnimation("Offset", _logoOffsetAnim);

        // Info Visual Offset
        if (_infoOffsetAnim is null)
        {
            // Start: (StartX, 0, 0) -> End: (EndX, Padding, 0)
            string exp = $"Lerp(Vector3(StartX, 0, 0), Vector3(EndX, Padding, 0), {ProgressExp})";
            _infoOffsetAnim = _compositor.CreateExpressionAnimation(exp);
            _infoOffsetAnim.SetReferenceParameter("scroller", _scrollerPropSet);
        }
        _infoOffsetAnim.SetScalarParameter("Padding", paddingSize);
        _infoOffsetAnim.SetScalarParameter("StartX", _PART_Logo.ActualSize.X + 16);
        _infoOffsetAnim.SetScalarParameter("EndX", (int)(_PART_Logo.ActualSize.X * LogoStickyScale) + 16);
        infoVisual.StartAnimation(nameof(infoVisual.Offset), _infoOffsetAnim);

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
    private UIElement _PART_Logo;
    private UIElement _PART_InfoGrid;
    private TextBlock _PART_TitleTextBlock;
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _PART_Root = GetTemplateChild("PART_Root") as Grid;
        _PART_Background = GetTemplateChild("PART_Background") as Border;
        _PART_Logo = GetTemplateChild("PART_Logo") as UIElement;
        _PART_InfoGrid = GetTemplateChild("PART_InfoGrid") as UIElement;
        _PART_TitleTextBlock = GetTemplateChild("PART_TitleTextBlock") as TextBlock;

        Loaded -= StickHeaderListView_Loaded;
        Loaded += StickHeaderListView_Loaded;
        Unloaded -= StickHeaderListView_Unloaded;
        Unloaded += StickHeaderListView_Unloaded;
    }

    private void StickHeaderListView_Loaded(object sender, RoutedEventArgs e)
    {
        SizeChanged += StickHeaderListView_SizeChanged;
        UpdateStickHeader();
    }

    private void StickHeaderListView_Unloaded(object sender, RoutedEventArgs e)
    {
        SizeChanged -= StickHeaderListView_SizeChanged;
        Loaded -= StickHeaderListView_Loaded;
        Unloaded -= StickHeaderListView_Unloaded;
    }

    private void StickHeaderListView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateStickHeader();
    }
    #endregion
}

