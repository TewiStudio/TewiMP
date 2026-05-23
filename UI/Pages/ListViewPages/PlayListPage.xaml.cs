using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Composition;
using Windows.System;
using Newtonsoft.Json.Linq;
using CommunityToolkit.WinUI;
using TewiMP.UI.Controls;
using TewiMP.UI.Windows;
using TewiMP.Core;
using TewiMP.Core.Music;
using TewiMP.Core.Models;
using TewiMP.Helpers;
using TewiMP.Services;
using TewiMP.Services.Media;
using TewiMP.Services.Storage;

namespace TewiMP.UI.Pages.ListViewPages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class PlayListPage : Page
{
    MusicListData musicListData { get; set; } = null;
    ObservableCollection<MusicDataViewModel> musicListBind { get; set; } = [];
    ScrollViewer scrollViewer;
    PageData PageData { get; set; }
    public string md5;
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        PageData = e.Parameter as PageData;
        if (PageData.Param is string m)
            md5 = m;
        else if (PageData.Param is MusicListData data)
        {
            md5 = null;
            musicListData = data;
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        if (scrollViewer is not null)
            PageData.VerticalOffset = scrollViewer.VerticalOffset;
        PageData = null;

        musicListData = null;
        App.MainWindowInstance.WindowDpiChanged -= MainWindow_WindowDpiChanged;
    }

    public PlayListPage()
    {
        InitializeComponent();
        //arrayList = new ArrayList(100000000);
    }

    // Items 更新时 CommandBar 宽度不会更新 >:(
    async void UpdateCommandBarWidth()
    {
        // 创可贴写法 :(
        ItemsList_Header_Info_CommandBar.Width = 0;
        await Task.Delay(50);
        InitShyHeader();
        ItemsList_Header_Info_CommandBar.Width = 0;
        ItemsList_Header_Info_CommandBar.Width = double.NaN;
        await Task.Delay(100);
        InitShyHeader();
        ItemsList_Header_Info_CommandBar.Width = 0;
        ItemsList_Header_Info_CommandBar.Width = double.NaN;
    }
    void MultiSelectDo(bool isChecked)
    {
        if (!this.IsLoaded) return;
        if (musicListBind is null) return;
        
        void SetVisibility(FrameworkElement element)
        {
            var tag = (string)element.Tag;
            if (string.IsNullOrEmpty(tag))
            {
                element.Visibility = isChecked ? Visibility.Collapsed : Visibility.Visible;
                return;
            }
            if (tag.Equals("multiSelect") || tag.Contains("move_")) return;
            if (tag.Contains("multi_"))
                element.Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;
            else
            {
                if (tag.Contains("edit_"))
                    element.Visibility = Visibility.Collapsed;
                    //element.Visibility = isChecked ? Visibility.Collapsed : musicListData?.ListDataType == DataType.LocalPlaylist ? Visibility.Visible : Visibility.Collapsed;
                else
                    element.Visibility = isChecked ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        foreach (FrameworkElement element in ItemsList_Header_Info_CommandBar.PrimaryCommands.Cast<FrameworkElement>())
            SetVisibility(element);
        foreach (FrameworkElement element in ItemsList_Header_Info_CommandBar.SecondaryCommands.Cast<FrameworkElement>())
            SetVisibility(element);

        ItemsList.SelectionMode = isChecked ? ListViewSelectionMode.Multiple : ListViewSelectionMode.None;
        UpdateCommandBarWidth();
        MusicDataItem.SetIsCloseMouseEvent(isChecked);
    }
    void MoveItemDo(bool isChecked)
    {
        if (!this.IsLoaded) return;
        if (musicListBind is null) return;

        void SetVisibility(FrameworkElement element)
        {
            var tag = (string)element.Tag;
            if (string.IsNullOrEmpty(tag))
            {
                element.Visibility = isChecked ? Visibility.Collapsed : Visibility.Visible;
                return;
            }
            if (tag.Equals("move") || tag.Contains("multi_")) return;
            if (tag.Contains("move_"))
                element.Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;
            else
            {
                if (tag.Contains("edit_"))
                    element.Visibility = Visibility.Collapsed;
                //element.Visibility = isChecked ? Visibility.Collapsed : musicListData?.ListDataType == DataType.LocalPlaylist ? Visibility.Visible : Visibility.Collapsed;
                else
                    element.Visibility = isChecked ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        foreach (FrameworkElement element in ItemsList_Header_Info_CommandBar.PrimaryCommands.Cast<FrameworkElement>())
            SetVisibility(element);
        foreach (FrameworkElement element in ItemsList_Header_Info_CommandBar.SecondaryCommands.Cast<FrameworkElement>())
            SetVisibility(element);

        moveButton.Label = isChecked ? "完成排序" : "排序";
        ItemsList.AllowDrop = isChecked;
        ItemsList.CanDragItems = isChecked;
        ItemsList.CanReorderItems = isChecked;
        ItemsList.SelectionMode = isChecked ? ListViewSelectionMode.Multiple : ListViewSelectionMode.None;
        MusicDataItem.SetIsCloseMouseEvent(isChecked ? true : false, true);
        App.MainWindowInstance.AllowDragEvents = !isChecked;
        UpdateCommandBarWidth();
    }
    async void MoveItemSave()
    {
        ItemsList_Header_Info_CommandBar.IsEnabled = false;
        var item = App.MainWindowInstance.AddNotify("正在保存排序...", null, NotifySeverity.Loading, TimeSpan.MaxValue);
        var data = await PlayListHelper.ReadData();
        musicListData.Songs.Clear();
        foreach (var i in musicListBind)
        {
            musicListData.Songs.Add(i.MusicData);
        }
        data[musicListData.ListName] = JObject.FromObject(musicListData);
        await PlayListHelper.SaveData(data);
        await App.Instance.PlayListReader.Refresh();
        InitInfo();
        InitBindings();
        item.SetNotifyItemData("保存排序完成。", null, NotifySeverity.Complete);
        App.MainWindowInstance.NotifyCountDown(item);
        ItemsList_Header_Info_CommandBar.IsEnabled = true;
        UpdateCommandBarWidth();
    }

    void SelectedReverseDo()
    {
        foreach (MusicDataViewModel item in ItemsList.Items.Cast<MusicDataViewModel>())
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
            var result = await App.MainWindowInstance.ShowDialog("移除歌曲", $"真的要从播放列表中移除这{ItemsList.SelectedItems.Count}首歌曲吗？", "取消", "确定", defaultButton: ContentDialogButton.Close);
            if (result == ContentDialogResult.Primary)
            {
                ItemsList_Header_Info_CommandBar.IsEnabled = false;
                var item = App.MainWindowInstance.AddNotify("删除歌曲", "正在准备删除歌曲...", NotifySeverity.Loading, TimeSpan.MaxValue);
                var jdata = await PlayListHelper.ReadData();
                int num = 0;
                string listName = musicListData.ListName;
                foreach (MusicDataViewModel data in ItemsList.SelectedItems.Cast<MusicDataViewModel>())
                {
                    num++;
                    item.HorizontalAlignment = HorizontalAlignment.Stretch;
                    item.SetNotifyItemData("删除歌曲", $"进度：{Math.Round(((decimal)num / ItemsList.SelectedItems.Count) * 100, 1)}%\n正在删除：{data.MusicData.Title} - {data.MusicData.ButtonName}", NotifySeverity.Loading);
                    item.SetProcess(ItemsList.SelectedItems.Count, num);
                    musicListData.Songs.Remove(data.MusicData);
                }
                jdata[musicListData.ListName] = JObject.FromObject(musicListData);

                item.HorizontalAlignment = HorizontalAlignment.Center;
                item.SetNotifyItemData("删除歌曲", "正在保存...", NotifySeverity.Loading);
                item.SetProcess(0, 0);
                await PlayListHelper.SaveData(jdata);
                await App.Instance.PlayListReader.Refresh();
                item.SetNotifyItemData("删除歌曲", "删除歌曲成功。", NotifySeverity.Complete);
                App.MainWindowInstance.NotifyCountDown(item);
                ItemsList_Header_Info_CommandBar.IsEnabled = true;
                InitInfo();
                InitBindings();
            }
            UpdateCommandBarWidth();
        }
    }
    void DownloadSelectedItemDo()
    {
        if (ItemsList.SelectedItems.Any())
        {
            foreach (MusicDataViewModel songItem in ItemsList.SelectedItems)
            {
                App.Instance.DownloadService.Add(songItem.MusicData);
            }
        }
    }
    void AddSelectedItemToPlayingDo()
    {
        if (ItemsList.SelectedItems.Any())
        {
            foreach (MusicDataViewModel item in ItemsList.SelectedItems.Cast<MusicDataViewModel>())
            {
                App.Instance.PlayingListService.Add(item.MusicData);
            }
        }
    }

    async void AddLocalFilesDo()
    {
        await App.MainWindowInstance.ShowDialog("添加本地文件", new DialogPages.AddFilesToMusicListDataPage() { musicListData = this.musicListData });
    }

    CompositionPropertySet scrollerPropertySet;
    Compositor compositor;
    Visual itemsStackPanelVisual;
    Visual scrollVisual;
    Visual headerVisual;
    Visual backgroundVisual;
    Visual imageVisual;
    Visual infoVisual;
    Visual commandBarVisual;
    Visual headerFootRootVisual;
    Visual searchRootVisual;
    InsetClip itemsStackPanelClip;
    ScalarKeyFrameAnimation commandBarVisualOpacityAnimation;
    void InitVisuals()
    {
        if (!IsLoaded) return;
        MultiSelectDo(false);
        MoveItemDo(false);

        var itemsStackPanel = CodeHelper.FindDescendant<ItemsStackPanel>(ItemsList);
        itemsStackPanelVisual = ElementCompositionPreview.GetElementVisual(itemsStackPanel);
        itemsStackPanelClip = itemsStackPanelVisual.Compositor.CreateInsetClip();
        itemsStackPanelVisual.Clip = itemsStackPanelClip;

        var headerPresenter = (UIElement)VisualTreeHelper.GetParent((UIElement)ItemsList.Header);
        var headerContainer = (UIElement)VisualTreeHelper.GetParent(headerPresenter);
        Canvas.SetZIndex(headerContainer, 1);

        scrollViewer = (VisualTreeHelper.GetChild(ItemsList, 0) as Border).Child as ScrollViewer;
        scrollViewer.CanContentRenderOutsideBounds = true;
        scrollViewer.ViewChanged -= ScrollViewer_ViewChanged;
        scrollViewer.ViewChanged += ScrollViewer_ViewChanged;
        scrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);

        compositor = scrollerPropertySet.Compositor;
        scrollVisual = ElementCompositionPreview.GetElementVisual(scrollViewer);
        headerVisual = ElementCompositionPreview.GetElementVisual(ItemsList_Header_Root);
        backgroundVisual = ElementCompositionPreview.GetElementVisual(ItemsList_Header_ImageInfo_BackgroundFill);
        imageVisual = ElementCompositionPreview.GetElementVisual(ItemsList_Header_Image_Root);
        infoVisual = ElementCompositionPreview.GetElementVisual(ItemsList_Header_Info_Root);
        headerFootRootVisual = ElementCompositionPreview.GetElementVisual(ItemsList_Header_Foot_Root);
        searchRootVisual = ElementCompositionPreview.GetElementVisual(ItemList_Header_Search_Root);
        commandBarVisual = ElementCompositionPreview.GetElementVisual(ItemsList_Header_Info_CommandBar);

        commandBarVisual.Opacity = 0;
        AnimateHelper.AnimateScalar(commandBarVisual, 1, 0.3, 0, 0, 0, 0, out commandBarVisualOpacityAnimation);
    }

    ExpressionAnimation logoHeaderScaleAnimation;
    ExpressionAnimation offsetExpression;
    ExpressionAnimation backgroundVisualOpacityAnimation;
    ExpressionAnimation imageVisualOffsetAnimation;
    ExpressionAnimation infoVisualOffsetAnimation;
    ExpressionAnimation commandBarVisualOffsetAnimation;
    ExpressionAnimation headerFootRootVisualOffsetAnimation;
    ExpressionAnimation searchRootVisualOffsetAnimation;
    private ExpressionAnimation _logoScaleAnim;
    private ExpressionAnimation _headerOffsetAnim;
    private ExpressionAnimation _itemsStackClipAnim;
    private ExpressionAnimation _bgOpacityAnim;
    private ExpressionAnimation _imgOffsetAnim;
    private ExpressionAnimation _infoOffsetAnim;
    private ExpressionAnimation _cmdBarOffsetAnim;
    private ExpressionAnimation _footerOffsetAnim;
    private ExpressionAnimation _searchOffsetAnim;

    private const string ProgressExp = "Clamp(-scroller.Translation.Y / HeightParam, 0, 1.0)";
    async Task InitShyHeader(bool imageSizeOnly = false, bool delay = false)
    {
        if (scrollViewer is null || compositor is null || !IsLoaded) return;

        if (delay) await Task.Delay(10);

        float anotherHeight = 154f;
        float imageSizeEnd = 0.45f;

        // 获取当前控件的尺寸
        float imgRootWidth = (float)ItemsList_Header_Image_Root.ActualWidth;
        float headerRootWidth = (float)ItemsList_Header_Root.ActualWidth;
        float headerRootHeight = (float)ItemsList_Header_Root.ActualHeight;
        float visualH = headerFootRootVisual.Size.Y;
        float actualH = (float)ActualHeight;

        // 计算 Width 逻辑
        if (headerRootWidth != 0)
        {
            float currentScaleX = imageVisual.Scale.X;
            var calculatedWidth = headerRootWidth - imgRootWidth * currentScaleX - 32 - 16;
            ItemsList_Header_Info_Root_SizeChanger.Width = calculatedWidth <= 0 ? 0 : calculatedWidth;
        }

        // Logo Scale 动画
        if (_logoScaleAnim is null)
        {
            string exp = $"Lerp(Vector2(1, 1), Vector2(TargetScale, TargetScale), {ProgressExp})";
            _logoScaleAnim = compositor.CreateExpressionAnimation(exp);
            _logoScaleAnim.SetReferenceParameter("scroller", scrollerPropertySet);
        }
        // 更新参数
        _logoScaleAnim.SetScalarParameter("HeightParam", anotherHeight);
        _logoScaleAnim.SetScalarParameter("TargetScale", imageSizeEnd);

        // 启动动画
        imageVisual.StartAnimation("Scale.xy", _logoScaleAnim);

        if (imageSizeOnly) return;

        // Header Offset 动画
        if (_headerOffsetAnim is null)
        {
            // -scroller.Y - (Progress * Height)
            string exp = $"-scroller.Translation.Y - ({ProgressExp} * HeightParam)";
            _headerOffsetAnim = compositor.CreateExpressionAnimation(exp);
            _headerOffsetAnim.SetReferenceParameter("scroller", scrollerPropertySet);
        }
        _headerOffsetAnim.SetScalarParameter("HeightParam", anotherHeight);
        headerVisual.StartAnimation("Offset.Y", _headerOffsetAnim);

        // ItemStackClip 动画
        if (_itemsStackClipAnim is null)
        {
            string exp = $"-scroller.Translation.Y - ({ProgressExp} * HeightParam)";
            _itemsStackClipAnim = compositor.CreateExpressionAnimation(exp);
            _itemsStackClipAnim.SetReferenceParameter("scroller", scrollerPropertySet);
        }
        _itemsStackClipAnim.SetScalarParameter("HeightParam", anotherHeight);
        itemsStackPanelClip.StartAnimation(nameof(itemsStackPanelClip.TopInset), _itemsStackClipAnim);

        // Search Offset
        if (_searchOffsetAnim is null)
        {
            string exp = $"Vector3(0, TargetY, 0)";
            _searchOffsetAnim = compositor.CreateExpressionAnimation(exp);
            // _searchOffsetAnim.SetReferenceParameter("scroller", scrollerPropertySet);
        }
        _searchOffsetAnim.SetScalarParameter("TargetY", headerRootHeight + 4);
        searchRootVisual.StartAnimation(nameof(searchRootVisual.Offset), _searchOffsetAnim);

        // Background Opacity 动画
        if (_bgOpacityAnim is null)
        {
            string exp = $"Lerp(0, 1, {ProgressExp})";
            _bgOpacityAnim = compositor.CreateExpressionAnimation(exp);
            _bgOpacityAnim.SetReferenceParameter("scroller", scrollerPropertySet);
        }
        _bgOpacityAnim.SetScalarParameter("HeightParam", anotherHeight);
        backgroundVisual.StartAnimation("Opacity", _bgOpacityAnim);

        // Image Visual Offset 动画
        if (_imgOffsetAnim is null)
        {
            string exp = $"Lerp(Vector3(0,0,0), Vector3(0, HeightParam, 0), {ProgressExp})";
            _imgOffsetAnim = compositor.CreateExpressionAnimation(exp);
            _imgOffsetAnim.SetReferenceParameter("scroller", scrollerPropertySet);
        }
        _imgOffsetAnim.SetScalarParameter("HeightParam", anotherHeight);
        imageVisual.StartAnimation("Offset", _imgOffsetAnim);

        // Info Visual Offset 动画
        if (_infoOffsetAnim is null)
        {
            // Start: (StartX, 0, 0) -> End: (EndX, HeightParam, 0)
            string exp = $"Lerp(Vector3(StartX, 0, 0), Vector3(EndX, HeightParam, 0), {ProgressExp})";
            _infoOffsetAnim = compositor.CreateExpressionAnimation(exp);
            _infoOffsetAnim.SetReferenceParameter("scroller", scrollerPropertySet);
        }
        _infoOffsetAnim.SetScalarParameter("HeightParam", anotherHeight);
        _infoOffsetAnim.SetScalarParameter("StartX", imgRootWidth + 16);
        _infoOffsetAnim.SetScalarParameter("EndX", (int)(imgRootWidth * imageSizeEnd) + 16);
        infoVisual.StartAnimation(nameof(infoVisual.Offset), _infoOffsetAnim);

        // Command Bar Offset 动画
        if (_cmdBarOffsetAnim is null)
        {
            // Start: (-6, StartY, 0) -> End: (-6, EndY, 0)
            string exp = $"Lerp(Vector3(-6, StartY, 0), Vector3(-6, EndY, 0), {ProgressExp})";
            _cmdBarOffsetAnim = compositor.CreateExpressionAnimation(exp);
            _cmdBarOffsetAnim.SetReferenceParameter("scroller", scrollerPropertySet);
        }
        float cmdBarH = commandBarVisual.Size.Y;
        float imgVisualH = imageVisual.Size.Y;

        _cmdBarOffsetAnim.SetScalarParameter("HeightParam", anotherHeight);
        _cmdBarOffsetAnim.SetScalarParameter("StartY", imgVisualH - cmdBarH + 6);
        _cmdBarOffsetAnim.SetScalarParameter("EndY", imgVisualH * imageSizeEnd - cmdBarH + 6);
        commandBarVisual.StartAnimation(nameof(commandBarVisual.Offset), _cmdBarOffsetAnim);

        // Header Foot Root Offset 动画
        if (_footerOffsetAnim is null)
        {
            string exp = $"Lerp(Vector3(-16, StartY, 0), Vector3(-16, EndY, 0), {ProgressExp})";
            _footerOffsetAnim = compositor.CreateExpressionAnimation(exp);
            _footerOffsetAnim.SetReferenceParameter("scroller", scrollerPropertySet);
        }
        _footerOffsetAnim.SetScalarParameter("HeightParam", anotherHeight);
        _footerOffsetAnim.SetScalarParameter("StartY", actualH - visualH - 8);
        _footerOffsetAnim.SetScalarParameter("EndY", anotherHeight + actualH - visualH - 8);
        headerFootRootVisual.StartAnimation("Offset", _footerOffsetAnim);
    }
    void DisposeVisuals()
    {
        logoHeaderScaleAnimation?.Dispose();
        offsetExpression?.Dispose(); 
        _itemsStackClipAnim?.Dispose();
        backgroundVisualOpacityAnimation?.Dispose();
        imageVisualOffsetAnimation?.Dispose();
        infoVisualOffsetAnimation?.Dispose();
        commandBarVisualOffsetAnimation?.Dispose();
        headerFootRootVisualOffsetAnimation?.Dispose();
        commandBarVisualOpacityAnimation?.Dispose();

        scrollVisual = null;
        scrollerPropertySet = null;
        compositor = null;
        headerVisual = null;
        backgroundVisual = null;
        imageVisual = null;
        infoVisual = null;
        commandBarVisual = null;
        headerFootRootVisual = null;
        logoHeaderScaleAnimation = null;
        offsetExpression = null;
        backgroundVisualOpacityAnimation = null;
        imageVisualOffsetAnimation = null;
        infoVisualOffsetAnimation = null;
        commandBarVisualOffsetAnimation = null;
        headerFootRootVisualOffsetAnimation = null;
        commandBarVisualOpacityAnimation = null;
    }

    bool isInInitBindings = false;
    List<PlaySort> listSortEnum = null;
    async void InitBindings()
    {
        if (!IsLoaded) return;
        if (isInInitBindings) return;
        isInInitBindings = true;
        LoadingTipControl.ShowLoading();

        if (musicListData.Songs is null && musicListData.ListFrom == MusicFrom.pluginMusicSource)
        {
            musicListData = await musicListData.GetMusicSourcePlugin().GetPlayList(musicListData.ID);
            InitInfo();
        }

        var sortedSongs = await GetSortedSongsAsync(musicListData);
        if (!IsLoaded || musicListData is null || sortedSongs is null)
        {
            sortedSongs = null; isInInitBindings = false;
            LoadingTipControl.UnShowLoading();
            return;
        }
        musicListBind.Clear();

        int count = 1;
        foreach (var musicData in sortedSongs)
        {
            musicListBind.Add(new(musicData, musicListData, count++));
        }

        SortComboBox.SelectedIndex = (int)musicListData.PlaySort;
        LoadingTipControl.UnShowLoading();
        isInInitBindings = false;

        await Task.Delay(10);
        if (PageData is not null && PageData.VerticalOffset is not 0) scrollViewer.ScrollToVerticalOffset(PageData.VerticalOffset);
    }

    void InitInfo()
    {
        if (!IsLoaded) return;
        if (md5 != null)
        {
            foreach (var mld in App.Instance.PlayListReader.NowMusicListData)
            {
                if (mld.MD5 == md5)
                {
                    musicListData = mld;
                    break;
                }
            }
        }

        if (musicListData is null) return;
        listSortEnum = [.. Enum.GetValues<PlaySort>().Cast<PlaySort>()];
        SortComboBox.ItemsSource = null;
        SortComboBox.ItemsSource = listSortEnum;
        SortComboBox.SelectedIndex = (int)musicListData.PlaySort;

        App.MainWindowInstance.WindowDpiChanged -= MainWindow_WindowDpiChanged;
        App.MainWindowInstance.WindowDpiChanged += MainWindow_WindowDpiChanged;
        ItemsList_Header_Info_TitleTextBlock.Text = musicListData.ListShowName;
        ItemsList_Header_Info_OtherTextBlock.Text = $"共 {musicListData.Songs?.Count} 首\n{musicListData.CreationTime.ToRelativeTime()}";
    }

    static Thickness thickness0 = new(0);
    static Thickness thickness1 = new(1);
    Uri imageSource = null;
    async void InitImage()
    {
        if (!IsLoaded) return;
        if (musicListData is null) return;
        ItemsList_Header_Image.Source = null;
        ItemsList_Header_Image.BorderThickness = thickness0;
        if (musicListData.ListDataType is DataType.LocalPlaylist or DataType.Playlist)
        {
            imageSource = await ImageService.GetImageUri(musicListData);
        }

        if (!IsLoaded || musicListData is null) return;
        ItemsList_Header_Image.BorderThickness = thickness1;
        ItemsList_Header_Image.Source = imageSource;
        await InitShyHeader();
        commandBarVisual.StartAnimation("Opacity", commandBarVisualOpacityAnimation);
        await InitAccentColor();
        //PlayAllButton.RequestedTheme = CodeHelper.IsAccentColorDark(color.Item1) ? ElementTheme.Dark : ElementTheme.Light;
    }

    void InitEvents()
    {
        App.Instance.PlayListReader.Updated -= PlayListReader_Updated;
        App.Instance.PlayListReader.Updated += PlayListReader_Updated;
        App.MainWindowInstance.InKeyDownEvent -= MainWindow_InKeyDownEvent;
        App.MainWindowInstance.InKeyDownEvent += MainWindow_InKeyDownEvent;
        App.MainWindowInstance.MusicPageViewStateChanged -= MainWindowInstance_MusicPageViewStateChanged;
        App.MainWindowInstance.MusicPageViewStateChanged += MainWindowInstance_MusicPageViewStateChanged;
        ItemList_Header_Search_Control.SearchingAItem -= ItemList_Header_Search_Control_SearchingAItem;
        ItemList_Header_Search_Control.SearchingAItem += ItemList_Header_Search_Control_SearchingAItem;
        ItemList_Header_Search_Control.IsOpenChanged -= ItemList_Header_Search_Control_IsOpenChanged;
        ItemList_Header_Search_Control.IsOpenChanged += ItemList_Header_Search_Control_IsOpenChanged;
    }

    void RemoveEvents()
    {
        App.Instance.PlayListReader.Updated -= PlayListReader_Updated;
        App.MainWindowInstance.InKeyDownEvent -= MainWindow_InKeyDownEvent;
        App.MainWindowInstance.MusicPageViewStateChanged -= MainWindowInstance_MusicPageViewStateChanged;
        ItemList_Header_Search_Control.SearchingAItem -= ItemList_Header_Search_Control_SearchingAItem;
        ItemList_Header_Search_Control.IsOpenChanged -= ItemList_Header_Search_Control_IsOpenChanged;
    }

    async Task InitAccentColor()
    {
        if (imageSource?.IsFile != true)
        {
            var color = App.Instance.PlayingListService.AlbumAccentColor;
            var textColor = App.Instance.PlayingListService.TextOnAlbumAccentColor;
            (Resources["AccentColorBrush"] as SolidColorBrush).Color = color;
            (Resources["AccentColorBrushDark1"] as SolidColorBrush).Color = color.Darken(.1f);
            (Resources["AccentColorBrushDark2"] as SolidColorBrush).Color = color.Darken(.2f);
            (Resources["TextOnAccentColorBrush"] as SolidColorBrush).Color = textColor;
            (Resources["TextOnAccentColorBrushDark1"] as SolidColorBrush).Color = textColor.Darken(.1f);
            (Resources["TextOnAccentColorBrushDark2"] as SolidColorBrush).Color = textColor.Darken(.2f);
        }
        else
        {
            var color = await CodeHelper.GetThemeColorAsync(imageSource.LocalPath);
            (Resources["AccentColorBrush"] as SolidColorBrush).Color = color.Item1;
            (Resources["AccentColorBrushDark1"] as SolidColorBrush).Color = color.Item1.Darken(.1f);
            (Resources["AccentColorBrushDark2"] as SolidColorBrush).Color = color.Item1.Darken(.2f);
            (Resources["TextOnAccentColorBrush"] as SolidColorBrush).Color = color.Item3;
            (Resources["TextOnAccentColorBrushDark1"] as SolidColorBrush).Color = color.Item3.Darken(.1f);
            (Resources["TextOnAccentColorBrushDark2"] as SolidColorBrush).Color = color.Item3.Darken(.2f);
        }
    }

    void Init()
    {
        InitEvents();
        InitInfo();
        InitVisuals();
        InitImage();
        InitShyHeader();
        InitBindings();
    }

    async Task<IEnumerable<MusicData>> GetSortedSongsAsync(MusicListData musicListData)
    {
        if (musicListData == null || musicListData.Songs == null)
            return Enumerable.Empty<MusicData>();

        var scs = musicListData.PlaySort;
        var songs = musicListData.Songs;

        var sortedSongs = await Task.Run(() =>
        {
            return scs switch
            {
                PlaySort.默认升序 => songs.AsEnumerable(),
                PlaySort.默认降序 => songs.AsEnumerable().Reverse(),
                PlaySort.名称升序 => songs.OrderBy(m => m.Title),
                PlaySort.名称降序 => songs.OrderByDescending(m => m.Title),
                PlaySort.艺术家升序 => songs.OrderBy(m => m.Artists.Count > 0 ? m.Artists[0].Name : "未知"),
                PlaySort.艺术家降序 => songs.OrderByDescending(m => m.Artists.Count > 0 ? m.Artists[0].Name : "未知"),
                PlaySort.专辑升序 => songs.OrderBy(m => m.Album.Title),
                PlaySort.专辑降序 => songs.OrderByDescending(m => m.Album.Title),
                PlaySort.时间升序 => songs.OrderBy(m => m.ReleaseTime ?? DateTime.MinValue),
                PlaySort.时间降序 => songs.OrderByDescending(m => m.ReleaseTime ?? DateTime.MinValue),
                PlaySort.索引升序 => songs.OrderBy(m => m.Index),
                PlaySort.索引降序 => songs.OrderByDescending(m => m.Index),
                _ => songs.AsEnumerable()
            };
        });

        return sortedSongs;
    }

    public ArrayList arrayList { get; set; }
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        DateTime time = DateTime.Now;
        Init();
        ItemsList.ItemsSource = musicListBind;
        ItemList_Header_Search_Control.SongItemBinds = musicListBind;

        LogService.Elapsed("PlayListPage", "Loaded in {0}.", time);
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        RemoveEvents();
        ItemList_Header_Search_Control.IsOpenChanged -= ItemList_Header_Search_Control_IsOpenChanged;
        App.MainWindowInstance.WindowDpiChanged -= MainWindow_WindowDpiChanged;
        DisposeVisuals();
        if (ItemsList_Header_Image != null) ItemsList_Header_Image.Source = null;
        if (ItemsList != null) ItemsList.ItemsSource = null;
        if (SortComboBox != null) SortComboBox.ItemsSource = null;
        musicListBind?.Clear();
        musicListBind = null;
        listSortEnum?.Clear();
        listSortEnum = null;
        musicListData = null;
        if (scrollViewer != null)
            scrollViewer.ViewChanged -= ScrollViewer_ViewChanged;
        Bindings.StopTracking();
        UnloadObject(this);
    }

    private void PlayListReader_Updated()
    {
        InitInfo();
        InitBindings();
        UpdateCommandBarWidth();
    }

    bool isDelayInitShyHeaderWhenScroll = false;
    private async void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (scrollViewer is null) return;
        //scrollViewer.ScrollToVerticalOffset(Math.Round(scrollViewer.VerticalOffset, 0));
        headerVisual.IsPixelSnappingEnabled = true;
        if (scrollViewer.VerticalOffset < 300)
        {
            isDelayInitShyHeaderWhenScroll = true;
            _ = InitShyHeader(true);
            await Task.Delay(200);
            _ = InitShyHeader(true);
        }
        else
        {
            if (isDelayInitShyHeaderWhenScroll)
            {
                isDelayInitShyHeaderWhenScroll = false;
                await Task.Delay(500);
                InitShyHeader(true);
                await Task.Delay(500);
                InitShyHeader(true);
            }
        }
    }

    private void ItemsList_Header_Image_Root_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        InitShyHeader();
    }

    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        InitShyHeader();
    }

    private void MainWindow_WindowDpiChanged(double nowDpi)
    {
        InitShyHeader();
    }

    private async void ItemsList_Header_Foot_Buttons_PositionButtonClick(object sender, RoutedEventArgs e)
    {
        switch ((ScrollFootButton.ButtonType)sender)
        {
            case ScrollFootButton.ButtonType.NowPlaying:
                foreach (var i in musicListBind)
                {
                    if (i.MusicData != App.Instance.AudioService.MusicData) continue;
                    await ItemsList.SmoothScrollIntoViewWithItemAsync(i, ScrollItemPlacement.Center);
                    await ItemsList.SmoothScrollIntoViewWithItemAsync(i, ScrollItemPlacement.Center, disableAnimation: true);
                    MusicDataItem.TryHighlightPlayingItem();
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
    private async void AppBarButton_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as AppBarButton;
        switch (btn.Tag)
        {
            case "playAll":
                if (musicListBind.Count == 0) return;
                if (App.Instance.PlayingListService.PlayBehavior == TewiMP.Services.PlayBehavior.随机播放)
                {
                    App.Instance.PlayingListService.ClearAll();
                }
                foreach (var songItem in musicListBind)
                {
                    App.Instance.PlayingListService.Add(songItem.MusicData, false);
                }
                await App.Instance.PlayingListService.Play(musicListBind.First().MusicData, true);
                App.Instance.PlayingListService.SetRandomPlay(App.Instance.PlayingListService.PlayBehavior);
                break;
            case "refresh":
                InitInfo();
                InitImage();
                InitBindings();
                UpdateCommandBarWidth();
                break;
            case "addLocal":
                AddLocalFilesDo();
                break;
            case "search":
                ItemList_Header_Search_Control.IsOpen = !ItemList_Header_Search_Control.IsOpen;
                break;
            case "edit_list":
                await DialogPages.EditPlayListPage.ShowDialog(musicListData);
                Init();
                break;
            case "move_Cancel":
                moveButton.IsChecked = !moveButton.IsChecked;
                MoveItemDo(false);
                InitBindings();
                break;
        }
    }

    private void AppBarToggleButton_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as AppBarToggleButton;
        switch (btn.Tag)
        {
            case "multiSelect":
                MultiSelectDo((bool)btn.IsChecked);
                break;
            case "move":
                MoveItemDo((bool)btn.IsChecked);
                if (btn.IsChecked == false)
                {
                    MoveItemSave();
                }
                break;
        }
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
            case "multi_downloadSelect":
                DownloadSelectedItemDo();
                break;
            case "multi_addSelectToPlaying":
                AddSelectedItemToPlayingDo();
                break;
        }
    }

    bool isInSave = false;
    private async void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isInSave) return;
        if (isInInitBindings) return;
        if (musicListData is null) return;
        if (SortComboBox is null) return;
        if (SortComboBox.SelectedIndex == -1) return;
        if (SortComboBox.SelectedIndex == (int)musicListData.PlaySort) return;
        isInSave = true;
        musicListData.PlaySort = (PlaySort)SortComboBox.SelectedIndex;
        var data = await PlayListHelper.ReadData();
        data[musicListData.ListName] = JObject.FromObject(musicListData);
        await PlayListHelper.SaveData(data);
        InitBindings();
        isInSave = false;
    }

    private void ItemList_Header_Search_Control_IsOpenChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
        {
            ItemList_Header_Search_Control.FocusToSearchBox();
            ItemsList_Header_Root.Margin = new(0, 0, 0, ItemList_Header_Search_Control.ActualHeight + 7);
        }
        else
        {
            ItemsList_Header_Root.Margin = new(0, 0, 0, 3);
        }
    }

    private void multi_addSelectToPlayList_flyout_Opening(object sender, object e)
    {
        MenuFlyout flyout = sender as MenuFlyout;
        foreach (var list in App.Instance.PlayListReader.NowMusicListData)
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
        App.MainWindowInstance.ShowLoadingDialog();
        var text = await PlayListHelper.ReadData();
        var list = flyoutItem.Tag as MusicListData;
        var listName = list.ListName;
        foreach (MusicDataViewModel item in ItemsList.SelectedItems.Cast<MusicDataViewModel>())
        {
            App.MainWindowInstance.SetLoadingText($"正在添加：{item.MusicData.Title} - {item.MusicData.ButtonName}");
            App.MainWindowInstance.SetLoadingProgressRingValue(ItemsList.SelectedItems.Count, ItemsList.SelectedItems.IndexOf(item));

            await Task.Run(() =>
            {
                PlayListHelper.AddMusicDataToPlayList(item.MusicData, list);
            });
        }
        text[listName] = JObject.FromObject(list);
        await PlayListHelper.SaveData(text);
        await App.Instance.PlayListReader.Refresh();
        App.MainWindowInstance.HideDialog();
    }

    private void multi_addSelectToPlayList_flyout_Closed(object sender, object e)
    {
        foreach (MenuFlyoutItem item in (sender as MenuFlyout).Items)
        {
            item.Click -= Item_Click;
        }
        (sender as MenuFlyout).Items.Clear();
    }

    MusicDataViewModel searchPointMusicDataViewModel = null;
    private async void ItemList_Header_Search_Control_SearchingAItem(MusicDataViewModel songItemBind)
    {
        searchPointMusicDataViewModel = songItemBind;
        var scrollPlacement = ScrollItemPlacement.Top;
        int additionalVerticalOffset = -214;
        bool tryHighlight = MusicDataItem.TryHighlight(songItemBind);
        await ItemsList.SmoothScrollIntoViewWithItemAsync(songItemBind, scrollPlacement, additionalVerticalOffset: additionalVerticalOffset);
        while (!tryHighlight)
        {
            if (!IsLoaded) break;
            if (searchPointMusicDataViewModel != songItemBind) break;
            await ItemsList.SmoothScrollIntoViewWithItemAsync(songItemBind, scrollPlacement, true, additionalVerticalOffset: additionalVerticalOffset);
            await ItemsList.SmoothScrollIntoViewWithItemAsync(songItemBind, scrollPlacement, true, additionalVerticalOffset: additionalVerticalOffset);
            tryHighlight = MusicDataItem.TryHighlight(songItemBind);
            await Task.Delay(80);
        }
        searchPointMusicDataViewModel = null;
    }

    private void MainWindow_InKeyDownEvent(VirtualKey key)
    {
        if (App.MainWindowInstance.isControlDown)
        {
            if (key == VirtualKey.F)
            {
                ItemList_Header_Search_Control.IsOpen = !ItemList_Header_Search_Control.IsOpen;
                if (!ItemList_Header_Search_Control.IsOpen)
                    ItemsList_Header_Info_CommandBar.Focus(FocusState.Programmatic);
            }
        }
    }

    private void MainWindowInstance_MusicPageViewStateChanged(MusicPages.MusicPageViewState musicPageViewState)
    {
    }

    private async void Page_ActualThemeChanged(FrameworkElement sender, object args)
    {
        await InitAccentColor();
    }
}
