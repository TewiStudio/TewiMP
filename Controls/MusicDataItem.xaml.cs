using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Composition;
using TewiMP.Media;
using TewiMP.Helpers;
using TewiMP.DataEditor;

namespace TewiMP.Controls
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
            App.audioPlayer.SourceChanged += (_) =>
            {
                foreach (MusicDataItem item in staticMusicDataItem)
                {
                    item.InitPlayingState();
                }
            };
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

        public static bool TryHighlight(SongItemBindBase songItemBind)
        {
            bool result = false;
            foreach (MusicDataItem item in staticMusicDataItem)
            {
                item.SetHighlight(item.songItemBind == songItemBind);
                if (item.songItemBind == songItemBind) result = true;
            }
            return result;
        }

        public static bool TryHighlight(MusicData musicData)
        {
            bool result = false;
            foreach (MusicDataItem item in staticMusicDataItem)
            {
                item.SetHighlight(item.songItemBind.MusicData == musicData);
                if (item.songItemBind.MusicData == musicData) result = true;
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

        bool _isImageShow = true;
        public bool IsImageShow
        {
            get => _isImageShow;
            set => _isImageShow = value;
        }

        public bool IsMusicDataPlaying
        {
            get => songItemBind?.MusicData == App.audioPlayer.MusicData;
        }

        SongItemBindBase songItemBind;
        public MusicDataItem()
        {
            initListen();
            InitializeComponent();
            InitVisuals();
            //arrayList = new ArrayList(10000000);
        }

        void InitInfo()
        {
            if (!IsLoaded) return;
            if (songItemBind is null) return;
            Info_Texts_CountRun.Text = songItemBind.MusicData.Count == 0 ? null : $"{songItemBind.MusicData.Count}. ";
            Info_Texts_TitleRun.Text = songItemBind.MusicData.Title;
            Info_Texts_Title2Run.Text = $" {songItemBind.MusicData.Title2}";
            Info_Texts_ButtonNameTextBlock.Text = songItemBind.MusicData.ButtonName;
        }

        int initImageCallCount = 0;
        async void InitImage()
        {
            if (Info_Image is null) return;
            Info_Image.Source = null;
            Info_Image_Root.Visibility = IsImageShow ? Visibility.Visible : Visibility.Collapsed;
            FileNotExists_Root.Visibility = Visibility.Collapsed;
            SetImageBorder(false);
            if (!IsLoaded) return;
            if (!IsImageShow) return;
            if (songItemBind is null) return;
            initImageCallCount++;
            await Task.Delay(200);
            initImageCallCount--;
            if (initImageCallCount != 0) return;
            if (!IsLoaded) return;
            if (songItemBind is null) return;
            if (songItemBind.MusicListData?.ListDataType == DataType.专辑) return;
            if (songItemBind.MusicData.From == MusicFrom.localMusic)
            {
                if (Path.GetExtension(songItemBind.MusicData.InLocal) == ".mid")
                {
                    Info_Image.Source = null;
                    Info_Image_Root.Visibility = Visibility.Collapsed;
                    SetImageBorder(false);
                    return;
                }
            }


            bool isExists = true;
            if (songItemBind.MusicData.From == MusicFrom.localMusic)
            {
                isExists = await Task.Run(() => File.Exists(songItemBind.MusicData.InLocal));
            }

            MusicData musicData = songItemBind.MusicData;
            ImageSource result = null;

            if (isExists)
            {
                var bitmapTuple = await ImageManage.GetImageSource(musicData, (int)(56 * MainWindow.NowDPI), (int)(56 * MainWindow.NowDPI), true);
                result = bitmapTuple.Item1;
                FileNotExists_Root.Visibility = Visibility.Collapsed;
            }
            else
            {
                FileNotExists_Root.Visibility = Visibility.Visible;
            }

            if (!IsLoaded) result = null;
            if (songItemBind is null) result = null;

            if (result != null)
            {
                if (musicData == songItemBind.MusicData)
                {
                    Info_Image.Source = result;
                    SetImageBorder(true);
                }
            }
            else
            {
                Info_Image.Source = null;
                Info_Image_Root.Visibility = Visibility.Collapsed;
                SetImageBorder(false);
            }
        }

        Visual backgroundFillVisual;
        Visual rightButtonVisual;
        Visual strokeVisual;
        ScalarKeyFrameAnimation rightButtonVisualShowAnimation;
        ScalarKeyFrameAnimation rightButtonVisualHideAnimation;
        ScalarKeyFrameAnimation backgroundFillVisualShowAnimation;
        ScalarKeyFrameAnimation backgroundFillVisualHideAnimation;
        ScalarKeyFrameAnimation strokeVisualShowAnimation;
        ScalarKeyFrameAnimation strokeVisualHideAnimation;
        void InitVisuals()
        {
            backgroundFillVisual = ElementCompositionPreview.GetElementVisual(Background_FillRectangle);
            rightButtonVisual = ElementCompositionPreview.GetElementVisual(Info_Buttons_Root);
            strokeVisual = ElementCompositionPreview.GetElementVisual(Background_HighlightRectangle);

            backgroundFillVisual.Opacity = 0;
            rightButtonVisual.Opacity = 0;
            strokeVisual.Opacity = 0;

            AnimateHelper.AnimateScalar(rightButtonVisual, 1, 0.1,
                0, 0, 0, 0,
                out rightButtonVisualShowAnimation);
            AnimateHelper.AnimateScalar(rightButtonVisual, 0, 0.1,
                0, 0, 0, 0,
                out rightButtonVisualHideAnimation);
            AnimateHelper.AnimateScalar(backgroundFillVisual,
                                        1, 0.1,
                                        0, 0, 0, 0,
                                        out backgroundFillVisualShowAnimation);
            AnimateHelper.AnimateScalar(backgroundFillVisual,
                0, 0.1,
                0, 0, 0, 0,
                out backgroundFillVisualHideAnimation);
            AnimateHelper.AnimateScalar(strokeVisual, 0, 3, 0, 0, 0, 0,
                out strokeVisualShowAnimation);
            AnimateHelper.AnimateScalar(strokeVisual, 0, 0.2, 0, 0, 0, 0,
                out strokeVisualHideAnimation);
        }

        bool last_IsMusicDataPlaying = false;
        void InitPlayingState()
        {
            if (!IsLoaded) return;
            if (songItemBind is null) return;

            if (IsMusicDataPlaying)
            {
                App.audioPlayer.PlayStateChanged -= AudioPlayer_PlayStateChanged;
                App.audioPlayer.PlayStateChanged += AudioPlayer_PlayStateChanged;
                SetPlayingIcon(App.audioPlayer.PlaybackState);
                OnMouseIn();
                Background_PlayingRectangle.Opacity = 1;
                last_IsMusicDataPlaying = true;
            }
            else
            {
                App.audioPlayer.PlayStateChanged -= AudioPlayer_PlayStateChanged;
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
                strokeVisual.Opacity = 1;
                strokeVisual.StartAnimation("Opacity", strokeVisualShowAnimation);
            }
            else
            {
                strokeVisual.StartAnimation("Opacity", strokeVisualHideAnimation);
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
            if (songItemBind is null) return;
            Info_Buttons_Root.Visibility = Visibility.Visible;
            backgroundFillVisual.StartAnimation("Opacity", backgroundFillVisualShowAnimation);
            rightButtonVisual.StartAnimation("Opacity", rightButtonVisualShowAnimation);
        }
        void OnMouseLeave()
        {
            if (songItemBind is null)
            {
                rightButtonVisual.Compositor.GetCommitBatch(CompositionBatchTypes.Animation).Completed -= MusicDataItem_Completed;
                return;
            }
            backgroundFillVisual.StartAnimation("Opacity", backgroundFillVisualHideAnimation);
            rightButtonVisual.StartAnimation("Opacity", rightButtonVisualHideAnimation);
            rightButtonVisual.Compositor.GetCommitBatch(CompositionBatchTypes.Animation).Completed -= MusicDataItem_Completed;
            rightButtonVisual.Compositor.GetCommitBatch(CompositionBatchTypes.Animation).Completed += MusicDataItem_Completed;
        }

        private void MusicDataItem_Completed(object sender, CompositionBatchCompletedEventArgs args)
        {
            if (!isPointEnter) Info_Buttons_Root.Visibility = Visibility.Collapsed;
            rightButtonVisual.Compositor.GetCommitBatch(CompositionBatchTypes.Animation).Completed -= MusicDataItem_Completed;
        }

        private void AudioPlayer_PlayStateChanged(AudioPlayer audioPlayer)
        {
            SetPlayingIcon(audioPlayer.PlaybackState);
        }

        private void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (sender is null) return;
            if (sender.DataContext is null) return;
            if (sender.DataContext is not SongItemBindBase) return;
            strokeVisual.Opacity = 0;
            songItemBind = sender.DataContext as SongItemBindBase;
            musicDataFlyout.SongItemBind = songItemBind;
            InitInfo();
            InitPlayingState();
            InitImage();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            staticMusicDataItem.Add(this);
            UserControl_DataContextChanged(sender as FrameworkElement, null);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (rightButtonVisual != null) 
            {
                rightButtonVisual.Compositor.GetCommitBatch(CompositionBatchTypes.Animation).Completed -= MusicDataItem_Completed;
            }
            App.audioPlayer.PlayStateChanged -= AudioPlayer_PlayStateChanged;
            staticMusicDataItem.Remove(this);
            songItemBind = null;
            Info_Image.Source = null;
            rightButtonVisualShowAnimation.Dispose();
            rightButtonVisualHideAnimation.Dispose();
            backgroundFillVisualShowAnimation.Dispose();
            backgroundFillVisualHideAnimation.Dispose();
            strokeVisualShowAnimation.Dispose();
            strokeVisualHideAnimation.Dispose();
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
            await App.playingList.Play(songItemBind.MusicData, true);
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
                if (App.audioPlayer.PlaybackState == NAudio.Wave.PlaybackState.Playing)
                    App.audioPlayer.SetPause();
                else
                    App.audioPlayer.SetPlay();
            }
            else
                await App.playingList.Play(songItemBind.MusicData, true);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {

        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            Pages.ListViewPages.ListViewPage.SetPageToListViewPage(new() { PageType = Pages.ListViewPages.PageType.Album, Param = songItemBind.MusicData.Album });
        }

        private void Info_Texts_FlyoutMenu_Artist_Item_Loaded(object sender, RoutedEventArgs e)
        {
            Info_Texts_FlyoutMenu_Album_Item.Text = $"专辑：{songItemBind.MusicData.Album.Title}";
            Info_Texts_FlyoutMenu_Artist_Item.Items.Clear();
            foreach (var artist in songItemBind.MusicData.Artists)
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
    }
}
