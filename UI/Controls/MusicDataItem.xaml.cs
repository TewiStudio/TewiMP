using CommunityToolkit.WinUI.Animations;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TewiMP.Core.Models;
using TewiMP.Core.Music;
using TewiMP.Helpers;
using TewiMP.Services.Media;
using TewiMP.Services.Media.Audio;

namespace TewiMP.UI.Controls
{
    public sealed partial class MusicDataItem : UserControl
    {
        #region Static Methods
        static bool isMouseEventClosed = false;
        static bool isStaticInited = false;
        static List<MusicDataItem> staticMusicDataItem = [];
        ArrayList arrayList;
        static void initListen()
        {
            if (isStaticInited) return;
            isStaticInited = true;
            App.Instance.AudioService.SourceChanged += (_) =>
            {
                foreach (MusicDataItem item in staticMusicDataItem)
                {
                    item.InitPlayingState();
                }
            };
        }

        public static void StartConnectAnimation(MusicData targetData)
        {
            MusicDataItem equaled = null;
            foreach (var i in staticMusicDataItem)
            {
                if (i?.ViewModel?.MusicData is not null)
                {
                    if (i.ViewModel.MusicData == targetData)
                    {
                        equaled = i;
                    }
                }
            }
            if (equaled is null)
            {
                //App.MainWindowInstance.PlayContent_Image.TransitionType = App.Instance.PlayingList.IsNextPlay == TewiMP.Background.SetPlayInfo.Previous ? ImageTransitionType.SlideRight : ImageTransitionType.SlideLeft;
                //App.MainWindowInstance.PlayContent_Image.TransitionType = ImageTransitionType.Blur;
            }
            else
            {
                //App.MainWindowInstance.PlayContent_Image.TransitionType = ImageTransitionType.None;
                equaled.Info_Root.Opacity = 0;
                ConnectedAnimation canimation =
                    ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("changeAnimation", equaled.Info_Image);
                canimation.Configuration = new BasicConnectedAnimationConfiguration();
                ConnectedAnimation animation =
                    ConnectedAnimationService.GetForCurrentView().GetAnimation("changeAnimation");
                if (animation != null)
                {
                    animation.Completed += (_, __) => equaled.Info_Root.Opacity = 1;
                    animation.TryStart(VisualTreeHelper.GetParent(App.MainWindowInstance.PlayContent) as UIElement);
                }

                ConnectedAnimation canimation1 =
                    ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("changeAnimation1", equaled.Info_Texts_Textblock);
                canimation1.Configuration = new BasicConnectedAnimationConfiguration();
                ConnectedAnimation animation1 =
                    ConnectedAnimationService.GetForCurrentView().GetAnimation("changeAnimation1");
                if (animation1 != null)
                {
                    animation1.TryStart(App.MainWindowInstance.PlayTitle);
                }
                ConnectedAnimation canimation2 =
                    ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("changeAnimation2", equaled.Info_Texts_ButtonNameTextBlock);
                canimation2.Configuration = new BasicConnectedAnimationConfiguration();
                ConnectedAnimation animation2 =
                    ConnectedAnimationService.GetForCurrentView().GetAnimation("changeAnimation2");
                if (animation2 != null)
                {
                    animation2.TryStart(App.MainWindowInstance.PlayArtist);
                }
            }
        }

        public static bool TryHighlightPlayingItem()
        {
            bool result = false;
            foreach (MusicDataItem item in staticMusicDataItem)
            {
                item.SetHighlight(item.IsMusicDataPlaying);
                if (item.IsMusicDataPlaying) result = true;
            }
            return result;
        }

        public static bool TryHighlight(MusicDataViewModel songItemBind)
        {
            bool result = false;
            foreach (MusicDataItem item in staticMusicDataItem)
            {
                item.SetHighlight(item.ViewModel == songItemBind);
                if (item.ViewModel == songItemBind) result = true;
            }
            return result;
        }

        public static bool TryHighlight(MusicData musicData)
        {
            bool result = false;
            foreach (MusicDataItem item in staticMusicDataItem)
            {
                item.SetHighlight(item.ViewModel.MusicData == musicData);
                if (item.ViewModel.MusicData == musicData) result = true;
            }
            return result;
        }

        public static void SetIsCloseMouseEvent(bool value, bool showMoveIcon = false)
        {
            isMouseEventClosed = value;
            foreach (MusicDataItem item in staticMusicDataItem)
            {
                item.Info_Texts_ButtonNameButton.IsHitTestVisible = !value;
                item.Info_MoveIcon.Visibility =
                    value ? showMoveIcon ? Visibility.Visible : Visibility.Collapsed : Visibility.Collapsed;
            }
        }
        #endregion

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(MusicDataViewModel), typeof(MusicDataItem), new PropertyMetadata(null));

        public MusicDataViewModel ViewModel
        {
            get => (MusicDataViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public bool IsMusicDataPlaying
        {
            get => ViewModel?.MusicData == App.Instance.AudioService.MusicData;
        }

        public MusicDataItem()
        {
            initListen(); // 静态初始化，只在程序第一次运行时执行一次
            InitializeComponent();
            //arrayList = new ArrayList(10000000);
        }

        void InitInfo()
        {

        }

        private CancellationTokenSource _imageLoadCts;
        async Task InitImage()
        {
            if (ViewModel is null || Info_Image is null) return;

            // 在专辑列表下时不加载图片
            if (ViewModel.MusicListData?.ListDataType == DataType.Album)
            {
                Info_Image_Root.Visibility = Visibility.Collapsed;
                return;
            }

            if (ViewModel.MusicData.From == MusicFrom.localMusic)
            {
                // 为 midi 文件时不加载图片
                if (Path.GetExtension(ViewModel.MusicData.InLocal) == ".mid")
                {
                    Info_Image.Source = null;
                    Info_Image_Root.Visibility = Visibility.Collapsed;
                    return;
                }
                // 文件不存在时不加载图片
                if (!File.Exists(ViewModel.MusicData.InLocal))
                {
                    Info_Image_Root.Visibility = Visibility.Collapsed;
                    FileNotExists_Root.Visibility = Visibility.Visible;
                    return;
                }
            }

            _imageLoadCts?.Cancel();
            _imageLoadCts?.Dispose();
            _imageLoadCts = new CancellationTokenSource();
            var token = _imageLoadCts.Token;
            try
            {
                SetImageBorder(false);
                Info_Image_Root.Visibility = Visibility.Visible;
                FileNotExists_Root.Visibility = Visibility.Collapsed;
                Info_Image.Source = null;

                await Task.Delay(200);
                if (token.IsCancellationRequested) return;

                MusicData targetMusicData = ViewModel.MusicData;
                Uri result = await ImageService.GetImageUri(targetMusicData);
                if (IsLoaded && ViewModel is not null &&
                    result != null && targetMusicData == ViewModel.MusicData &&
                    !token.IsCancellationRequested)
                {
                    Info_Image.Source = result;
                    SetImageBorder(true);
                }
                else
                {
                    Info_Image_Root.Visibility = Visibility.Collapsed;
                }
            }
            catch (TaskCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        Visual backgroundFillVisual;
        Visual rightButtonVisual;
        Visual strokeVisual;
        private static readonly AnimationBuilder MouseInOpacityFadeInAnimation = 
            AnimationBuilder.Create().Opacity(1, duration: TimeSpan.FromSeconds(.2));
        private static readonly AnimationBuilder MouseInOpacityFadeOutAnimation = 
            AnimationBuilder.Create().Opacity(0, duration: TimeSpan.FromSeconds(.2));
        private static readonly AnimationBuilder HighlightStrokeOpacityFadeOutAnimation = 
            AnimationBuilder.Create().Opacity().TimedKeyFrames(k => k
                .KeyFrame(TimeSpan.FromSeconds(1), 1)
                .KeyFrame(TimeSpan.FromSeconds(4), 0));
        private static readonly AnimationBuilder HighlightStrokeOpacityFadeOutFastAnimation = 
            AnimationBuilder.Create().Opacity(0, duration: TimeSpan.FromSeconds(.2));
        void InitVisuals()
        {
            backgroundFillVisual = ElementCompositionPreview.GetElementVisual(Background_FillRectangle);
            rightButtonVisual = ElementCompositionPreview.GetElementVisual(Info_Buttons_Root);
            strokeVisual = ElementCompositionPreview.GetElementVisual(Background_HighlightRectangle);

            backgroundFillVisual.Opacity = 0;
            rightButtonVisual.Opacity = 0;
            strokeVisual.Opacity = 0;
        }

        bool last_IsMusicDataPlaying = false;
        void InitPlayingState()
        {
            if (IsMusicDataPlaying)
            {
                App.Instance.AudioService.PlayStateChanged -= AudioService_PlayStateChanged;
                App.Instance.AudioService.PlayStateChanged += AudioService_PlayStateChanged;
                SetPlayingIcon(App.Instance.AudioService.PlaybackState);
                OnMouseIn();
                Background_PlayingRectangle.Opacity = 1;
                last_IsMusicDataPlaying = true;
            }
            else
            {
                App.Instance.AudioService.PlayStateChanged -= AudioService_PlayStateChanged;
                if (last_IsMusicDataPlaying) // 只有当上次调用此函数时 IsMusicDataPlaying 判断为 true 时才执行下面的恢复样式代码
                {
                    SetPlayingIcon(NAudio.Wave.PlaybackState.Paused);
                    OnMouseLeave();
                    Background_PlayingRectangle.Opacity = 0;
                }
                last_IsMusicDataPlaying = false;
            }
        }

        void SetHighlight(bool value)
        {
            if (value)
            {
                HighlightStrokeOpacityFadeOutAnimation.Start(Background_HighlightRectangle);
            }
            else
            {
                HighlightStrokeOpacityFadeOutFastAnimation.Start(Background_HighlightRectangle);
            }
        }

        void SetImageBorder(bool isShow)
        {
            if (isShow)
            {
                Info_Image_Root.Opacity = 1;
            }
            else
                Info_Image_Root.Opacity = 0;
        }

        void SetPlayingIcon(NAudio.Wave.PlaybackState playbackState)
        {
            if (playbackState == NAudio.Wave.PlaybackState.Playing)
            {
                Info_Buttons_MediaStateIcon.Glyph = "\xE769";
            }
            else
            {
                Info_Buttons_MediaStateIcon.Glyph = "\xE768";
            }
        }

        void OnMouseIn()
        {
            if (!IsLoaded && ViewModel is null) return;
            Info_Buttons_Root.Visibility = Visibility.Visible;

            if (App.Instance.UISettings.AnimationsEnabled)
            {
                MouseInOpacityFadeInAnimation.Start(Background_FillRectangle);
                MouseInOpacityFadeInAnimation.Start(Info_Buttons_Root);
            }
            else
            {
                if (backgroundFillVisual is not null)
                {
                    backgroundFillVisual.Opacity = 1;
                    rightButtonVisual.Opacity = 1;
                }
            }
        }

        async Task OnMouseLeave()
        {
            if (!IsLoaded || ViewModel is null) return;

            if (App.Instance.UISettings.AnimationsEnabled)
            {
                var ani1 = MouseInOpacityFadeOutAnimation.StartAsync(Background_FillRectangle);
                var ani2 = MouseInOpacityFadeOutAnimation.StartAsync(Info_Buttons_Root);
                await Task.WhenAll(ani1, ani2);
            }
            else
            {
                if (backgroundFillVisual is not null)
                {
                    backgroundFillVisual.Opacity = 0;
                    rightButtonVisual.Opacity = 0;
                }
            }
            if (!isPointEnter) Info_Buttons_Root.Visibility = Visibility.Collapsed;
        }

        async Task Play()
        {
            await App.Instance.PlayingListService.Play(ViewModel.MusicData, true);
        }

        private void MusicDataItem_Completed(object sender, CompositionBatchCompletedEventArgs args)
        {
            if (!isPointEnter) Info_Buttons_Root.Visibility = Visibility.Collapsed;
        }

        private void AudioService_PlayStateChanged(AudioService AudioService)
        {
            SetPlayingIcon(AudioService.PlaybackState);
        }

        private async void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (sender is null) return;
            if (sender.DataContext is null) return;
            if (sender.DataContext is not MusicDataViewModel) return;
            ViewModel = sender.DataContext as MusicDataViewModel;
            musicDataFlyout.SongItemBind = ViewModel;
            if (!IsLoaded) return;
            strokeVisual.Opacity = 0;
            if (ViewModel is null) return;
            InitInfo();
            InitPlayingState();
            await InitImage();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            InitVisuals();
            staticMusicDataItem.Add(this);
            UserControl_DataContextChanged(sender as FrameworkElement, null);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (rightButtonVisual != null) 
            {
                rightButtonVisual.Compositor.GetCommitBatch(CompositionBatchTypes.Animation).Completed -= MusicDataItem_Completed;
            }
            App.Instance.AudioService.PlayStateChanged -= AudioService_PlayStateChanged;
            
            _imageLoadCts?.Cancel();
            _imageLoadCts?.Dispose();

            staticMusicDataItem.Remove(this);
            ViewModel = null;
            Info_Image.Source = null;
        }

        bool isPointEnter = false;
        private void UserControl_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            isPointEnter = true;
            if (e.GetCurrentPoint(sender as UIElement).PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Touch)
            {
                Info_Texts_ButtonNameButton.Visibility = Visibility.Collapsed;
                return;
            }
            else Info_Texts_ButtonNameButton.Visibility = Visibility.Visible;
            if (isMouseEventClosed) return;
            OnMouseIn();
        }

        private void UserControl_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (IsMusicDataPlaying) return;
            if (isMouseEventClosed) return;
            if (!isPointEnter) return;
            isPointEnter = false;
            OnMouseLeave();
        }


        private async void UserControl_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (isMouseEventClosed) return;
            await Play();
        }

        private void UserControl_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (isMouseEventClosed) return;
            musicDataFlyout.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));
        }

        private void UserControl_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (isMouseEventClosed) return;
            musicDataFlyout.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            musicDataFlyout.ShowAt(sender as FrameworkElement);
        }

        private void Info_Texts_ButtonNameTextBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Info_Texts_ButtonNameButton.Width = Info_Texts_ButtonNameTextBlock.ActualWidth;
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (IsMusicDataPlaying)
            {
                if (App.Instance.AudioService.PlaybackState == NAudio.Wave.PlaybackState.Playing)
                    App.Instance.AudioService.SetPause();
                else
                    App.Instance.AudioService.SetPlay();
            }
            else
                await Play();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {

        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            Pages.ListViewPages.ListViewPage.SetPageToListViewPage(new() { PageType = Pages.ListViewPages.PageType.Album, Param = ViewModel.MusicData.Album });
        }

        private void Info_Texts_FlyoutMenu_Artist_Item_Loaded(object sender, RoutedEventArgs e)
        {
            Info_Texts_FlyoutMenu_Album_Item.Text = $"专辑：{ViewModel.MusicData.Album.Title}";
            Info_Texts_FlyoutMenu_Artist_Item.Items.Clear();
            foreach (var artist in ViewModel.MusicData.Artists)
            {
                var mfi = new MenuFlyoutItem()
                {
                    Text = artist.Name,
                    Tag = artist
                };
                mfi.Click += (_, __) =>
                {
                    Pages.ListViewPages.ListViewPage.SetPageToListViewPage(new() { PageType = Pages.ListViewPages.PageType.Artist, Param = (_ as FrameworkElement).Tag });
                };
                Info_Texts_FlyoutMenu_Artist_Item.Items.Add(mfi);
            }

        }

        private void Info_Texts_FlyoutMenu_Artist_Item_Unloaded(object sender, RoutedEventArgs e)
        {
            Info_Texts_FlyoutMenu_Artist_Item.Items.Clear();
        }

        private void UserControl_GotFocus(object sender, RoutedEventArgs e)
        {
            //if (isMouseEventClosed) return;
            //OnMouseIn();
            //Info_Buttons_StackPanel.Focus(FocusState.Keyboard);
        }

        private void UserControl_LostFocus(object sender, RoutedEventArgs e)
        {
            //OnMouseLeave();
        }

        private void Info_Image_ImageLoaded(bool isLoaded)
        {
            if (isLoaded)
                SetImageBorder(true);
        }
    }
}
