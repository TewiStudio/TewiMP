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
    public partial class SearchCard : Grid
    {
        public double ImageScaleDPI { get; set; } = 1.0;

        public new SearchItemBindBase DataContext => (SearchItemBindBase)base.DataContext;

        public SearchCard()
        {
            InitializeComponent();
        }

        private void Grid_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is null) return;

            InfoText.Text = $"{DataContext.Count}. ";
            switch (DataContext.DataType)
            {
                case SearchBindDataType.Artist:
                    if (DataContext.Artist != null)
                    {
                        Img.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(DataContext.Artist.PicturePath));
                        Title.Text = DataContext.Artist.Name;
                        SubTitle.Text = DataContext.Artist.Name2;
                    }
                    break;
                case SearchBindDataType.Album:
                    if (DataContext.Album != null)
                    {
                        Img.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(DataContext.Album.PicturePath));
                        Title.Text = $"{DataContext.Album.Title}";
                        Title2.Text = $" {DataContext.Album.Title2}";
                        SubTitle.Text = $"{DataContext.Album.ArtistName}";
                    }
                    break;
                case SearchBindDataType.PlayList:
                    if (DataContext.PlayList != null)
                    {
                        Img.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(DataContext.PlayList.PicturePath));
                        Title.Text = DataContext.PlayList.ListShowName;
                        SubTitle.Text = $"{DataContext.PlayList_Count} 首歌曲";
                    }
                    break;
            }
        }

        private async void UILoaded(object sender, RoutedEventArgs e)
        {

        }

        private void UIUnloaded(object sender, RoutedEventArgs e)
        {
            Img.Source = null;
            base.DataContext = null;
        }

        private void Grid_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(sender as UIElement).PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
            {
                BackColorBaseRectAngle.Opacity = 1;
            }
        }

        private void Grid_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(sender as UIElement).PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
            {
                BackColorBaseRectAngle.Opacity = 0;
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
            if (isPressed && DataContext != null)
            {
                if (isRightPressed)
                {
                    //FlyoutMenu.ShowAt(sender as FrameworkElement);
                    isRightPressed = false;
                }
                else
                {
                    ListViewPage.SetPageToListViewPage(new()
                    {
                        PageType = DataContext.DataType switch
                        {
                            SearchBindDataType.Artist => PageType.Artist,
                            SearchBindDataType.Album => PageType.Album,
                            SearchBindDataType.PlayList => PageType.PlayList,
                            _ => PageType.PlayList
                        },
                        Param = DataContext.DataType switch
                        {
                            SearchBindDataType.Artist => DataContext.Artist,
                            SearchBindDataType.Album => DataContext.Album,
                            SearchBindDataType.PlayList => DataContext.PlayList,
                            _ => null
                        }
                    });
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
