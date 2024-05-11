using System;
using Microsoft.UI.Xaml.Media.Animation;
using TewiMP.DataEditor;

namespace TewiMP.Pages.ListViewPages
{
    public enum PageType { PlayList, Album, Artist, Search }
    public class PageData
    {
        public PageType PageType { get; set; }
        public object Param { get; set; }
        public double VerticalOffset { get; set; } = 0;
    }

    public static class ListViewPage
    {
        public static void SetPageToListViewPage(PageData pageData)
        {
            Type pageType = pageData.PageType switch
            {
                PageType.PlayList => typeof(PlayListPage),
                PageType.Album => typeof(ItemListViewAlbum),
                PageType.Artist => typeof(ItemListViewArtist),
                PageType.Search => typeof(ItemListViewSearch),
                _ => null
            };
            MainWindow.SetNavViewContent(
                pageType,
                pageData,
                new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
        }
    }
}
