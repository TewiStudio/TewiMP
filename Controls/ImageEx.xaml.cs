using System;
using System.Collections;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using TewiMP.Helpers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TewiMP.Controls
{
    public enum ImageTransitionType
    {
        None,
        Fade,
        SlideLeft,
        SlideRight,
        Blur
    }

    public partial class ImageEx : Grid
    {
        public delegate void ImageLoadedDelegate(bool isLoaded);
        public event ImageLoadedDelegate ImageLoaded;
        public static bool ImageDarkMass { get; set; } = false;
        public ImageTransitionType TransitionType { get; set; } = ImageTransitionType.Fade;
        public enum PointInBehaviors { Tapped, OnlyLightUp, None }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(
                "Source",
                typeof(Uri),
                typeof(ImageEx),
                new(null, OnImageSourceChanged)
                );
        public Uri Source
        {
            get { return GetValue(SourceProperty) as Uri; }
            set { SetValue(SourceProperty, value); }
        }
        private static void OnImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ImageEx ie = d as ImageEx;
            var value = e.NewValue as Uri;
            ie.SetImageSource(value);
        }
        
        public Stretch Stretch
        {
            get => Image_Control.Stretch;
            set => Image_Control.Stretch = value;
        }
        public PointInBehaviors PointInBehavior { get; set; } = PointInBehaviors.Tapped;
        public string SaveName { get; set; } = null;
        /// <summary>
        /// 为 true 时图片切换时不会等到下一张图片加载完成后再显示动画
        /// </summary>
        public bool SwitchImageImmediateSetOpacity { get; set; } = false;

        ArrayList array = null;
        public ImageEx()
        {
            InitializeComponent();
            //array = new ArrayList(1000000);
        }

        bool isInitedVisuals = false;
        Visual controlVisual;
        Visual rootVisual;
        Visual gammaMassVisual; 
        ScalarKeyFrameAnimation animationMassOpacity_MouseIn = null;
        ScalarKeyFrameAnimation animationMassOpacity_MouseExited = null;
        ScalarKeyFrameAnimation animationSize_MouseIn = null;
        ScalarKeyFrameAnimation animationSize_MouseExited = null;
        ScalarKeyFrameAnimation animationSize_MousePressed = null;
        ScalarKeyFrameAnimation animationSize_MouseReleased = null;
        void InitVisual()
        {
            isInitedVisuals = true;
            root_Background.Visibility = Visibility.Collapsed;

            controlVisual = ElementCompositionPreview.GetElementVisual(control);
            rootVisual = ElementCompositionPreview.GetElementVisual(root);
            gammaMassVisual = ElementCompositionPreview.GetElementVisual(Image_GammaMass);
            rootVisual.Opacity = 1;
            gammaMassVisual.Opacity = 0;

            // 鼠标移入遮罩动画
            AnimateHelper.AnimateScalar(
                gammaMassVisual, 1f, 0.2,
                0.2f, 1, 0.22f, 1f,
                out animationMassOpacity_MouseIn);
            // 鼠标移入 Size 动画
            AnimateHelper.AnimateScalar(
                rootVisual, 1.07f, 0.2,
                0.2f, 1, 0.22f, 1f,
                out animationSize_MouseIn);
            // 鼠标移出遮罩动画
            AnimateHelper.AnimateScalar(
                gammaMassVisual, 0, 1.3,
                0, 0, 0, 0,
                out animationMassOpacity_MouseExited);
            // 鼠标移出 Size 动画
            AnimateHelper.AnimateScalar(
                rootVisual, 1f, 1.5,
                0.2f, 1, 0.22f, 1f,
                out animationSize_MouseExited);
            // 鼠标按下 Size 动画
            AnimateHelper.AnimateScalar(
                rootVisual, 0.93f, 0.5,
                0.2f, 1, 0.22f, 1f,
                out animationSize_MousePressed);
            // 鼠标松起 Size 动画
            AnimateHelper.AnimateScalar(
                rootVisual, 1.07f, 1.5,
                0.2f, 1, 0.22f, 1f,
                out animationSize_MouseReleased);
        }

        Uri currentImageSource;
        Uri currentOldImageSource;
        bool isInit = false; // 当之前的图片源为空时，值为 true
        private void SetImageSource(Uri imageSource)
        {
            if (controlVisual is null) return;
            ImageLoaded?.Invoke(false);

            isInit = currentImageSource is null;

            if (imageSource == null)
            {
                currentImageSource = null;
                currentOldImageSource = null;
                Image_ControlSources.UriSource = null;
                Image_Old_ControlSources.UriSource = null;
                Image_Control.Visibility = Visibility.Collapsed;
                Image_Old.Visibility = Visibility.Collapsed;
                Image_Control.Visibility = Visibility.Visible;
                Image_Old.Visibility = Visibility.Visible;
                return;
            }

            currentImageSource = imageSource;
            currentOldImageSource = Image_ControlSources.UriSource ?? imageSource;

            if (TransitionType == ImageTransitionType.None)
            {
                OneOpacityAnimation.Start();
                Image_ControlSources.UriSource = imageSource;
                return;
            }

            if (isInit) Image_Old.Visibility = Visibility.Collapsed;
            if (currentOldImageSource == Image_Old_ControlSources.UriSource)
            {
                Image_Old_ControlSources.UriSource = null;
            }
            Image_Old_ControlSources.UriSource = currentOldImageSource;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            InitVisual();
            SetImageSource(Source);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            rootVisual = null;
            controlVisual = null;
            gammaMassVisual = null;
            animationMassOpacity_MouseIn = null;
            animationMassOpacity_MouseExited = null;
            animationSize_MouseIn = null;
            animationSize_MouseExited = null;
            animationSize_MousePressed = null;
            animationSize_MouseReleased = null;
        }

        // 图片加载完成时加入淡入动画
        private async void Image_Control_ImageOpened(object sender, RoutedEventArgs e)
        {
            if (sender is not Image image) return;

            if (image.Tag as string == "Old")
            {
                if (Image_Old_ControlSources.UriSource != currentOldImageSource) return;

                Image_Old.Visibility = isInit ? Visibility.Collapsed : Visibility.Visible;
                ResetBlurAnimation.Start();
                ZeroOpacityAnimation.Start();
                Image_ControlSources.UriSource = currentImageSource;
            }
            else
            {
                ImageLoaded?.Invoke(true);
                if (TransitionType == ImageTransitionType.None) return;

                var oldUri = currentOldImageSource;

                switch (TransitionType)
                {
                    case ImageTransitionType.Fade:
                        ImageOpacityAnimation.Duration = TimeSpan.FromSeconds(4);
                        await OpacityAnimation.StartAsync();
                        break;

                    case ImageTransitionType.SlideLeft:
                    case ImageTransitionType.SlideRight:
                        ImageBlurAnimation.Duration = TimeSpan.FromSeconds(8);
                        ImageOpacityAnimation.Duration = TimeSpan.FromSeconds(4);
                        ImageSliderAnimation.From = $"{(TransitionType == ImageTransitionType.SlideRight ? -1 : 1) * Image_Control.ActualWidth / 6},0,0";
                        ImageSliderAnimation.To = "0,0,0";
                        ImageSliderAnimation.Duration = TimeSpan.FromSeconds(1.5f);
                        SliderAnimation.Start();
                        BlurAnimation.Start();
                        await OpacityAnimation.StartAsync();
                        break;

                    case ImageTransitionType.Blur:
                        ImageOpacityAnimation.Duration = TimeSpan.FromSeconds(4);
                        ImageBlurAnimation.Duration = TimeSpan.FromSeconds(4);
                        OpacityAnimation.Start();
                        if (!isInit)
                            await BlurAnimation.StartAsync();
                        break;
                }

                // Reset old image if necessary
                if (Image_Old_ControlSources.UriSource == oldUri)
                {
                    Image_Old_ControlSources.UriSource = null;
                    Image_Old.Visibility = Visibility.Collapsed;
                    Image_Old.Visibility = Visibility.Visible;
                }
            }
        }

        private void UserControl_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (IsMouse4Click) { IsMouse4Click = false; return; }
            if (PointInBehavior == PointInBehaviors.None || PointInBehavior == PointInBehaviors.OnlyLightUp) return;
            //if (e.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
            Windowed.ImageViewerWindow.ShowWindow(Image_ControlSources.UriSource, SaveName);
        }

        bool isPointEnter = false;
        private void UserControl_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (rootVisual is null) return;
            if (gammaMassVisual is null) return;
            if (PointInBehavior == PointInBehaviors.None) return;
            isPointEnter = true;

            gammaMassVisual.StartAnimation(nameof(gammaMassVisual.Opacity), animationMassOpacity_MouseIn);
            rootVisual.StartAnimation("Scale.X", animationSize_MouseIn);
            rootVisual.StartAnimation("Scale.Y", animationSize_MouseIn);
            root_Background.Visibility = Visibility.Visible;
        }

        CompositionScopedBatch pointerExitBatch = null;
        private void UserControl_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (rootVisual is null) return;
            if (gammaMassVisual is null) return;
            if (PointInBehavior == PointInBehaviors.None) return;
            isPointEnter = false;

            gammaMassVisual.StartAnimation(nameof(gammaMassVisual.Opacity), animationMassOpacity_MouseExited);
            rootVisual.StartAnimation("Scale.X", animationSize_MouseExited);
            rootVisual.StartAnimation("Scale.Y", animationSize_MouseExited);
            pointerExitBatch = rootVisual.Compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
            pointerExitBatch.Completed += OnPointerExitCompleted;
            pointerExitBatch.End();
        }

        bool IsMouse4Click = false;
        private void UserControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsXButton1Pressed ||
                e.GetCurrentPoint(this).Properties.IsXButton2Pressed)
            {
                IsMouse4Click = true;
            }

            if (PointInBehavior == PointInBehaviors.None) return;
            if (rootVisual is null) return;
            rootVisual.StartAnimation("Scale.X", animationSize_MousePressed);
            rootVisual.StartAnimation("Scale.Y", animationSize_MousePressed);
        }

        private void UserControl_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (PointInBehavior == PointInBehaviors.None) return;
            if (rootVisual is null) return;
            rootVisual.StartAnimation("Scale.X", animationSize_MouseReleased);
            rootVisual.StartAnimation("Scale.Y", animationSize_MouseReleased);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (rootVisual is null) return;
            rootVisual.CenterPoint = new((float)ActualWidth / 2, (float)ActualHeight / 2, 1);
        }

        private void OnPointerExitCompleted(object sender, CompositionBatchCompletedEventArgs args)
        {
            if (rootVisual != null)
            {
                pointerExitBatch.Completed -= OnPointerExitCompleted;
                pointerExitBatch.Dispose();
            }
            if (isPointEnter) return;
            root_Background.Visibility = Visibility.Collapsed;
        }
    }
}
