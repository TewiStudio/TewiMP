using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Controls;

namespace TewiMP.UI.Controls
{
    [ContentProperty(Name = "Content")]
    public sealed partial class AutoScrollView : Control
    {
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            nameof(Content),
            typeof(object),
            typeof(AutoScrollView),
            new PropertyMetadata(null)
        );

        public object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public static readonly DependencyProperty RepeatTimeProperty = DependencyProperty.Register(
            "RepeatTime",
            typeof(int),
            typeof(AutoScrollView),
            new PropertyMetadata(3000, null)
        );

        public int RepeatTime
        {
            get => (int)GetValue(RepeatTimeProperty);
            set => SetValue(RepeatTimeProperty, value);
        }

        public static readonly DependencyProperty ScrollSpeedRatioProperty = DependencyProperty.Register(
            "ScrollSpeedRatio",
            typeof(double),
            typeof(AutoScrollView),
            new PropertyMetadata(.55d, null)
        );

        public double ScrollSpeedRatio
        {
            get => (double)GetValue(ScrollSpeedRatioProperty);
            set => SetValue(ScrollSpeedRatioProperty, value);
        }

        public static readonly DependencyProperty MaskSizeProperty = DependencyProperty.Register(
            "MaskSize",
            typeof(int),
            typeof(AutoScrollView),
            new PropertyMetadata(15, new((_,  __) =>
            {
                (_ as AutoScrollView)?.ComputeGradientStops();
            })
        ));

        public int MaskSize
        {
            get => (int)GetValue(MaskSizeProperty);
            set => SetValue(MaskSizeProperty, value);
        }

        public static readonly DependencyProperty PauseProperty = DependencyProperty.Register(
            "Pause",
            typeof(bool),
            typeof(AutoScrollView),
            new PropertyMetadata(false, null)
        );

        public bool Pause
        {
            get => (bool)GetValue(PauseProperty);
            set
            {
                SetValue(PauseProperty, value);
                if (!value)
                {
                    RepeatChangeView();
                    _beforeIsHorizontalScrolling = 1;
                    IsHorizontalScrolling = 1;
                    ComputeGradientStops();
                }
                else
                {
                    _scrollView?.ScrollTo(0, 0, new(ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore));
                    _beforeIsHorizontalScrolling = 1;
                    IsHorizontalScrolling = 1;
                    ComputeGradientStops();
                }
            }
        }

        bool isAddedVelocity = false;
        int _isHorizontalScrolling = 1;
        int _beforeIsHorizontalScrolling = 1;

        /// <summary>
        /// 1 为不动, 0 为往回， 2 为往前
        /// </summary>
        public int IsHorizontalScrolling
        {
            get => _isHorizontalScrolling;
            set
            {
                _beforeIsHorizontalScrolling = _isHorizontalScrolling;
                _isHorizontalScrolling = value;
            }
        }
        public bool IsHorizontalContentOutOfBounds { get; private set; } = false;
        public bool IsVerticalContentOutOfBounds { get; private set; } = false;

        public AutoScrollView()
        {
            DefaultStyleKey = typeof(AutoScrollView);
        }

        #region Events
        private GradientStop _gs1;
        private GradientStop _gs2;
        private Rectangle _gs1a;
        private Rectangle _gs2a;
        private ContentPresenter _contentPresenter;
        private ScrollView _scrollView;
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Loaded += AutoScrollView_Loaded;
            Unloaded += AutoScrollView_Unloaded;
            _gs1 = GetTemplateChild("PART_GS1") as GradientStop;
            _gs2 = GetTemplateChild("PART_GS2") as GradientStop;
            _gs1a = GetTemplateChild("PART_GS1_animation") as Rectangle;
            _gs2a = GetTemplateChild("PART_GS2_animation") as Rectangle;
            _contentPresenter = GetTemplateChild("PART_ContentPresenter") as ContentPresenter;
            _scrollView = GetTemplateChild("PART_ScrollView") as ScrollView;

            if (_scrollView != null)
            {
                _scrollView.SizeChanged -= _scrollView_SizeChanged;
                _scrollView.SizeChanged += _scrollView_SizeChanged;

                _scrollView.ViewChanged -= _scrollView_ViewChanged;
                _scrollView.ViewChanged += _scrollView_ViewChanged;

                _scrollView.ExtentChanged -= _scrollView_ExtentChanged;
                _scrollView.ExtentChanged += _scrollView_ExtentChanged;

                _scrollView.ScrollCompleted -= _scrollView_ScrollCompleted;
                _scrollView.ScrollCompleted += _scrollView_ScrollCompleted;

                _scrollView.ViewChanged -= _scrollView_ViewChanged1;
                _scrollView.ViewChanged += _scrollView_ViewChanged1;
            }

            ComputeGradientStops();
        }

        private void AutoScrollView_Loaded(object sender, RoutedEventArgs e)
        {
            RepeatChangeView();
        }

        private void AutoScrollView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_scrollView != null)
            {
                _scrollView.SizeChanged -= _scrollView_SizeChanged;
                _scrollView.ViewChanged -= _scrollView_ViewChanged;
                _scrollView.ExtentChanged -= _scrollView_ExtentChanged;
                _scrollView.ScrollCompleted -= _scrollView_ScrollCompleted;
                _scrollView.ViewChanged -= _scrollView_ViewChanged1;
            }
            Loaded -= AutoScrollView_Loaded;
            Unloaded -= AutoScrollView_Unloaded;
        }

        private void _scrollView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            GetSizeResult();
            ComputeGradientStops();
            RepeatChangeView();
        }

        private void _scrollView_ViewChanged(ScrollView sender, object args)
        {

        }

        private void _scrollView_ExtentChanged(ScrollView sender, object args)
        {
            _scrollView.ScrollTo(0, 0, new(ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore));
            RepeatChangeView();
            ComputeGradientStops();
        }

        private void _scrollView_ScrollCompleted(ScrollView sender, ScrollingScrollCompletedEventArgs args)
        {
            IsHorizontalScrolling = 1;
            if (Pause) return;
            RepeatChangeView();
            ComputeGradientStops();
        }
        
        private void _scrollView_ViewChanged1(ScrollView sender, object args)
        {

        }
        #endregion

        /// <summary>
        /// 计算透明渐变偏移值
        /// </summary>
        public void ComputeGradientStops()
        {
            if (_gs1 is null || _gs2 is null) return;

            double gs1Offset = 0;
            double gs2Offset = 0;

            // 导致布局死循环
            // 渐变效果的偏移量计算，无论实际宽度是多少，偏移量的结果都应该是 MaskSize
            /*if (ActualWidth <= MaskSize)
            {
                // 当实际宽度太小时将偏移值设置为实际宽度的四分之一
                gs1Offset = ActualWidth / 2d;
                gs2Offset = 1d - ActualWidth / 2d;
                _contentPresenter.Padding = new(0, 0, ActualWidth / 4d, 0);
            }
            else*/
            
            // 计算 MaskSize 在新宽度中的占比
            gs1Offset = MaskSize / ActualWidth;
            gs2Offset = 1d - MaskSize / ActualWidth;
            _contentPresenter.Padding = new(0, 0, IsHorizontalContentOutOfBounds ? MaskSize : 0, 0);
            

            _gs1a.Width = MaskSize;
            _gs2a.Width = MaskSize;
            if (IsHorizontalScrolling == 1) // 当滚动停止时
            {
                if (_beforeIsHorizontalScrolling == 2)
                {
                    _gs1a.Opacity = 0;
                    _gs2a.Opacity = 1;
                }
                else if (_beforeIsHorizontalScrolling == 0)
                {
                    _gs1a.Opacity = 1;
                    _gs2a.Opacity = 0;
                }
                else
                {
                    if (IsHorizontalContentOutOfBounds)
                    {
                        _gs1a.Opacity = 1;
                        _gs2a.Opacity = 0;
                    }
                    else
                    {
                        _gs1a.Opacity = 1;
                        _gs2a.Opacity = 1;
                    }
                }
            }
            else if (IsHorizontalScrolling == 2 || IsHorizontalScrolling == 0) // 当向前向后滚动时
            {
                if (IsHorizontalScrolling == 0)
                {
                    _gs1a.Opacity = 1;
                    _gs2a.Opacity = 0;
                }
                else
                {
                    _gs1a.Opacity = 0;
                    _gs2a.Opacity = 0;
                }
            }

            _gs1.Offset = double.Clamp(gs1Offset, 0, 1);
            _gs2.Offset = double.Clamp(gs2Offset, 0, 1);
            //LogManager.Info("Debug", $"{_gs1.Offset} / {_gs2.Offset} | {ActualWidth * _gs1.Offset} : {ActualWidth}");
        }

        /// <summary>
        /// 当内容大小发生变化时，判断 Content 是否超出边界
        /// </summary>
        private void GetSizeResult()
        {
            if (_contentPresenter is null) return;
            var content = _contentPresenter;

            if (content.ActualWidth > ActualWidth) IsHorizontalContentOutOfBounds = true;
            else IsHorizontalContentOutOfBounds = false;
            if (content.ActualHeight > ActualHeight) IsVerticalContentOutOfBounds = true;
            else IsVerticalContentOutOfBounds = false;
            //LogManager.Info("Debug", $"IsHorizontalContentOutOfBounds: {IsHorizontalContentOutOfBounds} / IsVerticalContentOutOfBounds: {IsVerticalContentOutOfBounds}");
        }

        private async void RepeatChangeView()
        {
            if (Content is null) return;
            if (Visibility == Visibility.Collapsed) return;
            if (isAddedVelocity) return;
            if (ActualSize.X <= 0 || ActualSize.Y <= 0) return;

            GetSizeResult();
            if (!IsHorizontalContentOutOfBounds && !IsVerticalContentOutOfBounds) return;

            isAddedVelocity = true;
            await Task.Delay(RepeatTime);
            isAddedVelocity = false;

            if (Pause)
            {
                ComputeGradientStops();
                return;
            }

            GetSizeResult();
            if (!IsHorizontalContentOutOfBounds && !IsVerticalContentOutOfBounds) return;

            if (_scrollView.HorizontalOffset != 0) // 当前位置不位于起始时，滑动到起始
            {
                _scrollView.ScrollTo(0, 0, new(ScrollingAnimationMode.Enabled));
                IsHorizontalScrolling = 0;
                ComputeGradientStops();
                return;
            }

            // 计算 Content 实际长度并开始滚动
            if (IsHorizontalContentOutOfBounds)
            {
                float velocity = (float)Math.Min(80f, Math.Max(4, ActualWidth / 4 + _contentPresenter.ActualWidth / 12));
                _scrollView.AddScrollVelocity(new(velocity * (float)ScrollSpeedRatio, 0), new());
                IsHorizontalScrolling = 2;
            }
            if (IsVerticalContentOutOfBounds)
            {
                float velocity = Math.Min(80f, Math.Max(4f, ActualSize.Y / 4 + _contentPresenter.ActualSize.Y / 12));
                _scrollView.AddScrollVelocity(new(0, velocity * (float)ScrollSpeedRatio), new());
            }

            ComputeGradientStops();
        }

    }
}
