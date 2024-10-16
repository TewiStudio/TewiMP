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
using TewiMP.DataEditor;
using TewiMP.Windowed;
using TewiMP.Background;

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

        private void DelaySetParameter(string value)
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
            CachePath = DataFolderBase.CacheFolder;
            AudioCachePath = DataFolderBase.AudioCacheFolder;
            ImageCachePath = DataFolderBase.ImageCacheFolder;
            LyricCachePath = DataFolderBase.LyricCacheFolder;
            DownloadPath = DataFolderBase.DownloadFolder;

            //System.Diagnostics.Debug.WriteLine(App.downloadManager.br);
            /*
            switch (App.downloadManager.br)
            {
                case 128: DownloadFormatCb.SelectedIndex = 0; break;
                case 192: DownloadFormatCb.SelectedIndex = 1; break;
                case 320: DownloadFormatCb.SelectedIndex = 2; break;
                case 960: DownloadFormatCb.SelectedIndex = 3; break;
            }
            DownloadMaximumNb.Value = App.downloadManager.DownloadingMaxium;
            */
        }

        Visual headerVisual;
        ScrollViewer scrollViewer;
        public void UpdateShyHeader()
        {
            // 设置header为顶层
            var headerPresenter = (UIElement)VisualTreeHelper.GetParent((UIElement)ListViewBase.Header);
            if (headerPresenter is null) return;
            var headerContainer = (UIElement)VisualTreeHelper.GetParent(headerPresenter);
            Canvas.SetZIndex(headerContainer, 1);

            if (scrollViewer is null)
            {
                scrollViewer = (VisualTreeHelper.GetChild(ListViewBase, 0) as Border).Child as ScrollViewer;
                scrollViewer.CanContentRenderOutsideBounds = true;
                scrollViewer.ViewChanging += ScrollViewer_ViewChanging;
            }

            CompositionPropertySet scrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
            Compositor compositor = scrollerPropertySet.Compositor;

            var padingSize = 40;
            // Get the visual that represents our HeaderTextBlock 
            // And define the progress animation string
            headerVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseGrid);
            String progress = $"Clamp(-scroller.Translation.Y / {padingSize}, 0, 1.0)";

            // Shift the header by 50 pixels when scrolling down
            var offsetExpression = compositor.CreateExpressionAnimation($"-scroller.Translation.Y - {progress} * {padingSize}");
            offsetExpression.SetReferenceParameter("scroller", scrollerPropertySet);
            headerVisual.StartAnimation("Offset.Y", offsetExpression);

            /*
            Visual textVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseTextBlock);
            Vector3 finalOffset = new Vector3(0, 10, 0);
            var headerOffsetAnimation = compositor.CreateExpressionAnimation($"Lerp(Vector3(0,0,0), finalOffset, {progress})");
            headerOffsetAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            headerOffsetAnimation.SetVector3Parameter("finalOffset", finalOffset);
            textVisual.StartAnimation(nameof(Visual.Offset), headerOffsetAnimation);
            */

            // Logo scale and transform                                          from               to
            var logoHeaderScaleAnimation = compositor.CreateExpressionAnimation("Lerp(Vector2(1,1), Vector2(0.7, 0.7), " + progress + ")");
            logoHeaderScaleAnimation.SetReferenceParameter("scroller", scrollerPropertySet);

            var logoVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseTextBlock);
            logoVisual.StartAnimation("Scale.xy", logoHeaderScaleAnimation);

            var logoVisualOffsetYAnimation = compositor.CreateExpressionAnimation($"Lerp(0, 24, {progress})");
            logoVisualOffsetYAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            logoVisual.StartAnimation("Offset.Y", logoVisualOffsetYAnimation);

            var logoVisualOffsetXAnimation = compositor.CreateExpressionAnimation($"Lerp(0, -12, {progress})");
            logoVisualOffsetXAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            logoVisual.StartAnimation("Offset.X", logoVisualOffsetXAnimation);
            
            var backgroundVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseRectangle);
            var backgroundVisualOpacityAnimation = compositor.CreateExpressionAnimation($"Lerp(0, 1, {progress})");
            backgroundVisualOpacityAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            backgroundVisual.StartAnimation("Opacity", backgroundVisualOpacityAnimation);
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
                var res = await MainWindow.ShowDialog("输入地址", tbox, "取消", "确定");
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
            }

            await FileHelper.OpenFilePath(folderPath);
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

        private async void Button_Click_4(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                var result = await MainWindow.ShowDialog(
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
                        File.Delete(file);
                    }
                });

                (VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(button))) as CommunityToolkit.WinUI.Controls.SettingsCard).Description = "当前占用：0B";
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
            switch (App.downloadManager.DownloadQuality)
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
                    App.downloadManager.DownloadQuality = DataFolderBase.DownloadQuality.lossless;
                    break;
                case 1:
                    App.downloadManager.DownloadQuality = DataFolderBase.DownloadQuality.lossy_high;
                    break;
                case 2:
                    App.downloadManager.DownloadQuality = DataFolderBase.DownloadQuality.lossy_mid;
                    break;
                case 3:
                    App.downloadManager.DownloadQuality = DataFolderBase.DownloadQuality.lossy_low;
                    break;
            }
        }

        bool downloadMaximumLoading = false;
        private void DownloadMaximumBaseGrid_Loaded(object sender, RoutedEventArgs e)
        {
            downloadMaximumLoading = true;
            (sender as NumberBox).Value = App.downloadManager.DownloadingMaximum;
            downloadMaximumLoading = false;
        }

        private void NumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (downloadMaximumLoading) return;
            App.downloadManager.DownloadingMaximum = (int)sender.Value;
        }

        bool downloadNamedLoading = false;
        private void Download_NamedRadioButtons_Loaded(object sender, RoutedEventArgs e)
        {
            downloadNamedLoading = true;
            (sender as ComboBox).SelectedIndex = (int)App.downloadManager.DownloadNamedMethod;
            downloadNamedLoading = false;
        }

        private void Download_NamedRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (downloadNamedLoading) return;
            App.downloadManager.DownloadNamedMethod = (DataFolderBase.DownloadNamedMethod)(sender as ComboBox).SelectedIndex;
        }

        bool downloadOptionsLoading = false;
        private void Download_Options_Loaded(object sender, RoutedEventArgs e)
        {
            downloadOptionsLoading = true;
            var root = sender as StackPanel;
            (root.Children[0] as CheckBox).IsChecked = App.downloadManager.IDv3WriteImage;
            (root.Children[1] as CheckBox).IsChecked = App.downloadManager.IDv3WriteArtistImage;
            (root.Children[2] as CheckBox).IsChecked = App.downloadManager.IDv3WriteLyric;
            (root.Children[3] as CheckBox).IsChecked = App.downloadManager.SaveLyricToLrcFile;
            downloadOptionsLoading = false;
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (downloadOptionsLoading) return;
            var checkBox = sender as CheckBox;
            switch (checkBox.Tag)
            {
                case "0":
                    App.downloadManager.IDv3WriteImage = (bool)checkBox.IsChecked;
                    break;
                case "1":
                    App.downloadManager.IDv3WriteArtistImage = (bool)checkBox.IsChecked;
                    break;
                case "2":
                    App.downloadManager.IDv3WriteLyric = (bool)checkBox.IsChecked;
                    break;
                case "3":
                    App.downloadManager.SaveLyricToLrcFile = (bool)checkBox.IsChecked;
                    break;
            }
        }
        #endregion

        #region playExp
        bool combo1Loading = false;
        private void ComboBox_Loaded_1(object sender, RoutedEventArgs e)
        {
            combo1Loading = true;
            (sender as ComboBox).SelectedIndex = (int)App.playingList.PlayBehavior;
            combo1Loading = false;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo1Loading) return;
            App.playingList.PlayBehavior = (PlayBehavior)(sender as ComboBox).SelectedIndex;
        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            var sp = sender as StackPanel;
            (sp.Children[0] as CheckBox).IsChecked = App.playingList.PauseWhenPreviousPause;
            (sp.Children[1] as CheckBox).IsChecked = App.playingList.NextWhenPlayError;
            (sp.Children[2] as CheckBox).IsChecked = App.LoadLastExitPlayingSongAndSongList;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            switch (checkBox.Tag)
            {
                case "0":
                    App.playingList.PauseWhenPreviousPause = (bool)checkBox.IsChecked;
                    break;
                case "1":
                    App.playingList.NextWhenPlayError = (bool)checkBox.IsChecked;
                    break;
                case "2":
                    App.LoadLastExitPlayingSongAndSongList = (bool)checkBox.IsChecked;
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
            switch (MainWindow.SWindowGridBaseTop.RequestedTheme)
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
                    MainWindow.SWindowGridBaseTop.RequestedTheme = ElementTheme.Default;
                    break;
                case 1:
                    MainWindow.SWindowGridBaseTop.RequestedTheme = ElementTheme.Light;
                    break;
                case 2:
                    MainWindow.SWindowGridBaseTop.RequestedTheme = ElementTheme.Dark;
                    break;
            }
            MainWindow.UpdateWindowBackdropTheme();
        }

        bool musicpageThemeLoading = false;
        private void ComboBox_Loaded_3(object sender, RoutedEventArgs e)
        {
            musicpageThemeLoading = true;
            (sender as ComboBox).SelectedIndex = (int)MainWindow.SMusicPage.pageRoot.RequestedTheme;
            musicpageThemeLoading = false;
        }

        private void ComboBox_SelectionChanged_5(object sender, SelectionChangedEventArgs e)
        {
            if (musicpageThemeLoading) return;
            var combo = sender as ComboBox;
            switch (combo.SelectedIndex)
            {
                case 0:
                    MainWindow.SMusicPage.pageRoot.RequestedTheme = ElementTheme.Default;
                    break;
                case 1:
                    MainWindow.SMusicPage.pageRoot.RequestedTheme = ElementTheme.Light;
                    break;
                case 2:
                    MainWindow.SMusicPage.pageRoot.RequestedTheme = ElementTheme.Dark;
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
            //App.AccentColor = accentcolor_colorpicker.Color;
        }

        bool backgroundTypeLoading = false;
        private void ComboBox_Loaded_5(object sender, RoutedEventArgs e)
        {
            backgroundTypeLoading = true;
            (sender as ComboBox).SelectedIndex = (int)MainWindow.m_currentBackdrop;
            backgroundTypeLoading = false;
        }

        private void ComboBox_SelectionChanged_7(object sender, SelectionChangedEventArgs e)
        {
            int index = (sender as ComboBox).SelectedIndex;
            if (index == 3)
            {
                imageselect_root.Visibility = Visibility.Visible;
            }
            else
            {
                imageselect_root.Visibility = Visibility.Collapsed;
            }
            if (backgroundTypeLoading) return;
            switch (index)
            {
                case 0:
                    MainWindow.SetBackdrop(MainWindow.BackdropType.Mica);
                    break;
                case 1:
                    MainWindow.SetBackdrop(MainWindow.BackdropType.MicaAlt);
                    break;
                case 2:
                    MainWindow.SetBackdrop(MainWindow.BackdropType.DesktopAcrylic);
                    break;
                case 3:
                    MainWindow.SetBackdrop(MainWindow.BackdropType.Image);
                    break;
                case 4:
                    MainWindow.SetBackdrop(MainWindow.BackdropType.DefaultColor);
                    break;
            }
        }

        bool imageSelectLoading = false;
        private void imageselect_root_Loaded(object sender, RoutedEventArgs e)
        {
            imageSelectLoading = true;
            StackPanel stackPanel = sender as StackPanel;
            (stackPanel.Children[1] as Slider).Value = MainWindow.SBackgroundMass.Opacity * 100;
            imageSelectLoading = false;
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var path = await FileHelper.UserSelectFile(Windows.Storage.Pickers.PickerViewMode.Thumbnail, Windows.Storage.Pickers.PickerLocationId.PicturesLibrary);
            MainWindow.ImagePath = path.Path;
            MainWindow.SetBackdrop(MainWindow.BackdropType.Image);
        }

        private void Slider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (imageSelectLoading) return;
            MainWindow.SBackgroundMass.Opacity = (sender as Slider).Value / 100;
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
            DataFolderBase.JSettingData = DataFolderBase.SettingDefault;
            App.LoadSettings(DataFolderBase.JSettingData);
            MainWindow.AddNotify("恢复成功", "已将设置恢复到默认。", NotifySeverity.Complete);
            MainWindow.SetNavViewContent(typeof(SearchPage));
        }

        private void NumberBox_ValueChanged_1(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            ScrollViewer a = (ScrollViewer)VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(MainWindow.SWindowGridBaseTop)))));
            a.RasterizationScale = sender.Value;
            MainWindow.AsyncDialog.RasterizationScale = sender.Value;
        }

        private void ToggleSwitch_Toggled_1(object sender, RoutedEventArgs e)
        {
            var ts = sender as ToggleSwitch;
            if (ts.Tag as string == "0")
            {
                ScrollViewer b = (ScrollViewer)VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(MainWindow.SWindowGridBaseTop)))));
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
                App.SetFramePerSecondViewer(ts.IsOn);
            }
        }

        private void ToggleSwitch_Loaded(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            switch (toggleSwitch.Tag as string)
            {
                case "0":
                    toggleSwitch.IsOn = MainWindow.SNavView.PaneDisplayMode == NavigationViewPaneDisplayMode.Top;
                    break;
                case "1":
                    toggleSwitch.IsOn = NotifyIconWindow.IsVisible;
                    break;
                case "2":
                    toggleSwitch.IsOn = MainWindow.RunInBackground;
                    break;
            }
        }

        private void ToggleSwitch_Toggled_2(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            switch (toggleSwitch.Tag as string)
            {
                case "0":
                    MainWindow.SNavView.PaneDisplayMode = toggleSwitch.IsOn ? NavigationViewPaneDisplayMode.Top : NavigationViewPaneDisplayMode.Auto;
                    break;
                case "1":
                    NotifyIconWindow.IsVisible = toggleSwitch.IsOn;
                    break;
                case "2":
                    MainWindow.RunInBackground = toggleSwitch.IsOn;
                    break;
            }
        }

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            App.ExitApp();
        }

        private void ToggleSwitch_Loaded_1(object sender, RoutedEventArgs e)
        {
            var toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch.Tag as string == "0")
            {
                toggleSwitch.IsOn = MainWindow.SNavView.PaneDisplayMode == NavigationViewPaneDisplayMode.Top;
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
                MainWindow.SNavView.PaneDisplayMode = toggleSwitch.IsOn ? NavigationViewPaneDisplayMode.Top : NavigationViewPaneDisplayMode.Auto;
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

        private async void Button_Click_9(object sender, RoutedEventArgs e)
        {
            App.SaveSettings();
            MainWindow.AddNotify("保存设置成功", "已将设置数据写入设置文件中。", NotifySeverity.Complete);
        }

        private async void Button_Click_10(object sender, RoutedEventArgs e)
        {
            App.LoadSettings(DataEditor.DataFolderBase.JSettingData);
            MainWindow.AddNotify("读取设置成功", "已从设置文件中读取设置。", NotifySeverity.Complete);
            MainWindow.SetNavViewContent(typeof(SearchPage));
        }

        private void StackPanel_Loaded_4(object sender, RoutedEventArgs e)
        {
            desktoplyric_opacity_slider.Value = DesktopLyricWindow.LyricOpacity * 100;
        }

        private void desktoplyric_opacity_slider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            DesktopLyricWindow.LyricOpacity = e.NewValue / 100;
            if (MainWindow.DesktopLyricWindow != null) MainWindow.DesktopLyricWindow.SetLyricOpacity(DesktopLyricWindow.LyricOpacity);
        }

        private void HotKeySettings_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.SetNavViewContent(typeof(SettingHotKeyPage));
        }

        private async void SettingsCard_Click(object sender, RoutedEventArgs e)
        {
            await MainWindow.ShowEqualizerDialog();
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
            await MainWindow.ShowDialog("播放定时", TimeEventPage, "返回");

            if (TimeEventPage.TimingTimer is null)
            {
                TimeEventCard.Description = "启动定时并设置定时任务";
                return;
            }
            TimeEventPage.TimingTimer.Tick -= TimingTimer_Tick;
            TimeEventPage.TimingTimer.Tick += TimingTimer_Tick;
            TimingTimer_Tick(null, null);
        }
    }
}
