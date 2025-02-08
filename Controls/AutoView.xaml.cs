using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace TewiMP.Controls
{
    public enum ViewMode { Horizontal, Vertical, None };
    public partial class AutoScrollViewer : ScrollView
    {
        public static readonly DependencyProperty ViewModeProperty = DependencyProperty.Register(
            "ViewMode",
            typeof(ViewMode),
            typeof(AutoScrollViewer),
            new PropertyMetadata(ViewMode.Horizontal, null)
        );
        public static readonly DependencyProperty PauseProperty = DependencyProperty.Register(
            "Pause",
            typeof(bool),
            typeof(AutoScrollViewer),
            new PropertyMetadata(false, null)
        );

        public ViewMode ViewMode
        {
            get => (ViewMode)GetValue(ViewModeProperty);
            set => SetValue(ViewModeProperty, value);
        }

        public bool Pause
        {
            get => (bool)GetValue(PauseProperty);
            set
            {
                SetValue(PauseProperty, value);
                if (!value)
                {
                    RepeatChangeView();
                }
                else
                {
                    ScrollTo(0, 0, new(ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore));
                }
            }
        }
        public bool isHorizontalContentOutOfBounds { get; private set; } = false;
        public bool isVerticalContentOutOfBounds { get; private set; } = false;
        public int RepeatTime { get; set; } = 3000;

        public AutoScrollViewer()
        {
            InitializeComponent();
        }

        FrameworkElement content;
        private void GetSizeResult()
        {
            if (Content is null) return;
            content = (FrameworkElement)Content;
            if (content.ActualWidth > ActualWidth) isHorizontalContentOutOfBounds = true;
            else isHorizontalContentOutOfBounds = false;
            if (content.ActualHeight > ActualSize.Y) isVerticalContentOutOfBounds = true;
            else isVerticalContentOutOfBounds = false;
        }

        private void sizeChanged(object sender, SizeChangedEventArgs e)
        {
            //RGClip.Rect = new(0, 0, ActualWidth, ActualHeight);
            RepeatChangeView();
        }

        bool isAddedVelocity = false;
        bool pauseChanged = false;
        private async void RepeatChangeView()
        {
            //App.logManager.Log("autoview", $"Repeating. {Content}");
            if (Content is null) return;
            if (Visibility == Visibility.Collapsed) return;
            if (isAddedVelocity) return;
            if (ActualSize.X <= 0 || ActualSize.Y <= 0) return;

            GetSizeResult();
            if (!isHorizontalContentOutOfBounds && !isVerticalContentOutOfBounds) return;

            isAddedVelocity = true;
            await Task.Delay(RepeatTime);
            isAddedVelocity = false;

            if (Pause) return;

            GetSizeResult();
            if (!isHorizontalContentOutOfBounds && !isVerticalContentOutOfBounds) return;

            if (HorizontalOffset == 0 ? false : true)
            {
                ScrollTo(0, 0, new(ScrollingAnimationMode.Enabled));
                return;
            }

            if (isHorizontalContentOutOfBounds)
            {
                float velocity = (float)Math.Min(80f, Math.Max(4, ActualWidth / 4 + content.ActualWidth / 12));
                AddScrollVelocity(new(velocity, 0), new());
            }
            if (isVerticalContentOutOfBounds)
            {
                float velocity = Math.Min(80f, Math.Max(4f, ActualSize.Y / 4 + Content.ActualSize.Y / 12));
                AddScrollVelocity(new(0, velocity), new());
            }
        }

        private async void unloaded(object sender, RoutedEventArgs e)
        {

        }

        private void ScrollView_ScrollCompleted(ScrollView sender, ScrollingScrollCompletedEventArgs args)
        {
            if (Pause) return;
            RepeatChangeView();
        }

        private void ScrollView_ViewChanged(ScrollView sender, object args)
        {

        }

        private void ScrollView_ExtentChanged(ScrollView sender, object args)
        {
            if (Content is not null)
            {
                (Content as FrameworkElement).Unloaded += AutoScrollViewer_Unloaded;
                ScrollTo(0, 0, new(ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore));
                Pause = false;
                //App.logManager.Log("Content Changed.");
            }
        }

        private void AutoScrollViewer_Unloaded(object sender, RoutedEventArgs e)
        {
            Pause = true;
            //App.logManager.Log("Content Unloaded.");
            (sender as FrameworkElement).Unloaded -= AutoScrollViewer_Unloaded;
            content = null;
            UnloadObject(Content);
            UnloadObject(this);
        }
    }
}
