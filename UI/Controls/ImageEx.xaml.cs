using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Animations;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections;
using System.Numerics;
using System.Threading.Tasks;
using TewiMP.Helpers;
using TewiMP.Services;
using Windows.Storage;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TewiMP.UI.Controls
{
    public partial class ImageEx : Grid
    {
        public delegate void ImageLoadedDelegate(bool isLoaded);
        public event ImageLoadedDelegate ImageLoaded;
        public static bool ImageDarkMass { get; set; } = false;

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

        private static bool AnimationEnabled => App.Instance.UISettings.AnimationsEnabled;

        ArrayList array = null;
        public ImageEx()
        {
            InitializeComponent();
            //array = new ArrayList(1000000);
        }

        static AnimationBuilder sourceChangedAnimation = AnimationBuilder.Create().Opacity(1, 0, duration: TimeSpan.FromSeconds(2), easingType: EasingType.Quintic, easingMode: EasingMode.EaseOut);
        static AnimationBuilder mouseEnteredMassAnimation = AnimationBuilder.Create().Opacity(1, duration: TimeSpan.FromSeconds(.2), easingType: EasingType.Quintic, easingMode: EasingMode.EaseOut);
        static AnimationBuilder mouseEnteredSizeAnimation = AnimationBuilder.Create().Scale(1.07f, duration: TimeSpan.FromSeconds(.2), easingType: EasingType.Quintic, easingMode: EasingMode.EaseOut);
        static AnimationBuilder mouseExitedMassAnimation = AnimationBuilder.Create().Opacity(0, duration: TimeSpan.FromSeconds(1.3), easingType: EasingType.Linear, easingMode: EasingMode.EaseInOut);
        static AnimationBuilder mouseExitedSizeAnimation = AnimationBuilder.Create().Scale(Vector2.One, duration: TimeSpan.FromSeconds(1.5), easingType: EasingType.Quintic, easingMode: EasingMode.EaseOut);
        static AnimationBuilder mousePressedSizeAnimation = AnimationBuilder.Create().Scale(.93f, duration: TimeSpan.FromSeconds(.5), easingType: EasingType.Quintic, easingMode: EasingMode.EaseOut);
        static AnimationBuilder mouseReleasedSizeAnimation = AnimationBuilder.Create().Scale(1.07f, duration: TimeSpan.FromSeconds(1.5), easingType: EasingType.Quintic, easingMode: EasingMode.EaseOut);

        Visual rootVisual = null;
        Visual controlVisual = null;
        Visual image_GammaMassVisual = null;
        void InitVisual()
        {
            if (rootVisual is null)
                rootVisual = root.GetVisual();

            if (controlVisual is null)
                controlVisual = control.GetVisual();

            if (image_GammaMassVisual is null)
                image_GammaMassVisual = Image_GammaMass.GetVisual();

            controlVisual.Opacity = 0;
            image_GammaMassVisual.Opacity = 0;

            rootVisual.CenterPoint = new((float)ActualWidth / 2, (float)ActualHeight / 2, 1);
        }

        private void SetImageSource(Uri imageSource)
        {
            if (AnimationEnabled)
            {
                if (controlVisual is not null)
                    controlVisual.Opacity = 0;
            }
            Image_ControlSources.UriSource = null;
            Image_ControlSources.UriSource = imageSource;
        }

        private void Image_Control_ImageOpened(object sender, RoutedEventArgs e)
        {
            if (AnimationEnabled)
                sourceChangedAnimation.Start(control);
            else
                controlVisual.Opacity = 1;

            ImageLoaded?.Invoke(true);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            InitVisual();
            SetImageSource(Source);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Image_Control.Source = null;
            controlVisual = null;
            image_GammaMassVisual = null;
        }

        private async void UserControl_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (IsMouse4Click) { IsMouse4Click = false; return; }
            if (PointInBehavior == PointInBehaviors.None || PointInBehavior == PointInBehaviors.OnlyLightUp) return;
            //if (e.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
            Windows.ImageViewerWindow.ShowWindow(Image_ControlSources.UriSource, SaveName);
            /*
                        var file = await StorageFile.GetFileFromPathAsync(Image_ControlSources.UriSource.LocalPath);
                        await Launcher.LaunchFileAsync(file);*/
        }

        bool isPointEnter = false;
        private void UserControl_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (PointInBehavior == PointInBehaviors.None) return;
            isPointEnter = true;

            if (AnimationEnabled)
            {
                mouseEnteredMassAnimation.Start(Image_GammaMass);
                mouseEnteredSizeAnimation.Start(root);
            }
            else
            {
                image_GammaMassVisual.Opacity = 1;
                rootVisual.Scale = new(1.07f);
                //mouseEnteredSizeNoDurationAnimation.Start(root);
            }

            root_Background.Visibility = Visibility.Visible;
        }

        private async void UserControl_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (PointInBehavior == PointInBehaviors.None) return;
            isPointEnter = false;

            if (AnimationEnabled)
            {
                mouseExitedMassAnimation.Start(Image_GammaMass);
                await mouseExitedSizeAnimation.StartAsync(root);
            }
            else
            {
                image_GammaMassVisual.Opacity = 0;
                rootVisual.Scale = Vector3.One;
            }

            root_Background.Visibility = Visibility.Collapsed;
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

            if (AnimationEnabled) mousePressedSizeAnimation.Start(root);
            else rootVisual.Scale = new(0.93f);
        }

        private void UserControl_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (PointInBehavior == PointInBehaviors.None) return;
            if (AnimationEnabled) mouseReleasedSizeAnimation.Start(root);
            else rootVisual.Scale = new(1.07f);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (rootVisual is not null)
                rootVisual.CenterPoint = new((float)ActualWidth / 2, (float)ActualHeight / 2, 1);
        }
    }
}