using System;
using System.IO;
using System.Linq;
using System.Data;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Composition;
using TewiMP.Media;
using TewiMP.Helpers;
using TewiMP.Controls;
using TewiMP.DataEditor;
using Windows.Storage.Pickers;
using Newtonsoft.Json.Linq;
using CommunityToolkit.WinUI;
using Vanara.Extensions;

namespace TewiMP.Pages
{
    public partial class ItemListViewPlayList : Page, IPage
    {
        public bool IsNavigatedOutFromPage { get; set; } = false;
        private ScrollViewer scrollViewer { get; set; }
        public MusicListData NavToObj { get; set; }

        ObservableCollection<SongItemBindBase> searchMusicDatas = [];
        public ItemListViewPlayList()
        {
            InitializeComponent();
            //arrayList = new ArrayList(100000000);
            DataContext = this;
            var _enumval = Enum.GetValues(typeof(PlaySort)).Cast<PlaySort>();
            SortComboBox.ItemsSource = _enumval.ToList();
            SearchBox.ItemsSource = searchMusicDatas;
        }

        //public ArrayList arrayList { get; set; }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            IsNavigatedOutFromPage = false;
            MainWindow.InKeyDownEvent += MainWindow_InKeyDownEvent;
            MainWindow.MainViewStateChanged += MainWindow_MainViewStateChanged;
            App.playListReader.Updated += PlayListReader_Updated;
            App.audioPlayer.SourceChanged += AudioPlayer_SourceChanged;
            ImageManage.localImageCache.Clear();
            /*
            ConnectedAnimation animation =
                ConnectedAnimationService.GetForCurrentView().GetAnimation("forwardAnimation");
            if (animation != null)
            {
                animation.TryStart(PlayList_ImageBaseBorder);
            }
*/
            //PlayAllButton.Foreground = new SolidColorBrush(CodeHelper.IsAccentColorDark() ? Colors.White : Colors.Black);

            foreach (var mld in App.playListReader.NowMusicListData)
            {
                if (mld == (MusicListData)e.Parameter)
                {
                    NavToObj = mld;
                    break;
                }
            }
            InitData();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            DataContext = null;
            LeavingPageDo();
            //GC.SuppressFinalize(this);
            //System.Diagnostics.App.logManager.Log("Clear");
        }

        private void LeavingPageDo()
        {
            IsNavigatedOutFromPage = true;
            MainWindow.InKeyDownEvent -= MainWindow_InKeyDownEvent;
            MainWindow.MainViewStateChanged -= MainWindow_MainViewStateChanged;
            App.playListReader.Updated -= PlayListReader_Updated;
            App.audioPlayer.SourceChanged -= AudioPlayer_SourceChanged;

            DisposeAllVisual();

            MusicDataList?.Clear();
            searchMusicDatas?.Clear();
            Children.ItemsSource = null;
            SearchBox.ItemsSource = null;
            SortComboBox.ItemsSource = null;

            ImageManage.localImageCache.Clear();

            MusicDataList = null;
            searchMusicDatas = null;

            dropShadow?.Dispose();
            dropShadow = null;
            //PlayList_Image?.Dispose();
            //PlayList_Image?.DisposeVisualsAnimation();

            if (scrollViewer != null) scrollViewer.ViewChanging -= ScrollViewer_ViewChanging;
            scrollViewer = null;

            NavToObj = null;
            UnloadObject(this);
        }

        private void DisposeAllVisual()
        {/* //will crash program
            scrollerPropertySet?.Dispose();
            headerVisual?.Dispose();
            backgroundVisual?.Dispose();
            logoVisual?.Dispose();
            stackVisual?.Dispose();
            commandBarVisual?.Dispose();
            commandFootVisual?.Dispose();
            searchBaseVisual?.Dispose();*/
            offsetExpression?.Dispose();
            backgroundVisualOpacityAnimation?.Dispose();
            logoHeaderScaleAnimation?.Dispose();
            logoVisualOffsetXAnimation?.Dispose();
            logoVisualOffsetYAnimation?.Dispose();
            stackVisualOffsetAnimation?.Dispose();
            commandBarVisualOffsetAnimation?.Dispose();
            commandFootVisualOffsetAnimation?.Dispose();
            searchBaseVisualOffsetAnimation?.Dispose();

            scrollerPropertySet = null;
            compositor = null;
            headerVisual = null;
            backgroundVisual = null;
            logoVisual = null;
            stackVisual = null;
            commandBarVisual = null;
            commandFootVisual = null;
            searchBaseVisual = null;
            offsetExpression = null;
            backgroundVisualOpacityAnimation = null;
            logoHeaderScaleAnimation = null;
            logoVisualOffsetXAnimation = null;
            logoVisualOffsetYAnimation = null;
            stackVisualOffsetAnimation = null;
            commandBarVisualOffsetAnimation = null;
            commandFootVisualOffsetAnimation = null;
            searchBaseVisualOffsetAnimation = null;
        }

        private void MainWindow_MainViewStateChanged(bool isView)
        {
            if (IsNavigatedOutFromPage) return;
            if (NavToObj is null) return;
            AutoScrollViewerControl.Pause = !isView;
        }

        private void CrateShadow()
        {
            if (IsNavigatedOutFromPage) return;
            if (NavToObj is null) return;
            if (logoVisual is null) return;
            compositor = logoVisual.Compositor;

            var basicRectVisual = compositor.CreateSpriteVisual();
            basicRectVisual.Size = PlayList_Image.RenderSize.ToVector2();

            dropShadow = compositor.CreateDropShadow();
            dropShadow.BlurRadius = 30f;
            dropShadow.Opacity = 0.3f;
            dropShadow.Offset = new Vector3(0, 0, 0);

            basicRectVisual.Shadow = dropShadow;
            ElementCompositionPreview.SetElementChildVisual(PlayList_Image_DropShadowBase, basicRectVisual);
        }

        bool isLoading = false;
        public ObservableCollection<SongItemBindBase> MusicDataList = new();
        public async void InitData()
        {
            if (NavToObj is null)
            {
                LoadingTipControl.UnShowLoading();
                return;
            }
            if (isLoading) return;
            isLoading = true;

            if (IsNavigatedOutFromPage) return;
            App.logManager.Log("ItemListViewPlayList", $"开始初始化。");

            #region Collecter
            SelectorSeparator.Visibility = Visibility.Collapsed;
            AddSelectedToPlayingListButton.Visibility = Visibility.Collapsed;
            AddSelectedToPlayListButton.Visibility = Visibility.Collapsed;
            DeleteSelectedButton.Visibility = Visibility.Collapsed;
            DownloadSelectedButton.Visibility = Visibility.Collapsed;
            SelectReverseButton.Visibility = Visibility.Collapsed;
            SelectAllButton.Visibility = Visibility.Collapsed;
            AddLocalFilesButton.Visibility = Visibility.Collapsed;

            PlayList_BaseGrid.Visibility = Visibility.Visible;
            AddLocalFilesButton.Visibility = Visibility.Visible;
            if (Children.SelectionMode != ListViewSelectionMode.None)
                Button_Click_2(null, null);
            #endregion

            PlayList_TitleTextBlock.Text = NavToObj.ListShowName;
            PlayList_OtherTextBlock.Text = $"共{NavToObj.Songs.Count}首歌曲";
            if (NavToObj.Songs.Count == 0)
            {
                AtListBottomTb.Visibility = Visibility.Collapsed;
                ListEmptyPopup.Visibility = Visibility.Visible;
            }
            else
            {
                AtListBottomTb.Visibility = Visibility.Visible;
                ListEmptyPopup.Visibility = Visibility.Collapsed;
            }

            LoadingTipControl.ShowLoading();

            if (NavToObj != null)
            {
                LoadImage();

                MusicDataList.Clear();
                var dpi = CodeHelper.GetScaleAdjustment(App.WindowLocal);
                MusicData[] array = null;

                SortComboBox.SelectedItem = NavToObj.PlaySort;
                var scs = (PlaySort)SortComboBox.SelectedItem;
                await Task.Run(() =>
                {
                    switch (scs)
                    {
                        case PlaySort.默认升序:
                            array = NavToObj.Songs.ToArray();
                            break;
                        case PlaySort.默认降序:
                            List<MusicData> list = NavToObj.Songs;
                            list.Reverse();
                            array = [.. list];
                            break;
                        case PlaySort.名称升序:
                            array = NavToObj.Songs.OrderBy(m => m.Title).ToArray();
                            break;
                        case PlaySort.名称降序:
                            array = NavToObj.Songs.OrderByDescending(m => m.Title).ToArray();
                            break;
                        case PlaySort.艺术家升序:
                            array = NavToObj.Songs.OrderBy(m => m.Artists.Count != 0 ? m.Artists[0].Name : "未知").ToArray();
                            break;
                        case PlaySort.艺术家降序:
                            array = NavToObj.Songs.OrderByDescending(m => m.Artists.Count != 0 ? m.Artists[0].Name : "未知").ToArray();
                            break;
                        case PlaySort.专辑升序:
                            array = NavToObj.Songs.OrderBy(m => m.Album.Title).ToArray();
                            break;
                        case PlaySort.专辑降序:
                            array = NavToObj.Songs.OrderByDescending(m => m.Album.Title).ToArray();
                            break;
                        case PlaySort.时间升序:
                            array = NavToObj.Songs.OrderBy(m => m.ReleaseTime).ToArray();
                            break;
                        case PlaySort.时间降序:
                            array = NavToObj.Songs.OrderByDescending(m => m.ReleaseTime).ToArray();
                            break;
                        case PlaySort.索引升序:
                            array = NavToObj.Songs.OrderBy(m => m.Index).ToArray();
                            break;
                        case PlaySort.索引降序:
                            array = NavToObj.Songs.OrderByDescending(m => m.Index).ToArray();
                            break;
                    }
                });
                int count = 0;
                foreach (var i in array)
                {
                    if (i is null) continue;
                    count++;
                    i.Count = count;
                    MusicDataList.Add(new() { MusicData = i, MusicListData = NavToObj, ImageScaleDPI = dpi });
                }
                array = null;
                App.logManager.Log("ItemListViewPlayList", $"列表加载完成。");
            }
            isLoading = false;
            LoadingTipControl.UnShowLoading();
            UpdateShyHeader();
            App.logManager.Log("ItemListViewPlayList", $"加载完成。");
        }

        private void PlayListReader_Updated()
        {
            if (IsNavigatedOutFromPage || NavToObj is null)
            {
                App.playListReader.Updated -= PlayListReader_Updated;
                return;
            }
            foreach (var data in App.playListReader.NowMusicListData)
            {
                if (data == NavToObj)
                {
                    NavToObj.Songs = data.Songs;
                    InitData();
                    break;
                }
            }
        }

        private void AudioPlayer_SourceChanged(AudioPlayer audioPlayer)
        {

        }

        private async void LoadImage()
        {
            if (NavToObj is null || IsNavigatedOutFromPage) return;
            PlayList_Image.BorderThickness = new(0);
            if (NavToObj.ListDataType == DataType.本地歌单)
            {
                bool isExists = true;
                await Task.Run(() => { isExists = File.Exists(NavToObj.PicturePath); });
                if (isExists) PlayList_Image.Source = await FileHelper.GetImageSource(NavToObj.PicturePath);
            }
            else if (NavToObj.ListDataType == DataType.歌单)
            {
                PlayList_Image.Source = (await ImageManage.GetImageSource(NavToObj)).Item1;
            }
            if (NavToObj is null) return;
            if (PlayList_Image.Source is null)
            {
                PlayList_Image.Source = await FileHelper.GetImageSource("");
            }
            PlayList_Image.SaveName = NavToObj?.ListShowName;
            PlayList_Image.BorderThickness = new(1);
            App.logManager.Log("ItemListViewPlayList", $"图片加载完成。");
            await Task.Delay(100);
            UpdateShyHeader();
            UpdateInfoWidth();
            CrateShadow();
            await Task.Delay(1500);
            UpdateShyHeader();
            UpdateInfoWidth();
            CrateShadow();
        }

        CompositionPropertySet scrollerPropertySet;
        Compositor compositor;
        Visual headerVisual;
        Visual backgroundVisual;
        Visual logoVisual;
        Visual stackVisual;
        Visual commandBarVisual;
        Visual commandFootVisual;
        Visual searchBaseVisual;
        ExpressionAnimation offsetExpression;
        ExpressionAnimation backgroundVisualOpacityAnimation;
        ExpressionAnimation logoHeaderScaleAnimation;
        ExpressionAnimation logoVisualOffsetXAnimation;
        ExpressionAnimation logoVisualOffsetYAnimation;
        ExpressionAnimation stackVisualOffsetAnimation;
        ExpressionAnimation commandBarVisualOffsetAnimation;
        ExpressionAnimation commandFootVisualOffsetAnimation;
        ExpressionAnimation searchBaseVisualOffsetAnimation;
        int logoSizeCount = 0;
        public void UpdateShyHeader(bool xOnly = false)
        {
            if (NavToObj is null) return;
            if (IsNavigatedOutFromPage) return;
            if (scrollViewer is null) return;

            int logoHeight = 280;
            double anotherHeight = 154;
            int anotherXEnd = 150;
            double logoSizeEnd = 0.45;
            int commandYEnd = 84;
            int searchBaseYEnd = 158;

            if (logoSizeCount == 1)
            {
                logoHeight = 160;
                logoSizeEnd = 0.66;
                anotherHeight = 54;
                anotherXEnd = 131;
                commandYEnd = 64;
                searchBaseYEnd = 138;
            }
            int anotherX = 16 + logoHeight + 12;

            String progress = $"Clamp(-scroller.Translation.Y / {anotherHeight}, 0, 1.0)";

            if (scrollerPropertySet is null)
            {
                scrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
                compositor = scrollerPropertySet.Compositor;
                headerVisual = ElementCompositionPreview.GetElementVisual(menu_border);
                backgroundVisual = ElementCompositionPreview.GetElementVisual(BackColorBaseRectangle);
                logoVisual = ElementCompositionPreview.GetElementVisual(PlayList_Image_BaseGrid);
                stackVisual = ElementCompositionPreview.GetElementVisual(InfosBaseStackPanel);
                commandBarVisual = ElementCompositionPreview.GetElementVisual(CommandBarWidthChanger);
                commandFootVisual = ElementCompositionPreview.GetElementVisual(CommandFoot);
                searchBaseVisual = ElementCompositionPreview.GetElementVisual(SearchBase);
            }
            else
            {
                if (!xOnly)
                {
                    offsetExpression.Dispose();
                    backgroundVisualOpacityAnimation.Dispose();
                    logoVisualOffsetXAnimation.Dispose();
                    logoVisualOffsetYAnimation.Dispose();
                    stackVisualOffsetAnimation.Dispose();
                    commandBarVisualOffsetAnimation.Dispose();
                    offsetExpression = null;
                    backgroundVisualOpacityAnimation = null;
                    logoVisualOffsetXAnimation = null;
                    logoVisualOffsetYAnimation = null;
                    logoVisualOffsetYAnimation = null;
                    stackVisualOffsetAnimation = null;
                    commandBarVisualOffsetAnimation = null;
                }
                commandFootVisualOffsetAnimation?.Dispose();
                commandFootVisualOffsetAnimation = null;
                logoHeaderScaleAnimation.Dispose();
                logoHeaderScaleAnimation = null;
            }

            if (!xOnly)
            {
                offsetExpression = compositor.CreateExpressionAnimation($"-scroller.Translation.Y - {progress} * {anotherHeight}");
                offsetExpression.SetReferenceParameter("scroller", scrollerPropertySet);
                headerVisual.StartAnimation("Offset.Y", offsetExpression);

                backgroundVisualOpacityAnimation = compositor.CreateExpressionAnimation($"Lerp(0, 1, {progress})");
                backgroundVisualOpacityAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
                backgroundVisual.StartAnimation("Opacity", backgroundVisualOpacityAnimation);

                logoVisualOffsetYAnimation = compositor.CreateExpressionAnimation($"Lerp(16, 16, {progress})");
                logoVisualOffsetYAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
                logoVisual.StartAnimation("Offset.Y", logoVisualOffsetYAnimation);

                logoVisualOffsetXAnimation = compositor.CreateExpressionAnimation($"Lerp(16, 16, {progress})");
                logoVisualOffsetXAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
                logoVisual.StartAnimation("Offset.X", logoVisualOffsetXAnimation);

                stackVisualOffsetAnimation = compositor.CreateExpressionAnimation($"Lerp(Vector3({logoVisual.Size.X + 32},16,0), Vector3({(int)(logoVisual.Size.X * logoSizeEnd) + 32},{anotherHeight + 16},0), {progress})");
                stackVisualOffsetAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
                stackVisual.StartAnimation(nameof(stackVisual.Offset), stackVisualOffsetAnimation);
                
                searchBaseVisualOffsetAnimation = compositor.CreateExpressionAnimation($"Lerp(Vector3(16,{headerVisual.Size.Y + 4},0), Vector3(16,{anotherHeight + searchBaseYEnd + 4},0), {progress})");
                searchBaseVisualOffsetAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
                searchBaseVisual.StartAnimation(nameof(searchBaseVisual.Offset), searchBaseVisualOffsetAnimation);
            }

            string sizelogo = null;
            if (logoSizeCount == 0) sizelogo = "1,1";
            logoVisual.CenterPoint = new(0, logoVisual.Size.Y, 1);
            // Logo scale and transform                                          from               to
            logoHeaderScaleAnimation = compositor.CreateExpressionAnimation($"Lerp(Vector2(1,1), Vector2({logoSizeEnd}, {logoSizeEnd}), " + progress + ")");
            logoHeaderScaleAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            logoVisual.StartAnimation("Scale.xy", logoHeaderScaleAnimation);

            commandBarVisualOffsetAnimation = compositor.CreateExpressionAnimation($"Lerp(Vector3(-6,{logoHeight - commandBarVisual.Size.Y + 6},0), Vector3(-6,{commandYEnd},0), {progress})");
            commandBarVisualOffsetAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            commandBarVisual.StartAnimation(nameof(commandBarVisual.Offset), commandBarVisualOffsetAnimation);
            headerVisual.IsPixelSnappingEnabled = true;

            commandFootVisualOffsetAnimation = compositor.CreateExpressionAnimation(
                $"Lerp(" +
                    $"Vector3(" +
                        $"{ActualWidth} - {commandFootVisual.Size.X} - 16," +
                        $"{ActualHeight} - {commandFootVisual.Size.Y} - 8," +
                        $"0)," +
                    $"Vector3(" +
                        $"{ActualWidth} - {commandFootVisual.Size.X} - 16," +
                        $"{anotherHeight} + {ActualHeight} - {commandFootVisual.Size.Y} - 8," +
                        $"0)," +
                    $"{progress})");
            commandFootVisualOffsetAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            commandFootVisual.StartAnimation("Offset", commandFootVisualOffsetAnimation);
            /*
            Visual textVisual = ElementCompositionPreview.GetElementVisual(Result_Search_Header);
            Vector3 finalOffset = new Vector3(0, (float)Result_Search_Header.ActualHeight, 0);
            var headerOffsetAnimation = compositor.CreateExpressionAnimation($"Lerp(Vector3(0,0,0), finalOffset, {progress})");
            headerOffsetAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            headerOffsetAnimation.SetVector3Parameter("finalOffset", finalOffset);
            textVisual.StartAnimation(nameof(Visual.Offset), headerOffsetAnimation);*/
        }

        private async void UpdateCommandToolBarWidth()
        {
            ToolsCommandBar.Width = 0;
            await Task.Delay(10);
            ToolsCommandBar.Width = double.NaN;
        }

        private void UpdateInfoWidth()
        {
            if (NavToObj is null) return;
            if (IsNavigatedOutFromPage) return;
            if (logoVisual is null) return;
            var width = HeaderBaseGrid.ActualWidth - 50 - (PlayList_ImageBaseBorder.ActualWidth * logoVisual.Scale.X);
            if (width > 0)
            {
                WidthChanger.Width = width;
            }
        }

        private async void menu_border_Loaded(object sender, RoutedEventArgs e)
        {
            if (NavToObj is null) return;
            if (IsNavigatedOutFromPage) return;
            if (Children is null) return;
            if (scrollViewer is null)
            {
                scrollViewer = (VisualTreeHelper.GetChild(Children, 0) as Border).Child as ScrollViewer;
                scrollViewer.CanContentRenderOutsideBounds = true;
                scrollViewer.ViewChanging += ScrollViewer_ViewChanging;

                // 设置header为顶层
                var headerPresenter = (UIElement)VisualTreeHelper.GetParent((UIElement)Children.Header);
                var headerContainer = (UIElement)VisualTreeHelper.GetParent(headerPresenter);
                Canvas.SetZIndex(headerContainer, 1);
            }

            UpdateShyHeader();
            UpdateInfoWidth();
            await Task.Delay(100);
            UpdateShyHeader();
            UpdateInfoWidth();
            CrateShadow();

            await Task.Delay(1000);
            UpdateShyHeader();
            UpdateInfoWidth();
            CrateShadow();
        }

        bool isFirstScroll = true;
        private void ScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            if (!isFirstScroll) { isFirstScroll = false; return; }
            if (scrollViewer is null) return;
            UpdateShyHeader(true);
            UpdateInfoWidth();
            if (logoSizeCount == 1)
            {
                PlayList_Image.CornerRadius = new(Math.Min(Math.Max(scrollViewer.VerticalOffset / 4, 8), 13));
            }
            else
            {
                PlayList_Image.CornerRadius = new(Math.Min(Math.Max(scrollViewer.VerticalOffset / 8, 8), 15));
            }

        }

        private async void Result_BaseGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ActualWidth <= 660 || ActualHeight <= 348)
            {
                logoSizeCount = 1;
                //PlayList_ImageBaseBorder.Width = 160;
                PlayList_ImageBaseBorder.Height = 160;
            }
            else
            {
                logoSizeCount = 0;
                //PlayList_ImageBaseBorder.Width = 280;
                PlayList_ImageBaseBorder.Height = 280;
            }/*
            System.Diagnostics.App.logManager.Log(ActualWidth);
            System.Diagnostics.App.logManager.Log(ActualHeight);*/
            await Task.Delay(1);
            UpdateShyHeader();
            UpdateInfoWidth();
            CrateShadow();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Children.Items.Count == 0) return;
            if (App.playingList.PlayBehavior == TewiMP.Background.PlayBehavior.随机播放)
            {
                App.playingList.ClearAll();
            }
            foreach (var songItem in MusicDataList)
            {
                App.playingList.Add(songItem.MusicData, false);
            }
            await App.playingList.Play(MusicDataList.First().MusicData, true);
            App.playingList.SetRandomPlay(App.playingList.PlayBehavior);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            InitData();
        }

        DropShadow dropShadow;
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (SelectItemButton.IsChecked == true)
            {
                Children.SelectionMode = ListViewSelectionMode.Multiple;

                SortComboBoxParent.Visibility = Visibility.Collapsed;
                SearchButton.Visibility = Visibility.Collapsed;
                PlayAllButton.Visibility = Visibility.Collapsed;
                AddLocalFilesButton.Visibility = Visibility.Collapsed;
                RefreshButton.Visibility = Visibility.Collapsed;
                MoveItemButton.Visibility = Visibility.Collapsed;

                SelectItemButton.Visibility = Visibility.Visible;
                SelectorSeparator.Visibility = Visibility.Visible;
                AddSelectedToPlayingListButton.Visibility = Visibility.Visible;
                AddSelectedToPlayListButton.Visibility = Visibility.Visible;
                DeleteSelectedButton.Visibility = Visibility.Visible;
                DownloadSelectedButton.Visibility = Visibility.Visible;
                SelectReverseButton.Visibility = Visibility.Visible;
                SelectAllButton.Visibility = Visibility.Visible;

                foreach (SongItem songItem in SongItem.StaticSongItems)
                {
                    songItem.CanClickPlay = false;
                }
            }
            else
            {
                Children.SelectionMode = ListViewSelectionMode.None;

                SortComboBoxParent.Visibility = Visibility.Visible;
                SearchButton.Visibility = Visibility.Visible;
                PlayAllButton.Visibility = Visibility.Visible;
                AddLocalFilesButton.Visibility = Visibility.Visible;
                RefreshButton.Visibility = Visibility.Visible;
                MoveItemButton.Visibility = Visibility.Visible;
                SelectItemButton.Visibility = Visibility.Visible;

                SelectorSeparator.Visibility = Visibility.Collapsed;
                AddSelectedToPlayingListButton.Visibility = Visibility.Collapsed;
                AddSelectedToPlayListButton.Visibility = Visibility.Collapsed;
                DeleteSelectedButton.Visibility = Visibility.Collapsed;
                DownloadSelectedButton.Visibility = Visibility.Collapsed;
                SelectReverseButton.Visibility = Visibility.Collapsed;
                SelectAllButton.Visibility = Visibility.Collapsed;

                foreach (SongItem songItem in SongItem.StaticSongItems)
                {
                    songItem.CanClickPlay = true;
                }
            }
            UpdateCommandToolBarWidth();
        }

        private void AddSelectedToPlayingListButton_Click(object sender, RoutedEventArgs e)
        {
            if (Children.SelectedItems.Any())
            {
                foreach (SongItemBindBase item in Children.SelectedItems.Cast<SongItemBindBase>())
                {
                    App.playingList.Add(item.MusicData);
                }
            }
        }

        private async void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (Children.SelectedItems.Any())
            {
                var result = await MainWindow.ShowDialog("删除歌曲", $"真的要从歌单中删除这{Children.SelectedItems.Count}首歌曲吗？", "取消", "确定", defaultButton: ContentDialogButton.Close);
                if (result == ContentDialogResult.Primary)
                {
                    ToolsCommandBar.IsEnabled = false;
                    var item = MainWindow.AddNotify("删除歌曲", "正在准备删除歌曲...", NotifySeverity.Loading, TimeSpan.MaxValue);
                    var jdata = await PlayListHelper.ReadData();
                    int num = 0;
                    string listName = NavToObj.ListName;
                    foreach (SongItemBindBase data in Children.SelectedItems.Cast<SongItemBindBase>())
                    {
                        num++;
                        item.HorizontalAlignment = HorizontalAlignment.Stretch;
                        item.SetNotifyItemData("删除歌曲", $"进度：{Math.Round(((decimal)num / Children.SelectedItems.Count) * 100, 1)}%\n正在删除：{data.MusicData.Title} - {data.MusicData.ButtonName}", NotifySeverity.Loading);
                        item.SetProcess(Children.SelectedItems.Count, num);
                        await Task.Run(() => jdata = PlayListHelper.DeleteMusicDataFromPlayList(listName, data.MusicData, jdata));
                    }
                    item.HorizontalAlignment = HorizontalAlignment.Center;
                    item.SetNotifyItemData("删除歌曲", "正在保存...", NotifySeverity.Loading);
                    item.SetProcess(0, 0);
                    await PlayListHelper.SaveData(jdata);
                    await App.playListReader.Refresh();
                    item.SetNotifyItemData("删除歌曲", "删除歌曲成功。", NotifySeverity.Complete);
                    MainWindow.NotifyCountDown(item);
                    ToolsCommandBar.IsEnabled = true;

                    if (NavToObj != null)
                    {
                        foreach (var m in App.playListReader.NowMusicListData)
                        {
                            if (m.MD5 == NavToObj.MD5)
                            {
                                NavToObj = m;
                                break;
                            }
                        }
                        InitData();
                    }
                }
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            /*
            foreach (SongItem item in Children.Items)
            {
                (Children.ContainerFromIndex(Children.Items.IndexOf(item)) as ListViewItem).IsSelected = true;
            }*/
            Children.SelectAll();
        }

        private void SelectReverseButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (SongItemBindBase item in Children.Items.Cast<SongItemBindBase>())
            {
                if (Children.SelectedItems.Contains(item))
                {
                    Children.SelectedItems.Remove(item);
                }
                else
                {
                    Children.SelectedItems.Add(item);
                }
            }
        }

        private void AppBarButton_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private async void AddLocalFilesButton_Click(object sender, RoutedEventArgs e)
        {
            StackPanel stackPanel = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
            var ab = new Button() { Content = "多个音频文件", HorizontalAlignment = HorizontalAlignment.Stretch };
            var bb = new Button() { Content = "单个文件夹", HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new(0, 4, 0, 0) };

            ab.Click += async (_, __) =>
            {
                var files = await FileHelper.UserSelectFiles(
                    PickerViewMode.List, PickerLocationId.MusicLibrary);
                    //App.SupportedMediaFormats);
                if (files.Any())
                {
                    MainWindow.HideDialog();
                    ToolsCommandBar.IsEnabled = false;
                    var item = MainWindow.AddNotify("添加本地歌曲", "正在准备添加本地歌曲...", NotifySeverity.Loading, TimeSpan.MaxValue);
                    var jdata = await PlayListHelper.ReadData();
                    int count = 0;
                    string listName = NavToObj.ListName;
                    foreach (var i in files)
                    {
                        item.HorizontalAlignment = HorizontalAlignment.Stretch;
                        item.SetNotifyItemData("添加本地歌曲", $"进度：{count}/{files.Count}，{Math.Round(((decimal)count / files.Count) * 100, 1)}%\n正在添加：{i.Name}", NotifySeverity.Loading);
                        item.SetProcess(files.Count, count);
                        FileInfo fi = null;
                        await Task.Run(() => fi = new FileInfo(i.Path));
                        jdata = await PlayListHelper.AddLocalMusicDataToPlayList(listName, fi, jdata);
                        count++;
                    }
                    item.SetProcess(0, 0);
                    item.HorizontalAlignment = HorizontalAlignment.Center;
                    item.SetNotifyItemData("添加本地歌曲", "正在保存...", NotifySeverity.Loading);
                    await PlayListHelper.SaveData(jdata);
                    await App.playListReader.Refresh();
                    if (NavToObj != null)
                    {
                        foreach (var m in App.playListReader.NowMusicListData)
                        {
                            if (m.MD5 == NavToObj.MD5)
                            {
                                NavToObj = m;
                                break;
                            }
                        }
                        InitData();
                    }
                    ToolsCommandBar.IsEnabled = true;
                    item.SetNotifyItemData("添加本地歌曲", "添加本地歌曲成功。", NotifySeverity.Complete);
                    MainWindow.NotifyCountDown(item);
                }
            };
            bb.Click += async (_, __) =>
            {
                Windows.Storage.StorageFolder folder = await FileHelper.UserSelectFolder(PickerLocationId.MusicLibrary);
                if (folder != null)
                {
                    var jdata = await PlayListHelper.ReadData();
                    DirectoryInfo directory = null;
                    await Task.Run(() => directory = Directory.CreateDirectory(folder.Path));
                    foreach (var i in directory.GetFiles())
                    {
                        if (App.SupportedMediaFormats.Contains(i.Extension))
                        {
                            jdata = await PlayListHelper.AddLocalMusicDataToPlayList(NavToObj.ListName, i, jdata);
                        }
                    }
                    await PlayListHelper.SaveData(jdata);
                    await App.playListReader.Refresh();
                    foreach (var m in App.playListReader.NowMusicListData)
                    {
                        if (m.MD5 == NavToObj.MD5)
                        {
                            NavToObj = m;
                            break;
                        }
                    }
                    InitData();
                    MainWindow.AddNotify("添加本地歌曲成功。", null, NotifySeverity.Complete);
                }
            };

            stackPanel.Children.Add(ab);
            stackPanel.Children.Add(bb);

            await MainWindow.ShowDialog("添加本地文件", stackPanel);
        }

        private void DownloadSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (Children.SelectedItems.Any())
            {
                foreach (SongItemBindBase songItem in Children.SelectedItems)
                {
                    App.downloadManager.Add(songItem.MusicData);
                }
            }
        }

        private async void AddToPlayListFlyout_Opened(object sender, object e)
        {
            AddToPlayListFlyout.Items.Clear();
            foreach (var m in await PlayListHelper.ReadAllPlayList())
            {
                var a = new MenuFlyoutItem()
                {
                    Text = m.ListShowName,
                    Tag = m
                };
                a.Click += A_Click;

                AddToPlayListFlyout.Items.Add(a);
            }
        }

        private async void A_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ShowLoadingDialog();
            var text = await PlayListHelper.ReadData();
            var list = (sender as MenuFlyoutItem).Tag as MusicListData;
            var listName = list.ListName;
            foreach (SongItemBindBase item in Children.SelectedItems.Cast<SongItemBindBase>())
            {
                MainWindow.SetLoadingText($"正在添加：{item.MusicData.Title} - {item.MusicData.ButtonName}");
                MainWindow.SetLoadingProgressRingValue(Children.SelectedItems.Count, Children.SelectedItems.IndexOf(item));

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

        private void AddToPlayListFlyout_Closed(object sender, object e)
        {
            //AddToPlayListFlyout.Items.Clear();
        }

        private async void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsNavigatedOutFromPage) return;
            if (isLoading) return;
            if (NavToObj.PlaySort == (PlaySort)SortComboBox.SelectedItem) return;

            NavToObj.PlaySort = (PlaySort)SortComboBox.SelectedItem;
            InitData();
            var data = await PlayListHelper.ReadData();
            data[NavToObj.ListName] = JObject.FromObject(NavToObj);
            await PlayListHelper.SaveData(data);
        }

        private void Children_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var a = Children.ContainerFromItem(Children.SelectedItem);
        }

        private void SearchBox_AccessKeyInvoked(UIElement sender, Microsoft.UI.Xaml.Input.AccessKeyInvokedEventArgs args)
        {
            (sender as FrameworkElement).Focus(FocusState.Programmatic);
        }

        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            switch ((sender as Button).Tag)
            {
                case "0":
                    scrollViewer?.ChangeView(null, 0, null);
                    break;
                case "1":
                    scrollViewer?.ChangeView(null, scrollViewer.ScrollableHeight, null);
                    break;
                case "2":
                    foreach(var i in MusicDataList)
                    {
                        if (i.MusicData == App.audioPlayer.MusicData)
                        {
                            await Children.SmoothScrollIntoViewWithItemAsync(i, ScrollItemPlacement.Center);
                            await Children.SmoothScrollIntoViewWithItemAsync(i, ScrollItemPlacement.Center, true);
                            foreach (var j in SongItem.StaticSongItems)
                            {
                                if (j != null)
                                    if (j.MusicData == App.audioPlayer.MusicData)
                                        j.AnimateStroke();
                            }
                        }
                    }
                    break;
            }
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            MainWindow.CanKeyDownBack = false;
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            MainWindow.CanKeyDownBack = true;
        }

        private void Children_DropCompleted(UIElement sender, DropCompletedEventArgs args)
        {

        }

        private async void MoveItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavToObj.PlaySort != PlaySort.默认升序)
            {
                MoveItemButton.IsChecked = false;
                MainWindow.AddNotify("无法使用排序", "排序功能只能在此列表排序方式为 \"默认升序\" 时可使用。", NotifySeverity.Error);
                return;
            }

            if ((bool)MoveItemButton.IsChecked)
            {
                SortComboBoxParent.Visibility = Visibility.Collapsed;
                SearchButton.Visibility = Visibility.Collapsed;
                PlayAllButton.Visibility = Visibility.Collapsed;
                AddLocalFilesButton.Visibility = Visibility.Collapsed;
                RefreshButton.Visibility = Visibility.Collapsed;
                SelectItemButton.Visibility = Visibility.Collapsed;
                CancelMoveItemButton.Visibility = Visibility.Visible;
                MoveItemButton.Label = "完成排序";

                foreach (var i in SongItem.StaticSongItems) i.RemoveUnloadedEvent();
                Children.AllowDrop = true;
                Children.CanDragItems = true;
                Children.CanReorderItems = true;
                Children.SelectionMode = ListViewSelectionMode.Multiple;
                SelectItemButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                SortComboBoxParent.Visibility = Visibility.Visible;
                SearchButton.Visibility = Visibility.Visible;
                PlayAllButton.Visibility = Visibility.Visible;
                AddLocalFilesButton.Visibility = Visibility.Visible;
                RefreshButton.Visibility = Visibility.Visible;
                SelectItemButton.Visibility = Visibility.Visible;
                CancelMoveItemButton.Visibility = Visibility.Collapsed;
                MoveItemButton.Label = "排序";

                Children.AllowDrop = false;
                Children.CanDragItems = false;
                Children.CanReorderItems = false;
                Children.SelectionMode = ListViewSelectionMode.None;
                SelectItemButton.Visibility = Visibility.Visible;
                foreach (var i in SongItem.StaticSongItems) i.AddUnloadedEvent();

                var item = MainWindow.AddNotify("正在保存排序...", null, NotifySeverity.Loading, TimeSpan.MaxValue);
                var data = await PlayListHelper.ReadData();
                NavToObj.Songs.Clear();
                foreach (var i in MusicDataList)
                {
                    NavToObj.Songs.Add(i.MusicData);
                }
                data[NavToObj.ListName] = JObject.FromObject(NavToObj);
                await PlayListHelper.SaveData(data);
                await App.playListReader.Refresh();
                item.SetNotifyItemData("保存排序完成。", null, NotifySeverity.Complete);
                MainWindow.NotifyCountDown(item);
            }
            UpdateCommandToolBarWidth();
        }

        private void CancelMoveItemButton_Click(object sender, RoutedEventArgs e)
        {
            MoveItemButton.IsChecked = false;
            SortComboBoxParent.Visibility = Visibility.Visible;
            SearchButton.Visibility = Visibility.Visible;
            PlayAllButton.Visibility = Visibility.Visible;
            AddLocalFilesButton.Visibility = Visibility.Visible;
            RefreshButton.Visibility = Visibility.Visible;
            SelectItemButton.Visibility = Visibility.Visible;
            CancelMoveItemButton.Visibility = Visibility.Collapsed;
            MoveItemButton.Label = "排序";

            Children.AllowDrop = false;
            Children.CanDragItems = false;
            Children.CanReorderItems = false;
            Children.SelectionMode = ListViewSelectionMode.None;
            SelectItemButton.Visibility = Visibility.Visible;
            foreach (var i in SongItem.StaticSongItems) i.AddUnloadedEvent();
            UpdateCommandToolBarWidth();
        }

        private async void ChangeViewToSearchItem(SongItemBindBase item)
        {
            if (searchMusicDatas.Any())
            {
                searchNum = searchMusicDatas.IndexOf(item) - 1;
                SearchResultTextBlock.Text = $"{searchNum + 1} of {searchMusicDatas.Count}";
                await Children.SmoothScrollIntoViewWithItemAsync(item, ScrollItemPlacement.Center);
                await Children.SmoothScrollIntoViewWithItemAsync(item, ScrollItemPlacement.Center, true);
                foreach (var s in SongItem.StaticSongItems)
                {
                    if (s.MusicData == item.MusicData) s.AnimateStroke();
                }
            }
        }

        private async void ChangeViewToSearchItem(bool add = true)
        {
            if (searchMusicDatas.Any())
            {
                if (add) searchNum++;
                else searchNum--;

                if (searchNum > searchMusicDatas.Count - 1) searchNum = 0;
                if (searchNum <= -1) searchNum = searchMusicDatas.Count - 1;

                var item = searchMusicDatas[searchNum];
                SearchResultTextBlock.Text = $"{searchNum + 1} of {searchMusicDatas.Count}";

                var scrollPlacement = ActualHeight <= 450 ? ScrollItemPlacement.Bottom : ScrollItemPlacement.Center;
                await Children.SmoothScrollIntoViewWithItemAsync(item, scrollPlacement);
                await Children.SmoothScrollIntoViewWithItemAsync(item, scrollPlacement, true);

                foreach (var s in SongItem.StaticSongItems)
                {
                    if (s.MusicData == item.MusicData) s.AnimateStroke();
                }
            }
            else
            {
                SearchResultTextBlock.Text = "0 of 0";
            }
        }

        bool isQuery = false;
        int searchNum = -1;
        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            ChangeViewToSearchItem();
            AutoSuggestBox_TextChanged(null, new() { Reason = AutoSuggestionBoxTextChangeReason.UserInput });
        }

        private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (SearchModeComboBox.SelectedIndex == 0)
                SearchBox.Text = (args.SelectedItem as SongItemBindBase).MusicData.Title;
            else if(SearchModeComboBox.SelectedIndex == 1)
                SearchBox.Text = (args.SelectedItem as SongItemBindBase).MusicData.Title;
            else if(SearchModeComboBox.SelectedIndex == 2)
                SearchBox.Text = (args.SelectedItem as SongItemBindBase).MusicData.ArtistName;
            else if(SearchModeComboBox.SelectedIndex == 3)
                SearchBox.Text = (args.SelectedItem as SongItemBindBase).MusicData.Album.Title;

            searchNum = searchMusicDatas.IndexOf(args.SelectedItem as SongItemBindBase) - 1;
            SearchResultTextBlock.Text = $"{searchNum + 2} of {searchMusicDatas.Count}";
            //ChangeViewToSearchItem(args.SelectedItem as SongItemBindBase);
        }

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput) // 有 bug
            {
                if (string.IsNullOrEmpty(SearchBox.Text)) return;
                searchMusicDatas.Clear();
                string text = CompareString(SearchBox.Text);

                switch (SearchModeComboBox.SelectedIndex)
                {
                    case 0:
                        foreach (var i in MusicDataList)
                        {
                            if (CompareString(i.MusicData.Title).Contains(text))
                                if (!searchMusicDatas.Contains(i))
                                    searchMusicDatas.Add(i);
                            if (i.MusicData.Title2 is not null)
                            {
                                if (CompareString(i.MusicData.Title2).Contains(text))
                                    if (!searchMusicDatas.Contains(i))
                                        searchMusicDatas.Add(i);
                            }
                            if (i.MusicData.ArtistName is not null)
                            {
                                if (CompareString(i.MusicData.ArtistName).Contains(text))
                                    if (!searchMusicDatas.Contains(i))
                                        searchMusicDatas.Add(i);
                            }
                            if (CompareString(i.MusicData.Album.Title).Contains(text))
                                if (!searchMusicDatas.Contains(i))
                                    searchMusicDatas.Add(i);
                        }
                        break;
                    case 1:
                        foreach (var i in MusicDataList)
                        {
                            if (CompareString(i.MusicData.Title).Contains(text))
                                searchMusicDatas.Add(i);
                            else if (i.MusicData.Title2 is not null)
                            {
                                if (CompareString(i.MusicData.Title2).Contains(text))
                                    searchMusicDatas.Add(i);
                            }
                        }
                        break;
                    case 2:
                        foreach (var i in MusicDataList)
                        {
                            if (i.MusicData.ArtistName is not null)
                            {
                                if (CompareString(i.MusicData.ArtistName).Contains(text))
                                    searchMusicDatas.Add(i);
                            }
                        }
                        break;
                    case 3:
                        foreach (var i in MusicDataList)
                        {
                            if (CompareString(i.MusicData.Album.Title).Contains(text))
                                searchMusicDatas.Add(i);
                        }
                        break;
                }
                searchNum = 0;
                SearchResultTextBlock.Text = $"All of {searchMusicDatas.Count}";
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (SearchBase.IsHitTestVisible)
            {
                SearchBase.Opacity = 0;
                menu_border.Margin = new(0, 0, 0, 0);
            }
            else
            {
                SearchBase.Opacity = 1;
                menu_border.Margin = new(0, 0, 0, searchBaseVisual.Size.Y + 4 + 4);
                SearchBox.Focus(FocusState.Keyboard);
            }
            SearchBase.IsHitTestVisible = !SearchBase.IsHitTestVisible;
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn.Tag as string == "0")
            {
                ChangeViewToSearchItem(false);
            }
            else
            {
                ChangeViewToSearchItem();
            }
        }

        private string CompareString(string str)
        {
            return (bool)LowerCheckBox.IsChecked ? str : str.ToLower();
        }

        private void MainWindow_InKeyDownEvent(Windows.System.VirtualKey key)
        {
            if (MainWindow.isControlDown)
            {
                if (key == Windows.System.VirtualKey.F)
                {
                    SearchButton_Click(null, null);
                    if (!SearchBase.IsHitTestVisible)
                        ToolsCommandBar.Focus(FocusState.Programmatic);
                }
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            LeavingPageDo();
        }
    }
}