using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Windowing;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using TewiMP.Pages;
using TewiMP.Pages.MusicPages;
using TewiMP.Helpers;
using TewiMP.Controls;
using TewiMP.Windowed;
using TewiMP.DataEditor;
using TewiMP.Background;
using TewiMP.Background.HotKeys;
using Windows.UI;
using Windows.Storage;
using Windows.Graphics;
using Windows.ApplicationModel.DataTransfer;
using WinRT;
using NAudio.Wave;
using Newtonsoft.Json.Linq;
using CommunityToolkit.WinUI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TewiMP
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public static bool RunInBackground = false;

        public static AppWindow AppWindowLocal;
        public static OverlappedPresenter OverlappedPresenter;
        public static FullScreenPresenter FullScreenPresenter;
        public static IntPtr Handle;

        public static UIElement SContent;
        public static Window SWindow;
        public static Rectangle SBackgroundColor;
        public static Grid SBackgroundImageRoot;
        public static Rectangle SBackgroundMass;
        public static Image SBackgroundImage;
        public static MusicPage SMusicPage = new();
        public static Grid STopControlsBaseGrid;
        public static TextBlock SPlayTitle;
        public static TextBlock SPlayArtist;
        public static TextBlock SLyricTextBlock;
        public static Grid SWindowGridBaseTop;
        public static Grid SWindowGridBase;
        public static Grid SVolumeBaseGrid;
        public static Grid SMusicPageBaseGrid;
        public static ListView SPlayingListBaseView;
        public static Grid SPlayingListBaseGrid;
        public static Frame SMusicPageBaseFrame;
        public static Frame SPlayContent;
        public static Flyout teachingTipVolume;
        public static Flyout teachingTipPlayingList;
        public static ContentDialog AsyncDialog = null;
        public static StackPanel SNotifyStackPanel;
        public static ListView SNotifyListView;
        public static ScrollViewer SNotifyListViewScrollViewer;

        public static void InvokeDpiEvent() => WindowDpiChanged?.Invoke();
        public delegate void WindowDpiChangedDelegate();
        public static event WindowDpiChangedDelegate WindowDpiChanged;
        
        public delegate void WindowViewStateChangedDelegate(bool isView);
        public static event WindowViewStateChangedDelegate WindowViewStateChanged;

        public delegate void MainViewStateChangedDelegate(bool isView);
        public static event MainViewStateChangedDelegate MainViewStateChanged;

        public delegate void MusicPageViewStateChangedDelegate(MusicPageViewState musicPageViewState);
        public static event MusicPageViewStateChangedDelegate MusicPageViewStateChanged;

        private static ObservableCollection<NotifyItemData> NotifyList = new();

        public MainWindow()
        {
            SWindow = this;
            InitializeComponent();
            Handle = WindowHelperzn.WindowHelper.GetWindowHandle(this);
            OverlappedPresenter = OverlappedPresenter.Create();
            AppWindow.SetPresenter(OverlappedPresenter);
            AppWindowLocal = AppWindow;
            //FullScreenPresenter = FullScreenPresenter.Create();

            WindowGridBase.DataContext = this;
            SContent = Content;
            SBackgroundColor = BackgroundColor;
            SBackgroundImageRoot = BackgroundImageRoot;
            SBackgroundMass = BackgroundMass;
            SBackgroundImage = BackgroundImage;
            SNavView = NavView;
            SContentFrame = ContentFrame;
            SPlayTitle = PlayTitle;
            SPlayArtist = PlayArtist;
            SLyricTextBlock = LyricTextBlock;
            STopControlsBaseGrid = TopControlsBaseGrid;
            SWindowGridBaseTop = WindowGridBase;
            SWindowGridBase = GridBase;
            SVolumeBaseGrid = VolumeBaseGrid;
            SMusicPageBaseGrid = MusicPageBaseGrid;
            SPlayingListBaseGrid = PlayingListBaseGrid;
            SPlayingListBaseView = PlayingListBaseView;
            SMusicPageBaseFrame = MusicPageBaseFrame;
            teachingTipPlayingList = PlayingListBasePopup;
            teachingTipVolume = VolumeBasePopup;
            SPlayContent = PlayContent;
            SNotifyStackPanel = NotifyStackPanel;
            //SNotifyListView = NotifyListView;
            AsyncDialog = new ContentDialog()
            {
                XamlRoot = SContent.XamlRoot,
                CloseButtonCommand = null
            };

            equalizerPage = new Pages.DialogPages.EqualizerPage();
            //SubClassing();

            AppWindow.Title = App.AppName;
            AppWindow.SetIcon(System.IO.Path.Combine("Images", "Icons", "icon.ico"));

            InitializeTitleBar(SWindowGridBaseTop.RequestedTheme);

            m_wsdqHelper = new WindowHelperzn.WindowsSystemDispatcherQueueHelper();
            m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();
            SetDragRegionForCustomTitleBar(AppWindow);

            //Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += (_, __) => { TryGoBack(); };

            Activated += MainWindow_Activated;
            SWindowGridBaseTop.ActualThemeChanged += WindowGridBase_ActualThemeChanged;
            App.SMTC.ButtonPressed += SMTC_ButtonPressed;
            App.playListReader.Updated += () => UpdatePlayListButtonUI();
            App.audioPlayer.VolumeChanged += AudioPlayer_VolumeChanged;
            MusicPageViewStateChanged += MainWindow_MusicPageViewStateChanged;

            AppWindow.Closing += AppWindow_Closing;

            loadingst.Children.Add(loadingprogress);
            loadingst.Children.Add(loadingtextBlock);

            App.LoadSettings(App.StartingSettings);
            SetBackdrop(m_currentBackdrop);

            NavView.SelectedItem = NavView.MenuItems[1];
            NavView.IsBackEnabled = false;

            Canvas.SetZIndex(AppTitleBar, 1);

            StaringPrepare();
            LoadLastPlaying();
            //NotifyListView.ItemsSource = NotifyList;
            //PlayingListBasePopup.SystemBackdrop = new DesktopAcrylicBackdrop();
            //VolumeBasePopup.SystemBackdrop = new DesktopAcrylicBackdrop();
        }

        bool isBackground = false;
        static bool isShowClosingDialog = false;
        public void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            App.SaveSettings();
            SaveNowPlaying();

            if (RunInBackground)
            {
                args.Cancel = true;
                if (InOpenMusicPage) SMusicPage.MusicPageViewStateChange(MusicPageViewState.Hidden);
                else RemoveEvents();
                AppWindow.Hide();
                WindowGridBase.Opacity = 0;
                isBackground = true;
                return;
            }

            args.Cancel = true;
            if (isShowClosingDialog) return;
            isShowClosingDialog = true;
            HideDialog();
            //var result = await ShowDialog("再确认一次", "你真的要退出程序吗？", "取消", "确定", null, ContentDialogButton.Primary);
            //if (result == ContentDialogResult.Primary)
            //{
            App.Current.Exit();
            App.Current.Exit();
            App.Current.Exit();
            //}
            //else
            //{
            //    HideDialog();
            //}
            isShowClosingDialog = false;
        }

        public async void StaringPrepare()
        {
            var displayArea = CodeHelper.GetDisplayArea(this);
            var dpi = CodeHelper.GetScaleAdjustment(this);

            bool isPreparedActivate = true;
            var windowWidth = (int)(1140 * dpi);
            var windowHeight = (int)(640 * dpi);
            var windowPositionX = displayArea.WorkArea.Width / 2 - windowWidth / 2;
            var windowPositionY = displayArea.WorkArea.Height / 2 - windowHeight / 2;

            if (App.LaunchArgs != null)
            {
                //AddNotify("Args", string.Join(" ||| ", App.LaunchArgs), NotifySeverity.Warning, TimeSpan.FromSeconds(10));
                List<string> openFiles = new List<string>();
                foreach (var str in App.LaunchArgs)
                {
                    if (str.Contains(":\\"))
                    {
                        openFiles.Add(str);
                    }
                }
                foreach (var str in openFiles)
                {
                    if (App.LaunchArgs.Contains(str))
                    {
                        App.LaunchArgs.Remove(str);
                    }
                }
                AddOpeningMusic(openFiles.ToArray());

                var lagsString = string.Join(" ", App.LaunchArgs);
                var lags = lagsString.Split('-');

                var list = lags.ToList();
                foreach (var lagsItem in lags)
                {
                    if (string.IsNullOrEmpty(lagsItem)) continue;
                    if (lagsItem.Last() == ' ')
                    {
                        int index = list.IndexOf(lagsItem);
                        int emptyCharIndex = lagsItem.LastIndexOf(' ');
                        string result = lagsItem.Remove(emptyCharIndex);
                        list[index] = result;
                    }
                }
                lags = list.ToArray();

                List<string> unknowArg = new();
                // 处理启动参数
                foreach (string arg in lags)
                {
                    if (string.IsNullOrEmpty(arg)) continue;
                    if (arg.Contains("OpenWithWindows"))
                    {
                        isPreparedActivate = false;
                    }
                    else if (arg.Contains("Size"))
                    {
                        var s = arg.Split(' ');
                        if (s.Length != 3) continue;
                        bool widthComplete = int.TryParse(s[1], out int width);
                        bool heightComplete = int.TryParse(s[2], out int height);
                        if (widthComplete && heightComplete)
                        {
                            windowWidth = (int)(width * dpi);
                            windowHeight = (int)(height * dpi);
                        }
                    }
                    else if (arg.Contains("Position"))
                    {
                        var s = arg.Split(' ');
                        if (s.Length != 3) continue;
                        bool posXComplete = int.TryParse(s[1], out int posX);
                        bool posYComplete = int.TryParse(s[2], out int posY);
                        if (posXComplete && posYComplete)
                        {
                            windowPositionX = posX;
                            windowPositionY = posY;
                        }
                    }
                    else
                    {
                        unknowArg.Add(arg);
                    }
                }
                if (unknowArg.Any())
                {
                    AddNotify("未知的启动参数：", string.Join('、', unknowArg), NotifySeverity.Warning, TimeSpan.FromSeconds(10));
                }
            }

            var screenWidth = displayArea.WorkArea.Width;
            var screenHeight = displayArea.WorkArea.Height;
            // 设置参数
            if (screenWidth <= windowWidth ||
                 screenHeight<= windowHeight)
            {
                OverlappedPresenter.Maximize();
            }
            else
            {
                AppWindow.MoveAndResize(new(
                    windowPositionX, windowPositionY,
                    windowWidth, windowHeight));
            }

            if (isPreparedActivate) Activate();

            await Task.Delay(500);
            List<string> hotKeyUsed = new();
            foreach (var hotKey in App.hotKeyManager.RegistedHotKeys)
            {
                if (hotKey.IsUsed)
                {
                    hotKeyUsed.Add(HotKey.GetHotKeyIDString(hotKey.HotKeyID));
                }
            }
            if (hotKeyUsed.Any())
            {
                AddNotify("热键已被占用", $"你可以转到设置界面更改被占用的热键：\n{string.Join('、', hotKeyUsed)}。", NotifySeverity.Warning, TimeSpan.FromSeconds(5));
            }
        }

        static bool isOpeningMusicLoaded = false;
        public async void AddOpeningMusic(string[] fileName)
        {
            if (!fileName.Any()) return;
            isOpeningMusicLoaded = true;

            List<MusicData> mlist = new();
            foreach (string str in fileName)
            {
                foreach (var musicData in await MusicData.FromFile(str))
                {
                    App.playingList.Add(musicData); mlist.Add(musicData);
                }
            }
            if (mlist.Any())
            {
                await App.playingList.Play(mlist.First());
            }
        }

        public static async void SaveNowPlaying()
        {
            if (App.audioPlayer.MusicData is null) return;

            var path = System.IO.Path.Combine(DataFolderBase.UserDataFolder, "LastPlaying");
            if (!App.LoadLastExitPlayingSongAndSongList)
            {
                System.IO.File.Delete(path);
                return;
            }

            if (!System.IO.File.Exists(path)) System.IO.File.Create(path).Close();

            JArray array = new JArray();
            foreach (var a in App.playingList.PlayBehavior == PlayBehavior.随机播放 ? App.playingList.RandomSavePlayingList : App.playingList.NowPlayingList)
                array.Add(JObject.FromObject(a));
            JObject jobject = new JObject() {
                { "music", JObject.FromObject(App.audioPlayer.MusicData) },
                { "list", array }
            };
            await System.IO.File.WriteAllTextAsync(path, jobject.ToString());
            Debug.WriteLine("[SavePlayingList]: 正在播放列表已保存！");
        }

        public static async void LoadLastPlaying()
        {
            if (!App.LoadLastExitPlayingSongAndSongList) return;
            if (isOpeningMusicLoaded) return;

            var path = System.IO.Path.Combine(DataFolderBase.UserDataFolder, "LastPlaying");
            if (!System.IO.File.Exists(path)) return;

            MusicData musicData = null;
            await Task.Run(() =>
            {
                var texts = System.IO.File.ReadAllText(path);
                JObject jobject = JObject.Parse(texts);
                musicData = JsonNewtonsoft.FromJSON<MusicData>(jobject["music"].ToString());

                foreach (var m in jobject["list"])
                {
                    var md = JsonNewtonsoft.FromJSON<MusicData>(m.ToString());
                    App.playingList.NowPlayingList.Add(md);
                }
            });

            if (musicData is null) return;
            if (App.playingList.PlayBehavior == PlayBehavior.随机播放)
            {
                App.playingList.SetRandomPlay(PlayBehavior.随机播放);
            }
            await App.playingList.Play(musicData, false);
            App.audioPlayer.SetPause();
        }

        #region Window Events
        private void WindowGridBase_Loaded(object sender, RoutedEventArgs e)
        {
            PlayingListBaseView.ItemsSource = App.playingList.NowPlayingList;
#if DEBUG
            DebugViewPopup.XamlRoot = WindowGridBase.XamlRoot;
            DebugViewPopup.IsOpen = true;
#endif

        }

        private void WindowGridBase_ActualThemeChanged(FrameworkElement sender, object args)
        {
            if (MainWindow.m_currentBackdrop == BackdropType.DesktopAcrylic)
            {
                SetBackdrop(BackdropType.DesktopAcrylic);
            }
            InitializeTitleBar(SWindowGridBaseTop.RequestedTheme);
        }

        private void UpdateWhenDataLate()
        {
            AudioPlayer_SourceChanged(App.audioPlayer);
            AudioPlayer_PlayStateChanged(App.audioPlayer);
            AudioPlayer_CacheLoadedChanged(App.audioPlayer);
            AudioPlayer_TimingChanged(App.audioPlayer);
            AudioPlayer_VolumeChanged(App.audioPlayer, App.audioPlayer.Volume);
            PlayingList_NowPlayingImageLoaded(App.playingList.NowPlayingImage, null);
            LyricManager_PlayingLyricSelectedChange(App.lyricManager.NowLyricsData);
            PlayingList_PlayingListItemChange(App.playingList.NowPlayingList);
            UpdataDownloadPageButtonInfoBadgeText();
            App.audioPlayer.ReCallTiming();
            Debug.WriteLine("[MainWindow]: Data Updated.");
        }

        bool isAddEvents = false;
        private void AddEvents()
        {
            if (isAddEvents) return;
            //AutoScrollViewerFirst.Pause = false;
            //AutoScrollViewerSecond.Pause = false;
            App.audioPlayer.SourceChanged += AudioPlayer_SourceChanged;
            App.audioPlayer.PlayEnd += AudioPlayer_PlayEnd;
            App.audioPlayer.PlayStateChanged += AudioPlayer_PlayStateChanged;
            App.audioPlayer.TimingChanged += AudioPlayer_TimingChanged;
            App.audioPlayer.CacheLoadedChanged += AudioPlayer_CacheLoadedChanged;
            App.audioPlayer.CacheLoadingChanged += AudioPlayer_CacheLoadingChanged;
            App.playingList.NowPlayingImageLoading += PlayingList_NowPlayingImageLoading;
            App.playingList.NowPlayingImageLoaded += PlayingList_NowPlayingImageLoaded;
            App.playingList.PlayingListItemChange += PlayingList_PlayingListItemChange;

            App.downloadManager.AddDownload += DownloadManager_AddDownload;
            App.downloadManager.OnDownloadedSaving += DownloadManager_AddDownload;
            App.downloadManager.OnDownloadedPreview += DownloadManager_AddDownload;
            App.downloadManager.OnDownloaded += DownloadManager_AddDownload;
            App.downloadManager.OnDownloadError += DownloadManager_AddDownload;

            isAddEvents = true;
            UpdateWhenDataLate();
            MainViewStateChanged?.Invoke(true);
            Debug.WriteLine("[MainWindow]: Added Events.");
        }

        private void RemoveEvents()
        {
            //AutoScrollViewerFirst.Pause = true;
            //AutoScrollViewerSecond.Pause = true;
            App.audioPlayer.SourceChanged -= AudioPlayer_SourceChanged;
            App.audioPlayer.PlayEnd -= AudioPlayer_PlayEnd;
            App.audioPlayer.PlayStateChanged -= AudioPlayer_PlayStateChanged;
            App.audioPlayer.TimingChanged -= AudioPlayer_TimingChanged;
            App.audioPlayer.CacheLoadedChanged -= AudioPlayer_CacheLoadedChanged;
            App.audioPlayer.CacheLoadingChanged -= AudioPlayer_CacheLoadingChanged;
            App.playingList.NowPlayingImageLoading -= PlayingList_NowPlayingImageLoading;
            App.playingList.NowPlayingImageLoaded -= PlayingList_NowPlayingImageLoaded;
            App.lyricManager.PlayingLyricSelectedChange -= LyricManager_PlayingLyricSelectedChange;
            App.playingList.PlayingListItemChange -= PlayingList_PlayingListItemChange;

            App.downloadManager.AddDownload -= DownloadManager_AddDownload;
            App.downloadManager.OnDownloadedSaving -= DownloadManager_AddDownload;
            App.downloadManager.OnDownloadedPreview -= DownloadManager_AddDownload;
            App.downloadManager.OnDownloaded -= DownloadManager_AddDownload;
            App.downloadManager.OnDownloadError -= DownloadManager_AddDownload;

            isAddEvents = false;
            MainViewStateChanged?.Invoke(false);
            Debug.WriteLine("[MainWindow]: Removed Events.");
        }

        static ApplicationTheme applicationTheme = App.Current.RequestedTheme;
        public static bool isMinSize = false;
        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (isBackground)
            {
                isBackground = false;
                AddEvents();
            }
            if (args.WindowActivationState == WindowActivationState.PointerActivated ||
                args.WindowActivationState == WindowActivationState.CodeActivated)
            {
                if (!CodeHelper.IsIconic(Handle))
                {
                    isMinSize = false;
                    WindowViewStateChanged?.Invoke(true);
                    if (InOpenMusicPage) SMusicPage.MusicPageViewStateChange(MusicPageViewState.View);
                    else
                    {
                        AddEvents();
                    }
                    WindowGridBase.Opacity = 1;
                    AppTitleBar.Opacity = 1;
                }
            }
            else
            {
                if (CodeHelper.IsIconic(Handle))
                {
                    SaveNowPlaying();
                    App.SaveSettings();

                    isMinSize = true;
                    WindowViewStateChanged?.Invoke(false);
                    if (InOpenMusicPage) SMusicPage.MusicPageViewStateChange(MusicPageViewState.Hidden);
                    else RemoveEvents();
                    WindowGridBase.Opacity = 0;
                }
                else
                {
                    AppTitleBar.Opacity = 0.6;
                }
            }
        }

        private void MainWindow_MusicPageViewStateChanged(MusicPageViewState musicPageViewState)
        {
            if (musicPageViewState == MusicPageViewState.View)
            {
                RemoveEvents();
            }
            else
            {
                AddEvents();
            }
            SetDragRegionForCustomTitleBar(MainWindow.AppWindowLocal);
        }

        private static void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            //SetBackdrop(BackdropType.DesktopAcrylic);
            if (!isAcrylicBackdrop) UpdateWindowBackdropTheme();
            if (m_currentBackdrop != BackdropType.DefaultColor)
            {
                m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
            }
        }

        public static void UpdateWindowBackdropTheme()
        {
            switch (SWindowGridBaseTop.RequestedTheme)
            {
                case ElementTheme.Light:
                    m_configurationSource.Theme = SystemBackdropTheme.Light; break;
                case ElementTheme.Dark:
                    m_configurationSource.Theme = SystemBackdropTheme.Dark; break;
                default:
                    m_configurationSource.Theme = SystemBackdropTheme.Default; break;
            }
            m_micaController.SetSystemBackdropConfiguration(m_configurationSource);
            //m_micaController.Kind = MicaKind.BaseAlt;
        }

        private static void Window_Closed(object sender, WindowEventArgs args)
        {
            /*
            if (m_micaController != null)
            {
                m_micaController.Dispose();
                m_micaController = null;
            }
            if (m_acrylicController != null)
            {
                m_acrylicController.Dispose();
                m_acrylicController = null;
            }*/
            if (DesktopLyricWindow != null)
                DesktopLyricWindow.Close();
            SWindow.Activated -= Window_Activated;
            m_configurationSource = null;
        }

        private async void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetDragRegionForCustomTitleBar(MainWindow.AppWindowLocal);
#if DEBUG
            DebugView_Detail_WindowSizeRun.Text = $"{WindowGridBase.ActualWidth}x{WindowGridBase.ActualHeight}";
            DebugViewPopup.VerticalOffset = AppWindow.Size.Height;
#endif
            //NotifyListView.Padding = new(0, SWindowGridBase.ActualHeight, 0, 12);
            /*
            if (NotifyList.Any())
            {
                await Task.Delay(1);
                SNotifyListViewScrollViewer.ChangeView(null, SNotifyListViewScrollViewer.ScrollableHeight, null, true);
            }*/
        }

        private void ContentFrame_Loaded(object sender, RoutedEventArgs e)
        {
            SContentFrame = ContentFrame;
        }

        private void NotifyArea_Loaded(object sender, RoutedEventArgs e)
        {
            //SNotifyListViewScrollViewer = (VisualTreeHelper.GetChild(NotifyListView, 0) as Border).Child as ScrollViewer;
            //AddNotify("测试版本", "此应用程序是一份内测版本。", NotifySeverity.Warning, TimeSpan.MaxValue);
        }
        
        public static void UpdatePlayListFlyoutHeight()
        {
            try
            {
                if (InOpenMusicPage)
                    SPlayingListBaseGrid.Height = SWindowGridBaseTop.ActualHeight - 130;
                else
                    SPlayingListBaseGrid.Height = SWindowGridBaseTop.ActualHeight - 155;
            }
            catch { }
        }
        #endregion

        #region init TitleBar
        public void InitializeTitleBar(ElementTheme theme)
        {
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                InitializeTitleBara(AppWindowLocal.TitleBar, theme);
            }
            else
            {
                ExtendsContentIntoTitleBar = true;
                SetTitleBar(AppTitleBar);
                AppTitleBar.Height = 28;
                AppTitleBar.Margin = new Thickness(0);
                NavView.Margin = new Thickness(0, 28, 0, 0);
                AppTitleTextBlock.Margin = new Thickness(18, 0, 0, 0);
                if (theme == ElementTheme.Dark)
                {
                    WindowGridBase.Background = new SolidColorBrush(Color.FromArgb(255, 32, 32, 32));
                    AppTitleBar.Background = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
                }
                else
                {
                    //w10TitleBarColorReplaceBaseGrid.Visibility = Visibility.Visible;
                    WindowGridBase.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                    AppTitleBar.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                }
            }
        }

        public static void InitializeTitleBara(AppWindowTitleBar bar, ElementTheme theme)
        {
            bar.ExtendsContentIntoTitleBar = true;

            bool defaultLightTheme = false;
            bool defaultDarkTheme = false;
            AppWindowLocal.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
            if (theme == ElementTheme.Default)
            {
                defaultLightTheme = App.Current.RequestedTheme == ApplicationTheme.Light;
                defaultDarkTheme = App.Current.RequestedTheme == ApplicationTheme.Dark;
            }

            if (theme == ElementTheme.Light || defaultLightTheme)
            {
                bar.ButtonBackgroundColor = Colors.Transparent;
                bar.ButtonForegroundColor = Colors.Black;
                bar.ButtonHoverBackgroundColor = Color.FromArgb(20, 0, 0, 0);
                bar.ButtonHoverForegroundColor = Colors.Black;
                bar.ButtonPressedBackgroundColor = Color.FromArgb(10, 0, 0, 0);
                bar.ButtonPressedForegroundColor = Color.FromArgb(255, 0, 0, 0);
                bar.ButtonInactiveBackgroundColor = Colors.Transparent;
                bar.ButtonInactiveForegroundColor = Color.FromArgb(255, 150, 150, 150);
            }
            else if (theme == ElementTheme.Dark || defaultDarkTheme)
            {
                bar.ButtonBackgroundColor = Colors.Transparent;
                bar.ButtonForegroundColor = Colors.White;
                bar.ButtonHoverBackgroundColor = Color.FromArgb(20, 255, 255, 255);
                bar.ButtonHoverForegroundColor = Colors.White;
                bar.ButtonPressedBackgroundColor = Color.FromArgb(10, 255, 255, 255);
                bar.ButtonPressedForegroundColor = Color.FromArgb(255, 255, 255, 255);
                bar.ButtonInactiveBackgroundColor = Colors.Transparent;
                bar.ButtonInactiveForegroundColor = Color.FromArgb(255, 100, 100, 100);
            }
        }
        #endregion

        #region Dialog
        static ScrollViewer dialogScrollViewer = new() { HorizontalScrollMode = ScrollMode.Disabled };
        static bool dialogShow = false;
        static List<object[]> dialogShowObjects = new();
        public static async Task<ContentDialogResult> ShowDialog(
            object title, object content,
            string closeButtonText = "确定", string primaryButtonText = null, string secondaryButtonText = null,
            ContentDialogButton defaultButton = ContentDialogButton.None,
            bool fullSizeDesired = false)
        {
            try
            {
                ContentDialogResult result = default;
                if (!dialogShow)
                {
                    dialogShow = true;
                    AsyncDialog.Title = title;
                    if (content is string)
                    {
                        dialogScrollViewer.Content = new TextBlock() { Text = content as string, TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true, MinHeight = 26, Margin = new(0, 0, 16, 0) };
                        AsyncDialog.Content = dialogScrollViewer;
                    }
                    else
                        AsyncDialog.Content = content;
                    AsyncDialog.Background = App.Current.Resources["AcrylicNormal"] as AcrylicBrush;
                    AsyncDialog.CloseButtonText = closeButtonText;
                    AsyncDialog.PrimaryButtonText = primaryButtonText;
                    AsyncDialog.SecondaryButtonText = secondaryButtonText;
                    AsyncDialog.FullSizeDesired = fullSizeDesired;
                    AsyncDialog.CloseButtonCommand = null;
                    AsyncDialog.XamlRoot = SContent.XamlRoot;
                    AsyncDialog.RequestedTheme = SWindowGridBaseTop.RequestedTheme;
                    AsyncDialog.DefaultButton = defaultButton;
                    result = await AsyncDialog.ShowAsync();
                    dialogShow = false;

                    if (dialogShowObjects.Any())
                    {
                        var a = dialogShowObjects[0];
                        dialogShowObjects.Remove(a);
                        await ShowDialog(a[0], a[1], (string)a[2], (string)a[3], (string)a[4], (ContentDialogButton)a[5], (bool)a[6]);
                    }
                }
                else
                {
                    dialogShowObjects.Add(new object[] { title, content, closeButtonText, primaryButtonText, secondaryButtonText, defaultButton, fullSizeDesired });
                }
                return result;
            }
            catch
            {
                return ContentDialogResult.None;
            }
        }

        static Pages.DialogPages.EqualizerPage equalizerPage;
        public static async Task ShowEqualizerDialog()
        {
            await ShowDialog("音频设置", equalizerPage);
        }

        static StackPanel loadingst = new();
        static ProgressRing loadingprogress = new() { IsIndeterminate = true, Width = 50, Height = 50 };
        static TextBlock loadingtextBlock = new() { Text = "", HorizontalAlignment = HorizontalAlignment.Center, TextWrapping = TextWrapping.Wrap };
        public static async void ShowLoadingDialog(string title = "正在加载")
        {
            SetLoadingText("");
            SetLoadingProgressRingValue(100, 0);
            await ShowDialog(title, loadingst, "后台", null);
        }

        public static void SetLoadingText(string text)
        {
            loadingtextBlock.Text = text;
        }

        public static void SetLoadingProgressRingValue(int maximum, int value)
        {
            if (value == 0) loadingprogress.IsIndeterminate = true;
            else loadingprogress.IsIndeterminate = false;
            loadingprogress.Maximum = maximum;
            loadingprogress.Value = value;
        }

        public static void HideDialog()
        {
            AsyncDialog.Hide();
        }

        public static NotifyItem AddNotify(string title, string message, NotifySeverity severity = NotifySeverity.Info, TimeSpan? residenceTime = null)
        {
            return AddNotify(new(title, message, severity, residenceTime));
        }

        public static NotifyItem AddNotify(NotifyItemData notifyItemData)
        {
            var notifyItem = new NotifyItem();
            notifyItem.SetNotifyItemData(notifyItemData);
            /*{
                Title = notifyItemData.Title,
                Message = notifyItemData.Message,
                Severity = notifyItemData.Severity,
                IsOpen = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                IsClosable = false,
                BorderBrush = App.Current.Resources["ControlElevationBorderBrush"] as Brush,
                CornerRadius = new(8)
            };*/
            SNotifyStackPanel.Children.Add(notifyItem);
            if (notifyItemData.ResidenceTime != TimeSpan.MaxValue)
                NotifyCountDown(notifyItem);
            return notifyItem;
        }

        public static void RemoveNotifyItem(NotifyItem notifyItem)
        {
            if (SNotifyStackPanel.Children.Contains(notifyItem))
                SNotifyStackPanel.Children.Remove(notifyItem);
        }

        public static void NotifyCountDown(NotifyItem notifyItem)
        {
            NotifyCountDown(notifyItem.GetNotifyItemData(), notifyItem);
        }

        public static async void NotifyCountDown(NotifyItemData notifyItemData, NotifyItem notifyItem)
        {
            if (notifyItemData.ResidenceTime == TimeSpan.MaxValue) return;
            await Task.Delay(notifyItemData.ResidenceTime);
            SNotifyStackPanel.Children.Remove(notifyItem);
        }
        #endregion

        #region AudioPlayer Events
        private void DownloadManager_AddDownload(Background.DownloadData data)
        {
            UpdataDownloadPageButtonInfoBadgeText();
        }

        private void UpdataDownloadPageButtonInfoBadgeText()
        {
            if (App.downloadManager.AllDownloadData.Any())
            {
                if (App.downloadManager.DownloadingData.Any())
                {
                    DownloadPageButtonInfoBadge.Opacity = 1;
                    DownloadPageButtonInfoBadge.Value = App.downloadManager.AllDownloadData.Count - App.downloadManager.DownloadedData.Count;
                }
                else
                {
                    DownloadPageButtonInfoBadge.Opacity = 0;
                    DownloadPageButtonInfoBadge.Style = App.Current.Resources["SuccessIconInfoBadgeStyle"] as Style;
                    DownloadPageButtonInfoBadge.Value = -1;
                    DownloadPageButtonInfoBadge.IconSource = new FontIconSource() { Glyph = "\uE73E" };
                }
            }
            else
            {
                DownloadPageButtonInfoBadge.Opacity = 0;
            }
        }

        private void SetLyricToNormal()
        {
            AppTitleTextBlock.Text = $"{App.AppName}";
            LyricTextBlock.Text = null;
        }

        private void LyricManager_PlayingLyricSelectedChange(LyricData _)
        {
            try
            {
                if (_ == null) { SetLyricToNormal(); return; }
                if (_.Lyric == null) { SetLyricToNormal(); return; }
                if (!_.Lyric.Any()) { SetLyricToNormal(); return; }

                int tcount = 1;
                int num = App.lyricManager.NowPlayingLyrics.IndexOf(_);
                try
                {
                    while (_?.Lyric?.FirstOrDefault() == App.lyricManager.NowPlayingLyrics[num + tcount]?.Lyric?.FirstOrDefault())
                    {
                        tcount++;
                    }
                }
                catch { }

                string t1text = tcount == 1
                    ? _?.Lyric?.FirstOrDefault()
                    : $"{_?.Lyric?.FirstOrDefault()} (x{tcount})";

                AppTitleTextBlock.Text = $"{App.AppName} -";
                LyricTextBlock.Text = $" {t1text}";
            }
            catch (Exception err)
            {
                Debug.WriteLine("[MainWindow]: zn1-----" + err.Message);
            }
        }

        public static void Invoke(Action action)
        {
            SWindowGridBase.DispatcherQueue.TryEnqueue(() => action());
        }

        private void SMTC_ButtonPressed(Windows.Media.SystemMediaTransportControls sender, Windows.Media.SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            Invoke(() =>
            {
                switch (args.Button)
                {
                    case Windows.Media.SystemMediaTransportControlsButton.Play:
                        App.audioPlayer.SetPlay();
                        break;
                    case Windows.Media.SystemMediaTransportControlsButton.Pause:
                        App.audioPlayer.SetPause();
                        break;
                    case Windows.Media.SystemMediaTransportControlsButton.Previous:
                        PlayBeforeButton_Click(null, null);
                        break;
                    case Windows.Media.SystemMediaTransportControlsButton.Next:
                        PlayNextButton_Click(null, null);
                        break;
                    case Windows.Media.SystemMediaTransportControlsButton.Stop:
                        App.audioPlayer.SetStop();
                        break;
                }
            });
        }

        private void PlayingList_PlayingListItemChange(ObservableCollection<MusicData> nowPlayingList)
        {
            //PlayingListBaseView.SelectedItem = App.audioPlayer.MusicData;
        }

        private void AudioPlayer_VolumeChanged(Media.AudioPlayer audioPlayer, object data)
        {
            float volume = (float)data;
            VolumeSlider.Value = (int)volume;

            if (volume == 0)
            {
                VolumeIconBase.Glyph = "\xE198";
            }
            else
            {
                if (volume <= 100 && volume > 67)
                    VolumeIconBase.Glyph = "\xE995";
                else if (volume <= 67 && volume > 33)
                    VolumeIconBase.Glyph = "\xE994";
                else if (volume <= 33)
                    VolumeIconBase.Glyph = "\xE993";
            }
        }

        private void AudioPlayer_PlayEnd(Media.AudioPlayer audioPlayer)
        {
            if (true)
            {
                AudioPlayer_PlayStateChanged(audioPlayer);
            }
        }

        bool doNotChangeTiming = false;
        private void AudioPlayer_CacheLoadedChanged(Media.AudioPlayer audioPlayer)
        {
            PlayRing.Value = 0;
            PlayRing.IsIndeterminate = false;
            PlayingListBaseView.SelectedItem = audioPlayer.MusicData;
            isCodeChangedSilderValue = false;
            doNotChangeTiming = false;
            PlayRing.Foreground = App.Current.Resources["AccentAAFillColorDefaultBrush"] as SolidColorBrush;
        }

        private void AudioPlayer_CacheLoadingChanged(Media.AudioPlayer audioPlayer, object data)
        {
            doNotChangeTiming = true;
            PlayRing.Foreground = App.Current.Resources["SystemFillColorCautionBrush"] as SolidColorBrush;
            isCodeChangedSilderValue = true;
            PlayRing.Maximum = 100;
            if (data == null)
            {
                PlayRing.IsIndeterminate = true;
                PlayRing.Value = 0;
            }
            else
            {
                PlayRing.IsIndeterminate = false;
                PlayRing.Value = (int)data;
            }
        }

        static bool isDeleteImage = true;
        private static void PlayingList_NowPlayingImageLoading(ImageSource imageSource, string _)
        {
            if (SPlayContent.Content is not ImageEx)
            {
                SPlayContent.Content = new ImageEx() { MinWidth = 0, CornerRadius = new(3) };
            }
        }

        public static void PlayingList_NowPlayingImageLoaded(ImageSource imageSource, string _)
        {
            var im = SPlayContent.Content as ImageEx;
            if (im is null) return;
            if (imageSource is null)
            {
                im.BorderThickness = new(0);
                im.Source = null;
                return;
            }
            if (imageSource != im.Source)
            {
                im.Source = imageSource;
                im.SaveName = $"{App.audioPlayer.MusicData.Title} · {App.audioPlayer.MusicData.Album.Title}";
                im.BorderThickness = new(1);
                im.BorderBrush = App.Current.Resources["ControlElevationBorderBrush"] as Brush;
            }

        }

        MusicData pointConnectAnimationMusicData = null;
        private void AudioPlayer_SourceChanged(Media.AudioPlayer audioPlayer)
        {
            if (audioPlayer.MusicData == null) return;
            if (pointConnectAnimationMusicData == audioPlayer.MusicData) return;
            pointConnectAnimationMusicData = audioPlayer.MusicData;
            PlayTitle.Text = audioPlayer.MusicData.Title;
            PlayArtist.Text = audioPlayer.MusicData.ButtonName;
            PlayingListBaseView.SelectedItem = audioPlayer.MusicData;

            foreach (var i in SongItem.StaticSongItems)
            {
                if (i != null)
                {
                    if (i.MusicData == audioPlayer.MusicData)
                    {
                        i.InfoRoot.Opacity = 0;
                        ConnectedAnimation canimation =
                            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("changeAnimation", i.AlbumImage_BaseBorder);
                        canimation.Configuration = new BasicConnectedAnimationConfiguration();
                        ConnectedAnimation animation =
                            ConnectedAnimationService.GetForCurrentView().GetAnimation("changeAnimation");
                        if (animation != null)
                        {
                            animation.Completed += (_, __) => i.InfoRoot.Opacity = 1;
                            animation.TryStart(VisualTreeHelper.GetParent(SPlayContent) as UIElement);
                        }

                        ConnectedAnimation canimation1 =
                            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("changeAnimation1", i.TitleTextBlock);
                        canimation1.Configuration = new BasicConnectedAnimationConfiguration();
                        ConnectedAnimation animation1 =
                            ConnectedAnimationService.GetForCurrentView().GetAnimation("changeAnimation1");
                        if (animation1 != null)
                        {
                            animation1.TryStart(PlayTitle);
                        }
                        ConnectedAnimation canimation2 =
                            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("changeAnimation2", i.ButtonNameTextBlock);
                        canimation2.Configuration = new BasicConnectedAnimationConfiguration();
                        ConnectedAnimation animation2 =
                            ConnectedAnimationService.GetForCurrentView().GetAnimation("changeAnimation2");
                        if (animation2 != null)
                        {
                            animation2.TryStart(PlayArtist);
                        }
                    }
                    i.IsMusicDataPlaying = i.MusicData == audioPlayer.MusicData;
                }
            }
        }

        private void AudioPlayer_PlayStateChanged(Media.AudioPlayer audioPlayer)
        {
            if (audioPlayer.PlaybackState == PlaybackState.Playing)
            {
                PlayRing.Foreground = App.Current.Resources["AccentAAFillColorDefaultBrush"] as SolidColorBrush;
                App.lyricManager.PlayingLyricSelectedChange += LyricManager_PlayingLyricSelectedChange;
                App.lyricManager.StartTimer();
            }
            else
            {
                PlayRing.Foreground = App.Current.Resources["SystemFillColorCautionBrush"] as SolidColorBrush;
                App.lyricManager.PlayingLyricSelectedChange -= LyricManager_PlayingLyricSelectedChange;
            }

            MediaPlayStateViewer.PlaybackState = audioPlayer.PlaybackState;
        }

        private void AudioPlayer_TimingChanged(Media.AudioPlayer audioPlayer)
        {
            if (doNotChangeTiming) return;
            if (audioPlayer.FileReader == null) return;
            PlayRing.Minimum = 0;
            PlayRing.Maximum = audioPlayer.TotalTime.Ticks;
            PlayRing.Value = audioPlayer.CurrentTime.Ticks;
        }
        #endregion

        #region Enable Window Backdrop
        public enum BackdropType
        {
            Mica,
            MicaAlt,
            DesktopAcrylic,
            Image,
            DefaultColor
        }

        static WindowHelperzn.WindowsSystemDispatcherQueueHelper m_wsdqHelper;
        public static BackdropType m_currentBackdrop;
        static MicaController m_micaController;
        static DesktopAcrylicController m_acrylicController;
        static SystemBackdropConfiguration m_configurationSource;
        public static string ImagePath = null;


        public static void SetBackdrop(BackdropType type)
        {
            m_currentBackdrop = BackdropType.DefaultColor;
            if (m_micaController != null)
            {
                m_micaController.Dispose();
                m_micaController = null;
            }
            if (m_acrylicController != null)
            {
                m_acrylicController.Dispose();
                m_acrylicController = null;
            }
            SWindow.Activated -= Window_Activated;
            SWindow.Closed -= Window_Closed;
            m_configurationSource = null;

            if (type == BackdropType.Mica)
            {
                if (TrySetMicaBackdrop())
                {
                    m_currentBackdrop = type;
                }
                else
                {
                    type = BackdropType.DesktopAcrylic;
                }
            }
            if (type == BackdropType.MicaAlt)
            {
                if (TrySetMicaBackdrop(true))
                {
                    m_currentBackdrop = type;
                }
                else
                {
                    type = BackdropType.DesktopAcrylic;
                }
            }
            if (type == BackdropType.DesktopAcrylic)
            {
                if (TrySetAcrylicBackdrop())
                {
                    m_currentBackdrop = type;
                }
            }
            if (type == BackdropType.DefaultColor)
                m_currentBackdrop = BackdropType.DefaultColor;
            if (type == BackdropType.DefaultColor || type == BackdropType.Image)
            {
                SBackgroundColor.Visibility = Visibility.Visible;
            }
            else
            {
                SBackgroundColor.Visibility = Visibility.Collapsed;
            }
            if (type == BackdropType.Image)
            {
                m_currentBackdrop = BackdropType.Image;
                SBackgroundImageRoot.Visibility = Visibility.Visible;
                var image = new BitmapImage();
                if (ImagePath != null)
                    image.UriSource = new Uri(ImagePath);
                SBackgroundImage.Source = image;
            }
            else
            {
                SBackgroundImageRoot.Visibility = Visibility.Collapsed;
                SBackgroundImage.Source = null;
            }
        }

        static bool TrySetMicaBackdrop(bool alt = false)
        {
            if (MicaController.IsSupported())
            {
                m_configurationSource = new SystemBackdropConfiguration();
                SWindow.Activated += Window_Activated;
                SWindow.Closed += Window_Closed;

                m_configurationSource.IsInputActive = true;
                switch (SWindowGridBaseTop.RequestedTheme)
                {
                    case ElementTheme.Dark: m_configurationSource.Theme = SystemBackdropTheme.Dark; break;
                    case ElementTheme.Light: m_configurationSource.Theme = SystemBackdropTheme.Light; break;
                    case ElementTheme.Default: m_configurationSource.Theme = SystemBackdropTheme.Default; break;
                }

                m_micaController = new MicaController() { Kind = alt ? MicaKind.BaseAlt : MicaKind.Base };
                m_micaController.AddSystemBackdropTarget(SWindow.As<ICompositionSupportsSystemBackdrop>());
                m_micaController.SetSystemBackdropConfiguration(m_configurationSource);
                isAcrylicBackdrop = false;
                return true;
            }

            return false;
        }

        static bool isAcrylicBackdrop = false;
        static bool TrySetAcrylicBackdrop()
        {
            if (DesktopAcrylicController.IsSupported())
            {
                m_configurationSource = new SystemBackdropConfiguration();
                SWindow.Activated += Window_Activated;
                SWindow.Closed += Window_Closed;

                m_configurationSource.IsInputActive = true;
                switch (SWindowGridBaseTop.RequestedTheme)
                {
                    case ElementTheme.Dark: m_configurationSource.Theme = SystemBackdropTheme.Dark; break;
                    case ElementTheme.Light: m_configurationSource.Theme = SystemBackdropTheme.Light; break;
                    case ElementTheme.Default: m_configurationSource.Theme = SystemBackdropTheme.Default; break;
                }

                m_acrylicController = new DesktopAcrylicController();

                ElementTheme elementTheme = SWindowGridBaseTop.RequestedTheme;
                if (elementTheme == ElementTheme.Default)
                {
                    elementTheme = App.Current.RequestedTheme == ApplicationTheme.Light ? ElementTheme.Light : ElementTheme.Dark;
                }

                if (elementTheme == ElementTheme.Dark)
                {
                    m_acrylicController.LuminosityOpacity = 1f;
                    m_acrylicController.TintOpacity = 0.5f;
                    m_acrylicController.TintColor = Color.FromArgb(255, 32, 32, 32);
                }
                else if (elementTheme == ElementTheme.Light)
                {
                    m_acrylicController.LuminosityOpacity = 1f;
                    m_acrylicController.TintOpacity = 0.5f;
                    m_acrylicController.TintColor = Color.FromArgb(255, 245, 245, 245);
                }

                m_acrylicController.AddSystemBackdropTarget(SWindow.As<ICompositionSupportsSystemBackdrop>());
                m_acrylicController.SetSystemBackdropConfiguration(m_configurationSource);
                isAcrylicBackdrop = true;
                return true;
            }

            return false;
        }
        #endregion

        #region Window MinSize Setter
        private delegate IntPtr WinProc(IntPtr hWnd, PInvoke.User32.WindowMessage Msg, IntPtr wParam, IntPtr lParam);
        private WinProc newWndProc = null;
        private IntPtr oldWndProc = IntPtr.Zero;
        [DllImport("user32")]
        private static extern IntPtr SetWindowLong(IntPtr hWnd, PInvoke.User32.WindowLongIndexFlags nIndex, WinProc newProc);
        [DllImport("user32.dll")]
        static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, PInvoke.User32.WindowMessage Msg, IntPtr wParam, IntPtr lParam);

        private void SubClassing()
        {
            //Get the Window's HWND
            var hwnd = this.As<IWindowNative>().WindowHandle;

            newWndProc = new WinProc(NewWindowProc);
            oldWndProc = SetWindowLong(hwnd, PInvoke.User32.WindowLongIndexFlags.GWL_WNDPROC, newWndProc);
        }

        int MinWidth = 460;
        int MinHeight = 460;

        [StructLayout(LayoutKind.Sequential)]
        struct MINMAXINFO
        {
            public PInvoke.POINT ptReserved;
            public PInvoke.POINT ptMaxSize;
            public PInvoke.POINT ptMaxPosition;
            public PInvoke.POINT ptMinTrackSize;
            public PInvoke.POINT ptMaxTrackSize;
        }

        private IntPtr NewWindowProc(IntPtr hWnd, PInvoke.User32.WindowMessage Msg, IntPtr wParam, IntPtr lParam)
        {
            switch (Msg)
            {
                case PInvoke.User32.WindowMessage.WM_GETMINMAXINFO:
                    var dpi = PInvoke.User32.GetDpiForWindow(hWnd);
                    float scalingFactor = (float)dpi / 96;

                    MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    minMaxInfo.ptMinTrackSize.x = (int)(MinWidth * scalingFactor);
                    minMaxInfo.ptMinTrackSize.y = (int)(MinHeight * scalingFactor);
                    Marshal.StructureToPtr(minMaxInfo, lParam, true);
                    break;

            }
            return CallWindowProc(oldWndProc, hWnd, Msg, wParam, lParam);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB")]
        internal interface IWindowNative
        {
            IntPtr WindowHandle { get; }
        }
        #endregion

        #region Animate Icon Events
        private void Button_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            //AnimatedIcon.SetState(this.BackAnimatedIcon, "PointerOver");
        }

        private void Button_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            //AnimatedIcon.SetState(this.BackAnimatedIcon, "Normal");
        }

        private void SetDragRegionForCustomTitleBar(AppWindow appWindow)
        {
            // Check to see if customization is supported.
            // Currently only supported on Windows 11.
            if (AppWindowTitleBar.IsCustomizationSupported()
                && appWindow.TitleBar.ExtendsContentIntoTitleBar)
            {
                double scaleAdjustment = CodeHelper.GetScaleAdjustment(this);
                double rpc = appWindow.TitleBar.RightInset / scaleAdjustment;
                double lpc = appWindow.TitleBar.LeftInset / scaleAdjustment;

                //RightPaddingColumn.Width = new GridLength(appWindow.TitleBar.RightInset / scaleAdjustment);
                //LeftPaddingColumn.Width = new GridLength(appWindow.TitleBar.LeftInset / scaleAdjustment);

                List<RectInt32> dragRectsList = new();

                RectInt32 dragRectL;
                dragRectL.X = 0;
                dragRectL.Y = 0;
                dragRectL.Height = (int)(AppTitleBar.ActualHeight * scaleAdjustment);
                dragRectL.Width = (int)(0 * scaleAdjustment);
                dragRectsList.Add(dragRectL);

                RectInt32 dragRectR;
                // TOWAIT: when microsoft fix this winui3 bug
                dragRectR.X = (int)((NavView.DisplayMode == NavigationViewDisplayMode.Minimal ? 84 * scaleAdjustment : 44 * scaleAdjustment));
                dragRectR.Y = 0;
                dragRectR.Height = (int)(AppTitleBar.ActualHeight * scaleAdjustment);
                dragRectR.Width = (int)(scaleAdjustment * AppWindowLocal.Size.Width);
                dragRectsList.Add(dragRectR);

                RectInt32[] dragRects = dragRectsList.ToArray();

                appWindow.TitleBar.SetDragRectangles(dragRects);
            }
        }
        #endregion

        #region NavView Events
        public async void OpenPlayListNavView()
        {
            await App.playListReader.Refresh();
            if (NavView.DisplayMode == NavigationViewDisplayMode.Expanded)
            {
                MusicPlayListButton.IsExpanded = true;
            }
        }

        public void UpdatePlayListButtonUI()
        {
            foreach (NavigationViewItem nvi in MusicPlayListButton.MenuItems)
            {
                nvi.Tag = null;
            }
            MusicPlayListButton.MenuItems.Clear();
            foreach (var i in App.playListReader.NowMusicListData)
            {
                var nvi = new NavigationViewItem() { Content = i.ListShowName, Tag = i };
                MusicPlayListButton.MenuItems.Add(nvi);
            }
        }

        public void UpdateNavViewContentBaseRGClip()
        {
            NavViewContentBase_RGClip.Rect = new Windows.Foundation.Rect(0, 0,
                NavViewContentBase.ActualWidth,
                AppWindowLocal.Size.Height - AppTitleBar.ActualHeight);
        }

        public static void SetNavViewContent(Type type, object param = null, NavigationTransitionInfo navigationTransitionInfo = null)
        {
            if (navigationTransitionInfo == null) navigationTransitionInfo = new EntranceNavigationTransitionInfo();
            SContentFrame.Navigate(type, param, navigationTransitionInfo);
            SNavView.IsBackEnabled = SContentFrame.CanGoBack;
            UpdateNavViewSelectedItem(true);
            /*if (type == typeof(ItemListView))
            {
                SNavView.SelectedItem = null;
            }*/
        }

        public static NavigationView SNavView;
        public static Frame SContentFrame;
        public static bool IsBackRequest = false;
        private async void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (IsBackRequest || sender.SelectedItem == null || IsJustUpdate)
            {
                return;
            }

            NavigationViewItem item = sender.SelectedItem as NavigationViewItem;
            if (item is null) return;
            
            if (item == SNavView.MenuItems[1] as NavigationViewItem)
                SetNavViewContent(typeof(SearchPage));
            else if (item == SNavView.MenuItems[2] as NavigationViewItem)
                SetNavViewContent(typeof(BrowsePage));
            else if (item == SNavView.MenuItems[3] as NavigationViewItem)
                SetNavViewContent(typeof(DownloadPage));
            else if (item == SNavView.MenuItems[5] as NavigationViewItem)
                SetNavViewContent(typeof(PlayListPage));
            else if (item == SNavView.MenuItems[6] as NavigationViewItem)
                SetNavViewContent(typeof(HistoryPage));
            else if (item == SNavView.MenuItems[7] as NavigationViewItem)
                SetNavViewContent(typeof(LocalAudioPage));
            else if (item == SNavView.FooterMenuItems[0] as NavigationViewItem)
                SetNavViewContent(typeof(AboutPage));
            else
            {
                // TOFIX: 快速切换到 PlayList 页面会导致 SelectedItem 为 null
                if (sender.SelectedItem == null)
                {
                    Debug.WriteLine("[MainWindow][NavigationSelectionChanged] SelectedItem 为 null");
                }
                else if ((sender.SelectedItem as NavigationViewItem)?.Tag.GetType() == typeof(MusicListData))
                {
                    Pages.ListViewPages.ListViewPage.SetPageToListViewPage(new() { PageType = Pages.ListViewPages.PageType.PlayList, Param = ((sender.SelectedItem as NavigationViewItem).Tag as OnlyClass).MD5 });
                }
                else if (sender.SelectedItem as NavigationViewItem == NavView.SettingsItem as NavigationViewItem)
                {
                    SetNavViewContent(typeof(SettingPage));
                }
                else
                {
                    AddNotify("未添加此功能", $"未添加 \"{(sender.SelectedItem as NavigationViewItem).Content}\" 功能。", NotifySeverity.Error);
                }
            }
            if (ContentFrame.CanGoBack) NavView.IsBackEnabled = true;
            else NavView.IsBackEnabled = false;
        }

        static bool IsJustUpdate = false;
        public static async void UpdateNavViewSelectedItem(bool justUpdate = false)
        {
            if (justUpdate) IsJustUpdate = true;
            Type type = (SContentFrame.Content as Page).GetType();
            if (type == typeof(SearchPage))
                SNavView.SelectedItem = SNavView.MenuItems[1];
            else if (type == typeof(BrowsePage))
                SNavView.SelectedItem = SNavView.MenuItems[2];
            else if (type == typeof(DownloadPage))
                SNavView.SelectedItem = SNavView.MenuItems[3];
            else if (type == typeof(PlayListPage))
                SNavView.SelectedItem = SNavView.MenuItems[5];
            else if (type == typeof(HistoryPage))
                SNavView.SelectedItem = SNavView.MenuItems[6];
            else if (type == typeof(LocalAudioPage))
                SNavView.SelectedItem = SNavView.MenuItems[7];
            else if (type == typeof(AboutPage))
                SNavView.SelectedItem = SNavView.FooterMenuItems[0];
            else if (type == typeof(SettingPage))
                SNavView.SelectedItem = SNavView.SettingsItem;
            else if (type == typeof(ItemListViewPlayList))
            {
                //TODO:优化写法
                foreach (NavigationViewItem item in (SNavView.MenuItems[5] as NavigationViewItem).MenuItems)
                {
                    if ((SContentFrame.Content as ItemListViewPlayList).NavToObj == item.Tag as MusicListData)
                    {
                        SNavView.SelectedItem = item;
                        break;
                    }
                }
            }
            else if (type == typeof(Pages.ListViewPages.PlayListPage))
            {
                foreach (NavigationViewItem item in (SNavView.MenuItems[5] as NavigationViewItem).MenuItems)
                {
                    if ((SContentFrame.Content as Pages.ListViewPages.PlayListPage).md5 == (item.Tag as MusicListData).MD5)
                    {
                        SNavView.SelectedItem = item;
                        break;
                    }
                }
            }
            else if (type == typeof(ItemListViewSearch) || type == typeof(ItemListViewArtist) || type == typeof(ItemListViewAlbum))
                SNavView.SelectedItem = null;
            IsJustUpdate = false;
        }

        public static bool TryGoBack()
        {
            if (InOpenMusicPage)
            {
                OpenOrCloseMusicPage();
                return SContentFrame.CanGoBack;
            }

            if (!SContentFrame.CanGoBack)
                return false;

            IsBackRequest = true;
            SContentFrame.GoBack();
            UpdateNavViewSelectedItem();
            SNavView.IsBackEnabled = SContentFrame.CanGoBack;

            IsBackRequest = false;
            return true;
        }

        public static void TryGoForward()
        {
            if (SContentFrame.CanGoForward)
            {
                IsBackRequest = true;
                SContentFrame.GoForward();
                UpdateNavViewSelectedItem();
                SNavView.IsBackEnabled = SContentFrame.CanGoBack;
                IsBackRequest = false;
            }
        }

        private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            TryGoBack();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            TryGoBack();
        }

        private void NavView_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
        {
            if (sender.PaneDisplayMode == NavigationViewPaneDisplayMode.Top)
            {
                AppTitleBar.Margin = new Thickness(12, 0, 0, 0);
                NavView_LocalTextItem.Margin = new(0);
                AppTitleBar.Height = 32;
                sender.Margin = new(0, 32, 0, 0);
                SetDragRegionForCustomTitleBar(MainWindow.AppWindowLocal);
                return;
            }
            else
            {
                NavView_LocalTextItem.Margin = new(0, 12, 0, 0);
                AppTitleBar.Height = 48;
                sender.Margin = new(0);
            }

            if (sender.DisplayMode == NavigationViewDisplayMode.Minimal)
            {
                AppTitleBar.Margin = new Thickness(90, 0, 0, 0);
                NavigationViewMinSizeTopColorRectangle.Visibility = Visibility.Visible;
            }
            else
            {
                AppTitleBar.Margin = new Thickness(50, 0, 0, 0);
                NavigationViewMinSizeTopColorRectangle.Visibility = Visibility.Collapsed;
            }

            SetDragRegionForCustomTitleBar(AppWindowLocal);
        }

        private void NavViewContentBase_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateNavViewContentBaseRGClip();
        }

        private void NavView_PaneOpened(NavigationView sender, object args)
        {

        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            OpenPlayListNavView();
        }
        #endregion

        #region Bottom Buttons Events
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.audioPlayer.PlaybackState == PlaybackState.Playing)
            {
                App.audioPlayer.SetPause();
            }
            else
            {
                App.audioPlayer.SetPlay();
            }
        }

        private async void PlayBeforeButton_Click(object sender, RoutedEventArgs e)
        {
            await App.playingList.PlayPrevious();
        }

        private async void PlayNextButton_Click(object sender, RoutedEventArgs e)
        {
            await App.playingList.PlayNext();
        }

        // VolumeButton
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenOrCloseVolume();
        }

        // PlayingList
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            OpenOrClosePlayingList();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            OpenOrCloseMusicPage();
        }

        public static void OpenOrCloseVolume(
            HorizontalAlignment horizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment verticalAlignment = VerticalAlignment.Bottom,
            Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode flyoutPlacementMode = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.LeftEdgeAlignedBottom,
            Thickness placementMargin = default)
        {
            if (teachingTipVolume.IsOpen)
            {
                teachingTipVolume.Hide();
                return;
            }
            STopControlsBaseGrid.HorizontalAlignment = horizontalAlignment;
            STopControlsBaseGrid.VerticalAlignment = verticalAlignment;
            STopControlsBaseGrid.Margin = placementMargin == default ? new(0, 0, 12, 96) : placementMargin;
            teachingTipVolume.LightDismissOverlayMode = LightDismissOverlayMode.Off;
            teachingTipVolume.Placement = flyoutPlacementMode;
            teachingTipVolume.ShowAt(STopControlsBaseGrid);
        }

        public static async void OpenOrClosePlayingList(
            HorizontalAlignment horizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment verticalAlignment = VerticalAlignment.Bottom,
            Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode flyoutPlacementMode = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.LeftEdgeAlignedBottom,
            Thickness placementMargin = default)
        {
            UpdatePlayListFlyoutHeight();
            STopControlsBaseGrid.HorizontalAlignment = horizontalAlignment;
            STopControlsBaseGrid.VerticalAlignment = verticalAlignment;
            STopControlsBaseGrid.Margin = placementMargin == default ? new(4, 0, 12, 96) : placementMargin;
            teachingTipPlayingList.LightDismissOverlayMode = LightDismissOverlayMode.Off;
            teachingTipPlayingList.Placement = flyoutPlacementMode;
            teachingTipPlayingList.ShowAt(STopControlsBaseGrid);
            try
            {
                await Task.Delay(10);
                await SPlayingListBaseView.SmoothScrollIntoViewWithItemAsync(App.audioPlayer.MusicData, ScrollItemPlacement.Center);
                await SPlayingListBaseView.SmoothScrollIntoViewWithItemAsync(App.audioPlayer.MusicData, ScrollItemPlacement.Center, true);
                SPlayingListBaseView.SelectedItem = App.audioPlayer.MusicData;
            }
            catch { }
        }

        static Visual musicPageVisual;
        static Vector3KeyFrameAnimation musicPageVisualClosingAnimation;
        static Vector3KeyFrameAnimation musicPageVisualOpeningAnimation;
        static void InitMusicPageVisuals()
        {
            AnimateHelper.AnimateOffset(
                SMusicPageBaseFrame,
                0, (float)SMusicPageBaseGrid.ActualHeight, 0,
                0.22,
                0.5f, 0, 0.75f, 0,
                out musicPageVisual, out Compositor compositor, out musicPageVisualClosingAnimation);

            AnimateHelper.AnimateOffset(
                SMusicPageBaseFrame,
                0, 0, 0,
                0.5,
                0.16f, 1, 0.3f, 1,
                out musicPageVisual, out Compositor compositor1, out musicPageVisualOpeningAnimation);

            musicPageVisual.Offset = new(0, (float)SMusicPageBaseGrid.ActualHeight, 0);
            SMusicPageBaseFrame.Visibility = Visibility.Visible;
        }
        public static bool InOpenMusicPage { get; set; } = false;
        static bool isFirstInMusicPage = true;
        static bool isHiddenMusicPageAnimationNotCompleted = false;
        public static void OpenOrCloseMusicPage()
        {
            if (App.audioPlayer.MusicData == null) return;
            if (musicPageVisual == null) InitMusicPageVisuals();

            SMusicPageBaseFrame.Content = SMusicPage;
            if (InOpenMusicPage)
            {
                InOpenMusicPage = false;
                isHiddenMusicPageAnimationNotCompleted = true;

                Debug.WriteLine("[MainWindow]: 主界面被显示。");
                SWindowGridBase.Visibility = Visibility.Visible;
                musicPageVisual.StartAnimation(nameof(musicPageVisual.Offset), musicPageVisualClosingAnimation);
                musicPageVisual.Compositor.GetCommitBatch(CompositionBatchTypes.Animation).Completed += (_, __) =>
                {
                    if (!InOpenMusicPage)
                    {
                        SMusicPageBaseFrame.Visibility = Visibility.Collapsed;
                        isHiddenMusicPageAnimationNotCompleted = false;
                    }
                };

                SMusicPage.MusicPageViewStateChange(MusicPageViewState.Hidden);
                MusicPageViewStateChanged?.Invoke(MusicPageViewState.Hidden);

                ConnectedAnimation canimation =
                    ConnectedAnimationService.GetForCurrentView().GetAnimation("upAnimation");
                if (canimation != null)
                {
                    canimation.TryStart(VisualTreeHelper.GetParent(SPlayContent) as UIElement);
                }
                ConnectedAnimation canimation1 =
                    ConnectedAnimationService.GetForCurrentView().GetAnimation("upAnimation1");
                if (canimation1 != null)
                {
                    canimation1.TryStart(SPlayTitle);
                }
                ConnectedAnimation canimation2 =
                    ConnectedAnimationService.GetForCurrentView().GetAnimation("upAnimation2");
                if (canimation2 != null)
                {
                    canimation2.TryStart(SPlayArtist);
                }

                if (App.lyricManager.NowPlayingLyrics.Any())
                {
                    ConnectedAnimation canimation3 =
                    ConnectedAnimationService.GetForCurrentView().GetAnimation("upAnimation3");
                    if (canimation3 != null)
                    {
                        canimation3.TryStart(SLyricTextBlock);
                    }
                }
            }
            else
            {
                InOpenMusicPage = true;

                SMusicPageBaseFrame.Visibility = Visibility.Visible;
                musicPageVisual.StartAnimation(nameof(musicPageVisual.Offset), musicPageVisualOpeningAnimation);
                musicPageVisual.Compositor.GetCommitBatch(CompositionBatchTypes.Animation).Completed += (_, __) =>
                {
                    if (InOpenMusicPage && !isHiddenMusicPageAnimationNotCompleted)
                    {
                        SWindowGridBase.Visibility = Visibility.Collapsed;
#if DEBUG
                        Debug.WriteLine("[MainWindow]: 主界面被隐藏。");
#endif
                    }
                };

                SMusicPage.MusicPageViewStateChange(MusicPageViewState.View);
                MusicPageViewStateChanged?.Invoke(MusicPageViewState.View);
            }
        }

        private static async void SetMainPageVisibility(bool visibility)
        {
            if (visibility)
            {
                SWindowGridBase.Visibility = Visibility.Visible;
                Debug.WriteLine("[MainWindow]: 主界面被显示。");
                await Task.Delay(220);
                if (!InOpenMusicPage)
                    SMusicPageBaseFrame.Visibility = Visibility.Collapsed;
            }
            else
            {
                SMusicPageBaseFrame.Visibility = Visibility.Visible;
                await Task.Delay(500);
                if (InOpenMusicPage)
                {
                    SWindowGridBase.Visibility = Visibility.Collapsed;
                    Debug.WriteLine("[MainWindow]: 主界面被隐藏。");
                }
            }
        }

        int volumePopupCounter = 0;
        private async void VolumeAppButton_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(sender as UIElement).Properties.MouseWheelDelta > 0)
                //VolumeSlider.Value += 1.1f;
                App.audioPlayer.Volume += 1f;
            else
                App.audioPlayer.Volume -= 1f;

            (VolumePopup.Content as TextBlock).Text = $"音量：{App.audioPlayer.Volume}";
            VolumePopup.ShowAt(sender as DependencyObject, new() { Placement = FlyoutPlacementMode.Top, ShowMode = FlyoutShowMode.Transient });
            volumePopupCounter++;
            await Task.Delay(3000);
            volumePopupCounter--;
            if (volumePopupCounter == 0) VolumePopup.Hide();
        }

        private void CommandBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var cb = sender as CommandBar;
            if (cb.ActualWidth <= 300 || true) cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed;
            else cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Right;
        }
        #endregion

        #region Key Events
        public delegate void InKeyDown(Windows.System.VirtualKey key);
        public static event InKeyDown InKeyDownEvent;
        public static event InKeyDown InKeyUpEvent;

        public static bool isAltDown = false;
        public static bool isControlDown = false;
        public static bool isShiftDown = false;
        public static bool CanKeyDownBack = true;
        private void Grid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case Windows.System.VirtualKey.GoBack:
                    if (CanKeyDownBack) TryGoBack();
                    break;
                case Windows.System.VirtualKey.GoForward:
                    TryGoForward();
                    break;
                case Windows.System.VirtualKey.Menu:
                    isAltDown = true;
                    break;
                case Windows.System.VirtualKey.Control:
                    isControlDown = true;
                    break;
                case Windows.System.VirtualKey.Shift:
                    isShiftDown = true;
                    break;
                case Windows.System.VirtualKey.Left:
                    if (isAltDown)
                        TryGoBack();
                    break;
                case Windows.System.VirtualKey.Right:
                    if (isAltDown)
                        TryGoForward();
                    break;
            }
            InKeyDownEvent?.Invoke(e.Key);
        }

        private void Grid_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case Windows.System.VirtualKey.Menu:
                    isAltDown = false;
                    break;
                case Windows.System.VirtualKey.Control:
                    isControlDown = false;
                    break;
                default:
                    break;
            }
            InKeyUpEvent?.Invoke(e.Key);
        }

        private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(sender as UIElement).Properties.IsXButton1Pressed)
            {
                TryGoBack();
            }
            else if (e.GetCurrentPoint(sender as UIElement).Properties.IsXButton2Pressed)
            {
                TryGoForward();
            }
        }

        private void Grid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {

        }

        public delegate void DriveInTypeDelegate(Microsoft.UI.Input.PointerDeviceType deviceType);
        public static event DriveInTypeDelegate DriveInTypeEvent;
        public static Microsoft.UI.Input.PointerDeviceType DriveInType = Microsoft.UI.Input.PointerDeviceType.Mouse;
        private void Grid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            bool a = false;

            if (e.PointerDeviceType != DriveInType)
                a = true;

            DriveInType = e.PointerDeviceType;

            if (a)
                DriveInTypeEvent?.Invoke(DriveInType);
        }
        #endregion

        #region Top Controls Events
        bool willChangeVolume = false;
        private void VolumeSlider_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            willChangeVolume = true;
        }

        private void VolumeSlider_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            willChangeVolume = false;
        }

        private void VolumeSlider_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(sender as UIElement).Properties.MouseWheelDelta > 0)
                //VolumeSlider.Value += 1.1f;
                App.audioPlayer.Volume += 1f;
            else
                App.audioPlayer.Volume -= 1f;
        }

        private void VolumeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (willChangeVolume)
            {
                App.audioPlayer.Volume = (float)(sender as Slider).Value;
            }
        }
        #endregion

        #region Volume Grid Button Events
        // 均衡器按钮点击事件
        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            OpenOrCloseVolume();
            await ShowEqualizerDialog();
        }

        public static float NoVolumeValue = 0;
        // 静音按钮点击事件
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if (App.audioPlayer.Volume != 0)
            {
                NoVolumeValue = App.audioPlayer.Volume;
                App.audioPlayer.Volume = 0;
            }
            else
            {
                App.audioPlayer.Volume = NoVolumeValue;
            }
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            App.playingList.NowPlayingList.Remove((sender as Button).DataContext as MusicData);
        }
        #endregion

        #region PlayingListView Events
        bool inSelectionChange = false;
        private async void PlayingListBaseView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            inSelectionChange = true;
            if (PlayingListBaseView.SelectedItem != null)
            {
                MusicData data = (MusicData)PlayingListBaseView.SelectedItem;
                if (App.audioPlayer.MusicData != data)
                    await App.playingList.Play(data);
            }
            inSelectionChange = false;
        }

        private async void Button_Click_5(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;

            if (btn.Content as string == "定位")
            {
                if (SPlayingListBaseView.Items.Contains(App.audioPlayer.MusicData))
                {
                    await SPlayingListBaseView.SmoothScrollIntoViewWithItemAsync(App.audioPlayer.MusicData, ScrollItemPlacement.Center);
                    await SPlayingListBaseView.SmoothScrollIntoViewWithItemAsync(App.audioPlayer.MusicData, ScrollItemPlacement.Center, true);
                }
            }
            else
                App.playingList.ClearAll();
        }

        public void UpdatePlayingListShyHeader()
        {
            /*
            // 设置header为顶层
            var headerPresenter = (UIElement)VisualTreeHelper.GetParent((UIElement)PlayingListBaseView.Header);
            var headerContainer = (UIElement)VisualTreeHelper.GetParent(headerPresenter);
            Canvas.SetZIndex(headerContainer, 1);

            var scrollViewer = (VisualTreeHelper.GetChild(PlayingListBaseView, 0) as Border).Child as ScrollViewer;

            CompositionPropertySet scrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
            Compositor compositor = scrollerPropertySet.Compositor;

            var padingSize = 40;
            // Get the visual that represents our HeaderTextBlock 
            // And define the progress animation string
            var headerVisual = ElementCompositionPreview.GetElementVisual(PlayingListBaseGrid1);
            String progress = $"Clamp(-scroller.Translation.Y / {padingSize}, 0, 1.0)";

            // Shift the header by 50 pixels when scrolling down
            var offsetExpression = compositor.CreateExpressionAnimation($"-scroller.Translation.Y - {progress} * {padingSize}");
            offsetExpression.SetReferenceParameter("scroller", scrollerPropertySet);
            headerVisual.StartAnimation("Offset.Y", offsetExpression);

            
            Visual textVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseTextBlock);
            Vector3 finalOffset = new Vector3(0, 10, 0);
            var headerOffsetAnimation = compositor.CreateExpressionAnimation($"Lerp(Vector3(0,0,0), finalOffset, {progress})");
            headerOffsetAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            headerOffsetAnimation.SetVector3Parameter("finalOffset", finalOffset);
            textVisual.StartAnimation(nameof(Visual.Offset), headerOffsetAnimation);
            

            // Logo scale and transform                                          from               to
            var logoHeaderScaleAnimation = compositor.CreateExpressionAnimation("Lerp(Vector2(1,1), Vector2(0.7, 0.7), " + progress + ")");
            logoHeaderScaleAnimation.SetReferenceParameter("scroller", scrollerPropertySet);

            var logoVisual = ElementCompositionPreview.GetElementVisual(PlayingListBaseTextBlock);
            logoVisual.StartAnimation("Scale.xy", logoHeaderScaleAnimation);

            var logoVisualOffsetYAnimation = compositor.CreateExpressionAnimation($"Lerp(0, 24, {progress})");
            logoVisualOffsetYAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            logoVisual.StartAnimation("Offset.Y", logoVisualOffsetYAnimation);

            var logoVisualOffsetXAnimation = compositor.CreateExpressionAnimation($"Lerp(0, -12, {progress})");
            logoVisualOffsetXAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            logoVisual.StartAnimation("Offset.X", logoVisualOffsetXAnimation);
            
            var stackVisual = ElementCompositionPreview.GetElementVisual(PlayingListBaseStackPanel);
            var stackVisualOffsetXAnimation = compositor.CreateExpressionAnimation($"Lerp(144, 330, {progress})");
            stackVisualOffsetXAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            stackVisual.StartAnimation("Offset.X", stackVisualOffsetXAnimation);

            var stackVisualOffsetYAnimation = compositor.CreateExpressionAnimation($"Lerp(12, 20, {progress})");
            stackVisualOffsetYAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            stackVisual.StartAnimation("Offset.Y", stackVisualOffsetYAnimation);

            var backgroundVisual = ElementCompositionPreview.GetElementVisual(PlayingListBaseRectangle);
            var backgroundVisualOpacityAnimation = compositor.CreateExpressionAnimation($"Lerp(0, 1, {progress})");
            backgroundVisualOpacityAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            backgroundVisual.StartAnimation("Opacity", backgroundVisualOpacityAnimation);
            */
        }
        #endregion

        #region Desktop Lyric Window Events
        public delegate void DesktopLyricDelegate();
        public static event DesktopLyricDelegate DesktopLyricWindowOpenedEvent;
        public static event DesktopLyricDelegate DesktopLyricWindowClosedEvent;
        public static bool IsDesktopLyricWindowOpen = false;
        public static DesktopLyricWindow DesktopLyricWindow = null;
        static bool isInChangingLyricWindow = false;

        public static async void OpenDesktopLyricWindow(bool timeDelay = true)
        {
            if (!isInChangingLyricWindow)
            {
                isInChangingLyricWindow = true;
                IsDesktopLyricWindowOpen = true;
                if (DesktopLyricWindow == null)
                {
                    DesktopLyricWindow = new();
                    DesktopLyricWindow.Closed += DesktopLyricWindow_Closed;

                    DesktopLyricWindow.AppWindow.Show(false);
                    DesktopLyricWindow.Activate();
                    DesktopLyricWindowOpenedEvent?.Invoke();
                }
                else
                {
                    DesktopLyricWindow.Closed -= DesktopLyricWindow_Closed;
                    DesktopLyricWindow.RemoveEvents();
                    DesktopLyricWindow.Close();
                    DesktopLyricWindow_Closed(null, null);
                    DesktopLyricWindow = null;
                }
                if (timeDelay)
                    await Task.Delay(400);
                isInChangingLyricWindow = false;
            }
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            OpenDesktopLyricWindow();
        }

        private static void DesktopLyricWindow_Closed(object sender, WindowEventArgs args)
        {
            DesktopLyricWindow = null;
            IsDesktopLyricWindowOpen = false;
            DesktopLyricWindowClosedEvent?.Invoke();
        }
        #endregion

        #region PlayTimePopup
        private void PlayButton_Holding(object sender, HoldingRoutedEventArgs e)
        {
            InitPlayTimePopup();
        }

        private void PlayButton_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            InitPlayTimePopup();
        }

        private void InitPlayTimePopup()
        {
            PlayTimeSliderBasePopup.IsOpen = !PlayTimeSliderBasePopup.IsOpen;
            PlayTimeSliderBasePopup.IsLightDismissEnabled = true;
            PlayTimeSliderBasePopup.Closed += PlayTimeSliderBasePopup_Closed;
            App.audioPlayer.TimingChanged += AudioPlayer_TimingChanged1;
            PlayTimeSlider.ValueChanged += PlayTimeSlider_ValueChanged;
        }

        private void PlayTimeSliderBasePopup_Closed(TeachingTip sender, TeachingTipClosedEventArgs args)
        {
            PlayTimeSliderBasePopup.Closed -= PlayTimeSliderBasePopup_Closed;
            App.audioPlayer.TimingChanged -= AudioPlayer_TimingChanged1;
            PlayTimeSlider.ValueChanged -= PlayTimeSlider_ValueChanged;
        }

        private void AudioPlayer_TimingChanged1(Media.AudioPlayer audioPlayer)
        {
            if (audioPlayer.FileReader != null)
            {
                isCodeChangedSilderValue = true;
                PlayTimeSlider.Minimum = 0;
                PlayTimeSlider.Maximum = audioPlayer.TotalTime.Ticks;
                PlayTimeSlider.Value = audioPlayer.CurrentTime.Ticks;
                isCodeChangedSilderValue = false;

                PlayTimeTextBlock.Text =
                        $"{audioPlayer.CurrentTime:mm\\:ss}/{audioPlayer.TotalTime:mm\\:ss}";
            }
        }

        bool isCodeChangedSilderValue = false;
        private void PlayTimeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!isCodeChangedSilderValue && App.audioPlayer.FileReader != null)
            {
                App.audioPlayer.CurrentTime = TimeSpan.FromTicks((long)PlayTimeSlider.Value);
            }
        }
        #endregion

        #region Other
        private void DebugViewPopup_SizeChanged(object sender, SizeChangedEventArgs e)
        {
#if DEBUG
            DebugViewPopup.VerticalOffset = AppWindow.Size.Height;
#endif
        }

        DispatcherTimer ramTimer;
        private void DebugViewPopup_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            DebugViewPopup.VerticalOffset = AppWindow.Size.Height;
            ramTimer = new();
            ramTimer.Tick += RamTimer_Tick;
            ramTimer.Interval = TimeSpan.FromSeconds(0.5);
            ramTimer.Start();
#endif
        }

        private void DebugViewPopup_Unloaded(object sender, RoutedEventArgs e)
        {

#if DEBUG
            ramTimer.Stop();
            ramTimer.Tick -= RamTimer_Tick;
            ramTimer = null;
#endif
        }

        private void RamTimer_Tick(object sender, object e)
        {
            try
            {
                DebugView_Detail_RAM.Text = $"RAM: {CodeHelper.GetAutoSizeString(Windows.System.MemoryManager.AppMemoryUsage, 2)}/{CodeHelper.GetAutoSizeString(GC.GetTotalMemory(false), 2)}";
            }
            catch { }
        }

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            GC.Collect();
        }

        ObservableCollection<string> oc = new();
        private void ItemsView_Loaded(object sender, RoutedEventArgs e)
        {
            var iv = sender as ItemsView;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                oc.Add(a.GetName().Name);
            }
            iv.ItemsSource = oc;
        }

        private void ItemsView_Unloaded(object sender, RoutedEventArgs e)
        {
            var iv = sender as ItemsView;
            iv.ItemsSource = null;
            oc.Clear();
        }

        public static bool AllowDragEvents = true;
        private void WindowGridBase_DragOver(object sender, DragEventArgs e)
        {
            if (!AllowDragEvents) return;
            DropInfo_Root.Opacity = 1;
            e.AcceptedOperation = DataPackageOperation.Link;
            e.DragUIOverride.Caption = "打开";
        }

        private async void WindowGridBase_Drop(object sender, DragEventArgs e)
        {
            if (!AllowDragEvents) return;
            DropInfo_Root.Opacity = 0;
            if (!e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                AddNotify("无法打开此文件类型", "仅允许打开文件。", NotifySeverity.Error);
                return;
            }
            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count <= 0) return;

            List<string> files = new();
            foreach (StorageFile file in items)
            {
                files.Add(file.Path);
            }
            AddOpeningMusic([.. files]);
        }

        private void WindowGridBase_DragLeave(object sender, DragEventArgs e)
        {
            DropInfo_Root.Opacity = 0;
        }

        private void Button_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            MusicDataFlyout.SongItemBind = new() { MusicData = App.audioPlayer.MusicData };
            MusicDataFlyout.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));
        }

        private void Button_Holding(object sender, HoldingRoutedEventArgs e)
        {
            MusicDataFlyout.SongItemBind = new() { MusicData = App.audioPlayer.MusicData };
            MusicDataFlyout.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));
        }
        #endregion
    }

    public enum NotifySeverity { Info, Error, Warning, Complete, Loading }
    /// <summary>
    /// 显示通知的数据类型
    /// </summary>
    public class NotifyItemData
    {
        /// <summary>
        /// 通知标题
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 通知信息
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// 通知类型
        /// </summary>
        public NotifySeverity Severity { get; set; }
        /// <summary>
        /// 通知滞留时间，默认5秒
        /// </summary>
        public TimeSpan ResidenceTime { get; set; } = TimeSpan.FromSeconds(5);

        public NotifyItemData(
            string title,
            string message,
            NotifySeverity severity = NotifySeverity.Info,
            TimeSpan? residenceTime = null)
        {
            Title = title;
            Message = message;
            Severity = severity;
            ResidenceTime = residenceTime == null ? TimeSpan.FromSeconds(5) : (TimeSpan)residenceTime;
        }
    }
}
