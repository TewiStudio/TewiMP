using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Composition;
using CommunityToolkit.WinUI.UI;
using TinyPinyin;
using TewiMP.Controls;
using TewiMP.DataEditor;
using Newtonsoft.Json.Linq;
using TewiMP.Helpers;
using System.IO;
using System.Threading.Tasks;

namespace TewiMP.Pages
{
    public partial class LocalAudioPage : Page
    {
        static bool isFirstLoadedPage = true;
        public ArrayList arrayList { get; set; }
        public LocalAudioPage()
        {
            InitializeComponent();
            //arrayList = new ArrayList(100000000);
        }

        async void UpdateCommandBarWidth()
        {
            ItemsList_Header_CommandBar.Width = 0;
            await Task.Delay(50);
            ItemsList_Header_CommandBar.Width = double.NaN;
        }
        void MultiSelectDo(bool isChecked)
        {
            foreach (FrameworkElement element in ItemsList_Header_CommandBar.PrimaryCommands)
            {
                if (element.Tag as string == "multi") continue;
                if ((element.Tag as string).Contains("multi"))
                    element.Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;
                else
                    element.Visibility = isChecked ? Visibility.Collapsed : Visibility.Visible;
                ItemsList.SelectionMode = isChecked ? ListViewSelectionMode.Multiple : ListViewSelectionMode.None;
                MusicDataItem.SetIsCloseMouseEvent(isChecked ? true : false);
            }
            UpdateCommandBarWidth();
        }
        void SelectedReverseDo()
        {
            foreach (SongItemBindBase item in ItemsList.Items.Cast<SongItemBindBase>())
            {
                if (ItemsList.SelectedItems.Contains(item))
                {
                    ItemsList.SelectedItems.Remove(item);
                }
                else
                {
                    ItemsList.SelectedItems.Add(item);
                }
            }
        }
        async void DeleteSelectedItemDo()
        {
            if (ItemsList.SelectedItems.Any())
            {
                var result = await MainWindow.ShowDialog("删除歌曲", $"真的要删除这 {ItemsList.SelectedItems.Count} 首歌曲吗？\n这将会把这些歌曲从存储空间中删除，且难以找回。", "取消", "确定", defaultButton: ContentDialogButton.Close);
                if (result == ContentDialogResult.Primary)
                {
                    ItemsList_Header_CommandBar.IsEnabled = false;
                    var item = MainWindow.AddNotify("删除歌曲", "正在准备删除歌曲...", NotifySeverity.Loading, TimeSpan.MaxValue);
                    int num = 0;
                    foreach (SongItemBindBase data in ItemsList.SelectedItems.Cast<SongItemBindBase>())
                    {
                        num++;
                        item.HorizontalAlignment = HorizontalAlignment.Stretch;
                        item.SetNotifyItemData("删除歌曲", $"进度：{Math.Round(((decimal)num / ItemsList.SelectedItems.Count) * 100, 1)}%\n正在删除：{data.MusicData.Title} - {data.MusicData.ButtonName}", NotifySeverity.Loading);
                        item.SetProcess(ItemsList.SelectedItems.Count, num);
                        await Task.Run(() =>
                        {
                            try
                            {
                                if (File.Exists(data.MusicData.InLocal))
                                    File.Delete(data.MusicData.InLocal);
                            }
                            catch { }
                        });
                    }

                    item.HorizontalAlignment = HorizontalAlignment.Center;
                    MainWindow.NotifyCountDown(item);
                    item.SetNotifyItemData("删除歌曲", "正在加载...", NotifySeverity.Loading);
                    item.SetProcess(0, 0);
                    await App.localMusicManager.ReAnalysisMusicDatas();
                    await App.localMusicManager.Refresh();
                    item.SetNotifyItemData("删除歌曲", "删除歌曲成功。", NotifySeverity.Complete);
                    ItemsList_Header_CommandBar.IsEnabled = true;
                }
            }
        }
        void AddSelectedItemToPlayingDo()
        {
            if (ItemsList.SelectedItems.Any())
            {
                foreach (SongItemBindBase item in ItemsList.SelectedItems.Cast<SongItemBindBase>())
                {
                    App.playingList.Add(item.MusicData);
                }
            }
        }

        bool inInit = false;
        async void Init()
        {
            inInit = true;
            InitVisual();
            InitShyHeader();
            InitEvents();
            CallEventsWhenDataLated();
            inInit = false;

            if (isFirstLoadedPage)
            {
                isFirstLoadedPage = false;
                await Task.Delay(3000);
                await App.localMusicManager.ReAnalysisMusicDatas();
                await App.localMusicManager.Refresh();
            }
        }

        ScrollViewer scrollViewer;
        Visual scrollViewerVisual;
        Visual headerVisual;
        Visual backgroundFillVisual;
        Visual labelVisual;
        void InitVisual()
        {
            if (!IsLoaded) return;
            MultiSelectDo(false);

            CommandBar_SortComboBox.ItemsSource = new string[] { "标题", "艺术家", "专辑", "发行日期", "文件创建日期" };
            CommandBar_SortComboBox.SelectedIndex = 0;

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
            if (!IsLoaded) return;

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
            App.localMusicManager.DataAnalyzing -= LocalMusicManager_DataAnalyzing;
            App.localMusicManager.DataAnalyzing += LocalMusicManager_DataAnalyzing;
            App.localMusicManager.DataAnalyzed -= LocalMusicManager_DataAnalyzed;
            App.localMusicManager.DataAnalyzed += LocalMusicManager_DataAnalyzed;
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
            App.localMusicManager.DataAnalyzing -= LocalMusicManager_DataAnalyzing;
            App.localMusicManager.DataAnalyzed -= LocalMusicManager_DataAnalyzed;
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

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            CommandBar_SortComboBox.ItemsSource = null;
            ItemsList_SongGroup.Source = null;
            RemoveEvents();
        }

        private void ScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            scrollViewerVisual.IsPixelSnappingEnabled = true;
        }

        private void LocalMusicManager_DataAnalyzing()
        {
            ItemsList_Analyzing_Root.Visibility = Visibility.Visible;
        }

        private void LocalMusicManager_DataAnalyzed()
        {
            ItemsList_Analyzing_Root.Visibility = Visibility.Collapsed;
        }

        double vOffset = 0;
        private void LocalMusicManager_DataChanging()
        {

        }

        private async void LocalMusicManager_DataChanged()
        {
            dynamic groupsResult = null;

            switch (CommandBar_SortComboBox.SelectedItem)
            {
                case "标题":
                    Resources["GroupItemWidth"] = 60;
                    groupsResult = await Task.Run(async () =>
                    {
                        using Kawazu.KawazuConverter converter = new();
                        Dictionary<MusicData, string> array = [];
                        foreach (var i in App.localMusicManager.LocalMusicItems)
                        {
                            if (array.ContainsKey(i.MusicData)) continue;
                            string a = i.MusicData.Title;
                            a = PinyinHelper.GetPinyin(a).ToUpper().First().ToString();
                            a = (await converter.Convert(a, Kawazu.To.Romaji, Kawazu.Mode.Spaced, Kawazu.RomajiSystem.Nippon)).ToUpper().First().ToString();
                            array.Add(i.MusicData, a);
                        }

                        return App.localMusicManager.LocalMusicItems.GroupBy(t => array[t.MusicData].ToUpper().First()).OrderBy(t => t.Key);
                    });
                    break;
                case "艺术家":
                    Resources["GroupItemWidth"] = 500;
                    groupsResult = await Task.Run(() =>
                    {
                        return App.localMusicManager.LocalMusicItems.GroupBy(t => t.MusicData.ArtistName).OrderBy(t => t.Key);
                    });
                    break;
                case "专辑":
                    Resources["GroupItemWidth"] = 500;
                    groupsResult = await Task.Run(() =>
                    {
                        return App.localMusicManager.LocalMusicItems.GroupBy(t => t.MusicData.Album.Title).OrderBy(t => t.Key);
                    });
                    break;
                case "发行日期":
                    Resources["GroupItemWidth"] = 500;
                    groupsResult = await Task.Run(() =>
                    {
                        return App.localMusicManager.LocalMusicItems.GroupBy(t => t.MusicData.ReleaseTime ?? DateTime.MinValue).OrderBy(t => t.Key);
                    });
                    break;
                case "文件创建日期":
                    Resources["GroupItemWidth"] = 500;
                    groupsResult = await Task.Run(() =>
                    {
                        return App.localMusicManager.LocalMusicItems.GroupBy(t => t.MusicData.FileCreateTime ?? DateTime.MinValue).OrderBy(t => t.Key);
                    });
                    break;
            }

            vOffset = scrollViewer.VerticalOffset;
            ItemsList_SongGroup.Source = groupsResult;
            ItemsList_HeaderGridView.ItemsSource = ItemsList_SongGroup.View.CollectionGroups;
            ItemsList_Header_Label_Count.Text = $"{App.localMusicManager.LocalMusicItems.Count} 首歌曲";
            scrollViewer.ChangeView(null, vOffset, null, true);
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
                case "play":
                    if (App.localMusicManager.LocalMusicItems.Count == 0) return;
                    if (App.playingList.PlayBehavior == TewiMP.Background.PlayBehavior.随机播放)
                    {
                        App.playingList.ClearAll();
                    }
                    foreach (var songItem in )
                    {
                        System.Diagnostics.Debug.WriteLine(songItem.Key);
                        //App.playingList.Add(, false);
                    }
                    await App.playingList.Play(App.localMusicManager.LocalMusicItems.First().MusicData, true);
                    App.playingList.SetRandomPlay(App.playingList.PlayBehavior);
                    break;
                case "refresh":
                    await App.localMusicManager.Refresh();
                    break;
                case "manageFolder":
                    await MainWindow.ShowDialog("管理本地音乐文件夹", new Controls.ManageLocalMusicFolderControl(), "完成");
                    break;
                case "reAnalysis":
                    button.IsEnabled = false;
                    await App.localMusicManager.ReAnalysisMusicDatas();
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

        private void AppBarToggleButton_Click(object sender, RoutedEventArgs e)
        {
            AppBarToggleButton button = sender as AppBarToggleButton;
            MultiSelectDo((bool)button.IsChecked);
        }

        private void multiButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as AppBarButton;
            switch (btn.Tag)
            {
                case "multi_selectAll":
                    ItemsList.SelectAll();
                    break;
                case "multi_selectReverse":
                    SelectedReverseDo();
                    break;
                case "multi_deleteSelect":
                    DeleteSelectedItemDo();
                    break;
                case "multi_addSelectToPlaying":
                    AddSelectedItemToPlayingDo();
                    break;
            }
        }

        private void multi_addSelectToPlayList_flyout_Opening(object sender, object e)
        {
            MenuFlyout flyout = sender as MenuFlyout;
            foreach (var list in App.playListReader.NowMusicListData)
            {
                MenuFlyoutItem item = new MenuFlyoutItem()
                {
                    Text = list.ListShowName,
                    Tag = list
                };
                item.Click += Item_Click;
                flyout.Items.Add(item);
            }
        }

        private async void Item_Click(object sender, RoutedEventArgs e)
        {
            var flyoutItem = sender as MenuFlyoutItem;
            flyoutItem.Click -= Item_Click;
            MainWindow.ShowLoadingDialog();
            var text = await PlayListHelper.ReadData();
            var list = flyoutItem.Tag as MusicListData;
            var listName = list.ListName;
            foreach (SongItemBindBase item in ItemsList.SelectedItems.Cast<SongItemBindBase>())
            {
                MainWindow.SetLoadingText($"正在添加：{item.MusicData.Title} - {item.MusicData.ButtonName}");
                MainWindow.SetLoadingProgressRingValue(ItemsList.SelectedItems.Count, ItemsList.SelectedItems.IndexOf(item));

                await Task.Run(() =>
                {
                    PlayListHelper.AddMusicDataToPlayList(item.MusicData, list);
                });
            }
            text[listName] = JObject.FromObject(list);
            await PlayListHelper.SaveData(text);
            await App.playListReader.Refresh();
            MainWindow.HideDialog();
        }

        private void multi_addSelectToPlayList_flyout_Closed(object sender, object e)
        {
            foreach (MenuFlyoutItem item in (sender as MenuFlyout).Items)
            {
                item.Click -= Item_Click;
            }
            (sender as MenuFlyout).Items.Clear();
        }

        private async void CommandBar_SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (inInit) return;
            await App.localMusicManager.Refresh();
        }
    }
}
