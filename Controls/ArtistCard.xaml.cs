using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Composition;
using TewiMP.Helpers;
using TewiMP.DataEditor;
using TewiMP.Pages;
using TewiMP.Pages.ListViewPages;

namespace TewiMP.Controls
{
    public partial class ArtistCard : Grid, IDisposable
    {
        public double ImageScaleDPI { get; set; } = 1.0;
        Artist _artist;
        public Artist Artist
        {
            get => _artist;
            set
            {
                _artist = value;
                DataContext = Artist;
            }
        }

        public ArtistCard()
        {
            InitializeComponent();
        }

        Compositor compositor;
        DropShadow dropShadow;
        private void CrateShadow()
        {
            var visual = ElementCompositionPreview.GetElementVisual(ShadowBaseRectangle);
            compositor = visual.Compositor;

            var basicRectVisual = compositor.CreateSpriteVisual();
            basicRectVisual.Size = new Vector2((float)(ActualWidth - 8), (float)ActualHeight);

            dropShadow = compositor.CreateDropShadow();
            dropShadow.BlurRadius = 40f;
            dropShadow.Opacity = 0f;
            dropShadow.Offset = new Vector3(0, 2, 0);

            basicRectVisual.Shadow = dropShadow;
            ElementCompositionPreview.SetElementChildVisual(ShadowBaseRectangle, basicRectVisual);
        }

        private async void UILoaded(object sender, RoutedEventArgs e)
        {
            CrateShadow();
            if (Artist != null)
                PlayListImage.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(Artist.PicturePath));
        }

        private void UIUnloaded(object sender, RoutedEventArgs e)
        {
            PlayListImage.Source = null;
        }

        public void Dispose()
        {
            PlayListImage.Source = null;
            Artist = null;
            DataContext = null;
        }

        private void Grid_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(sender as UIElement).PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
            {
                AnimateHelper.AnimateOffset(
                    Children[1] as Border,
                    0, -2, 0, 0.5,
                    0.2f, 1f, 0.22f, 1f,
                    out Visual visual, out Compositor compositor, out Vector3KeyFrameAnimation animation);
                visual.StartAnimation(nameof(visual.Offset), animation);

                if (dropShadow != null)
                {
                    ScalarKeyFrameAnimation blurAnimation = compositor.CreateScalarKeyFrameAnimation();
                    blurAnimation.InsertKeyFrame(0.5f, 0.15f);
                    blurAnimation.Duration = TimeSpan.FromSeconds(0.5);
                    dropShadow.StartAnimation("Opacity", blurAnimation);
                }
            }
        }

        private void Grid_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(sender as UIElement).PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
            {
                AnimateHelper.AnimateOffset(
                    Children[1] as Border,
                    0, 0, 0, 0.5,
                    0.2f, 1f, 0.22f, 1f,
                    out Visual visual, out Compositor compositor, out Vector3KeyFrameAnimation animation);

                visual.StartAnimation(nameof(visual.Offset), animation);

                if (dropShadow != null)
                {
                    ScalarKeyFrameAnimation blurAnimation = compositor.CreateScalarKeyFrameAnimation();
                    blurAnimation.InsertKeyFrame(0.5f, 0f);
                    blurAnimation.Duration = TimeSpan.FromSeconds(0.5);
                    dropShadow.StartAnimation("Opacity", blurAnimation);
                }
            }
        }

        bool isPressed = false;
        bool isRightPressed = false;
        private void Grid_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            isPressed = true;
            isRightPressed = e.GetCurrentPoint(sender as UIElement).Properties.IsRightButtonPressed;
        }

        private void Grid_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (isPressed && Artist != null)
            {
                if (isRightPressed)
                {
                    //FlyoutMenu.ShowAt(sender as FrameworkElement);
                    isRightPressed = false;
                }
                else
                {
                    ListViewPage.SetPageToListViewPage(new() { PageType = PageType.Artist, Param = Artist });
                }
            }
            isPressed = false;
        }

        private void Grid_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {

        }

        private void Grid_Holding(object sender, Microsoft.UI.Xaml.Input.HoldingRoutedEventArgs e)
        {

        }
    }
}
