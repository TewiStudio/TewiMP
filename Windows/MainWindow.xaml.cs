using WinRT;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
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
using Windows.UI;
using Windows.Storage;
using Windows.Graphics;
using Windows.ApplicationModel.DataTransfer;
using NAudio.Wave;
using Newtonsoft.Json.Linq;
using CommunityToolkit.WinUI;
using WinUIEx;
using TewiMP.Pages;
using TewiMP.Pages.MusicPages;
using TewiMP.Helpers;
using TewiMP.Controls;
using TewiMP.Windowed;
using TewiMP.DataEditor;
using TewiMP.Background;
using TewiMP.Background.HotKeys;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TewiMP
{
    public enum BackdropType
    {
        Mica,
        MicaAlt,
        DesktopAcrylic,
        Blur,
        Transparent,
        Image,
        DefaultColor
    }

    public sealed partial class MainWindow : WindowEx
    {
        public bool RunInBackground = false;
        public string ImagePath = null;

        public IntPtr Handle;
        public BackdropType CurrentBackdrop;
        public MusicPage SMusicPage = new();
        public ScrollViewer PlayingListBaseViewScrollViewer;
        public  ContentDialog AsyncDialog = null;
        public double NowDPI
        {
            get
            {
                return this.GetDpiForWindow() / 100.0;
            }
        }
        public void InvokeDpiEvent()
        {
            WindowDpiChanged?.Invoke(NowDPI);
        }
        public delegate void WindowDpiChangedDelegate(double newDPI);
        public event WindowDpiChangedDelegate WindowDpiChanged;
        
        public delegate void WindowViewStateChangedDelegate(bool isView);
        public event WindowViewStateChangedDelegate WindowViewStateChanged;

        public delegate void MainViewStateChangedDelegate(bool isView);
        public event MainViewStateChangedDelegate MainViewStateChanged;

        public delegate void MusicPageViewStateChangedDelegate(MusicPageViewState musicPageViewState);
        public event MusicPageViewStateChangedDelegate MusicPageViewStateChanged;

        public MainWindow()
        {
            LogManager.Info("Staring", "初始化 MainWindow.");
            InitializeComponent();

            Handle = this.GetWindowHandle();
            WindowGridBase.DataContext = this;

            Activated += MainWindow_Activated;
            WindowGridBase.ActualThemeChanged += WindowGridBase_ActualThemeChanged;
            MusicPageViewStateChanged += MainWindow_MusicPageViewStateChanged;
            AppWindow.Closing += AppWindow_Closing;
            loadingst.Children.Add(loadingprogress);
            loadingst.Children.Add(loadingtextBlock);

            InitDialog();
            equalizerPage = new Pages.DialogPages.EqualizerPage();
            //SubClassing();

            AppWindow.Title = App.Instance.AppName;
            AppWindow.SetIcon(System.IO.Path.Combine("Images", "Icons", "icon.ico"));

            InitializeTitleBar(WindowGridBase.RequestedTheme);
            SetDragRegionForCustomTitleBar();

            LogManager.Info("MainWindow", "Inited");
        }

        internal static SystemBackdropConfiguration systemBackdropConfiguration;
        internal static DesktopAcrylicController desktopAcrylicController;
        internal static MicaBackdrop micaBackdrop = new();
        internal static MicaBackdrop micaAltBackdrop = new() { Kind = MicaKind.BaseAlt };
        private static BlurredBackdrop blurBackdrop = new();
        private static TransparentTintBackdrop transparentTintBackdrop = new();
        public void SetBackdrop(BackdropType backdropType)
        {
            desktopAcrylicController?.Dispose();
            SystemBackdrop = null;
            CurrentBackdrop = backdropType;
            BackgroundImageRoot.Visibility = Visibility.Collapsed;
            BackgroundColor.Visibility = Visibility.Collapsed;
            BackgroundMass.Visibility = Visibility.Collapsed;
            switch (backdropType)
            {
                case BackdropType.Mica: SystemBackdrop = micaBackdrop; break;
                case BackdropType.MicaAlt: SystemBackdrop = micaAltBackdrop; break;
                case BackdropType.DesktopAcrylic:
                    ElementTheme elementTheme = WindowGridBase.RequestedTheme;
                    if (elementTheme == ElementTheme.Default)
                    {
                        elementTheme = App.Current.RequestedTheme == ApplicationTheme.Light ? ElementTheme.Light : ElementTheme.Dark;
                    }
                    systemBackdropConfiguration = new();
                    desktopAcrylicController = new()
                    {
                        LuminosityOpacity = 1f,
                        TintOpacity = .5f,
                        TintColor = elementTheme == ElementTheme.Dark ?
                            Color.FromArgb(255, 32, 32, 32) :
                            Color.FromArgb(255, 245, 245, 245)
                    };
                    SystemBackdrop = null;
                    desktopAcrylicController.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
                    desktopAcrylicController.SetSystemBackdropConfiguration(systemBackdropConfiguration);
                    break;
                case BackdropType.Blur:
                    SystemBackdrop = blurBackdrop;
                    BackgroundMass.Visibility = Visibility.Visible;
                    break;
                case BackdropType.Transparent:
                    SystemBackdrop = transparentTintBackdrop;
                    BackgroundMass.Visibility = Visibility.Visible;
                    break;
                case BackdropType.Image:
                    BackgroundImageRoot.Visibility = Visibility.Visible;
                    BackgroundColor.Visibility = Visibility.Visible;
                    BackgroundMass.Visibility = Visibility.Visible;
                    if (ImagePath is not null) BackgroundImage.Source = FileHelper.GetImageSource(new Uri(ImagePath));
                    SystemBackdrop = null;
                    break;
                case BackdropType.DefaultColor:
                    BackgroundColor.Visibility = Visibility.Visible;
                    SystemBackdrop = null;
                    break;
                default: SystemBackdrop = micaBackdrop; break;
            }
        }

        bool isBackground = false;
        static bool isShowClosingDialog = false;
        public void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            LogManager.Log("MainWindow", "Closing...");
            App.Instance.SaveSettings();
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
            App.Instance.ExitApp();
            isShowClosingDialog = false;
        }

        public void StaringPrepare()
        {
            var displayArea = CodeHelper.GetDisplayArea(this);
            var dpi = CodeHelper.GetScaleAdjustment(this);

            bool isPreparedActivate = true;
            var windowWidth = (int)(1140 * dpi);
            var windowHeight = (int)(640 * dpi);
            var windowPositionX = displayArea.WorkArea.Width / 2 - windowWidth / 2;
            var windowPositionY = displayArea.WorkArea.Height / 2 - windowHeight / 2;

            if (App.Instance.LaunchArgs != null)
            {
                //AddNotify("Args", string.Join(" ||| ", App.Instance.LaunchArgs), NotifySeverity.Warning, TimeSpan.FromSeconds(10));
                List<string> openFiles = [];
                foreach (var str in App.Instance.LaunchArgs)
                {
                    if (str.Contains(@":\"))
                    {
                        openFiles.Add(str);
                    }
                }
                foreach (var str in openFiles)
                {
                    if (App.Instance.LaunchArgs.Contains(str))
                    {
                        App.Instance.LaunchArgs.Remove(str);
                    }
                }
                AddOpeningMusic(openFiles.ToArray());

                var lagsString = string.Join(" ", App.Instance.LaunchArgs);
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
                lags = [.. list];

                List<string> unknowArg = [];
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
                if (unknowArg.Count != 0)
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
                if (isPreparedActivate)
                    this.Maximize();
            }
            else
            {
                AppWindow.MoveAndResize(new(
                    windowPositionX, windowPositionY,
                    windowWidth, windowHeight));
            }

            if (isPreparedActivate) Activate();

            List<string> hotKeyUsed = new();
            foreach (var hotKey in App.Instance.hotKeyManager.RegisteredHotKeys)
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
            if (fileName.Length == 0) return;
            isOpeningMusicLoaded = true;

            List<MusicData> mlist = new();
            foreach (string str in fileName)
            {
                foreach (var musicData in await MusicData.FromFile(str))
                {
                    App.Instance.playingList.Add(musicData); mlist.Add(musicData);
                }
            }
            if (mlist.Count > 0)
            {
                await App.Instance.playingList.Play(mlist.First());
            }
        }

        public async Task SaveNowPlaying()
        {
            if (App.Instance.audioPlayer.MusicData is null) return;

            var path = System.IO.Path.Combine(DataFolderBase.UserDataFolder, "LastPlaying");
            if (!App.Instance.LoadLastExitPlayingSongAndSongList)
            {
                await Task.Run(() => System.IO.File.Delete(path));
                return;
            }

            if (!await Task.Run(() => System.IO.File.Exists(path))) await Task.Run(() => System.IO.File.Create(path).Close());

            JObject jObject = null;
            await Task.Run(() =>
            {
                JArray array = new JArray();
                foreach (var a in App.Instance.playingList.PlayBehavior == PlayBehavior.随机播放 ? App.Instance.playingList.RandomSavePlayingList : App.Instance.playingList.NowPlayingList)
                    array.Add(JObject.FromObject(a));
                jObject = new JObject() {
                    { "music", JObject.FromObject(App.Instance.audioPlayer.MusicData) },
                    { "list", array }
                };
            });
            if (jObject is null) return;
            await System.IO.File.WriteAllTextAsync(path, jObject.ToString());
            LogManager.Log("SaveNowPlaying", "正在播放列表已保存！");
        }

        public async void LoadLastPlaying()
        {
            if (!App.Instance.LoadLastExitPlayingSongAndSongList) return;
            if (isOpeningMusicLoaded) return;

            var path = System.IO.Path.Combine(DataFolderBase.UserDataFolder, "LastPlaying");
            if (!System.IO.File.Exists(path)) return;

            MusicData musicData = null;
            JObject jobject = null;
            await Task.Run(() =>
            {
                var texts = System.IO.File.ReadAllText(path);
                jobject = JObject.Parse(texts);
                musicData = JsonNewtonsoft.FromJSON<MusicData>(jobject["music"].ToString());
            });
            foreach (var m in jobject["list"])
            {
                var md = JsonNewtonsoft.FromJSON<MusicData>(m.ToString());
                App.Instance.playingList.NowPlayingList.Add(md);
            }

            if (musicData is null) return;
            if (App.Instance.playingList.PlayBehavior == PlayBehavior.随机播放)
            {
                App.Instance.playingList.SetRandomPlay(PlayBehavior.随机播放);
            }
            await App.Instance.playingList.Play(musicData, false);
        }

        #region Window Events
        private void WindowGridBase_Loaded(object sender, RoutedEventArgs e)
        {
            SetBackdrop(CurrentBackdrop);
            App.Instance.SMTC.ButtonPressed += SMTC_ButtonPressed;
            App.Instance.playListReader.Updated += UpdatePlayListButtonUI;
            App.Instance.audioPlayer.VolumeChanged += AudioPlayer_VolumeChanged;
            PlayingListScrollControl.PositionToNowPlaying_Button.Click += async (_, __) =>
            {
                if (PlayingListBaseView.Items.Contains(App.Instance.audioPlayer.MusicData))
                {
                    await PlayingListBaseView.SmoothScrollIntoViewWithItemAsync(App.Instance.audioPlayer.MusicData, ScrollItemPlacement.Center);
                    await PlayingListBaseView.SmoothScrollIntoViewWithItemAsync(App.Instance.audioPlayer.MusicData, ScrollItemPlacement.Center, true);
                }
            };
            PlayingListScrollControl.PositionToTop_Button.Click += (_, __) =>
            {
                if (PlayingListBaseViewScrollViewer is null)
                    PlayingListBaseViewScrollViewer = (VisualTreeHelper.GetChild(PlayingListBaseView, 0) as Border).Child as ScrollViewer;
                PlayingListBaseViewScrollViewer.ChangeView(null, 0, null);
            };
            PlayingListScrollControl.PositionToBottom_Button.Click += (_, __) =>
            {
                if (PlayingListBaseViewScrollViewer is null)
                    PlayingListBaseViewScrollViewer = (VisualTreeHelper.GetChild(PlayingListBaseView, 0) as Border).Child as ScrollViewer;
                PlayingListBaseViewScrollViewer.ChangeView(null, PlayingListBaseViewScrollViewer.ScrollableHeight, null);
            };

            Canvas.SetZIndex(AppTitleBar, 1);

            StaringPrepare();
            LoadLastPlaying();
            //NotifyListView.ItemsSource = NotifyList;
            //PlayingListBasePopup.SystemBackdrop = new DesktopAcrylicBackdrop();
            //VolumeBasePopup.SystemBackdrop = new DesktopAcrylicBackdrop();

            NavView.SelectedItem = NavView.MenuItems[1];
            NavView.IsBackEnabled = false;

            PlayingListBaseView.ItemsSource = App.Instance.playingList.NowPlayingList;
            //SystemNavigationManager.GetForCurrentView().BackRequested += (_, __) => { TryGoBack(); };
#if DEBUG
            DebugViewPopup.XamlRoot = WindowGridBase.XamlRoot;
            DebugViewPopup.IsOpen = true;
#endif
        }

        private void WindowGridBase_ActualThemeChanged(FrameworkElement sender, object args)
        {
            if (CurrentBackdrop == BackdropType.DesktopAcrylic)
            {
                SetBackdrop(CurrentBackdrop);
            }
            InitializeTitleBar(WindowGridBase.RequestedTheme);
        }

        private void UpdateWhenDataLated()
        {
            AudioPlayer_SourceChanged(App.Instance.audioPlayer);
            AudioPlayer_PlayStateChanged(App.Instance.audioPlayer);
            AudioPlayer_CacheLoadedChanged(App.Instance.audioPlayer);
            AudioPlayer_TimingChanged(App.Instance.audioPlayer);
            AudioPlayer_VolumeChanged(App.Instance.audioPlayer, App.Instance.audioPlayer.Volume);
            PlayingList_NowPlayingImageLoaded(App.Instance.playingList.NowPlayingImage, null);
            LyricManager_PlayingLyricSelectedChange(App.Instance.lyricManager.NowLyricsData);
            PlayingList_PlayingListItemChange(App.Instance.playingList.NowPlayingList);
            UpdataDownloadPageButtonInfoBadgeText();
            App.Instance.audioPlayer.ReCallTiming();
            LogManager.Log("MainWindow", "Data Updated.");
        }

        bool isFirstWindowActivity = true;
        bool isAddEvents = false;
        private void AddEvents()
        {
            //isFirstWindowActivity = true;
            if (isFirstWindowActivity)
            {
                isFirstWindowActivity = false;
                App.Instance.CheckUpdate();
            }
            if (isAddEvents) return;
            //AutoScrollViewerFirst.Pause = false;
            //AutoScrollViewerSecond.Pause = false;
            App.Instance.audioPlayer.SourceChanged += AudioPlayer_SourceChanged;
            App.Instance.audioPlayer.PlayEnd += AudioPlayer_PlayEnd;
            App.Instance.audioPlayer.PlayStateChanged += AudioPlayer_PlayStateChanged;
            App.Instance.audioPlayer.TimingChanged += AudioPlayer_TimingChanged;
            App.Instance.audioPlayer.CacheLoadedChanged += AudioPlayer_CacheLoadedChanged;
            App.Instance.audioPlayer.CacheLoadingChanged += AudioPlayer_CacheLoadingChanged;
            App.Instance.playingList.NowPlayingImageLoading += PlayingList_NowPlayingImageLoading;
            App.Instance.playingList.NowPlayingImageLoaded += PlayingList_NowPlayingImageLoaded;
            App.Instance.playingList.PlayingListItemChange += PlayingList_PlayingListItemChange;

            App.Instance.downloadManager.AddDownload += DownloadManager_AddDownload;
            App.Instance.downloadManager.OnDownloadedSaving += DownloadManager_AddDownload;
            App.Instance.downloadManager.OnDownloadedPreview += DownloadManager_AddDownload;
            App.Instance.downloadManager.OnDownloaded += DownloadManager_AddDownload;
            App.Instance.downloadManager.OnDownloadError += DownloadManager_AddDownload;

            isAddEvents = true;
            UpdateWhenDataLated();
            MainViewStateChanged?.Invoke(true);
            LogManager.Log("MainWindow", "Added Events.");
        }

        private void RemoveEvents()
        {
            //AutoScrollViewerFirst.Pause = true;
            //AutoScrollViewerSecond.Pause = true;
            App.Instance.audioPlayer.SourceChanged -= AudioPlayer_SourceChanged;
            App.Instance.audioPlayer.PlayEnd -= AudioPlayer_PlayEnd;
            App.Instance.audioPlayer.PlayStateChanged -= AudioPlayer_PlayStateChanged;
            App.Instance.audioPlayer.TimingChanged -= AudioPlayer_TimingChanged;
            App.Instance.audioPlayer.CacheLoadedChanged -= AudioPlayer_CacheLoadedChanged;
            App.Instance.audioPlayer.CacheLoadingChanged -= AudioPlayer_CacheLoadingChanged;
            App.Instance.playingList.NowPlayingImageLoading -= PlayingList_NowPlayingImageLoading;
            App.Instance.playingList.NowPlayingImageLoaded -= PlayingList_NowPlayingImageLoaded;
            App.Instance.lyricManager.PlayingLyricSelectedChanged -= LyricManager_PlayingLyricSelectedChange;
            App.Instance.playingList.PlayingListItemChange -= PlayingList_PlayingListItemChange;

            App.Instance.downloadManager.AddDownload -= DownloadManager_AddDownload;
            App.Instance.downloadManager.OnDownloadedSaving -= DownloadManager_AddDownload;
            App.Instance.downloadManager.OnDownloadedPreview -= DownloadManager_AddDownload;
            App.Instance.downloadManager.OnDownloaded -= DownloadManager_AddDownload;
            App.Instance.downloadManager.OnDownloadError -= DownloadManager_AddDownload;

            isAddEvents = false;
            MainViewStateChanged?.Invoke(false);
            LogManager.Log("MainWindow", "Removed Events.");
        }

        public bool isMinSize = false;
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
                    App.Instance.SaveSettings();

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
            SetDragRegionForCustomTitleBar();
        }

        private async void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetDragRegionForCustomTitleBar();
#if DEBUG
            DebugView_Detail_WindowSizeRun.Text = $"{WindowGridBase.ActualWidth}x{WindowGridBase.ActualHeight}";
            DebugViewPopup.VerticalOffset = AppWindow.Size.Height;
#endif
            //NotifyListView.Padding = new(0, GridBase.ActualHeight, 0, 12);
            /*
            if (NotifyList.Any())
            {
                await Task.Delay(1);
                SNotifyListViewScrollViewer.ChangeView(null, SNotifyListViewScrollViewer.ScrollableHeight, null, true);
            }*/
        }

        private void NotifyArea_Loaded(object sender, RoutedEventArgs e)
        {
            //SNotifyListViewScrollViewer = (VisualTreeHelper.GetChild(NotifyListView, 0) as Border).Child as ScrollViewer;
            //AddNotify("测试版本", "此应用程序是一份内测版本。", NotifySeverity.Warning, TimeSpan.MaxValue);
        }
        
        public void UpdatePlayListFlyoutHeight()
        {
            try
            {
                if (InOpenMusicPage)
                    PlayingListBaseGrid.Height = WindowGridBase.ActualHeight - 130;
                else
                    PlayingListBaseGrid.Height = WindowGridBase.ActualHeight - 155;
            }
            catch { }
        }
        #endregion

        #region init TitleBar
        public void InitializeTitleBar(ElementTheme theme)
        {
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                InitializeTitleBar(AppWindow.TitleBar, theme);
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

        public static void InitializeTitleBar(AppWindowTitleBar bar, ElementTheme theme)
        {
            bar.ExtendsContentIntoTitleBar = true;

            bool defaultLightTheme = false;
            bool defaultDarkTheme = false;
            bar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
            if (theme == ElementTheme.Default)
            {
                defaultLightTheme = App.Current.RequestedTheme == ApplicationTheme.Light;
                defaultDarkTheme = App.Current.RequestedTheme == ApplicationTheme.Dark;
            }
            bar.PreferredHeightOption = TitleBarHeightOption.Standard;

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
        async void InitDialog()
        {
            AsyncDialog = new ContentDialog()
            {
                XamlRoot = Content.XamlRoot,
                CloseButtonCommand = null
            };
            //ShowDialog("Initing Dialog...", "");
            HideDialog();
        }

        bool isFirstDialogShow = true;
        ScrollViewer dialogScrollViewer = new() { HorizontalScrollMode = ScrollMode.Disabled };
        bool dialogShow = false;
        List<object[]> dialogShowObjects = new();
        public async Task<ContentDialogResult> ShowDialog(
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
                    AsyncDialog.XamlRoot = Content.XamlRoot;
                    AsyncDialog.RequestedTheme = WindowGridBase.RequestedTheme;
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
                    dialogShowObjects.Add([title, content, closeButtonText, primaryButtonText, secondaryButtonText, defaultButton, fullSizeDesired]);
                }
                return result;
            }
            catch
            {
                return ContentDialogResult.None;
            }
        }

        Pages.DialogPages.EqualizerPage equalizerPage { get; set; }
        public async Task ShowEqualizerDialog()
        {
            await ShowDialog("音频设置", new Pages.DialogPages.EqualizerPage());
        }

        StackPanel loadingst = new();
        ProgressRing loadingprogress = new() { IsIndeterminate = true, Width = 50, Height = 50 };
        TextBlock loadingtextBlock = new() { Text = "", HorizontalAlignment = HorizontalAlignment.Center, TextWrapping = TextWrapping.Wrap };
        public async void ShowLoadingDialog(string title = "正在加载")
        {
            SetLoadingText("");
            SetLoadingProgressRingValue(100, 0);
            await ShowDialog(title, loadingst, "后台", null);
        }

        public void SetLoadingText(string text)
        {
            loadingtextBlock.Text = text;
        }

        public void SetLoadingProgressRingValue(int maximum, int value)
        {
            if (value == 0) loadingprogress.IsIndeterminate = true;
            else loadingprogress.IsIndeterminate = false;
            loadingprogress.Maximum = maximum;
            loadingprogress.Value = value;
        }

        public void HideDialog()
        {
            AsyncDialog.Hide();
        }

        public NotifyItem AddNotify(string title, string message, NotifySeverity severity = NotifySeverity.Info, TimeSpan? residenceTime = null, string buttonMessage = null, Action buttonAction = null)
        {
            return AddNotify(new(title, message, severity, residenceTime, buttonMessage, buttonAction));
        }

        public NotifyItem AddNotify(NotifyItemData notifyItemData)
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
            NotifyStackPanel.Children.Add(notifyItem);
            if (notifyItemData.ResidenceTime != TimeSpan.MaxValue)
                NotifyCountDown(notifyItem);
            return notifyItem;
        }

        public void RemoveNotifyItem(NotifyItem notifyItem)
        {
            if (NotifyStackPanel.Children.Contains(notifyItem))
                NotifyStackPanel.Children.Remove(notifyItem);
        }

        public void NotifyCountDown(NotifyItem notifyItem)
        {
            NotifyCountDown(notifyItem.GetNotifyItemData(), notifyItem);
        }

        public async void NotifyCountDown(NotifyItemData notifyItemData, NotifyItem notifyItem)
        {
            if (notifyItemData.ResidenceTime == TimeSpan.MaxValue) return;
            await Task.Delay(notifyItemData.ResidenceTime);
            NotifyStackPanel.Children.Remove(notifyItem);
        }
        #endregion

        #region AudioPlayer Events
        private void DownloadManager_AddDownload(Background.DownloadData data)
        {
            UpdataDownloadPageButtonInfoBadgeText();
        }

        private void UpdataDownloadPageButtonInfoBadgeText()
        {
            if (App.Instance.downloadManager.AllDownloadData.Any())
            {
                if (App.Instance.downloadManager.DownloadingData.Any())
                {
                    DownloadPageButtonInfoBadge.Opacity = 1;
                    DownloadPageButtonInfoBadge.Value = App.Instance.downloadManager.AllDownloadData.Count - App.Instance.downloadManager.DownloadedData.Count;
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
            AppTitleTextBlock.Text = $"{App.Instance.AppName}";
            LyricTextBlock.Text = null;
        }

        private void LyricManager_PlayingLyricSelectedChange(LyricData _)
        {
            try
            {
                if (_ is null) { SetLyricToNormal(); return; }
                if (_.Lyric is null) { SetLyricToNormal(); return; }
                if (!_.Lyric.Any()) { SetLyricToNormal(); return; }

                int tcount = 1;
                int num = App.Instance.lyricManager.NowPlayingLyrics.IndexOf(_);
                try
                {
                    while (_?.Lyric?.FirstOrDefault() == App.Instance.lyricManager.NowPlayingLyrics[num + tcount]?.Lyric?.FirstOrDefault())
                    {
                        tcount++;
                    }
                }
                catch { }

                string t1text = tcount == 1
                    ? _?.Lyric?.FirstOrDefault()
                    : $"{_?.Lyric?.FirstOrDefault()} (x{tcount})";

                AppTitleTextBlock.Text = $"{App.Instance.AppName} -";
                LyricTextBlock.Text = $" {t1text}";
            }
            catch (Exception err)
            {
                LogManager.Log("MainWindow", "LyricManager_PlayingLyricSelectedChange: " + err.Message);
            }
        }

        public void Invoke(Action action)
        {
            if (GridBase is not null)
            {
                if (GridBase.DispatcherQueue is not null) GridBase.DispatcherQueue?.TryEnqueue(() => action());
                else action();
            }
            else action();
        }

        private void SMTC_ButtonPressed(Windows.Media.SystemMediaTransportControls sender, Windows.Media.SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            Invoke(() =>
            {
                switch (args.Button)
                {
                    case Windows.Media.SystemMediaTransportControlsButton.Play:
                        App.Instance.audioPlayer.SetPlay();
                        break;
                    case Windows.Media.SystemMediaTransportControlsButton.Pause:
                        App.Instance.audioPlayer.SetPause();
                        break;
                    case Windows.Media.SystemMediaTransportControlsButton.Previous:
                        PlayBeforeButton_Click(null, null);
                        break;
                    case Windows.Media.SystemMediaTransportControlsButton.Next:
                        PlayNextButton_Click(null, null);
                        break;
                    case Windows.Media.SystemMediaTransportControlsButton.Stop:
                        App.Instance.audioPlayer.SetStop();
                        break;
                }
            });
        }

        private void PlayingList_PlayingListItemChange(ObservableCollection<MusicData> nowPlayingList)
        {
            //PlayingListBaseView.SelectedItem = App.Instance.audioPlayer.MusicData;
        }

        private void AudioPlayer_VolumeChanged(Media.AudioPlayer audioPlayer, object data)
        {
            float volume = (float)data;
            VolumeSlider.Value = (int)volume;

            if (volume == 0)
            {
                VolumeIconBase.Glyph = "\xE198";
                VolumeIconBase1.Glyph = "\xE198";
            }
            else
            {
                if (volume <= 100 && volume > 67)
                {
                    VolumeIconBase.Glyph = "\xE995";
                    VolumeIconBase1.Glyph = "\xE995";
                }
                else if (volume <= 67 && volume > 33)
                {
                    VolumeIconBase.Glyph = "\xE994";
                    VolumeIconBase1.Glyph = "\xE994";
                }
                else if (volume <= 33)
                {
                    VolumeIconBase.Glyph = "\xE993";
                    VolumeIconBase1.Glyph = "\xE993";
                }
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
            if (data is null)
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

        }

        public void PlayingList_NowPlayingImageLoaded(ImageSource imageSource, string _)
        {
            var im = PlayContent.Content as ImageEx;
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
                im.SaveName = $"{App.Instance.audioPlayer.MusicData.Title} · {App.Instance.audioPlayer.MusicData.Album.Title}";
                im.BorderThickness = new(1);
            }

        }

        MusicData pointConnectAnimationMusicData = null;
        private void AudioPlayer_SourceChanged(Media.AudioPlayer audioPlayer)
        {
            if (audioPlayer.MusicData is null) return;
            if (pointConnectAnimationMusicData == audioPlayer.MusicData) return;
            pointConnectAnimationMusicData = audioPlayer.MusicData;
            PlayTitle.Text = audioPlayer.MusicData.Title;
            PlayArtist.Text = audioPlayer.MusicData.ButtonName;
            //PlayingListBaseView.SelectedItem = audioPlayer.MusicData;

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
                            animation.TryStart(VisualTreeHelper.GetParent(PlayContent) as UIElement);
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
                App.Instance.lyricManager.PlayingLyricSelectedChanged += LyricManager_PlayingLyricSelectedChange;
                App.Instance.lyricManager.StartTimer();
            }
            else
            {
                PlayRing.Foreground = App.Current.Resources["SystemFillColorCautionBrush"] as SolidColorBrush;
                App.Instance.lyricManager.PlayingLyricSelectedChanged -= LyricManager_PlayingLyricSelectedChange;
            }

            MediaPlayStateViewer.PlaybackState = audioPlayer.PlaybackState;
        }

        private void AudioPlayer_TimingChanged(Media.AudioPlayer audioPlayer)
        {
            if (doNotChangeTiming) return;
            if (audioPlayer.FileReader is null) return;
            PlayRing.Minimum = 0;
            PlayRing.Maximum = audioPlayer.TotalTime.Ticks;
            PlayRing.Value = audioPlayer.CurrentTime.Ticks;
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

        private void SetDragRegionForCustomTitleBar()
        {
            // Check to see if customization is supported.
            // Currently only supported on Windows 11.
            if (AppWindowTitleBar.IsCustomizationSupported()
                && AppWindow.TitleBar.ExtendsContentIntoTitleBar)
            {
                double scaleAdjustment = CodeHelper.GetScaleAdjustment(this);
                double rpc = AppWindow.TitleBar.RightInset / scaleAdjustment;
                double lpc = AppWindow.TitleBar.LeftInset / scaleAdjustment;

                //RightPaddingColumn.Width = new GridLength(AppWindow.TitleBar.RightInset / scaleAdjustment);
                //LeftPaddingColumn.Width = new GridLength(AppWindow.TitleBar.LeftInset / scaleAdjustment);

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
                dragRectR.Width = (int)(scaleAdjustment * AppWindow.Size.Width);
                dragRectsList.Add(dragRectR);

                RectInt32[] dragRects = dragRectsList.ToArray();

                AppWindow.TitleBar.SetDragRectangles(dragRects);
            }
        }
        #endregion

        #region NavView Events
        public async void OpenPlayListNavView()
        {
            await App.Instance.playListReader.Refresh();
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
            foreach (var i in App.Instance.playListReader.NowMusicListData)
            {
                var nvi = new NavigationViewItem() { Content = i.ListShowName, Tag = i };
                MusicPlayListButton.MenuItems.Add(nvi);
            }
        }

        public void UpdateNavViewContentBaseRGClip()
        {
            NavViewContentBase_RGClip.Rect = new Windows.Foundation.Rect(0, 0,
                NavViewContentBase.ActualWidth,
                AppWindow.Size.Height - AppTitleBar.ActualHeight);
        }

        public void SetNavViewContent(Type type, object param = null, NavigationTransitionInfo navigationTransitionInfo = null)
        {
            if (navigationTransitionInfo is null) navigationTransitionInfo = new EntranceNavigationTransitionInfo();
            ContentFrame.Navigate(type, param, navigationTransitionInfo);
            NavView.IsBackEnabled = ContentFrame.CanGoBack;
            UpdateNavViewSelectedItem(true);
            /*if (type == typeof(ItemListView))
            {
                NavView.SelectedItem = null;
            }*/
        }

        public bool IsBackRequest = false;
        private async void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (IsBackRequest || sender.SelectedItem is null || IsJustUpdate)
            {
                return;
            }

            NavigationViewItem item = sender.SelectedItem as NavigationViewItem;
            if (item is null) return;
            
            if (item == NavView.MenuItems[1] as NavigationViewItem)
                SetNavViewContent(typeof(SearchPage));
            else if (item == NavView.MenuItems[2] as NavigationViewItem)
                SetNavViewContent(typeof(BrowsePage));
            else if (item == NavView.MenuItems[3] as NavigationViewItem)
                SetNavViewContent(typeof(DownloadPage));
            else if (item == NavView.MenuItems[5] as NavigationViewItem)
                SetNavViewContent(typeof(LocalAudioPage));
            else if (item == NavView.MenuItems[6] as NavigationViewItem)
                SetNavViewContent(typeof(PlayListPage));
            else if (item == NavView.MenuItems[7] as NavigationViewItem)
                SetNavViewContent(typeof(HistoryPage));
            else if (item == NavView.FooterMenuItems[0] as NavigationViewItem)
                SetNavViewContent(typeof(AboutPage));
            else
            {
                // TOFIX: 快速切换到 PlayList 页面会导致 SelectedItem 为 null
                if (sender.SelectedItem is null)
                {
                    LogManager.Log("MainWindow", "NavigationSelectionChanged: SelectedItem 为 null");
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

        bool IsJustUpdate = false;
        public void UpdateNavViewSelectedItem(bool justUpdate = false)
        {
            if (justUpdate) IsJustUpdate = true;
            Type type = (ContentFrame.Content as Page).GetType();
            if (type == typeof(SearchPage))
                NavView.SelectedItem = NavView.MenuItems[1];
            else if (type == typeof(BrowsePage))
                NavView.SelectedItem = NavView.MenuItems[2];
            else if (type == typeof(DownloadPage))
                NavView.SelectedItem = NavView.MenuItems[3];
            else if (type == typeof(LocalAudioPage))
                NavView.SelectedItem = NavView.MenuItems[5];
            else if (type == typeof(PlayListPage))
                NavView.SelectedItem = NavView.MenuItems[6];
            else if (type == typeof(HistoryPage))
                NavView.SelectedItem = NavView.MenuItems[7];
            else if (type == typeof(AboutPage))
                NavView.SelectedItem = NavView.FooterMenuItems[0];
            else if (type == typeof(SettingPage))
                NavView.SelectedItem = NavView.SettingsItem;
            else if (type == typeof(ItemListViewPlayList))
            {
                //TODO:优化写法
                foreach (NavigationViewItem item in (NavView.MenuItems[6] as NavigationViewItem).MenuItems)
                {
                    if ((ContentFrame.Content as ItemListViewPlayList).NavToObj == item.Tag as MusicListData)
                    {
                        NavView.SelectedItem = item;
                        break;
                    }
                }
            }
            else if (type == typeof(Pages.ListViewPages.PlayListPage))
            {
                foreach (NavigationViewItem item in (NavView.MenuItems[6] as NavigationViewItem).MenuItems)
                {
                    if ((ContentFrame.Content as Pages.ListViewPages.PlayListPage).md5 == (item.Tag as MusicListData).MD5)
                    {
                        NavView.SelectedItem = item;
                        break;
                    }
                }
            }
            else if (type == typeof(ItemListViewSearch) || type == typeof(ItemListViewArtist) || type == typeof(ItemListViewAlbum))
                NavView.SelectedItem = null;
            IsJustUpdate = false;
        }

        public bool TryGoBack()
        {
            if (InOpenMusicPage)
            {
                OpenOrCloseMusicPage();
                return ContentFrame.CanGoBack;
            }

            if (!ContentFrame.CanGoBack)
                return false;

            IsBackRequest = true;
            ContentFrame.GoBack();
            UpdateNavViewSelectedItem();
            NavView.IsBackEnabled = ContentFrame.CanGoBack;

            IsBackRequest = false;
            return true;
        }

        public void TryGoForward()
        {
            if (ContentFrame.CanGoForward)
            {
                IsBackRequest = true;
                ContentFrame.GoForward();
                UpdateNavViewSelectedItem();
                NavView.IsBackEnabled = ContentFrame.CanGoBack;
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
                SetDragRegionForCustomTitleBar();
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

            SetDragRegionForCustomTitleBar();
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
            if (App.Instance.audioPlayer.PlaybackState == PlaybackState.Playing)
            {
                App.Instance.audioPlayer.SetPause();
            }
            else
            {
                App.Instance.audioPlayer.SetPlay();
            }
        }

        private async void PlayBeforeButton_Click(object sender, RoutedEventArgs e)
        {
            await App.Instance.playingList.PlayPrevious();
        }

        private async void PlayNextButton_Click(object sender, RoutedEventArgs e)
        {
            await App.Instance.playingList.PlayNext();
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

        public void OpenOrCloseVolume(
            HorizontalAlignment horizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment verticalAlignment = VerticalAlignment.Bottom,
            FlyoutPlacementMode flyoutPlacementMode = FlyoutPlacementMode.LeftEdgeAlignedBottom,
            Thickness placementMargin = default)
        {
            if (VolumeBasePopup.IsOpen)
            {
                VolumeBasePopup.Hide();
                return;
            }
            TopControlsBaseGrid.HorizontalAlignment = horizontalAlignment;
            TopControlsBaseGrid.VerticalAlignment = verticalAlignment;
            TopControlsBaseGrid.Margin = placementMargin == default ? new(0, 0, 12, 96) : placementMargin;
            VolumeBasePopup.LightDismissOverlayMode = LightDismissOverlayMode.Off;
            VolumeBasePopup.Placement = flyoutPlacementMode;
            VolumeBasePopup.ShowAt(TopControlsBaseGrid);
        }

        public async void OpenOrClosePlayingList(
            HorizontalAlignment horizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment verticalAlignment = VerticalAlignment.Bottom,
            FlyoutPlacementMode flyoutPlacementMode = FlyoutPlacementMode.LeftEdgeAlignedBottom,
            Thickness placementMargin = default)
        {
            UpdatePlayListFlyoutHeight();
            AddPlayingListPopupEvents();
            TopControlsBaseGrid.HorizontalAlignment = horizontalAlignment;
            TopControlsBaseGrid.VerticalAlignment = verticalAlignment;
            TopControlsBaseGrid.Margin = placementMargin == default ? new(4, 0, 12, 96) : placementMargin;
            PlayingListBasePopup.LightDismissOverlayMode = LightDismissOverlayMode.Off;
            PlayingListBasePopup.Placement = flyoutPlacementMode;
            PlayingListBasePopup.ShowAt(TopControlsBaseGrid);
            try
            {
                await PlayingListBaseView.SmoothScrollIntoViewWithItemAsync(App.Instance.audioPlayer.MusicData, ScrollItemPlacement.Center);
                await PlayingListBaseView.SmoothScrollIntoViewWithItemAsync(App.Instance.audioPlayer.MusicData, ScrollItemPlacement.Center, true);
                PlayingListBaseView.SelectedItem = App.Instance.audioPlayer.MusicData;
            }
            catch { }
        }

        private void AddPlayingListPopupEvents()
        {
            PlayingListBasePopup.Opened -= TeachingTipPlayingList_Opened;
            PlayingListBasePopup.Closed -= TeachingTipPlayingList_Closed;
            PlayingListBasePopup.Opened += TeachingTipPlayingList_Opened;
            PlayingListBasePopup.Closed += TeachingTipPlayingList_Closed;
        }

        private void TeachingTipPlayingList_Opened(object sender, object e)
        {
            ToolTipService.SetToolTip(PlayModeSelector, $"当前播放模式：{App.Instance.playingList.PlayBehavior}");
            SetPlayModeIconAndName(App.Instance.playingList.PlayBehavior);
            PlayingListScrollControl.Translation = new(
                -16,
                (float)(PlayingListBaseView.ActualHeight - PlayingListScrollControl.ActualHeight - 20 - 16),
                0);
        }

        private void TeachingTipPlayingList_Closed(object sender, object e)
        {
            PlayingListBasePopup.Opened -= TeachingTipPlayingList_Opened;
            PlayingListBasePopup.Closed -= TeachingTipPlayingList_Closed;
        }

        private void SetPlayModeIconAndName(PlayBehavior playBehavior)
        {
            PlayModeSelector_Icon.Glyph = playBehavior.GetIcon();
            PlayModeSelector_Name.Text = playBehavior.ToString();
        }

        private void PlayModeSelector_Click(object sender, RoutedEventArgs e)
        {
            var pList = Enum.GetNames(typeof(PlayBehavior)).ToList();
            PlayModeFlyout.Items.Clear();
            foreach (var p in pList)
            {
                var b = new MenuFlyoutItem() { Text = p, Tag = pList.IndexOf(p) };
                b.Click += B_Click;
                b.Unloaded += B_Unloaded;
                b.Icon = new FontIcon()
                {
                    Glyph = ((PlayBehavior)b.Tag).GetIcon()
                };
                PlayModeFlyout.Items.Add(b);
            }
        }

        private void B_Click(object sender, RoutedEventArgs e)
        {
            var a = (PlayBehavior)(sender as MenuFlyoutItem).Tag;
            App.Instance.playingList.PlayBehavior = a;
            SetPlayModeIconAndName(App.Instance.playingList.PlayBehavior);
            ToolTipService.SetToolTip(PlayModeSelector, $"当前播放模式：{a}");
        }

        private void B_Unloaded(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item)
            {
                item.Click -= B_Click;
                item.Unloaded -= B_Unloaded;
            }
        }

        Visual musicPageVisual;
        Vector3KeyFrameAnimation musicPageVisualClosingAnimation;
        Vector3KeyFrameAnimation musicPageVisualOpeningAnimation;
        void InitMusicPageVisuals(bool onlyUpdateOffset = false)
        {
            musicPageVisualClosingAnimation?.Dispose();
            musicPageVisualOpeningAnimation?.Dispose();
            AnimateHelper.AnimateOffset(
                MusicPageBaseFrame,
                0, (float)MusicPageBaseGrid.ActualHeight, 0,
                0.22,
                0.5f, 0, 0.75f, 0,
                out musicPageVisual, out Compositor compositor, out musicPageVisualClosingAnimation);

            AnimateHelper.AnimateOffset(
                MusicPageBaseFrame,
                0, 0, 0,
                0.5,
                0.16f, 1, 0.3f, 1,
                out musicPageVisual, out Compositor compositor1, out musicPageVisualOpeningAnimation);

            if (onlyUpdateOffset) return;
            musicPageVisual.Offset = new(0, (float)MusicPageBaseGrid.ActualHeight, 0);
            MusicPageBaseFrame.Visibility = Visibility.Collapsed;
        }
        public bool InOpenMusicPage { get; set; } = false;
        bool isFirstInMusicPage = true;
        bool isHiddenMusicPageAnimationNotCompleted = false;
        public void OpenOrCloseMusicPage()
        {
            if (App.Instance.audioPlayer.MusicData is null) return;
            if (musicPageVisual is null) InitMusicPageVisuals();
            else InitMusicPageVisuals(true);

            MusicPageBaseFrame.Content = SMusicPage;
            if (InOpenMusicPage)
            {
                InOpenMusicPage = false;
                isHiddenMusicPageAnimationNotCompleted = true;

                LogManager.Log("MainWindow", "主界面被显示。");
                GridBase.Visibility = Visibility.Visible;
                InitializeTitleBar(WindowGridBase.RequestedTheme);
                musicPageVisual.StartAnimation(nameof(musicPageVisual.Offset), musicPageVisualClosingAnimation);
                musicPageVisual.Compositor.GetCommitBatch(CompositionBatchTypes.Animation).Completed += (_, __) =>
                {
                    if (!InOpenMusicPage)
                    {
                        MusicPageBaseFrame.Visibility = Visibility.Collapsed;
                        isHiddenMusicPageAnimationNotCompleted = false;
                    }
                };

                SMusicPage.MusicPageViewStateChange(MusicPageViewState.Hidden);
                MusicPageViewStateChanged?.Invoke(MusicPageViewState.Hidden);

                ConnectedAnimation canimation =
                    ConnectedAnimationService.GetForCurrentView().GetAnimation("upAnimation");
                if (canimation != null)
                {
                    canimation.TryStart(VisualTreeHelper.GetParent(PlayContent) as UIElement);
                }
                ConnectedAnimation canimation1 =
                    ConnectedAnimationService.GetForCurrentView().GetAnimation("upAnimation1");
                if (canimation1 != null)
                {
                    canimation1.TryStart(PlayTitle);
                }
                ConnectedAnimation canimation2 =
                    ConnectedAnimationService.GetForCurrentView().GetAnimation("upAnimation2");
                if (canimation2 != null)
                {
                    canimation2.TryStart(PlayArtist);
                }

                if (App.Instance.lyricManager.NowPlayingLyrics.Any())
                {
                    ConnectedAnimation canimation3 =
                    ConnectedAnimationService.GetForCurrentView().GetAnimation("upAnimation3");
                    if (canimation3 != null)
                    {
                        canimation3.TryStart(LyricTextBlock);
                    }
                }
            }
            else
            {
                InOpenMusicPage = true;

                MusicPageBaseFrame.Visibility = Visibility.Visible;
                InitializeTitleBar(SMusicPage.pageRoot.RequestedTheme);
                musicPageVisual.Offset = new(0, (float)MusicPageBaseGrid.ActualHeight, 0);
                musicPageVisual.StartAnimation(nameof(musicPageVisual.Offset), musicPageVisualOpeningAnimation);
                musicPageVisual.Compositor.GetCommitBatch(CompositionBatchTypes.Animation).Completed += (_, __) =>
                {
                    if (InOpenMusicPage && !isHiddenMusicPageAnimationNotCompleted)
                    {
                        GridBase.Visibility = Visibility.Collapsed;
#if DEBUG
                        LogManager.Log("MainWindow", "主界面被隐藏。");
#endif
                    }
                };

                SMusicPage.MusicPageViewStateChange(MusicPageViewState.View);
                MusicPageViewStateChanged?.Invoke(MusicPageViewState.View);
            }
        }

        private async void SetMainPageVisibility(bool visibility)
        {
            if (visibility)
            {
                GridBase.Visibility = Visibility.Visible;
                LogManager.Log("MainWindow", "主界面被显示。");
                await Task.Delay(220);
                if (!InOpenMusicPage)
                    MusicPageBaseFrame.Visibility = Visibility.Collapsed;
            }
            else
            {
                MusicPageBaseFrame.Visibility = Visibility.Visible;
                await Task.Delay(500);
                if (InOpenMusicPage)
                {
                    GridBase.Visibility = Visibility.Collapsed;
                    LogManager.Log("MainWindow", "主界面被隐藏。");
                }
            }
        }

        int volumePopupCounter = 0;
        private async void VolumeAppButton_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(sender as UIElement).Properties.MouseWheelDelta > 0)
                //VolumeSlider.Value += 1.1f;
                App.Instance.audioPlayer.Volume += 1f;
            else
                App.Instance.audioPlayer.Volume -= 1f;

            (VolumePopup.Content as TextBlock).Text = $"音量：{App.Instance.audioPlayer.Volume}";
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
        public event InKeyDown InKeyDownEvent;
        public event InKeyDown InKeyUpEvent;

        public bool isAltDown = false;
        public bool isControlDown = false;
        public bool isShiftDown = false;
        public bool CanKeyDownBack = true;
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
        public event DriveInTypeDelegate DriveInTypeEvent;
        public Microsoft.UI.Input.PointerDeviceType DriveInType = Microsoft.UI.Input.PointerDeviceType.Mouse;
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
                App.Instance.audioPlayer.Volume += 1f;
            else
                App.Instance.audioPlayer.Volume -= 1f;
        }

        private void VolumeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (willChangeVolume)
            {
                App.Instance.audioPlayer.Volume = (float)(sender as Slider).Value;
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

        public float NoVolumeValue = 0;
        // 静音按钮点击事件
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            MuteOrUnmuteVolume();
        }

        public void MuteOrUnmuteVolume()
        {
            if (App.Instance.audioPlayer.Volume != 0)
            {
                NoVolumeValue = App.Instance.audioPlayer.Volume;
                App.Instance.audioPlayer.Volume = 0;
            }
            else
            {
                App.Instance.audioPlayer.Volume = NoVolumeValue;
            }
        }
        #endregion

        #region PlayingListView Events
        bool inSelectionChange = false;
        private async void PlayingListBaseView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {/*
            inSelectionChange = true;
            if (PlayingListBaseView.SelectedItem != null)
            {
                MusicData data = (MusicData)PlayingListBaseView.SelectedItem;
                if (App.Instance.audioPlayer.MusicData != data)
                    await App.Instance.playingList.Play(data);
            }
            inSelectionChange = false;*/
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            App.Instance.playingList.ClearAll();
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var name = $"{DateTime.Now} 时的播放列表";
            var musicPlayList = new MusicListData(
                name, name, "", MusicFrom.localMusic, null, [.. App.Instance.playingList.NowPlayingList], DataType.本地歌单);
            await PlayListHelper.AddPlayList(musicPlayList);
            await App.Instance.playListReader.Refresh();
            AddNotify("播放列表已添加！", $"播放列表 \"{name}\" 已添加。", buttonMessage: "打开播放列表", buttonAction: () =>
            {
                Pages.ListViewPages.ListViewPage.SetPageToListViewPage(new()
                {
                    PageType = Pages.ListViewPages.PageType.PlayList,
                    Param = musicPlayList
                });
            });
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
        public event DesktopLyricDelegate DesktopLyricWindowOpenedEvent;
        public event DesktopLyricDelegate DesktopLyricWindowClosedEvent;
        public bool IsDesktopLyricWindowOpen = false;
        public DesktopLyricWindow DesktopLyricWindow = null;
        bool isInChangingLyricWindow = false;

        public async void OpenDesktopLyricWindow(bool timeDelay = true)
        {
            if (!isInChangingLyricWindow)
            {
                isInChangingLyricWindow = true;
                IsDesktopLyricWindowOpen = true;
                if (DesktopLyricWindow is null)
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

        private void DesktopLyricWindow_Closed(object sender, WindowEventArgs args)
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
            App.Instance.audioPlayer.TimingChanged += AudioPlayer_TimingChanged1;
            PlayTimeSlider.ValueChanged += PlayTimeSlider_ValueChanged;
        }

        private void PlayTimeSliderBasePopup_Closed(TeachingTip sender, TeachingTipClosedEventArgs args)
        {
            PlayTimeSliderBasePopup.Closed -= PlayTimeSliderBasePopup_Closed;
            App.Instance.audioPlayer.TimingChanged -= AudioPlayer_TimingChanged1;
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
            if (!isCodeChangedSilderValue && App.Instance.audioPlayer.FileReader != null)
            {
                App.Instance.audioPlayer.CurrentTime = TimeSpan.FromTicks((long)PlayTimeSlider.Value);
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

        void ShowDropInfo_Root()
        {
            DropInfo_Root.Visibility = Visibility.Visible;
            DropInfo_Root.Opacity = 1;
        }

        async void HideDropInfo_Root()
        {
            DropInfo_Root.Opacity = 0;
            await Task.Delay(300);
            if (DropInfo_Root.Opacity == 0)
                DropInfo_Root.Visibility = Visibility.Collapsed;
        }

        public bool AllowDragEvents = true;
        private void WindowGridBase_DragOver(object sender, DragEventArgs e)
        {
            if (!AllowDragEvents) return;
            if (!e.DataView.AvailableFormats.Contains("FileDrop")) return; // 当拖动的是应用程序自身的控件时返回
            ShowDropInfo_Root();
            e.AcceptedOperation = DataPackageOperation.Link;
            e.DragUIOverride.Caption = "打开";
        }

        private async void WindowGridBase_Drop(object sender, DragEventArgs e)
        {
            if (!AllowDragEvents) return;
            HideDropInfo_Root();
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
            HideDropInfo_Root();
        }

        private void Button_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            MusicDataFlyout.SongItemBind = new() { MusicData = App.Instance.audioPlayer.MusicData };
            MusicDataFlyout.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));
        }

        private void Button_Holding(object sender, HoldingRoutedEventArgs e)
        {
            MusicDataFlyout.SongItemBind = new() { MusicData = App.Instance.audioPlayer.MusicData };
            MusicDataFlyout.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));
        }
        #endregion

        private partial class BlurredBackdrop : CompositionBrushBackdrop
        {
            protected override Windows.UI.Composition.CompositionBrush CreateBrush(Windows.UI.Composition.Compositor compositor)
                => compositor.CreateHostBackdropBrush();
        }
    }

    public enum NotifySeverity { Info, Error, Warning, Complete, Loading }
    public static class NotifySeverityStatic
    {
        public static LogLevel? ToLogLevel(this NotifySeverity severity)
        {
            return severity switch
            {
                NotifySeverity.Warning => LogLevel.Warning,
                NotifySeverity.Error => LogLevel.Error,
                NotifySeverity.Info => LogLevel.Information,
                _ => null,
            };
        }
    }

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
        /// 按钮信息
        /// </summary>
        public string ButtonMessage { get; set; }
        /// <summary>
        /// 按钮按下后触发
        /// </summary>
        public Action ButtonAction { get; set; }
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
            TimeSpan? residenceTime = null,
            string buttonMessage = null,
            Action buttonAction = null)
        {
            Title = title;
            Message = message;
            Severity = severity;
            ResidenceTime = residenceTime is null ? TimeSpan.FromSeconds(5) : (TimeSpan)residenceTime;
            ButtonMessage = buttonMessage;
            ButtonAction = buttonAction;
        }
    }
}
