using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Composition;
using TewiMP.Helpers;
using TewiMP.Plugin;
using TewiMP.Windowed;
using TewiMP.Services.Storage;
using TewiMP.Services;

namespace TewiMP.Pages
{
    public partial class SettingPage : Page
    {
        public SettingPage()
        {
            InitializeComponent();
            DataContext = this;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var a = (string)e.Parameter;
            if (!string.IsNullOrEmpty(a))
            {
                DelaySetParameter(a);
            }
        }

        private async Task DelaySetParameter(string value)
        {
            CommunityToolkit.WinUI.Controls.SettingsExpander expander = null;
            switch (value)
            {
                case "open download":
                    expander = DownloadEpd;
                    break;
            }
            expander.IsExpanded = true;
            ListViewBase.ScrollIntoView(expander);
        }

        public async void ToAudioCachePlaceSize()
        {
            AudioCachePlaceSizeBusy = true;
            AudioCachePlaceSize = "计算中...";
            AudioCachePlaceSize = "当前占用：" + CodeHelper.GetAutoSizeString(await CodeHelper.GetDirctoryLength(DataFolderBase.AudioCacheFolder), 2);
            AudioCachePlaceSizeBusy = false;
        }
        
        public async void ToImageCachePlaceSize()
        {
            ImageCachePlaceSizeBusy = true;
            ImageCachePlaceSize = "计算中...";
            ImageCachePlaceSize = "当前占用：" + CodeHelper.GetAutoSizeString(await CodeHelper.GetDirctoryLength(DataFolderBase.ImageCacheFolder), 2);
            ImageCachePlaceSizeBusy = false;
        }
        
        public async void ToLyricCachePlaceSize()
        {
            LyricCachePlaceSizeBusy = true;
            LyricCachePlaceSize = "计算中...";
            LyricCachePlaceSize = "当前占用：" + CodeHelper.GetAutoSizeString(await CodeHelper.GetDirctoryLength(DataFolderBase.LyricCacheFolder), 2);
            LyricCachePlaceSizeBusy = false;
        }

        public string UserDataPath { get; set; } = null;
        public string CachePath { get; set; } = null;
        public string AudioCachePath { get; set; } = null;
        public string ImageCachePath { get; set; } = null;
        public string LyricCachePath { get; set; } = null;
        public string DownloadPath { get; set; } = null;
        public string AudioCachePlaceSize { get; set; } = null;
        public bool AudioCachePlaceSizeBusy { get; set; } = false;
        public string ImageCachePlaceSize { get; set; } = null;
        public bool ImageCachePlaceSizeBusy { get; set; } = false;
        public string LyricCachePlaceSize { get; set; } = null;
        public bool LyricCachePlaceSizeBusy { get; set; } = false;

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ToAudioCachePlaceSize();
            ToImageCachePlaceSize();
            ToLyricCachePlaceSize();
            UserDataPath = DataFolderBase.UserDataFolder;
            CachePath = DataFolderBase.CacheFolder;
            AudioCachePath = DataFolderBase.AudioCacheFolder;
            ImageCachePath = DataFolderBase.ImageCacheFolder;
            LyricCachePath = DataFolderBase.LyricCacheFolder;
            DownloadPath = DataFolderBase.DownloadFolder;

            AudioDownloadPathCard.Description = DownloadPath;

            //System.Diagnostics.LogManager.Log(App.Instance.downloadManager.br);
            /*
            switch (App.Instance.downloadManager.br)
            {
                case 128: DownloadFormatCb.SelectedIndex = 0; break;
                case 192: DownloadFormatCb.SelectedIndex = 1; break;
                case 320: DownloadFormatCb.SelectedIndex = 2; break;
                case 960: DownloadFormatCb.SelectedIndex = 3; break;
            }
            DownloadMaximumNb.Value = App.Instance.downloadManager.DownloadingMaxium;
            */
        }

        Visual headerVisual;
        private ExpressionAnimation _headerOffsetAnim;
        private ExpressionAnimation _logoScaleAnim;
        private ExpressionAnimation _logoOffsetAnim;
        private ExpressionAnimation _bgOpacityAnim;

        private ScrollViewer _cachedScrollViewer;
        private CompositionPropertySet _scrollerPropSet;
        private Compositor _compositor;

        // 预定义常量表达式
        private const string ProgressExp = "Clamp(-scroller.Translation.Y / Padding, 0, 1.0)";

        // 安全获取 ScrollViewer
        private ScrollViewer GetScrollViewer(DependencyObject root)
        {
            // 如果已经缓存且有效，直接返回
            if (_cachedScrollViewer != null) return _cachedScrollViewer;

            // 尝试查找
            if (VisualTreeHelper.GetChildrenCount(root) > 0)
            {
                var child = VisualTreeHelper.GetChild(root, 0) as Border;
                if (child?.Child is ScrollViewer sv)
                {
                    _cachedScrollViewer = sv;
                    _cachedScrollViewer.CanContentRenderOutsideBounds = true;
                    return sv;
                }
            }
            return null;
        }

        public void UpdateShyHeader()
        {
            // 1. 获取 ScrollViewer
            var scrollViewer = GetScrollViewer(ListViewBase);
            if (scrollViewer is null) return;

            // 2. 处理 ZIndex (仅当需要时处理)
            // 注意：修改 ListView 内部容器的 ZIndex 是为了让 Header 浮在 Item 上面
            if (ListViewBase.Header != null)
            {
                var headerPresenter = VisualTreeHelper.GetParent((UIElement)ListViewBase.Header) as UIElement;
                if (headerPresenter != null)
                {
                    var headerContainer = VisualTreeHelper.GetParent(headerPresenter) as UIElement;
                    // 只有当 ZIndex 不对时才设置，避免重复调用
                    if (headerContainer != null && Canvas.GetZIndex(headerContainer) != 1)
                    {
                        Canvas.SetZIndex(headerContainer, 1);
                    }
                }
            }

            // 3. 初始化 Compositor 和 PropertySet
            if (_scrollerPropSet is null)
            {
                _scrollerPropSet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
                _compositor = _scrollerPropSet.Compositor;
            }

            // 4. 准备参数
            float paddingSize = 40f;

            // 获取 Visuals
            var headerVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseGrid);
            var logoVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseTextBlock);
            var backgroundVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseRectangle);

            // 动画 1: Header Offset Y (Sticky Effect)
            if (_headerOffsetAnim is null)
            {
                // 逻辑: -scroller.Y - (Progress * Padding)
                string exp = $"-scroller.Translation.Y - ({ProgressExp} * Padding)";
                _headerOffsetAnim = _compositor.CreateExpressionAnimation(exp);
                _headerOffsetAnim.SetReferenceParameter("scroller", _scrollerPropSet);
            }
            _headerOffsetAnim.SetScalarParameter("Padding", paddingSize);
            headerVisual.StartAnimation("Offset.Y", _headerOffsetAnim);

            // 动画 2: Logo Scale
            if (_logoScaleAnim is null)
            {
                string exp = $"Lerp(Vector2(1,1), Vector2(0.7, 0.7), {ProgressExp})";
                _logoScaleAnim = _compositor.CreateExpressionAnimation(exp);
                _logoScaleAnim.SetReferenceParameter("scroller", _scrollerPropSet);
            }
            _logoScaleAnim.SetScalarParameter("Padding", paddingSize);
            logoVisual.StartAnimation("Scale.xy", _logoScaleAnim);

            // 动画 3: Logo Offset (合并 X 和 Y)
            // X: 0 -> -12, Y: 0 -> 24
            if (_logoOffsetAnim is null)
            {
                string exp = $"Lerp(Vector3(0,0,0), Vector3(-12, 24, 0), {ProgressExp})";
                _logoOffsetAnim = _compositor.CreateExpressionAnimation(exp);
                _logoOffsetAnim.SetReferenceParameter("scroller", _scrollerPropSet);
            }
            _logoOffsetAnim.SetScalarParameter("Padding", paddingSize);
            logoVisual.StartAnimation(nameof(logoVisual.Offset), _logoOffsetAnim);

            // 动画 4: Background Opacity
            if (_bgOpacityAnim is null)
            {
                string exp = $"Lerp(0, 1, {ProgressExp})";
                _bgOpacityAnim = _compositor.CreateExpressionAnimation(exp);
                _bgOpacityAnim.SetReferenceParameter("scroller", _scrollerPropSet);
            }
            _bgOpacityAnim.SetScalarParameter("Padding", paddingSize);
            backgroundVisual.StartAnimation("Opacity", _bgOpacityAnim);
        }

        private void ScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            headerVisual.IsPixelSnappingEnabled = true;
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateShyHeader();
        }

        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).Content as string == "打开文件")
            {
                var res = await FileHelper.UserSelectFile(Windows.Storage.Pickers.PickerViewMode.List, Windows.Storage.Pickers.PickerLocationId.VideosLibrary, new[] { ".mp4", "*" });
                if (res != null)
                {
                    new MediaPlayerWindow(res.Path);
                }
            }
            else
            {
                var tbox = new TextBox() { PlaceholderText = "请输入媒体文件地址" };
                var res = await App.MainWindowInstance.ShowDialog("输入地址", tbox, "取消", "确定");
                if (res == ContentDialogResult.Primary)
                {
                    new MediaPlayerWindow(tbox.Text);
                }
            }
        }

        private void Page_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
        }

        #region cacheExp
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button is null) return;

            string tagObj = button.Tag as string;
            string folderPath = null;
            switch (tagObj)
            {
                case "0":
                    folderPath = DataFolderBase.CacheFolder;
                    break;
                case "1":
                    folderPath = DataFolderBase.AudioCacheFolder;
                    break;
                case "2":
                    folderPath = DataFolderBase.ImageCacheFolder;
                    break;
                case "3":
                    folderPath = DataFolderBase.LyricCacheFolder;
                    break;
                case "4":
                    folderPath = DataFolderBase.DownloadFolder;
                    break;
                case "5":
                    folderPath = DataFolderBase.UserDataFolder;
                    break;
            }

            await FileHelper.ExploreFolder(folderPath);
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button is null) return;

            string tagObj = button.Tag as string;
            var folder = await FileHelper.UserSelectFolder();
            if (folder is null) return;
            var folderPath = folder.Path;
            switch (tagObj)
            {
                case "0":
                    DataFolderBase.CacheFolder = folderPath;
                    break;
                case "1":
                    DataFolderBase.AudioCacheFolder = folderPath;
                    break;
                case "2":
                    DataFolderBase.ImageCacheFolder = folderPath;
                    break;
                case "3":
                    DataFolderBase.LyricCacheFolder = folderPath;
                    break;
                case "4":
                    DataFolderBase.DownloadFolder = folderPath;
                    break;
            }
            (VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(button)))) as CommunityToolkit.WinUI.Controls.SettingsCard).Description = folderPath;
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string defaultSetting = null;
                switch ((string)button.Tag)
                {
                    case "0":
                        DataFolderBase.CacheFolder = null; // 自定义属性已检查路径是否为空，如果是则设置为默认值
                        defaultSetting = DataFolderBase.CacheFolder;
                        break;
                    case "1":
                        defaultSetting = SettingEditHelper.GetSetting<string>(DataFolderBase.SettingDefault, DataFolderBase.SettingParams.DownloadFolderPath);
                        DataFolderBase.DownloadFolder = defaultSetting;
                        break;
                }
                (VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(button)))) as CommunityToolkit.WinUI.Controls.SettingsCard).Description = defaultSetting;
            }
        }

        private async void Button_Click_4(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                var result = await App.MainWindowInstance.ShowDialog(
                    "删除缓存",
                    "此操作会将缓存路径中的文件全部删除，\n如果缓存路径中存在其它文件数据，也会一并删除。\n是否确定删除？",
                    "取消", "确定删除", null, ContentDialogButton.Primary);
                if (result != ContentDialogResult.Primary) return;

                string tagObj = button.Tag as string;
                string folderPath = null;
                switch (tagObj)
                {
                    case "0":
                        folderPath = AudioCachePath;
                        break;
                    case "1":
                        folderPath = ImageCachePath;
                        break;
                    case "2":
                        folderPath = LyricCachePath;
                        break;
                }
                await Task.Run(() =>
                {
                    var files = Directory.EnumerateFiles(folderPath);
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {

                        }
                    }
                });

                (VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(button))) as CommunityToolkit.WinUI.Controls.SettingsCard).Description = "当前占用：" + CodeHelper.GetAutoSizeString(await CodeHelper.GetDirctoryLength(folderPath), 2);
            }
        }
        #endregion

        #region downloadExp
        bool combo0loading = false;
        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            combo0loading = true;
            var combo = sender as ComboBox;
            int index = 0;
            switch (App.Instance.DownloadService.DownloadQuality)
            {
                case DataFolderBase.DownloadQuality.lossless: index = 0; break;
                case DataFolderBase.DownloadQuality.lossy_high: index = 1; break;
                case DataFolderBase.DownloadQuality.lossy_mid: index = 2; break;
                case DataFolderBase.DownloadQuality.lossy_low: index = 3; break;
            }
            combo.SelectedIndex = index;
            combo0loading = false;
        }

        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (combo0loading) return;
            var combo = sender as ComboBox;
            switch (combo.SelectedIndex)
            {
                case 0:
                    App.Instance.DownloadService.DownloadQuality = DataFolderBase.DownloadQuality.lossless;
                    break;
                case 1:
                    App.Instance.DownloadService.DownloadQuality = DataFolderBase.DownloadQuality.lossy_high;
                    break;
                case 2:
                    App.Instance.DownloadService.DownloadQuality = DataFolderBase.DownloadQuality.lossy_mid;
                    break;
                case 3:
                    App.Instance.DownloadService.DownloadQuality = DataFolderBase.DownloadQuality.lossy_low;
                    break;
            }
        }

        bool downloadMaximumLoading = false;
        private void DownloadMaximumBaseGrid_Loaded(object sender, RoutedEventArgs e)
        {
            downloadMaximumLoading = true;
            (sender as NumberBox).Value = App.Instance.DownloadService.DownloadingMaximum;
            downloadMaximumLoading = false;
        }

        private void NumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (downloadMaximumLoading) return;
            App.Instance.DownloadService.DownloadingMaximum = (int)sender.Value;
        }

        bool downloadNamedLoading = false;
        private void Download_NamedRadioButtons_Loaded(object sender, RoutedEventArgs e)
        {
            downloadNamedLoading = true;
            (sender as ComboBox).SelectedIndex = (int)App.Instance.DownloadService.DownloadNamedMethod;
            downloadNamedLoading = false;
        }

        private void Download_NamedRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (downloadNamedLoading) return;
            App.Instance.DownloadService.DownloadNamedMethod = (DataFolderBase.DownloadNamedMethod)(sender as ComboBox).SelectedIndex;
        }

        bool downloadOptionsLoading = false;
        private void Download_Options_Loaded(object sender, RoutedEventArgs e)
        {
            downloadOptionsLoading = true;
            var root = sender as StackPanel;
            (root.Children[0] as CheckBox).IsChecked = App.Instance.DownloadService.IDv3WriteImage;
            (root.Children[1] as CheckBox).IsChecked = App.Instance.DownloadService.IDv3WriteArtistImage;
            (root.Children[2] as CheckBox).IsChecked = App.Instance.DownloadService.IDv3WriteLyric;
            (root.Children[3] as CheckBox).IsChecked = App.Instance.DownloadService.SaveLyricToLrcFile;
            downloadOptionsLoading = false;
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (downloadOptionsLoading) return;
            var checkBox = sender as CheckBox;
            switch (checkBox.Tag)
            {
                case "0":
                    App.Instance.DownloadService.IDv3WriteImage = (bool)checkBox.IsChecked;
                    break;
                case "1":
                    App.Instance.DownloadService.IDv3WriteArtistImage = (bool)checkBox.IsChecked;
                    break;
                case "2":
                    App.Instance.DownloadService.IDv3WriteLyric = (bool)checkBox.IsChecked;
                    break;
                case "3":
                    App.Instance.DownloadService.SaveLyricToLrcFile = (bool)checkBox.IsChecked;
                    break;
            }
        }
        #endregion

        #region playExp
        bool combo1Loading = false;
        private void ComboBox_Loaded_1(object sender, RoutedEventArgs e)
        {
            combo1Loading = true;
            (sender as ComboBox).SelectedIndex = (int)App.Instance.PlayingListService.PlayBehavior;
            combo1Loading = false;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo1Loading) return;
            App.Instance.PlayingListService.PlayBehavior = (PlayBehavior)(sender as ComboBox).SelectedIndex;
        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            var sp = sender as StackPanel;
            (sp.Children[0] as CheckBox).IsChecked = App.Instance.PlayingListService.PauseWhenPreviousPause;
            (sp.Children[1] as CheckBox).IsChecked = App.Instance.PlayingListService.NextWhenPlayError;
            (sp.Children[2] as CheckBox).IsChecked = App.Instance.LoadLastExitPlayingSongAndSongList;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            switch (checkBox.Tag)
            {
                case "0":
                    App.Instance.PlayingListService.PauseWhenPreviousPause = (bool)checkBox.IsChecked;
                    break;
                case "1":
                    App.Instance.PlayingListService.NextWhenPlayError = (bool)checkBox.IsChecked;
                    break;
                case "2":
                    App.Instance.LoadLastExitPlayingSongAndSongList = (bool)checkBox.IsChecked;
                    break;
            }
        }
        #endregion

        #region themeExp
        bool themeloading = false;
        private void ComboBox_Loaded_2(object sender, RoutedEventArgs e)
        {
            themeloading = true;
            var themeCombo = sender as ComboBox;
            switch (App.MainWindowInstance.WindowGridBase.RequestedTheme)
            {
                case ElementTheme.Default:
                    themeCombo.SelectedIndex = 0;
                    break;
                case ElementTheme.Light:
                    themeCombo.SelectedIndex = 1;
                    break;
                case ElementTheme.Dark:
                    themeCombo.SelectedIndex = 2;
                    break;
            }
            themeloading = false;
        }

        private void ComboBox_SelectionChanged_4(object sender, SelectionChangedEventArgs e)
        {
            if (themeloading) return;
            var themeCombo = sender as ComboBox;
            switch (themeCombo.SelectedIndex)
            {
                case 0:
                    App.MainWindowInstance.WindowGridBase.RequestedTheme = ElementTheme.Default;
                    break;
                case 1:
                    App.MainWindowInstance.WindowGridBase.RequestedTheme = ElementTheme.Light;
                    break;
                case 2:
                    App.MainWindowInstance.WindowGridBase.RequestedTheme = ElementTheme.Dark;
                    break;
            }
        }

        bool musicpageThemeLoading = false;
        private void ComboBox_Loaded_3(object sender, RoutedEventArgs e)
        {
            musicpageThemeLoading = true;
            (sender as ComboBox).SelectedIndex = (int)App.MainWindowInstance.SMusicPage.pageRoot.RequestedTheme;
            musicpageThemeLoading = false;
        }

        private void ComboBox_SelectionChanged_5(object sender, SelectionChangedEventArgs e)
        {
            if (musicpageThemeLoading) return;
            var combo = sender as ComboBox;
            switch (combo.SelectedIndex)
            {
                case 0:
                    App.MainWindowInstance.SMusicPage.pageRoot.RequestedTheme = ElementTheme.Default;
                    break;
                case 1:
                    App.MainWindowInstance.SMusicPage.pageRoot.RequestedTheme = ElementTheme.Light;
                    break;
                case 2:
                    App.MainWindowInstance.SMusicPage.pageRoot.RequestedTheme = ElementTheme.Dark;
                    break;
            }
        }

        bool accentColorLoading = false;
        private void ComboBox_Loaded_4(object sender, RoutedEventArgs e)
        {
            accentColorLoading = true;
            (sender as ComboBox).SelectedIndex = 0;
            accentColorLoading = false;
        }

        private void ComboBox_SelectionChanged_6(object sender, SelectionChangedEventArgs e)
        {
            switch ((sender as ComboBox).SelectedIndex)
            {
                case 0:
                    accentcolor_applysettings_button.Visibility = Visibility.Collapsed;
                    accentcolor_colorpicker.Visibility = Visibility.Collapsed;
                    break;
                case 1:
                    accentcolor_applysettings_button.Visibility = Visibility.Visible;
                    accentcolor_colorpicker.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void ColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            accentcolor_accentcolor_presenter_root.Background = new SolidColorBrush(sender.Color);
        }

        private void accentcolor_applysettings_button_Click(object sender, RoutedEventArgs e)
        {
            //App.Instance.AccentColor = accentcolor_colorpicker.Color;
        }

        bool backgroundTypeLoading = false;
        private void ComboBox_Loaded_5(object sender, RoutedEventArgs e)
        {
            backgroundTypeLoading = true;
            (sender as ComboBox).SelectedIndex = (int)App.MainWindowInstance.CurrentBackdrop;
            backgroundTypeLoading = false;
        }

        private void ComboBox_SelectionChanged_7(object sender, SelectionChangedEventArgs e)
        {
            int index = (sender as ComboBox).SelectedIndex;
            if (index == 3 || index == 4 || index == 5)
            {
                imageselect_root.Visibility = Visibility.Visible;
                if (index == 5) background_selectimage_button.Visibility = Visibility.Visible;
                else background_selectimage_button.Visibility = Visibility.Collapsed;
            }
            else
            {
                imageselect_root.Visibility = Visibility.Collapsed;
            }
            if (backgroundTypeLoading) return;
            switch (index)
            {
                case 0:
                    App.MainWindowInstance.SetBackdrop(BackdropType.Mica);
                    break;
                case 1:
                    App.MainWindowInstance.SetBackdrop(BackdropType.MicaAlt);
                    break;
                case 2:
                    App.MainWindowInstance.SetBackdrop(BackdropType.DesktopAcrylic);
                    break;
                case 3:
                    App.MainWindowInstance.SetBackdrop(BackdropType.Blur);
                    break;
                case 4:
                    App.MainWindowInstance.SetBackdrop(BackdropType.Transparent);
                    break;
                case 5:
                    App.MainWindowInstance.SetBackdrop(BackdropType.Image);
                    break;
                case 6:
                    App.MainWindowInstance.SetBackdrop(BackdropType.DefaultColor);
                    break;
            }
        }

        bool imageSelectLoading = false;
        private void imageselect_root_Loaded(object sender, RoutedEventArgs e)
        {
            imageSelectLoading = true;
            StackPanel stackPanel = sender as StackPanel;
            (stackPanel.Children[1] as Slider).Value = App.MainWindowInstance.BackgroundMass.Opacity * 100;
            imageSelectLoading = false;
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var path = await FileHelper.UserSelectFile(Windows.Storage.Pickers.PickerViewMode.Thumbnail, Windows.Storage.Pickers.PickerLocationId.PicturesLibrary);
            if (path is null) return;
            App.MainWindowInstance.ImagePath = path.Path;
            App.MainWindowInstance.SetBackdrop(BackdropType.Image);
        }

        private void Slider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (imageSelectLoading) return;
            App.MainWindowInstance.BackgroundMass.Opacity = (sender as Slider).Value / 100;
        }
        #endregion

        #region desktopExp
        private void StackPanel_Loaded_1(object sender, RoutedEventArgs e)
        {
            var stackPanel = sender as StackPanel;
            (stackPanel.Children[0] as ComboBox).SelectedIndex = (int)DesktopLyricWindow.LyricTextBehavior;
            (stackPanel.Children[1] as ComboBox).SelectedIndex = (int)DesktopLyricWindow.LyricTextPosition;
        }

        private void StackPanel_Loaded_2(object sender, RoutedEventArgs e)
        {
            var stackPanel = sender as StackPanel;
            (stackPanel.Children[0] as ComboBox).SelectedIndex = (int)DesktopLyricWindow.LyricTranslateTextBehavior;
            (stackPanel.Children[1] as ComboBox).SelectedIndex = (int)DesktopLyricWindow.LyricTranslateTextPosition;
        }

        private void StackPanel_Loaded_3(object sender, RoutedEventArgs e)
        {
            var stackPanel = sender as StackPanel;
            (stackPanel.Children[0] as CheckBox).IsChecked = DesktopLyricWindow.PauseButtonVisible;
            (stackPanel.Children[1] as CheckBox).IsChecked = DesktopLyricWindow.ProgressUIVisible;
            (stackPanel.Children[2] as CheckBox).IsChecked = DesktopLyricWindow.ProgressUIPercentageVisible;
            (stackPanel.Children[3] as CheckBox).IsChecked = DesktopLyricWindow.MusicChangeUIVisible;
        }

        private void ComboBox_SelectionChanged_8(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            switch (comboBox.Tag as string)
            {
                case "0":
                    DesktopLyricWindow.LyricTextBehavior = (LyricTextBehavior)comboBox.SelectedIndex;
                    break;
                case "1":
                    DesktopLyricWindow.LyricTextPosition = (LyricTextPosition)comboBox.SelectedIndex;
                    break;
                case "2":
                    DesktopLyricWindow.LyricTranslateTextBehavior = (LyricTranslateTextBehavior)comboBox.SelectedIndex;
                    break;
                case "3":
                    DesktopLyricWindow.LyricTranslateTextPosition = (LyricTranslateTextPosition)comboBox.SelectedIndex;
                    break;
            }
        }
        private void CheckBox_Click_1(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            switch (checkBox.Tag as string)
            {
                case "0":
                    DesktopLyricWindow.PauseButtonVisible = (bool)checkBox.IsChecked;
                    break;
                case "1":
                    DesktopLyricWindow.ProgressUIVisible = (bool)checkBox.IsChecked;
                    break;
                case "2":
                    DesktopLyricWindow.ProgressUIPercentageVisible = (bool)checkBox.IsChecked;
                    break;
                case "3":
                    DesktopLyricWindow.MusicChangeUIVisible = (bool)checkBox.IsChecked;
                    break;
            }
        }
        #endregion

        private void CheckBox_Click_2(object sender, RoutedEventArgs e)
        {

        }

        private void CheckBox_Click_3(object sender, RoutedEventArgs e)
        {

        }

        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            //ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            //timeup_event_base.Visibility = toggleSwitch.IsOn ? Visibility.Visible : Visibility.Collapsed;
            //timeup_event_description.Visibility = toggleSwitch.IsOn ? Visibility.Collapsed : Visibility.Visible;
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void ComboBox_SelectionChanged_2(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ComboBox_SelectionChanged_3(object sender, SelectionChangedEventArgs e)
        {

        }

        private async void Button_Click_7(object sender, RoutedEventArgs e)
        {
            var result = await App.MainWindowInstance.ShowDialog(
                "恢复默认设置",
                "确定恢复默认设置吗？此操作会使你的设置数据全部恢复到程序初始设置，但不会影响歌单数据、历史记录等数据。",
                "取消", "恢复",
                defaultButton: ContentDialogButton.Primary);
            if (result != ContentDialogResult.Primary) return;
            DataFolderBase.JSettingData = DataFolderBase.SettingDefault;
            App.Instance.LoadSettings();
            App.MainWindowInstance.AddNotify("恢复成功", "已将设置恢复到默认。", NotifySeverity.Complete);
            App.MainWindowInstance.SetNavViewContent(typeof(SearchPage));
        }

        private void NumberBox_ValueChanged_1(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            ScrollViewer a = (ScrollViewer)VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(App.MainWindowInstance.WindowGridBase)))));
            a.RasterizationScale = sender.Value;
            App.MainWindowInstance.AsyncDialog.RasterizationScale = sender.Value;
        }

        private void ToggleSwitch_Toggled_1(object sender, RoutedEventArgs e)
        {
            var ts = sender as ToggleSwitch;
            if (ts.Tag as string == "0")
            {
                ScrollViewer b = (ScrollViewer)VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(App.MainWindowInstance.WindowGridBase)))));
                if (ts.IsOn)
                {
                    b.ZoomMode = ZoomMode.Enabled;
                    b.HorizontalScrollMode = ScrollMode.Enabled;
                    b.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                    b.VerticalScrollMode = ScrollMode.Enabled;
                    b.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                    b.ZoomToFactor(1);
                }
                else
                {
                    b.ZoomMode = ZoomMode.Disabled;
                    b.HorizontalScrollMode = ScrollMode.Disabled;
                    b.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    b.VerticalScrollMode = ScrollMode.Disabled;
                    b.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    b.ZoomToFactor(1);
                }
            }
            else
            {
                App.Instance.SetFramePerSecondViewer(ts.IsOn);
            }
        }

        private void ToggleSwitch_Loaded(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            switch (toggleSwitch.Tag as string)
            {
                case "0":
                    toggleSwitch.IsOn = App.MainWindowInstance.NavView.PaneDisplayMode == NavigationViewPaneDisplayMode.Top;
                    break;
                case "1":
                    toggleSwitch.IsOn = NotifyIconWindow.IsVisible;
                    break;
                case "2":
                    toggleSwitch.IsOn = App.MainWindowInstance.RunInBackground;
                    break;
                case "3":
                    toggleSwitch.IsOn = File.Exists(DataFolderBase.StartupShortcutPath);
                    break;
                case "4":
                    toggleSwitch.IsOn = LyricService.UseRomajiLyric;
                    break;
            }
        }

        private void ToggleSwitch_Toggled_2(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            switch (toggleSwitch.Tag as string)
            {
                case "0":
                    App.MainWindowInstance.NavView.PaneDisplayMode = toggleSwitch.IsOn ? NavigationViewPaneDisplayMode.Top : NavigationViewPaneDisplayMode.Auto;
                    break;
                case "1":
                    NotifyIconWindow.IsVisible = toggleSwitch.IsOn;
                    break;
                case "2":
                    App.MainWindowInstance.RunInBackground = toggleSwitch.IsOn;
                    break;
                case "3":
                    App.Instance.SetStartupWithWindows(toggleSwitch.IsOn);
                    break;
                case "4":
                    LyricService.UseRomajiLyric = toggleSwitch.IsOn;
                    break;
            }
        }

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            App.Instance.ExitApp();
        }

        private void ToggleSwitch_Loaded_1(object sender, RoutedEventArgs e)
        {
            var toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch.Tag as string == "0")
            {
                toggleSwitch.IsOn = App.MainWindowInstance.NavView.PaneDisplayMode == NavigationViewPaneDisplayMode.Top;
            }
            else
            {
                toggleSwitch.IsOn = Controls.ImageEx.ImageDarkMass;
            }
        }

        private void ToggleSwitch_Toggled_3(object sender, RoutedEventArgs e)
        {
            var toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch is null) return;
            if (toggleSwitch.Tag as string == "0")
            {
                App.MainWindowInstance.NavView.PaneDisplayMode = toggleSwitch.IsOn ? NavigationViewPaneDisplayMode.Top : NavigationViewPaneDisplayMode.Auto;
            }
            else 
            {
                Controls.ImageEx.ImageDarkMass = toggleSwitch.IsOn;
            }
        }

        private void ToggleSwitch_Loaded_2(object sender, RoutedEventArgs e)
        {
        }

        private void ToggleSwitch_Toggled_4(object sender, RoutedEventArgs e)
        {
        }

        private void Button_Click_9(object sender, RoutedEventArgs e)
        {
            App.Instance.SaveSettings();
            App.MainWindowInstance.AddNotify("保存设置成功", "已将设置数据写入设置文件中。", NotifySeverity.Complete);
        }

        private void Button_Click_10(object sender, RoutedEventArgs e)
        {
            App.Instance.LoadSettings();
            App.MainWindowInstance.AddNotify("读取设置成功", "已从设置文件中读取设置。", NotifySeverity.Complete);
            App.MainWindowInstance.SetNavViewContent(typeof(SearchPage));
        }

        private void StackPanel_Loaded_4(object sender, RoutedEventArgs e)
        {
            desktoplyric_opacity_slider.Value = DesktopLyricWindow.LyricOpacity * 100;
        }

        private void desktoplyric_opacity_slider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            DesktopLyricWindow.LyricOpacity = e.NewValue / 100;
            if (App.MainWindowInstance.DesktopLyricWindow != null) App.MainWindowInstance.DesktopLyricWindow.SetLyricOpacity(DesktopLyricWindow.LyricOpacity);
        }

        private void HotKeySettings_Click(object sender, RoutedEventArgs e)
        {
            App.MainWindowInstance.SetNavViewContent(typeof(SettingHotKeyPage));
        }

        private void PluginSettings_Click(object sender, RoutedEventArgs e)
        {
            App.MainWindowInstance.SetNavViewContent(typeof(SettingPlugin));
        }

        private void SettingsCard_Click(object sender, RoutedEventArgs e)
        {
            //await App.MainWindowInstance.ShowEqualizerDialog();
            App.MainWindowInstance.SetNavViewContent(typeof(SettingEqPage));
        }

        private void TimeEventCard_Loaded(object sender, RoutedEventArgs e)
        {
            if (TimeEventPage.TimingTimer is null) return;
            TimeEventPage.TimingTimer.Tick -= TimingTimer_Tick;
            TimeEventPage.TimingTimer.Tick += TimingTimer_Tick;
            TimingTimer_Tick(null, null);
        }

        private void TimeEventCard_UnLoaded(object sender, RoutedEventArgs e)
        {
            if (TimeEventPage.TimingTimer is null) return;
            TimeEventPage.TimingTimer.Tick -= TimingTimer_Tick;
        }

        private void TimingTimer_Tick(object sender, object e)
        {
            TimeEventCard.Description =
                TimeEventPage.LeftTime < TimeSpan.Zero ? 
                "启动定时并设置定时任务" : 
                $"定时剩余时间：{TimeEventPage.LeftTime}";
        }


        public static DialogPages.TimeEventPage TimeEventPage = new DialogPages.TimeEventPage();
        private async void SettingsCard_Click_1(object sender, RoutedEventArgs e)
        {
            await App.MainWindowInstance.ShowDialog("播放定时", TimeEventPage, "返回");

            if (TimeEventPage.TimingTimer is null)
            {
                TimeEventCard.Description = "启动定时并设置定时任务";
                return;
            }
            TimeEventPage.TimingTimer.Tick -= TimingTimer_Tick;
            TimeEventPage.TimingTimer.Tick += TimingTimer_Tick;
            TimingTimer_Tick(null, null);
        }

        private void SettingsCard_Click_2(object sender, RoutedEventArgs e)
        {
            LogWindow.ShowWindow();
        }

        private void SettingsCard_Click_3(object sender, RoutedEventArgs e)
        {
            /*var window = new BackgroundTransparentTestWindow();
            window.Activate();*/
            var mw = new MainWindow();
            mw.Activate();
        }
    }
}
