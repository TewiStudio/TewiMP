using CommunityToolkit.WinUI;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using NAudio.Wave;
using System;
using System.Linq;
using System.Threading.Tasks;
using TewiMP.Background;
using TewiMP.Controls;
using TewiMP.DataEditor;
using TewiMP.Helpers;
using Vanara.PInvoke;
using Windows.Graphics.Imaging;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace TewiMP.Pages.MusicPages
{
    public partial class MusicPage : Page, IMusicPage
    {
        private GestureRecognizer _recognizer;
        public MusicPageViewStateChangeDelegate MusicPageViewStateChange { get; set; }

        public string Title
        {
            get => App.Instance.audioPlayer.MusicData.Title;
        }

        private bool _showLrcPage = true;
        public bool ShowLrcPage
        {
            get => _showLrcPage;
            set
            {/*
                if (value == _showLrcPage) return;
                _showLrcPage = value;
                if (LrcButton.IsChecked != value) LrcButton.IsChecked = value;

                if (value)
                {
                    LrcPageColumn.MaxWidth = double.MaxValue;
                    LrcBaseGrid.Visibility = Visibility.Visible;
                    SelectedChangedDo();
                }
                else
                {
                    LrcPageColumn.MaxWidth = 0;
                    LrcBaseGrid.Visibility = Visibility.Collapsed;
                }
                UpdateInterfaceDesign();*/
            }
        }

        private TextAlignment _lrcTextAlignment = TextAlignment.Left;
        public TextAlignment LrcTextAlignment
        {
            get => _lrcTextAlignment;
            set
            {
                _lrcTextAlignment = value;
            }
        }

        private ImageSource _imageSources = null;
        public ImageSource ImageSources
        {
            get => _imageSources;
            set
            {
                _imageSources = value;
            }
        }

        public MusicPage()
        {
            InitializeComponent();
            MusicPageViewStateChange = new MusicPageViewStateChangeDelegate(ViewChange);
            DataContext = this;
            SizeChanged += MusicPage_SizeChanged;

            UpdateInterfaceDesign();
        }

        private void UpdateWhenDataLated()
        {
            isCodeChangedLrcItem = true;
            AudioPlayer_SourceChanged(App.Instance.audioPlayer);
            AudioPlayer_PlayStateChanged(App.Instance.audioPlayer);
            AudioPlayer_CacheLoadedChanged(App.Instance.audioPlayer);
            AudioPlayer_TimingChanged(App.Instance.audioPlayer);
            AudioPlayer_VolumeChanged(App.Instance.audioPlayer, App.Instance.audioPlayer.Volume);
            PlayingList_NowPlayingImageLoaded(App.Instance.playingList.NowPlayingImage, null);
            LyricManager_PlayingLyricSelectedChange1(App.Instance.lyricManager.NowLyricsData);
            SelectedChangedDo(true);
            isCodeChangedLrcItem = false;
            App.Instance.audioPlayer.ReCallTiming();
            //LogManager.Log("MusicPage Updateed Events.");
        }

        bool isAddEvents = false;
        private void AddEvents()
        {
            if (isAddEvents) return;
            AutoScrollViewer1.Pause = false; AutoScrollViewer2.Pause = false;
            AutoScrollViewer3.Pause = false; AutoScrollViewer4.Pause = false;
            AutoScrollViewer5.Pause = false;
            App.Instance.audioPlayer.SourceChanged -= AudioPlayer_SourceChanged;
            App.Instance.audioPlayer.SourceChanged += AudioPlayer_SourceChanged;
            App.Instance.audioPlayer.PlayStateChanged -= AudioPlayer_PlayStateChanged;
            App.Instance.audioPlayer.PlayStateChanged += AudioPlayer_PlayStateChanged;
            App.Instance.audioPlayer.TimingChanged -= AudioPlayer_TimingChanged;
            App.Instance.audioPlayer.TimingChanged += AudioPlayer_TimingChanged;
            App.Instance.audioPlayer.PlayEnd -= AudioPlayer_PlayEnd;
            App.Instance.audioPlayer.PlayEnd += AudioPlayer_PlayEnd;
            App.Instance.audioPlayer.VolumeChanged -= AudioPlayer_VolumeChanged;
            App.Instance.audioPlayer.VolumeChanged += AudioPlayer_VolumeChanged;
            App.Instance.audioPlayer.CacheLoadingChanged -= AudioPlayer_CacheLoadingChanged;
            App.Instance.audioPlayer.CacheLoadingChanged += AudioPlayer_CacheLoadingChanged;
            App.Instance.audioPlayer.CacheLoadedChanged -= AudioPlayer_CacheLoadedChanged;
            App.Instance.audioPlayer.CacheLoadedChanged += AudioPlayer_CacheLoadedChanged;
            App.Instance.playingList.NowPlayingImageLoading -= PlayingList_NowPlayingImageLoading;
            App.Instance.playingList.NowPlayingImageLoading += PlayingList_NowPlayingImageLoading;
            App.Instance.playingList.NowPlayingImageLoaded -= PlayingList_NowPlayingImageLoaded;
            App.Instance.playingList.NowPlayingImageLoaded += PlayingList_NowPlayingImageLoaded;
            App.Instance.lyricManager.PlayingLyricSourceChanged -= LyricManager_PlayingLyricSourceChange;
            App.Instance.lyricManager.PlayingLyricSourceChanged += LyricManager_PlayingLyricSourceChange;
            App.Instance.lyricManager.PlayingLyricSelectedChanged -= LyricManager_PlayingLyricSelectedChange1;
            App.Instance.lyricManager.PlayingLyricSelectedChanged += LyricManager_PlayingLyricSelectedChange1;
#if DEBUG
            App.Instance.lyricManager.LyricTimingChanged -= LyricManager_LyricTimingChanged;
            App.Instance.lyricManager.LyricTimingChanged += LyricManager_LyricTimingChanged;
#endif
            isAddEvents = true;
            UpdateWhenDataLated();
            //LogManager.Log("MusicPage Added Events.");
        }

        private void RemoveEvents()
        {
            AutoScrollViewer1.Pause = true; AutoScrollViewer2.Pause = true;
            AutoScrollViewer3.Pause = true; AutoScrollViewer4.Pause = true;
            AutoScrollViewer5.Pause = true;
            App.Instance.audioPlayer.SourceChanged -= AudioPlayer_SourceChanged;
            App.Instance.audioPlayer.PlayStateChanged -= AudioPlayer_PlayStateChanged;
            App.Instance.audioPlayer.TimingChanged -= AudioPlayer_TimingChanged;
            App.Instance.audioPlayer.PlayEnd -= AudioPlayer_PlayEnd;
            App.Instance.audioPlayer.VolumeChanged -= AudioPlayer_VolumeChanged;
            App.Instance.audioPlayer.CacheLoadingChanged -= AudioPlayer_CacheLoadingChanged;
            App.Instance.audioPlayer.CacheLoadedChanged -= AudioPlayer_CacheLoadedChanged;
            App.Instance.playingList.NowPlayingImageLoading -= PlayingList_NowPlayingImageLoading;
            App.Instance.playingList.NowPlayingImageLoaded -= PlayingList_NowPlayingImageLoaded;
            App.Instance.lyricManager.PlayingLyricSourceChanged -= LyricManager_PlayingLyricSourceChange;
            App.Instance.lyricManager.PlayingLyricSelectedChanged -= LyricManager_PlayingLyricSelectedChange1;
#if DEBUG
            App.Instance.lyricManager.LyricTimingChanged -= LyricManager_LyricTimingChanged;
#endif
            isAddEvents = false;
            //LogManager.Log("MusicPage Removed Events.");
        }

        public MusicPageViewState ViewState = MusicPageViewState.Hidden;
        private void ViewChange(MusicPageViewState musicPageViewState)
        {
            ViewState = musicPageViewState;
            if (ViewState == MusicPageViewState.View)
            {
                LrcBaseListView.ItemsSource = App.Instance.lyricManager.NowPlayingLyrics;
                AddEvents();
                UpdateInterfaceDesign();
            }
            else
            {
                ConnectedAnimation canimation =
                    ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("upAnimation", AlbumImageBorder);
                canimation.Configuration = new BasicConnectedAnimationConfiguration();
                ConnectedAnimation canimation1 =
                    ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("upAnimation1", TitleRunText);
                canimation1.Configuration = new BasicConnectedAnimationConfiguration();
                ConnectedAnimation canimation2 =
                    ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("upAnimation2", ArtistRunText);
                canimation2.Configuration = new BasicConnectedAnimationConfiguration();
                if (App.Instance.lyricManager.NowPlayingLyrics.Any())
                {
                    var e = (isMiniPage ? LrcSecondListView : LrcBaseListView).ContainerFromIndex(LrcBaseListView.SelectedIndex) as UIElement;
                    if (e != null)
                    {
                        ConnectedAnimation canimation3 =
                            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("upAnimation3", e);
                        canimation3.Configuration = new BasicConnectedAnimationConfiguration();
                    }
                }

                RemoveEvents();
                RemoveLyricListItemSourceAsync();
            }
            LogManager.Log("MusicPage", $"ViewState 已被设置为 {musicPageViewState}.");
        }

        private async void RemoveLyricListItemSourceAsync()
        {
            await Task.Delay(200);
            if (ViewState == MusicPageViewState.Hidden)
            {
                LrcBaseListView.ItemsSource = null;
                LogManager.Log("MusicPage", $"LrcBaseListView.ItemSource 已被设置为 null.");
            }
        }

        private void PlayingList_NowPlayingImageLoading(ImageSource imageSource, string _)
        {
            /*if (App.Instance.audioPlayer.MusicData?.AlbumID != MusicData?.AlbumID)
            {
                if (BackgroundBaseImage.Source != null)
                    BackgroundBaseImageAnimate.Source = null;
                BackgroundBaseImageAnimate.Source = BackgroundBaseImage.Source;
                BackgroundBaseImage.Source = null;
                //AlbumImageBase.Dispose();
            }*/
        }

        string imagePath;
        private void PlayingList_NowPlayingImageLoaded(ImageSource imageSource, string _)
        {
            imagePath = _;
            if (imageSource is null)
            {
                BackgroundBaseImage.Source = null;
                AlbumImageBase.Source = null;
                //BackgroundFillBase.Opacity = 0;
                return;
            }
            if (imageSource == ImageSources) return;
            ImageSources = imageSource;
            BackgroundBaseImage.Source = ImageSources;
            AlbumImageBase.Source = imageSource;
            AlbumImageBase.SaveName = $"{MusicData.Title} · {MusicData.Album.Title}";
            //BackgroundFillBase.Opacity = 1;
            LogManager.Log("MusicPage", $"图片已被更改.");
            GetImageColor();
        }

        private async Task GetImageColor()
        {
            if (string.IsNullOrEmpty(imagePath)) return;
            var themeColor = await GetThemeColorAsync(imagePath);
            (pageRoot.Resources["AccentBrush"] as SolidColorBrush).Color = themeColor;
            //(pageRoot.Resources["AccentFillColorDefaultBrush"] as SolidColorBrush).Color = themeColor;
        }

        public async Task<Color> GetThemeColorAsync(string file)
        {
            using var bitmap = new System.Drawing.Bitmap(file);
            var colorThief = new ColorThiefDotNet.ColorThief();
            var qColor = await Task.Run(() => colorThief.GetColor(bitmap, 4));
            var c = qColor.Color;
            var result = Color.FromArgb(c.A, c.R, c.G, c.B);
            result.ColorToHSV(out var h, out var s, out var v);
            return CodeHelper.ColorFromHSV(h, s + .4, v + .4);
        }

        private void BackgroundBaseImage_Loaded(object sender, RoutedEventArgs e)
        {
            CreatBlurVisualToImage();
        }

        Microsoft.Graphics.Canvas.Effects.GaussianBlurEffect blur = new Microsoft.Graphics.Canvas.Effects.GaussianBlurEffect()
        {
            Source = new Microsoft.UI.Composition.CompositionEffectSourceParameter("Source"),
            BlurAmount = 250
        };
        private void CreatBlurVisualToImage()
        {
            return;
            var imageVisual = ElementCompositionPreview.GetElementVisual(BackgroundBaseImage);
            var compositor = imageVisual.Compositor;
            var blurFactory = compositor.CreateEffectFactory(blur);
            var blurBrush = blurFactory.CreateBrush();
            blurBrush.SetSourceParameter("Source", compositor.CreateBackdropBrush());
            var blurVisual = compositor.CreateSpriteVisual();
            blurVisual.Brush = blurBrush;
            blurVisual.RelativeSizeAdjustment = new System.Numerics.Vector2(1f, 1f);
            ElementCompositionPreview.SetElementChildVisual(BackgroundBaseImage, blurVisual);
        }

        bool isMiniPage = false;
        bool isMiniPageOnlyLyric = false;
        bool isMiniPageLyricCenter = false;
        public void UpdateInterfaceDesign()
        {
            if (!ShowLrcPage)
            {
                if (InfoBaseGrid.ActualHeight >= 850)
                {
                }
                else
                {
                }
            }
            else
            {
                if (ActualWidth >= 900)
                {
                    isMiniPage = false;
                    LyricSccondRow.Height = new(0);
                    LrcPageColumn.Width = new(1.4, GridUnitType.Star);
                    //BridgeTb.TextAlignment = TextAlignment.Left;
                    LyricItem.SetTextAlignmentS(TextAlignment.Left);
                    InfoBaseGrid.Margin = new(0, 0, 30, 0);

                    ImageVer.Height = new(1, GridUnitType.Star);
                    AlbumImageBorder.VerticalAlignment = VerticalAlignment.Center;
                    AlbumImageBorder.MaxWidth = double.MaxValue;
                    AlbumImageBorder.MaxHeight = double.MaxValue;
                }
                else
                {
                    isMiniPage = true;
                    LyricSccondRow.Height = new(1, GridUnitType.Star);
                    LrcPageColumn.Width = new(0);
                    //BridgeTb.TextAlignment = TextAlignment.Center;
                    LyricItem.SetTextAlignmentS(TextAlignment.Center);
                    InfoBaseGrid.Margin = new(0);
                    AlbumImageBorder.VerticalAlignment = VerticalAlignment.Top;
                    AlbumImageBorder.MaxWidth = InfoBaseGrid.ActualHeight;
                    AlbumImageBorder.MaxHeight = InfoBaseGrid.ActualHeight / 1.5;

                    if (InfoBaseGrid.ActualHeight <= 616 || InfoBaseGrid.ActualWidth <= 160)
                    {
                        isMiniPageOnlyLyric = true;
                        ImageVer.Height = new(0, GridUnitType.Pixel);
                    }
                    else
                    {
                        isMiniPageOnlyLyric = false;
                        ImageVer.Height = new(1, GridUnitType.Auto);
                    }

                    if (LyricSecondPlaceGrid.ActualHeight >= 260 || isMiniPageOnlyLyric)
                        isMiniPageLyricCenter = true;
                    else
                        isMiniPageLyricCenter = false;
                }
                SelectedChangedDo(true);
            }
        }

        public async void SelectedChangedDo(bool disableAnimation = false)
        {
            if (App.Instance.lyricManager is null) return;
            if (App.Instance.lyricManager.NowLyricsData is null) return;

            isCodeChangedLrcItem = true;
            LrcBaseListView.SelectedItem = App.Instance.lyricManager.NowLyricsData;
            LrcSecondListView.SelectedItem = App.Instance.lyricManager.NowLyricsData;
            isCodeChangedLrcItem = false;

            var sv = isMiniPage ? scrollViewer1 : scrollViewer;
            if (sv != null && !inScroll && App.Instance.lyricManager.NowLyricsData.Lyric != null)
            {
                var c = isMiniPage ?
                    LrcSecondListView.ContainerFromIndex(LrcBaseListView.SelectedIndex) as UIElement :
                    LrcBaseListView.ContainerFromIndex(LrcBaseListView.SelectedIndex) as UIElement;
                if (c is null)
                {
                    if (!isMiniPage)
                    {
                        LrcBaseListView.ScrollIntoView(App.Instance.lyricManager.NowLyricsData, ScrollIntoViewAlignment.Default);
                        c = LrcBaseListView.ContainerFromIndex(LrcBaseListView.SelectedIndex) as UIElement;
                        LrcBaseListView.ScrollIntoView(App.Instance.lyricManager.NowLyricsData, ScrollIntoViewAlignment.Default);
                    }
                    else
                    {
                        LrcSecondListView.ScrollIntoView(App.Instance.lyricManager.NowLyricsData, ScrollIntoViewAlignment.Default);
                        c = LrcSecondListView.ContainerFromIndex(LrcSecondListView.SelectedIndex) as UIElement;
                        LrcSecondListView.ScrollIntoView(App.Instance.lyricManager.NowLyricsData, ScrollIntoViewAlignment.Default);
                    }
                }
                if (c != null)
                {
                    if (!isMiniPage)
                    {
                        //ScrollViewerBehavior.(scrollViewer, c.ActualOffset.Y + c.ActualSize.Y / 2 + LrcBaseListView.ActualHeight / 25 + 48);
                        //sv.GetAnimationBaseValue(sv.ver);
                        //if (disableAnimation)
                        sv.ChangeView(null, c.ActualOffset.Y + c.ActualSize.Y / 2, null, disableAnimation);
                        /*
                        else
                            sv.ChangeView(null, c.ActualOffset.Y + c.ActualSize.Y / 2, null);*/
                        //await LrcBaseListView.SmoothScrollIntoViewWithItemAsync(App.Instance.lyricManager.NowLyricsData, ScrollItemPlacement.Center);
                    }
                    else
                    {
                        if (isMiniPageLyricCenter)
                            //await LrcSecondListView.SmoothScrollIntoViewWithItemAsync(App.Instance.lyricManager.NowLyricsData, ScrollItemPlacement.Center, disableAnimation);
                            sv.ChangeView(null, c.ActualOffset.Y + c.ActualSize.Y / 2, null, disableAnimation);
                        else
                            await LrcSecondListView.SmoothScrollIntoViewWithItemAsync(App.Instance.lyricManager.NowLyricsData, ScrollItemPlacement.Top, disableAnimation);
                            //sv.ChangeView(null, c.ActualOffset.Y - c.ActualSize.Y, null, disableAnimation);
                    }
                }
            }
#if DEBUG
            //LogManager.Log($"MusicPage: 选中歌词已被更改为: {App.Instance.lyricManager.NowLyricsData?.Lyric[0]}");
#endif
        }

        //todo：优化性能
        private void LyricManager_PlayingLyricSelectedChange1(LyricData nowLyricsData)
        {
            if (ShowLrcPage)
            {
                if (nowLyricsData != null) App.Instance.lyricManager.StartTimer();
                SelectedChangedDo();
            }
        }

        private void LyricManager_PlayingLyricSourceChange(System.Collections.ObjectModel.ObservableCollection<DataEditor.LyricData> nowPlayingLyrics)
        {

        }

        bool inScroll = false;
        int scrollCount = 0;
        async void ScrollingLrcView()
        {
            scrollCount++;
            inScroll = true;
            await Task.Delay(2500);
            if (scrollCount <= 1)
            {
                inScroll = false;
                SelectedChangedDo();
            }
            scrollCount--;
        }

        private void LrcSecondListView_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            ScrollingLrcView();
        }
        
        private void MusicPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateInterfaceDesign();

            CloseMusicPageButton.Width = App.MainWindowInstance.NavView.DisplayMode == NavigationViewDisplayMode.Minimal ? 86 : 44;
        }

        private void LyricManager_LyricTimingChanged(LyricData nowLyricsData)
        {
#if DEBUG
            if (debugNowTimingChangeEventLightVisual != null)
            {
                debugNowTimingChangeEventLightVisual.Opacity = .8f;
                debugNowTimingChangeEventLightVisual.StartAnimation(nameof(debugNowTimingChangeEventLightVisual.Opacity),
                    debugNowTimingChangeEventLightAnimation);
            }
#endif
        }

        MusicData MusicData;
        private void AudioPlayer_SourceChanged(Media.AudioPlayer audioPlayer)
        {
            if (audioPlayer.MusicData is null) return;
            TitleRunText.Text = audioPlayer.MusicData.Title;
            Title2RunText.Text = audioPlayer.MusicData.Title2;
            ArtistRunText.Text = audioPlayer.MusicData.ArtistName;
            AlbumRunText.Text = audioPlayer.MusicData.Album.Title;
            OtherRunText.Text = audioPlayer.MusicData.From == MusicFrom.pluginMusicSource ? audioPlayer.MusicData.PluginInfo.Name : MusicFrom.localMusic.ToString();
            AudioInfoRunText.Text = audioPlayer.WaveInfo;

            if (audioPlayer.MusicData != MusicData)
            {
                MusicData = audioPlayer.MusicData;
            }
        }

        private void AudioPlayer_PlayStateChanged(Media.AudioPlayer audioPlayer)
        {
            MediaPlayStateViewer1.PlaybackState = audioPlayer.PlaybackState;
/*
            if (audioPlayer.PlaybackState == PlaybackState.Playing)
            {
                App.Instance.lyricManager.PlayingLyricSourceChanged += LyricManager_PlayingLyricSourceChange;
                App.Instance.lyricManager.PlayingLyricSelectedChanged += LyricManager_PlayingLyricSelectedChange1;
            }
            else
            {
                App.Instance.lyricManager.PlayingLyricSourceChanged -= LyricManager_PlayingLyricSourceChange;
                App.Instance.lyricManager.PlayingLyricSelectedChanged -= LyricManager_PlayingLyricSelectedChange1;
            }*/
        }

        private void AudioPlayer_VolumeChanged(Media.AudioPlayer audioPlayer, object data)
        {
            float volume = (float)data;

            if (volume == 0)
            {
                VolumeIcon.Glyph = "\xE198";
            }
            else
            {
                if (volume <= 100 && volume > 67)
                    VolumeIcon.Glyph = "\xE995";
                else if (volume <= 67 && volume > 33)
                    VolumeIcon.Glyph = "\xE994";
                else if (volume <= 33)
                    VolumeIcon.Glyph = "\xE993";
            }
        }

        private void AudioPlayer_TimingChanged(Media.AudioPlayer audioPlayer)
        {
            if (audioPlayer.FileReader is null) return;
#if DEBUG
            Debug_NowPlayingDurationText.Text = $"{audioPlayer.CurrentTime} - {App.Instance.lyricManager.UpdateInterval}ms";
#endif

            isCodeChangedSliderValue = true;
            PlaySlider.Minimum = 0;
            PlaySlider.Maximum = audioPlayer.TotalTime.Ticks;
            PlaySlider.Value = audioPlayer.CurrentTime.Ticks;
            isCodeChangedSliderValue = false;
            NowPlayTimeTb.Text =
                $"{audioPlayer.CurrentTime:mm\\:ss}/{audioPlayer.TotalTime.ToString(@"mm\:ss")}";
            NowAtherTimeTb.Text =
                (audioPlayer.TotalTime - audioPlayer.CurrentTime).ToString(@"mm\:ss");
        }

        private void AudioPlayer_PlayEnd(Media.AudioPlayer audioPlayer)
        {
            PlaySlider.Maximum = 0;
            PlaySlider.Value = 0;
        }

        private void AudioPlayer_CacheLoadingChanged(Media.AudioPlayer audioPlayer, object data)
        {
            isCodeChangedSliderValue = true;
            PlayButton.IsEnabled = true;
            AudioLoadingProressRing.IsIndeterminate = true;
        }

        private void AudioPlayer_CacheLoadedChanged(Media.AudioPlayer audioPlayer)
        {
            isCodeChangedSliderValue = false;
            PlayButton.IsEnabled = true;
            AudioLoadingProressRing.IsIndeterminate = false;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            App.MainWindowInstance.OpenOrCloseMusicPage();
        }

        private async void BeforeButton_Click(object sender, RoutedEventArgs e)
        {
            await App.Instance.playingList.PlayPrevious();
        }

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

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            await App.Instance.playingList.PlayNext();
        }

        private void PlayListButton_Click(object sender, RoutedEventArgs e)
        {
            App.MainWindowInstance.OpenOrClosePlayingList(
                HorizontalAlignment.Left,
                flyoutPlacementMode: Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.RightEdgeAlignedBottom,
                placementMargin: new(30, 0, 0, ControlBar.ActualHeight - PlaySlider.ActualHeight + 8));
        }

        private void VolumeButton_Click(object sender, RoutedEventArgs e)
        {
            App.MainWindowInstance.OpenOrCloseVolume(
                HorizontalAlignment.Left,
                flyoutPlacementMode: Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.RightEdgeAlignedBottom,
                placementMargin: new(30, 0, 0, ControlBar.ActualHeight - PlaySlider.ActualHeight + 8));
        }

        static ScrollViewer scrollViewer = null;
        static ScrollViewer scrollViewer1 = null;
        private void LrcBaseListView_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Recognizer_ManipulationUpdated(GestureRecognizer sender, ManipulationUpdatedEventArgs args)
        {
            //System.Diagnostics.LogManager.Log("U");
            /*double destY = (PlaybackDetailFrame.RenderTransform as CompositeTransform).TranslateY + args.Delta.Translation.Y;
            if (destY <= 0) destY = 0;
            if (destY >= ActualHeight) destY = ActualHeight;
            (PlaybackDetailFrame.RenderTransform as CompositeTransform).TranslateY = destY;*/
        }

        private void Recognizer_ManipulationCompleted(GestureRecognizer sender, ManipulationCompletedEventArgs args)
        {
            //System.Diagnostics.LogManager.Log("C");
            /*
            double destY = (PlaybackDetailFrame.RenderTransform as CompositeTransform).TranslateY;
            SetPlaybackDetailFrameVisibility(destY <= ActualHeight * 0.4);*/
        }

        bool isCodeChangedLrcItem = false;
        int changeCount = 0;
        private void LrcBaseListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (changeCount <= 1)
            {
                changeCount++;
                return;
            }
            if (isCodeChangedLrcItem) return;

            var lrcItem = (sender as ListView).SelectedItem as LyricData;
            if (lrcItem != null )
            {
                if (lrcItem.Lyric is null) return;
                // 加1ms，否则会短时间判定到上一句歌词
                App.Instance.audioPlayer.CurrentTime = lrcItem.LyricTimeSpan + TimeSpan.FromMilliseconds(App.Instance.audioPlayer.Latency + 1);
                LogManager.Log("MusicPage", "Changed AudioPlayer current time.");
            }
        }

#if DEBUG
        private Visual debugNowTimingChangeEventLightVisual;
        private ScalarKeyFrameAnimation debugNowTimingChangeEventLightAnimation;
#endif
        private void LrcBaseListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            LrcBaseListView.Padding = new Thickness(0, LrcBaseListView.ActualHeight / 2, 0, LrcBaseListView.ActualHeight / 2);
            LrcSecondListView.Padding = new Thickness(0, LrcSecondListView.ActualHeight / 2, 0, LrcSecondListView.ActualHeight / 2);
        }

        private void pageRoot_Loaded(object sender, RoutedEventArgs e)
        {
            //ShowLrcPage = ShowLrcPage;
            var a = VisualTreeHelper.GetChild(LrcBaseListView, 0) as Border;
            var b = VisualTreeHelper.GetChild(LrcSecondListView, 0) as Border;
            if (a != null)
                scrollViewer = a.Child as ScrollViewer;
            if (b != null)
                scrollViewer1 = b.Child as ScrollViewer;
            scrollViewer.CanContentRenderOutsideBounds = false;
            scrollViewer1.CanContentRenderOutsideBounds = false;
            //scrollViewer.ViewChanging += ScrollViewer_ViewChanging;
            //scrollViewer1.ViewChanging += ScrollViewer_ViewChanging;
            scrollViewer.AddHandler(ScrollViewer.PointerWheelChangedEvent, new PointerEventHandler(LrcSecondListView_PointerWheelChanged), true);
            scrollViewer1.AddHandler(ScrollViewer.PointerWheelChangedEvent, new PointerEventHandler(LrcSecondListView_PointerWheelChanged), true);

            UpdateInterfaceDesign();
            Debug_Root.Visibility = Visibility.Collapsed;
#if DEBUG
            Debug_Root.Visibility = Visibility.Visible;
            AnimateHelper.AnimateScalar(Debug_NowTimingChangeEventLight, .4f, .2f, 1, 1, 1, 1,
                out debugNowTimingChangeEventLightVisual, out var c, out debugNowTimingChangeEventLightAnimation);
#endif
        }

        private void ScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            //ScrollingLrcView();
        }

        private void LrcButton_Checked(object sender, RoutedEventArgs e)
        {
            ShowLrcPage = true;
        }

        private void LrcButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ShowLrcPage = false;
        }

        bool isCodeChangedSliderValue = false;
        bool isFirstChangeValue = true;
        private void PlaySlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (isFirstChangeValue)
            {
                isFirstChangeValue = false;
                return;
            }
            if (!isCodeChangedSliderValue && !isCodeChangedLrcItem && App.Instance.audioPlayer.FileReader != null)
            {
                App.Instance.audioPlayer.CurrentTime = TimeSpan.FromTicks((long)PlaySlider.Value);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            App.MainWindowInstance.OpenOrCloseMusicPage();
        }

        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.MainWindowInstance.AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen)
            {
                FullScreenIcon.Glyph = "\xE1D9";
                App.MainWindowInstance.PresenterKind = AppWindowPresenterKind.Default;
            }
            else
            {
                FullScreenIcon.Glyph = "\xE1D8";
                App.MainWindowInstance.PresenterKind = AppWindowPresenterKind.FullScreen;
            }
        }

        private async void EqButton_Click(object sender, RoutedEventArgs e)
        {
            await App.MainWindowInstance.ShowEqualizerDialog();
        }

        private void InfoBaseTitle_Loaded(object sender, RoutedEventArgs e)
        {/*
            foreach (var data in App.Instance.audioPlayer.MusicData.Artists)
            {
                var item = new MenuFlyoutItem()
                {
                    Text = data.Name,
                    Tag = data
                };
                item.Click += (sender, e) =>
                {
                    ListViewPages.ListViewPage.SetPageToListViewPage<ItemListViewArtist>((sender as MenuFlyoutItem).Tag);
                    App.MainWindowInstance.OpenOrCloseMusicPage();
                };
                ArtistFlyout.Items.Add(item);
            }*/
        }

        private void ArtistFlyout_Opening(object sender, object e)
        {
            ArtistFlyout.Items.Clear();
            foreach (var data in App.Instance.audioPlayer.MusicData.Artists)
            {
                var item = new MenuFlyoutItem()
                {
                    Text = data.Name,
                    Tag = data
                };
                item.Click += (sender, e) =>
                {
                    ListViewPages.ListViewPage.SetPageToListViewPage(new() { PageType = ListViewPages.PageType.Artist, Param = (sender as MenuFlyoutItem).Tag });
                    App.MainWindowInstance.OpenOrCloseMusicPage();
                };
                ArtistFlyout.Items.Add(item);
            }
        }

        private void ArtistRunButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TitleRunButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AlbumRunButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void OtherRunButton_Click(object sender, RoutedEventArgs e)
        {
            string url = null;
            switch (MusicData.From)
            {
                case MusicFrom.pluginMusicSource:
                    url = $"https://music.163.com/#/song?id={MusicData.ID}";
                    break;
            }

            if (url != null)
            {
                var success = await CodeHelper.OpenInBrowser(url);
            }
        }

        private void TitleSearchMenuItem_Click(object sender, RoutedEventArgs e)
        {
            App.MainWindowInstance.SetNavViewContent(typeof(SearchPage), MusicData.Title);
            App.MainWindowInstance.OpenOrCloseMusicPage();
        }

        private void CopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dp = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dp.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            dp.SetText(MusicData.Title);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);
        }

        private async void AlbumMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ListViewPages.ListViewPage.SetPageToListViewPage(new() { PageType = ListViewPages.PageType.Album, Param = MusicData.Album });
            App.MainWindowInstance.OpenOrCloseMusicPage();
        }

        private async void AudioInfoButton_Click(object sender, RoutedEventArgs e)
        {
            await App.MainWindowInstance.ShowDialog("播放信息", new DialogPages.AudioInfoPage());
        }

        private void TitleFlyout_Opening(object sender, object e)
        {
            if (string.IsNullOrEmpty(MusicData.Title2))
                TitleMenuFlyoutText.Text = MusicData.Title;
            else
                TitleMenuFlyoutText.Text = $"{MusicData.Title}（{MusicData.Title2}）";
        }

        private void AlbumFlyout_Opening(object sender, object e)
        {
            AlbumFlyout.Items.Clear();
            var a = new MenuFlyoutItem() { Text = "复制" };
            var a1 = new MenuFlyoutItem() { Text = "打开专辑详情页" };
            a.Click += A_Click;
            a1.Click += A_Click;
            AlbumFlyout.Items.Add(a);
            AlbumFlyout.Items.Add(a1);
        }

        private void A_Click(object sender, RoutedEventArgs e)
        {
            var a = sender as MenuFlyoutItem;
            if (a != null)
            {
                if  (a.Text == "复制")
                {
                    var dp = new Windows.ApplicationModel.DataTransfer.DataPackage();
                    dp.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
                    dp.SetText(MusicData.Album.Title);
                    Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);
                }
                else
                {
                    ListViewPages.ListViewPage.SetPageToListViewPage(new() { PageType = ListViewPages.PageType.Album, Param = MusicData.Album });
                    App.MainWindowInstance.OpenOrCloseMusicPage();
                }
            }
        }

        private void BackgroundFillBase_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            MusicDataFlyout.SongItemBind = new() { MusicData = MusicData };
            MusicDataFlyout.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));
        }
    }
}
