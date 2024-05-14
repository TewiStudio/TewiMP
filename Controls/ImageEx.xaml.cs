using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Composition;
using TewiMP.Helpers;
using System.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TewiMP.Controls
{
    public partial class ImageEx : Grid
    {
        public static bool ImageDarkMass { get; set; } = false;

        public enum PointInBehaviors { Tapped, OnlyLightUp, None }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(
                "Source",
                typeof(ImageSource),
                typeof(ImageEx),
                new(null, OnImageSourceChanged)
                );
        public ImageSource Source
        {
            get { return GetValue(SourceProperty) as ImageSource; }
            set { SetValue(SourceProperty, value); }
        }
        private static void OnImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ImageEx ie = d as ImageEx;
            var value = e.NewValue as ImageSource;
            ie.SetImageSource(value);
        }
        
        public Stretch Stretch
        {
            get => Image_Control.Stretch;
            set => Image_Control.Stretch = value;
        }
        public PointInBehaviors PointInBehavior { get; set; } = PointInBehaviors.Tapped;
        public string SaveName { get; set; } = null;

        ArrayList array = null;
        public ImageEx()
        {
            InitializeComponent();
            //array = new ArrayList(1000000);
        }

        bool isInitedVisuals = false;
        Visual rootVisual;
        Visual imageVisual;
        Visual gammaMassVisual;
        ScalarKeyFrameAnimation animationOpacity_SourceChanged = null;
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

            rootVisual = ElementCompositionPreview.GetElementVisual(root);
            imageVisual = ElementCompositionPreview.GetElementVisual(Image_Control);
            gammaMassVisual = ElementCompositionPreview.GetElementVisual(Image_GammaMass);
            rootVisual.Opacity = 1;
            gammaMassVisual.Opacity = 0;

            // 图片源切换动画
            AnimateHelper.AnimateScalar(
                rootVisual, 1, 02, 
                0.2f, 1, 0.22f, 1f,
                out animationOpacity_SourceChanged);
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

        private void SetImageSource(ImageSource imageSource)
        {
            if (imageVisual == null) return;
            if (isInitedVisuals) imageVisual.Opacity = 0;
            imageVisual.StartAnimation("Opacity", animationOpacity_SourceChanged);
            Image_Control.Source = imageSource;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            InitVisual();
            SetImageSource(Source);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Image_Control.Source = null;
            animationOpacity_SourceChanged?.Dispose();
            animationMassOpacity_MouseIn?.Dispose();
            animationMassOpacity_MouseExited?.Dispose();
            animationSize_MouseIn?.Dispose();
            animationSize_MouseExited?.Dispose();
            animationSize_MousePressed?.Dispose();
            animationSize_MouseReleased?.Dispose();
            rootVisual = null;
            imageVisual = null;
            gammaMassVisual = null;
            animationOpacity_SourceChanged = null;
            animationMassOpacity_MouseIn = null;
            animationMassOpacity_MouseExited = null;
            animationSize_MouseIn = null;
            animationSize_MouseExited = null;
            animationSize_MousePressed = null;
            animationSize_MouseReleased = null;
        }

        private void UserControl_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (IsMouse4Click) { IsMouse4Click = false; return; }
            if (PointInBehavior == PointInBehaviors.None || PointInBehavior == PointInBehaviors.OnlyLightUp) return;
            //if (e.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
            Windowed.ImageViewerWindow.ShowWindow(Source, SaveName);
        }

        bool isPointEnter = false;
        private void UserControl_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (rootVisual == null) return;
            if (gammaMassVisual == null) return;
            if (PointInBehavior == PointInBehaviors.None) return;
            isPointEnter = true;

            gammaMassVisual.StartAnimation(nameof(gammaMassVisual.Opacity), animationMassOpacity_MouseIn);
            rootVisual.StartAnimation("Scale.X", animationSize_MouseIn);
            rootVisual.StartAnimation("Scale.Y", animationSize_MouseIn);
            root_Background.Visibility = Visibility.Visible;
        }

        private void UserControl_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (rootVisual == null) return;
            if (gammaMassVisual == null) return;
            if (PointInBehavior == PointInBehaviors.None) return;
            isPointEnter = false;

            gammaMassVisual.StartAnimation(nameof(gammaMassVisual.Opacity), animationMassOpacity_MouseExited);
            rootVisual.StartAnimation("Scale.X", animationSize_MouseExited);
            rootVisual.StartAnimation("Scale.Y", animationSize_MouseExited);
            rootVisual.Compositor.GetCommitBatch(CompositionBatchTypes.Animation).Completed -= ImageEx_Completed;
            rootVisual.Compositor.GetCommitBatch(CompositionBatchTypes.Animation).Completed += ImageEx_Completed;
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
            if (rootVisual == null) return;
            rootVisual.StartAnimation("Scale.X", animationSize_MousePressed);
            rootVisual.StartAnimation("Scale.Y", animationSize_MousePressed);
        }

        private void UserControl_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (PointInBehavior == PointInBehaviors.None) return;
            if (rootVisual == null) return;
            rootVisual.StartAnimation("Scale.X", animationSize_MouseReleased);
            rootVisual.StartAnimation("Scale.Y", animationSize_MouseReleased);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (rootVisual == null) return;
            rootVisual.CenterPoint = new((float)ActualWidth / 2, (float)ActualHeight / 2, 1);
        }

        private void ImageEx_Completed(object sender, CompositionBatchCompletedEventArgs args)
        {
            rootVisual.Compositor.GetCommitBatch(CompositionBatchTypes.Animation).Completed -= ImageEx_Completed;
            if (isPointEnter) return;
            root_Background.Visibility = Visibility.Collapsed;
        }
    }
}
