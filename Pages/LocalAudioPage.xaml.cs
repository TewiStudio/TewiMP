using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Composition;
using System.Collections;
using System.Linq;
using System.Collections.ObjectModel;
using TewiMP.Helpers;
using Microsoft.VisualBasic;
using TinyPinyin;
using System.Threading.Tasks;
using CommunityToolkit.Common;
using System.Collections.Generic;
using TewiMP.DataEditor;
using static System.Net.Mime.MediaTypeNames;
using CommunityToolkit.WinUI.UI;
using TewiMP.Controls;

namespace TewiMP.Pages
{
    public partial class LocalAudioPage : Page
    {
        public ArrayList arrayList { get; set; }
        public LocalAudioPage()
        {
            InitializeComponent();
            //arrayList = new ArrayList(100000000);
        }

        void Init()
        {
            InitVisual();
            InitShyHeader();
            InitEvents();
            CallEventsWhenDataLated();
        }

        ScrollViewer scrollViewer;
        Visual scrollViewerVisual;
        Visual headerVisual;
        Visual backgroundFillVisual;
        Visual labelVisual;
        void InitVisual()
        {
            if (isUnloaded) return;

            scrollViewer = (VisualTreeHelper.GetChild(ItemsList, 0) as Border).Child as ScrollViewer;
            scrollViewer.CanContentRenderOutsideBounds = true;
            scrollViewer.ViewChanging -= ScrollViewer_ViewChanging;
            scrollViewer.ViewChanging += ScrollViewer_ViewChanging;

            // 设置 header 为顶层
            /*var headerPresenter = (UIElement)VisualTreeHelper.GetParent((UIElement)ItemsList.Header);
            var headerContainer = (UIElement)VisualTreeHelper.GetParent(headerPresenter);
            Canvas.SetZIndex(headerContainer, 10);*/

            scrollViewerVisual = ElementCompositionPreview.GetElementVisual(scrollViewer);
            headerVisual = ElementCompositionPreview.GetElementVisual(ItemsList_Header_Root);
            backgroundFillVisual = ElementCompositionPreview.GetElementVisual(ItemsList_Header_BackgroundFill);
            labelVisual = ElementCompositionPreview.GetElementVisual(ItemsList_Header_Label);
        }

        CompositionPropertySet scrollerPropertySet;
        ExpressionAnimation offsetExpression;
        ExpressionAnimation labelVisualScaleAnimation;
        ExpressionAnimation labelVisualOffsetYAnimation;
        ExpressionAnimation labelVisualOffsetXAnimation;
        ExpressionAnimation backgroundFillVisualOpacityAnimation;
        void InitShyHeader()
        {
            return;
            if (headerVisual == null) return;
            if (isUnloaded) return;

            scrollerPropertySet?.Dispose();
            offsetExpression?.Dispose();
            labelVisualScaleAnimation?.Dispose();
            labelVisualOffsetYAnimation?.Dispose();
            labelVisualOffsetXAnimation?.Dispose();
            backgroundFillVisualOpacityAnimation?.Dispose();

            scrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
            Compositor compositor = scrollerPropertySet.Compositor;

            var padingSize = ItemsList_Header_Root.ActualHeight - ItemsList_Header_Segmented.ActualHeight - 4;//40;
            String progress = $"Clamp(-scroller.Translation.Y / {padingSize}, 0, 1.0)";

            offsetExpression = compositor.CreateExpressionAnimation($"-scroller.Translation.Y - {progress} * {padingSize}");
            offsetExpression.SetReferenceParameter("scroller", scrollerPropertySet);
            headerVisual.StartAnimation("Offset.Y", offsetExpression);

            labelVisualScaleAnimation = compositor.CreateExpressionAnimation("Lerp(Vector2(1,1), Vector2(0.7, 0.7), " + progress + ")");
            labelVisualScaleAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            labelVisual.StartAnimation("Scale.xy", labelVisualScaleAnimation);

            labelVisualOffsetYAnimation = compositor.CreateExpressionAnimation($"Lerp(0, 24, {progress})");
            labelVisualOffsetYAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            labelVisual.StartAnimation("Offset.Y", labelVisualOffsetYAnimation);

            labelVisualOffsetXAnimation = compositor.CreateExpressionAnimation($"Lerp(0, -12, {progress})");
            labelVisualOffsetXAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            labelVisual.StartAnimation("Offset.X", labelVisualOffsetXAnimation);

            backgroundFillVisualOpacityAnimation = compositor.CreateExpressionAnimation($"Lerp(0, 1, {progress})");
            backgroundFillVisualOpacityAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            backgroundFillVisual.StartAnimation("Opacity", backgroundFillVisualOpacityAnimation);
        }

        void InitEvents()
        {
            scrollViewer.ViewChanging -= ScrollViewer_ViewChanging;
            scrollViewer.ViewChanging += ScrollViewer_ViewChanging;
            App.localMusicManager.DataChanging -= LocalMusicManager_DataChanging;
            App.localMusicManager.DataChanging += LocalMusicManager_DataChanging;
            App.localMusicManager.DataChanged -= LocalMusicManager_DataChanged;
            App.localMusicManager.DataChanged += LocalMusicManager_DataChanged;
            ItemsView_BottomButtons.PositionToNowPlaying_Button.Click -= Position_Button_Click;
            ItemsView_BottomButtons.PositionToNowPlaying_Button.Click += Position_Button_Click;
            ItemsView_BottomButtons.PositionToTop_Button.Click -= Position_Button_Click;
            ItemsView_BottomButtons.PositionToTop_Button.Click += Position_Button_Click;
            ItemsView_BottomButtons.PositionToBottom_Button.Click -= Position_Button_Click;
            ItemsView_BottomButtons.PositionToBottom_Button.Click += Position_Button_Click;
        }

        void RemoveEvents()
        {
            scrollViewer.ViewChanging -= ScrollViewer_ViewChanging;
            App.localMusicManager.DataChanging -= LocalMusicManager_DataChanging;
            App.localMusicManager.DataChanged -= LocalMusicManager_DataChanged;
            ItemsView_BottomButtons.PositionToNowPlaying_Button.Click -= Position_Button_Click;
            ItemsView_BottomButtons.PositionToTop_Button.Click -= Position_Button_Click;
            ItemsView_BottomButtons.PositionToBottom_Button.Click -= Position_Button_Click;
        }
        void CallEventsWhenDataLated()
        {
            LocalMusicManager_DataChanged();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Init();
        }

        bool isUnloaded = false;
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            isUnloaded = true;
            ItemsList_SongGroup.Source = null;
            RemoveEvents();
        }

        private void ScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            scrollViewerVisual.IsPixelSnappingEnabled = true;
        }


        private async void LocalMusicManager_DataChanged()
        {
            using Kawazu.KawazuConverter converter = new();
            Dictionary<MusicData, string> array = [];
            foreach (var i in App.localMusicManager.LocalMusicItems)
            {
                string a = i.MusicData.Title;
                a = PinyinHelper.GetPinyin(a).ToUpper().First().ToString();
                a = (await converter.Convert(a, Kawazu.To.Romaji, Kawazu.Mode.Spaced, Kawazu.RomajiSystem.Nippon)).ToUpper().First().ToString();
                array.Add(i.MusicData, a);
            }

            var groupsResult = App.localMusicManager.LocalMusicItems.GroupBy(t => array[t.MusicData].ToUpper().First()).OrderBy(t => t.Key);

            ItemsList_SongGroup.Source = groupsResult;
            ItemsList_HeaderGridView.ItemsSource = ItemsList_SongGroup.View.CollectionGroups;
        }

        private void LocalMusicManager_DataChanging()
        {

        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //UpdateShyHeader();
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            AppBarButton button = (AppBarButton)sender;
            switch (button.Tag as string)
            {
                case "manageFolder":
                    await MainWindow.ShowDialog("管理本地音乐文件夹", new Controls.ManageLocalMusicFolderControl(), "完成");
                    break;
                case "refresh":
                    await App.localMusicManager.Refresh();
                    break;
                case "reAnalysis":
                    button.IsEnabled = false;
                    await LocalMusicHelper.ReAnalysisMusicDatas();
                    await App.localMusicManager.Refresh();
                    button.IsEnabled = true;
                    break;
            }
        }

        private async void Position_Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            switch ((ScrollFootButton.ButtonType)btn.Tag)
            {
                case ScrollFootButton.ButtonType.NowPlaying:
                    foreach (var i in App.localMusicManager.LocalMusicItems)
                    {
                        if (i.MusicData != App.audioPlayer.MusicData) continue;
                        await ItemsList.SmoothScrollIntoViewWithItemAsync(i, ScrollItemPlacement.Center);
                        await ItemsList.SmoothScrollIntoViewWithItemAsync(i, ScrollItemPlacement.Center, disableAnimation: true);
                        MusicDataItem.TryHighlightPlayingItem();
                        break;
                    }
                    break;
                case ScrollFootButton.ButtonType.Top:
                    scrollViewer.ChangeView(null, 0, null);
                    break;
                case ScrollFootButton.ButtonType.Bottom:
                    scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null);
                    break;
            }
        }


        private void ItemsList_Header_Segmented_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
