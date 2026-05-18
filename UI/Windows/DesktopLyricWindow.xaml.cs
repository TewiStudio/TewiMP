using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using Microsoft.UI.Composition;
using Windows.UI;
using Windows.Graphics;
using WinUIEx;
using NAudio.Wave;
using Vanara.PInvoke;
using TewiMP.Core;
using TewiMP.Helpers;
using TewiMP.UI.Pages;
using TewiMP.Services;
using TewiMP.Services.Storage;
using TewiMP.Services.Media.Audio;

namespace TewiMP.UI.Windows;

public enum LyricTextPosition { Default, Left, Right, Center }
public enum LyricTranslateTextPosition { Center, Left, Right }
public enum LyricTextBehavior { Exchange, MainLyric, NextLyric, OnlyMainLyric }
public enum LyricTranslateTextBehavior { MainLyric, TranslateLyric, OnlyMainLyric, OnlyTranslate }
public sealed partial class DesktopLyricWindow : WindowEx
{
    public OverlappedPresenter overlappedPresenter { get; private set; }
    private IntPtr hWndMain = IntPtr.Zero;
    private SUBCLASSPROC subClassProc;
    bool transparent = true;
    public static double LyricOpacity { get; set; } = 1.0;
    public static bool PauseButtonVisible { get; set; } = true;
    public static bool ProgressUIVisible { get; set; } = true;
    public static bool ProgressUIPercentageVisible { get; set; } = true;
    public static bool MusicChangeUIVisible { get; set; } = true;
    public static LyricTextPosition LyricTextPosition { get; set; } = LyricTextPosition.Default;
    public static LyricTranslateTextPosition LyricTranslateTextPosition { get; set; } = LyricTranslateTextPosition.Center;
    public static LyricTextBehavior LyricTextBehavior { get; set; } = LyricTextBehavior.Exchange;
    public static LyricTranslateTextBehavior LyricTranslateTextBehavior { get; set; } = LyricTranslateTextBehavior.MainLyric;

    private void SetPresenter(bool isLockStyle = false)
    {
        IsMaximizable = false;
        IsMinimizable = false;
        IsAlwaysOnTop = true;
        subClassProc = new SUBCLASSPROC(SubClassWndProc);
        var windowHandle = new IntPtr((long)AppWindow.Id.Value);
        SetWindowSubclass(windowHandle, subClassProc, 0, 0);
    }
    public DesktopLyricWindow()
    {
        InitializeComponent();
        //LyricRomajiPopup1.XamlRoot = root.XamlRoot;
        /*
        WindowHelper.Window.hWnd = WindowHelper.Window.GetHWnd(this);
        WindowHelper.Window.MakeTransparent();*/

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            SetPresenter();
            AppWindow.IsShownInSwitchers = false;
            AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            AppWindow.Title = "DesktopLyric Window";
            AppWindow.SetIcon(DataFolderBase.IconICOPath);
            AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            AppWindow.TitleBar.ForegroundColor = Colors.Transparent;
            AppWindow.TitleBar.ButtonForegroundColor = Colors.Transparent;
            AppWindow.TitleBar.InactiveForegroundColor = Colors.Transparent;
            AppWindow.TitleBar.ButtonInactiveForegroundColor = Colors.Transparent;
            AppWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;

            var dpi = CodeHelper.GetScaleAdjustment(this);
            if (IsMoved)
            {
                AppWindow.Move(lastWindowPosition);
                AppWindow.Resize(lastWindowSize);
            }
            else
            {
                AppWindow.Resize(new SizeInt32() { Width = (int)(850 * dpi), Height = (int)(132 * dpi) });
                if (!App.MainWindowInstance.isMinSize)
                {
                    PointInt32 pointInt32 = new(
                        App.MainWindowInstance.AppWindow.Position.X + App.MainWindowInstance.AppWindow.Size.Width - AppWindow.Size.Width,
                        App.MainWindowInstance.AppWindow.Position.Y + App.MainWindowInstance.AppWindow.Size.Height - AppWindow.Size.Height);
                    AppWindow.Move(pointInt32);
                }
            }
        }
        SystemBackdrop = transparentTintBackdrop;
        transparentTintBackdrop.TintColor = Color.FromArgb(0, 0, 0, 0);
        AppWindow.Closing += AppWindow_Closing;
    }

    Visual tb1Visual;
    Visual tb2Visual;
    Vector3KeyFrameAnimation tb1Animation;
    Vector3KeyFrameAnimation tb2Animation;
    private void root_Loaded(object sender, RoutedEventArgs e)
    {
        AddEvents();
        RestartTimer();
        SetLyric(App.Instance.LyricService.NowLyricsData);
        /*
        AnimateHelper.AnimateOffset(T1BaseViewbox, 0, 0, 0, 0.2, 0, 0, 0, 0,
            out tb1Visual, out var compositor, out tb1Animation);
        AnimateHelper.AnimateOffset(T2BaseViewbox, 0, 0, 0, 0.2, 0, 0, 0, 0,
            out tb2Visual, out var compositor1, out tb2Animation);*/
        /*
        T1Base.SizeChanged += T1Base_SizeChanged;
        T2Base.SizeChanged += T1Base_SizeChanged;
        LyricRomajiPopup_tb.SizeChanged += T1Base_SizeChanged;*/
    }
    /*
        private void T1Base_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CrateShadow();
        }

        Visual T1Visual = null;
        Visual T2Visual = null;
        Visual RTVisual = null;
        private async void CrateShadow()
        {
            await Task.Delay(10);
            T1Visual = ElementCompositionPreview.GetElementVisual(T1Base);
            T2Visual = ElementCompositionPreview.GetElementVisual(T2Base);
            RTVisual = ElementCompositionPreview.GetElementVisual(LyricRomajiPopup_tb);

            TextBlock[] crateShadowElementList = { T1Base, T2Base, LyricRomajiPopup_tb };
            Visual[] crateShadowVisualList = { T1Visual, T2Visual, RTVisual };
            DropShadow[] shadowList = new DropShadow[3];
            for (int i = 0; i < 3; i++)
            {
                shadowList[i]?.Dispose();

                var element = crateShadowElementList[i];
                var visual = crateShadowVisualList[i];
                var compositor = visual.Compositor;
                var basicRectVisual = compositor.CreateSpriteVisual();
                basicRectVisual.Size = element.RenderSize.ToVector2();

                DropShadow dropShadow = compositor.CreateDropShadow();
                dropShadow.BlurRadius = 15f;
                dropShadow.Opacity = 1f;
                dropShadow.Color = Windows.UI.Color.FromArgb(255, 50, 50, 50);
                dropShadow.Mask = element.GetAlphaMask();
                shadowList[i] = dropShadow;

                basicRectVisual.Shadow = dropShadow;
                ElementCompositionPreview.SetElementChildVisual(element, basicRectVisual);
            }
        }
    */
    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        RemoveEvents();
        //tb1Animation?.Dispose();
        //tb2Animation?.Dispose();
    }

    public void AddEvents()
    {
        LogService.Log(nameof(DesktopLyricWindow), "Add Events.");
        App.Instance.AudioService.SourceChanged += AudioService_SourceChanged;
        App.Instance.AudioService.PlayStateChanged += AudioService_PlayStateChanged;
        App.Instance.AudioService.VolumeChanged += AudioService_VolumeChanged;
        App.Instance.AudioService.TimingChanged += AudioService_TimingChanged;
        App.Instance.LyricService.LyricTimingChanged += LyricManager_LyricTimingChanged;
        App.Instance.PlayingListService.NowPlayingImageLoaded += PlayingListService_NowPlayingImageLoaded;
        AudioService_PlayStateChanged(App.Instance.AudioService);
        AudioService_TimingChanged(App.Instance.AudioService);
        LyricManager_LyricTimingChanged(App.Instance.LyricService.NowLyricsData);
        App.Instance.AudioService.ReCallTiming();
        SetLyricOpacity(LyricOpacity);
    }

    public void RemoveEvents()
    {
        LogService.Log(nameof(DesktopLyricWindow), "Removed Events.");
        AppWindow.Closing -= AppWindow_Closing;
        App.Instance.AudioService.SourceChanged -= AudioService_SourceChanged;
        App.Instance.AudioService.PlayStateChanged -= AudioService_PlayStateChanged;
        App.Instance.AudioService.VolumeChanged -= AudioService_VolumeChanged;
        App.Instance.AudioService.TimingChanged -= AudioService_TimingChanged;
        App.Instance.LyricService.LyricTimingChanged -= LyricManager_LyricTimingChanged;
        App.Instance.PlayingListService.NowPlayingImageLoaded -= PlayingListService_NowPlayingImageLoaded;
    }

    public void SetLyricOpacity(double value)
    {
        root.Opacity = value;
    }

    private void SetLyricIntervalControlProgress(double value)
    {
        double disableOpacity = .1;
        if (value >= .8)
        {
            LyricIntervalCircle1.Opacity = 1;
            LyricIntervalCircle2.Opacity = 1;
            LyricIntervalCircle3.Opacity = 1;
            LyricIntervalCircle4.Opacity = 1;
            LyricIntervalCircle5.Opacity = 1;
        }
        else if (value >= .6)
        {
            LyricIntervalCircle1.Opacity = 1;
            LyricIntervalCircle2.Opacity = 1;
            LyricIntervalCircle3.Opacity = 1;
            LyricIntervalCircle4.Opacity = 1;
            LyricIntervalCircle5.Opacity = disableOpacity;
        }
        else if (value >= .4)
        {
            LyricIntervalCircle1.Opacity = 1;
            LyricIntervalCircle2.Opacity = 1;
            LyricIntervalCircle3.Opacity = 1;
            LyricIntervalCircle4.Opacity = disableOpacity;
            LyricIntervalCircle5.Opacity = disableOpacity;
        }
        else if (value >= .2)
        {
            LyricIntervalCircle1.Opacity = 1;
            LyricIntervalCircle2.Opacity = 1;
            LyricIntervalCircle3.Opacity = disableOpacity;
            LyricIntervalCircle4.Opacity = disableOpacity;
            LyricIntervalCircle5.Opacity = disableOpacity;
        }
        else if (value > 0)
        {
            LyricIntervalCircle1.Opacity = 1;
            LyricIntervalCircle2.Opacity = disableOpacity;
            LyricIntervalCircle3.Opacity = disableOpacity;
            LyricIntervalCircle4.Opacity = disableOpacity;
            LyricIntervalCircle5.Opacity = disableOpacity;
        }
        else
        {
            LyricIntervalCircle1.Opacity = disableOpacity;
            LyricIntervalCircle2.Opacity = disableOpacity;
            LyricIntervalCircle3.Opacity = disableOpacity;
            LyricIntervalCircle4.Opacity = disableOpacity;
            LyricIntervalCircle5.Opacity = disableOpacity;
            LyricIntervalRoot.Visibility = Visibility.Collapsed;
        }

    }

    TimeSpan lyricIntervalStart = TimeSpan.Zero;
    TimeSpan lyricIntervalEnd = TimeSpan.MinValue;
    readonly TimeSpan fiveSecond = TimeSpan.FromSeconds(5);
    private void LyricManager_LyricTimingChanged(LyricData nowLyricsData)
    {
        if (lyricIntervalEnd != TimeSpan.MinValue)
        {
            App.Instance.LyricService.FastUpdateMode = true;

            var c1 = App.Instance.AudioService.CurrentTime - (lyricIntervalEnd - fiveSecond);
            //LogManager.Log("DEBUG", $"{c1} | {c2} | {c3}");
            double result = double.Clamp(c1 / fiveSecond, 0, 1);
            LyricIntervalRoot.Visibility = Visibility.Visible;
            SetLyricIntervalControlProgress(1 - result);
            if (result >= 1) lyricIntervalEnd = TimeSpan.MinValue;
        }
        else
        {
            LyricIntervalRoot.Visibility = Visibility.Collapsed;
        }
    }

    int showBorderCount = 0;
    bool isAddedEvent = false;
    private void AudioService_PlayStateChanged(AudioService AudioService)
    {
        PlayStateElement.PlaybackState = AudioService.PlaybackState;
        if (AudioService.PlaybackState == PlaybackState.Playing)
        {
            InfoBorder.Opacity = 0;

            if (!isAddedEvent)
            {
                isAddedEvent = true;
                App.Instance.LyricService.PlayingLyricSourceChanged += LyricManager_PlayingLyricSourceChange;
                App.Instance.LyricService.PlayingLyricSelectedChanged += LyricManager_PlayingLyricSelectedChange;
                App.Instance.LyricService.StartTimer();
                LyricManager_PlayingLyricSelectedChange(App.Instance.LyricService.NowLyricsData);
            }
        }
        else
        {
            if (PauseButtonVisible) InfoBorder.Opacity = 1;
            isAddedEvent = false;
            App.Instance.LyricService.PlayingLyricSourceChanged -= LyricManager_PlayingLyricSourceChange;
            App.Instance.LyricService.PlayingLyricSelectedChanged -= LyricManager_PlayingLyricSelectedChange;
        }
    }

    private void AudioService_VolumeChanged(AudioService AudioService, object data)
    {

        ShowInfo($"音量：{Math.Round(AudioService.Volume)}");
    }

    private void AudioService_SourceChanged(AudioService AudioService)
    {
        if (!MusicChangeUIVisible) return;
        ShowInfo($"正在播放：{AudioService.MusicData.Title}");
    }

    private void PlayingListService_NowPlayingImageLoaded(Uri imageSource, string path)
    {
    }

    int showCount = 0;
    private async void ShowInfo(string text)
    {
        InfoTBBorder.Opacity = 1;
        InfoTB.Text = text;

        showCount++;
        await Task.Delay(5000);
        showCount--;

        if (showCount <= 0) InfoTBBorder.Opacity = 0;
    }

    void animationTextVisual(int tbIndex)
    {
        return;
        if (tbIndex == 1)
        {
            tb1Visual.Offset = new(0, 40, 0);
            tb1Visual.StartAnimation("Offset", tb1Animation);
        }
        else if (tbIndex == 2)
        {
            tb2Visual.Offset = new(0, 40, 0);
            tb2Visual.StartAnimation("Offset", tb2Animation);
        }
    }
    private void SetLyric(LyricData nowLyricsData, bool isNext = false)
    {
        T11.Text = null;
        T21.Text = null;
        LyricRomajiPopup_tb.Text = null;
        lyricIntervalEnd = TimeSpan.MinValue;
        App.Instance.LyricService.FastUpdateMode = false;

        var lrcForeground = App.Current.Resources["LrcForeground"] as SolidColorBrush;
        var lrcSecondForeground = App.Current.Resources["LrcSecondForeground"] as SolidColorBrush;

        if (nowLyricsData is null)
        {
            if (App.Instance.AudioService.MusicData != null)
            {
                T1.Text = App.Instance.AudioService.MusicData.Title;
                T2.Text = App.Instance.AudioService.MusicData.ArtistName;
            }

            T1.Foreground = lrcForeground;
            T2.Foreground = lrcForeground;
            T1Shadow.Color = lrcForeground.Color;
            T2Shadow.Color = lrcForeground.Color;

            if (LyricTranslateTextPosition == LyricTranslateTextPosition.Left)
            {
                V1.HorizontalAlignment = HorizontalAlignment.Left;
                V2.HorizontalAlignment = HorizontalAlignment.Left;
            }
            else if (LyricTranslateTextPosition == LyricTranslateTextPosition.Right)
            {
                V1.HorizontalAlignment = HorizontalAlignment.Right;
                V2.HorizontalAlignment = HorizontalAlignment.Right;
            }
            else
            {
                V1.HorizontalAlignment = HorizontalAlignment.Center;
                V2.HorizontalAlignment = HorizontalAlignment.Center;
            }
            animationTextVisual(1);
            animationTextVisual(2);
            return;
        }
        if (nowLyricsData.Lyric is null)
        {
            T1.Text = App.Instance.AudioService.MusicData.Title;
            T2.Text = App.Instance.AudioService.MusicData.ArtistName;
            T1.Foreground = lrcForeground;
            T2.Foreground = lrcForeground;
            T1Shadow.Color = lrcForeground.Color;
            T2Shadow.Color = lrcForeground.Color;

            if (LyricTranslateTextPosition == LyricTranslateTextPosition.Left)
            {
                V1.HorizontalAlignment = HorizontalAlignment.Left;
                V2.HorizontalAlignment = HorizontalAlignment.Left;
            }
            else if (LyricTranslateTextPosition == LyricTranslateTextPosition.Right)
            {
                V1.HorizontalAlignment = HorizontalAlignment.Right;
                V2.HorizontalAlignment = HorizontalAlignment.Right;
            }
            else
            {
                V1.HorizontalAlignment = HorizontalAlignment.Center;
                V2.HorizontalAlignment = HorizontalAlignment.Center;
            }
            animationTextVisual(1);
            animationTextVisual(2);
            return;
        }
        if (nowLyricsData.Lyric.First() == LyricHelper.NoneLyricString)
        {
            if (App.Instance.LyricService.NowPlayingLyrics.Any())
            {
                var index = App.Instance.LyricService.NowPlayingLyrics.IndexOf(nowLyricsData) + 1;
                if (index > App.Instance.LyricService.NowPlayingLyrics.Count - 1) return;
                SetLyric(App.Instance.LyricService.NowPlayingLyrics[index], true);
            }
            return;
        }
        /*
                var accentBrush = (SolidColorBrush)(App.MainWindowInstance.WindowGridBase.ActualTheme == ElementTheme.Light ?
                    App.Current.Resources["MusicAlbumAccentBrushReverse"] :
                    App.Current.Resources["MusicAlbumAccentBrush"]);
        */
        var accentBrush = (SolidColorBrush)App.Current.Resources["MusicAlbumAccentBrush"];
        int nowLyricNum = App.Instance.LyricService.NowPlayingLyrics.IndexOf(nowLyricsData);
        LyricData nextLyric = null;
        LyricData beforeLyric = null;
        if (nowLyricNum != -1)
        {
            nextLyric = App.Instance.LyricService.NowPlayingLyrics[nowLyricNum + 1];
            if (nowLyricNum > 0)
                beforeLyric = App.Instance.LyricService.NowPlayingLyrics[nowLyricNum - 1];
        }

        var doubleLyricLineMode = LyricTranslateTextBehavior == LyricTranslateTextBehavior.MainLyric || LyricTranslateTextBehavior == LyricTranslateTextBehavior.TranslateLyric;

        if (nowLyricsData?.Lyric.Count > 1 && doubleLyricLineMode)
        {
            IsT1Focus = true;

            int tCount = 1;
            try
            {
                while (nowLyricsData?.Lyric?.FirstOrDefault() == App.Instance.LyricService.NowPlayingLyrics[nowLyricNum + tCount]?.Lyric?.FirstOrDefault())
                {
                    tCount++;
                }
            }
            catch { }

            bool RomajiEnable = !string.IsNullOrEmpty(nowLyricsData.Romaji);
            if (RomajiEnable)
            {
                LyricRomajiPopup_tb.Text = nowLyricsData.Romaji;
                //LyricRomajiPopup.IsOpen = true;
            }
            else
            {
                LyricRomajiPopup_tb.Text = "";
            }

            string t1text = nowLyricsData?.Lyric?.FirstOrDefault();
            if (LyricTranslateTextPosition == LyricTranslateTextPosition.Center)
            {
                V1.HorizontalAlignment = HorizontalAlignment.Center;
                V2.HorizontalAlignment = HorizontalAlignment.Center;
                TRBaseParent.HorizontalAlignment = HorizontalAlignment.Center;
                //progressRoot.HorizontalAlignment = HorizontalAlignment.Center;
            }
            else if (LyricTranslateTextPosition == LyricTranslateTextPosition.Left)
            {
                V1.HorizontalAlignment = HorizontalAlignment.Left;
                V2.HorizontalAlignment = HorizontalAlignment.Left;
                TRBaseParent.HorizontalAlignment = HorizontalAlignment.Left;
                //progressRoot.HorizontalAlignment = HorizontalAlignment.Left;
            }
            else
            {
                V1.HorizontalAlignment = HorizontalAlignment.Right;
                V2.HorizontalAlignment = HorizontalAlignment.Right;
                TRBaseParent.HorizontalAlignment = HorizontalAlignment.Right;
                //progressRoot.HorizontalAlignment = HorizontalAlignment.Right;
            }
            if (LyricTranslateTextBehavior == LyricTranslateTextBehavior.MainLyric)
            {
                if (tCount == 1) T11.Text = null;
                else T11.Text = $"x{tCount}";

                T1.Text = t1text;
                T2.Text = nowLyricsData?.Lyric[1];
            }
            else if (LyricTranslateTextBehavior == LyricTranslateTextBehavior.TranslateLyric)
            {
                if (tCount == 1) T21.Text = null;
                else T21.Text = $"x{tCount}";

                T1.Text = nowLyricsData?.Lyric[1];
                T2.Text = t1text;
            }
            else if (LyricTranslateTextBehavior == LyricTranslateTextBehavior.OnlyMainLyric)
            {
                if (tCount == 1) T11.Text = null;
                else T11.Text = $"x{tCount}";

                T1.Text = t1text;
                T2.Text = null;
            }
            else
            {
                if (tCount == 1) T11.Text = null;
                else T11.Text = $"x{tCount}";

                T1.Text = nowLyricsData?.Lyric[1];
                T2.Text = null;
            }

            if (!isNext)
            {
                T1.Foreground = accentBrush;
                T2.Foreground = accentBrush;
                T1Shadow.Color = accentBrush.Color;
                T2Shadow.Color = accentBrush.Color;
            }
            else
            {
                if (nowLyricsData.LyricTimeSpan - beforeLyric.LyricTimeSpan >= TimeSpan.FromSeconds(5))
                {
                    lyricIntervalStart = beforeLyric.LyricTimeSpan;
                    lyricIntervalEnd = nowLyricsData.LyricTimeSpan;
                }
                T1.Foreground = lrcForeground;
                T2.Foreground = lrcForeground;
                T1Shadow.Color = lrcForeground.Color;
                T2Shadow.Color = lrcForeground.Color;
            }

            animationTextVisual(1);
            animationTextVisual(2);
        }
        else
        {
            if (LyricTextPosition == LyricTextPosition.Default)
            {
                V1.HorizontalAlignment = HorizontalAlignment.Left;
                V2.HorizontalAlignment = HorizontalAlignment.Right;
                TRBaseParent.HorizontalAlignment = HorizontalAlignment.Center;
            }
            else if (LyricTextPosition == LyricTextPosition.Left)
            {
                V1.HorizontalAlignment = HorizontalAlignment.Left;
                V2.HorizontalAlignment = HorizontalAlignment.Left;
                TRBaseParent.HorizontalAlignment = HorizontalAlignment.Left;
            }
            else if (LyricTextPosition == LyricTextPosition.Right)
            {
                V1.HorizontalAlignment = HorizontalAlignment.Right;
                V2.HorizontalAlignment = HorizontalAlignment.Right;
                TRBaseParent.HorizontalAlignment = HorizontalAlignment.Right;
            }
            else if (LyricTextPosition == LyricTextPosition.Center)
            {
                V1.HorizontalAlignment = HorizontalAlignment.Center;
                V2.HorizontalAlignment = HorizontalAlignment.Center;
                TRBaseParent.HorizontalAlignment = HorizontalAlignment.Center;
            }

            bool RomajiEnable = !string.IsNullOrEmpty(nowLyricsData.Romaji);
            if (RomajiEnable)
            {
                LyricRomajiPopup_tb.Text = nowLyricsData.Romaji;
            }
            else
            {
                LyricRomajiPopup_tb.Text = "";
            }

            LyricData nextData = new(null, null, TimeSpan.Zero);
            try
            {
                int num1 = App.Instance.LyricService.NowPlayingLyrics.IndexOf(nowLyricsData);
                do
                {
                    num1++;
                    nextData = App.Instance.LyricService.NowPlayingLyrics[num1];
                }
                while (nextData?.Lyric?.FirstOrDefault() == LyricHelper.NoneLyricString);
            }
            catch { }

            int tCount = 1;
            int num2 = App.Instance.LyricService.NowPlayingLyrics.IndexOf(nowLyricsData);
            try
            {
                while (nowLyricsData?.Lyric?.FirstOrDefault() == App.Instance.LyricService.NowPlayingLyrics[num2 + tCount]?.Lyric?.FirstOrDefault())
                {
                    tCount++;
                }
            }
            catch { }

            bool onlyTranslation = nowLyricsData?.Lyric.Count > 1 && LyricTranslateTextBehavior == LyricTranslateTextBehavior.OnlyTranslate;
            string t1text = onlyTranslation ? nowLyricsData?.Lyric[1] : nowLyricsData?.Lyric?.FirstOrDefault();
            string t2text = onlyTranslation ? nextData?.Lyric[1] : nextData?.Lyric?.FirstOrDefault();

            if (LyricTextBehavior == LyricTextBehavior.Exchange)
            {
                if (IsT1Focus)
                {
                    T1.Text = t1text;
                    if (isNext)
                    {
                        T1.Foreground = lrcForeground;
                        T2.Foreground = lrcSecondForeground;
                        T1Shadow.Color = lrcForeground.Color;
                        T2Shadow.Color = lrcSecondForeground.Color;
                    }
                    else
                    {
                        IsT1Focus = false;
                        T1.Foreground = accentBrush;
                        T2.Foreground = lrcForeground;
                        T1Shadow.Color = accentBrush.Color;
                        T2Shadow.Color = lrcForeground.Color;
                    }

                    if (nextData.Lyric != null) T2.Text = t2text;
                    else // 最后一句歌词
                    {
                        T2.Foreground = accentBrush;
                        T2Shadow.Color = accentBrush.Color;
                    }
                }
                else
                {
                    T2.Text = t1text;

                    T1.Foreground = lrcForeground;
                    T1Shadow.Color = lrcForeground.Color;
                    if (isNext)
                    {
                        if (nowLyricsData.LyricTimeSpan - beforeLyric.LyricTimeSpan >= TimeSpan.FromSeconds(5))
                        {
                            lyricIntervalStart = beforeLyric.LyricTimeSpan;
                            lyricIntervalEnd = nowLyricsData.LyricTimeSpan;
                        }
                        T1.Foreground = lrcSecondForeground;
                        T2.Foreground = lrcForeground;
                        T1Shadow.Color = lrcSecondForeground.Color;
                        T2Shadow.Color = lrcForeground.Color;
                    }
                    else
                    {
                        IsT1Focus = true;
                        T1.Foreground = lrcForeground;
                        T2.Foreground = accentBrush;
                        T1Shadow.Color = lrcForeground.Color;
                        T2Shadow.Color = accentBrush.Color;
                    }

                    if (nextData.Lyric != null) T1.Text = t2text;
                    else
                    {
                        T1.Foreground = accentBrush;
                        T1Shadow.Color = accentBrush.Color;
                    }
                }
            }
            else if (LyricTextBehavior == LyricTextBehavior.MainLyric)
            {
                T1.Text = t1text;
                T2.Text = t2text;
                if (isNext)
                {
                    T1.Foreground = lrcForeground;
                    T1Shadow.Color = lrcForeground.Color;
                }
                else
                {
                    T1.Foreground = accentBrush;
                    T1Shadow.Color = accentBrush.Color;
                }
                T2.Foreground = lrcForeground;
                T2Shadow.Color = lrcForeground.Color;
            }
            else if (LyricTextBehavior == LyricTextBehavior.NextLyric)
            {
                T1.Text = t2text;
                T2.Text = t1text;
                if (isNext)
                {
                    T2.Foreground = lrcForeground;
                    T2Shadow.Color = lrcForeground.Color;
                }
                else
                {
                    T2.Foreground = accentBrush;
                    T2Shadow.Color = accentBrush.Color;
                }
                T1.Foreground = lrcForeground;
                T2Shadow.Color = lrcForeground.Color;
            }
            else if (LyricTextBehavior == LyricTextBehavior.OnlyMainLyric)
            {
                T1.Text = t1text;
                T2.Text = null;
                if (isNext)
                {
                    T1.Foreground = lrcForeground;
                    T1Shadow.Color = lrcForeground.Color;
                }
                else
                {
                    T1.Foreground = accentBrush;
                    T1Shadow.Color = accentBrush.Color;
                }
                T2.Foreground = lrcForeground;
                T2Shadow.Color = lrcForeground.Color;
            }

            if (nowLyricsData.LyricTimeSpan - beforeLyric?.LyricTimeSpan >= TimeSpan.FromSeconds(5))
            {
                lyricIntervalStart = beforeLyric.LyricTimeSpan;
                lyricIntervalEnd = nowLyricsData.LyricTimeSpan;
            }
        }

        animationTextVisual(1);
        animationTextVisual(2);
    }

    private void AudioService_TimingChanged(AudioService AudioService)
    {
        if (!ProgressUIVisible)
        {
            App.Instance.AudioService.TimingChanged -= AudioService_TimingChanged;
            progressRoot.Visibility = Visibility.Collapsed;
            return;
        }
        else
        {
            progressRoot.Visibility = Visibility.Visible;
        }

        progressBase.Maximum = AudioService.TotalTime.Ticks;
        progressBase.Value = AudioService.CurrentTime.Ticks;
        if (ProgressUIPercentageVisible)
        {
            progressPresent.Visibility = Visibility.Visible;
            progressPresent.Text = $"{Math.Round(AudioService.CurrentTime / AudioService.TotalTime * 100)}%";
        }
        else
        {
            progressPresent.Visibility = Visibility.Collapsed;
        }
    }

    private void LyricManager_PlayingLyricSourceChange(ObservableCollection<LyricData> nowPlayingLyrics)
    {
        SetLyric(null);
    }

    bool IsT1Focus = true;
    private void LyricManager_PlayingLyricSelectedChange(LyricData nowLyricsData)
    {
        SetLyric(nowLyricsData);
    }

    private void Window_SizeChanged(object sender, WindowSizeChangedEventArgs args)
    {
        //LogService.Info("DEBUG", $"{AppWindow.Size.Width}x{AppWindow.Size.Height}");
    }

    private void Grid_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateDragSize();
    }

    static bool IsMoved = false;
    static PointInt32 lastWindowPosition = default;
    static SizeInt32 lastWindowSize = default;

    private DesktopAcrylicBackdrop acrylicBackdrop = new();
    private TransparentTintBackdrop transparentTintBackdrop = new();
    public bool IsLock = false;
    public void Lock()
    {
        if (IsLock)
        {
            IsLock = !IsLock;

            this.SetExtendedWindowStyle(this.GetExtendedWindowStyle() & ~(ExtendedWindowStyle.Layered | ExtendedWindowStyle.Transparent));
            this.SetWindowStyle(this.GetWindowStyle() | WindowStyle.Caption | WindowStyle.ThickFrame | WindowStyle.MinimizeBox | WindowStyle.MaximizeBox);
            root.Padding = new(0);
            ToolButtonsBase.Visibility = Visibility.Visible;
        }
        else
        {
            IsLock = !IsLock;

            this.SetExtendedWindowStyle(this.GetExtendedWindowStyle() | ExtendedWindowStyle.Layered | ExtendedWindowStyle.Transparent);
            this.SetWindowStyle(this.GetWindowStyle() & ~(WindowStyle.Caption | WindowStyle.ThickFrame | WindowStyle.MinimizeBox | WindowStyle.MaximizeBox));
            root.Padding = new(8, 0, 8, 8); // 透明窗口后会导致窗口左右下往外增大 8 像素
            ToolButtonsBase.Visibility = Visibility.Collapsed;
            ShowInfo($"按下 {App.Instance.HotKeyService.GetHotKey(HotKeyID.LockLyricWindow)} 切换窗口锁定状态");
        }
        RestartTimer();
    }

    private void LockButton_Click(object sender, RoutedEventArgs e)
    {
        Lock();
    }

    public void UpdateDragSize()
    {
        double dpi = CodeHelper.GetScaleAdjustment(this);
        int windowWidth = (int)(AppWindow.Size.Width * dpi);
        int windowHeight = (int)(AppWindow.Size.Height * dpi);
        int toolBarWidth = (int)((ToolButtonsStackPanel.ActualWidth) * dpi);
        int toolBarHeight = (int)(ToolButtonsStackPanel.ActualHeight * dpi);

        RectInt32[] rectInt32s = default;
        if (IsLock)
        {
            rectInt32s = [new(0, 0, windowWidth, windowHeight)];
        }
        else
        {
            rectInt32s = [
                new(toolBarWidth, 0, windowWidth - toolBarWidth, windowHeight),
                new(0, toolBarHeight, toolBarWidth, windowHeight - toolBarHeight)
            ];
        }

        AppWindow.TitleBar.SetDragRectangles(rectInt32s);
    }

    private void DesktopLyricWindow_Closed(object sender, WindowEventArgs args)
    {
        IsMoved = true;
        lastWindowPosition = AppWindow.Position;
        lastWindowSize = AppWindow.Size;
        App.Instance.LyricService.PlayingLyricSourceChanged -= LyricManager_PlayingLyricSourceChange;
        App.Instance.LyricService.PlayingLyricSelectedChanged -= LyricManager_PlayingLyricSelectedChange;
    }

    private void ToolButtonsBase_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        TestMouseHoverState(true);
    }

    private void ToolButtonsBase_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
    }

    public bool IsMouseHoverWindow = false;
    private IntPtr SubClassWndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, uint dwRefData)
    {
        var msg = (User32.WindowMessage)uMsg;
        if (msg == User32.WindowMessage.WM_ERASEBKGND)
        {
            if (User32.GetClientRect(hWnd, out var rect))
            {
                using var brush = Gdi32.CreateSolidBrush((uint)System.Drawing.ColorTranslator.ToWin32(System.Drawing.Color.FromArgb(0, 0, 0, 0)));
                User32.FillRect(wParam, rect, brush);
                return new IntPtr(1);
            }
        }
        if (msg is User32.WindowMessage.WM_NCMOUSEMOVE or User32.WindowMessage.WM_SIZE or User32.WindowMessage.WM_MOVE)
        {
            IsMouseHoverWindow = true;
            TestMouseHoverState(true);
        }
        if (uMsg == 49900 || msg is User32.WindowMessage.WM_POINTERLEAVE) // mouse leave
        {
            IsMouseHoverWindow = false;
            TestMouseHoverState(false);
        }
        //LogManager.Info("message", $"msg: {(User32.WindowMessage)uMsg}, w: {wParam}, l: {lParam}");

        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    DispatcherTimer hoverTimer;
    private void TestMouseHoverState(bool isHover)
    {
        if (isHover && !IsLock)
        {
            //RestartTimer();
            BackgroundFill.Opacity = 1;
        }
        else
        {
            BackgroundFill.Opacity = IsLock ? 0 : .5;
        }
    }

    private void RestartTimer()
    {
        if (hoverTimer is null)
        {
            hoverTimer = new();
            hoverTimer.Interval = TimeSpan.FromSeconds(5);
            hoverTimer.Tick += HoverTimer_Tick;
        }
        hoverTimer.Stop();
        hoverTimer.Start();
    }

    private void HoverTimer_Tick(object sender, object e)
    {
        BackgroundFill.Opacity = IsLock ? 0 : .5;
        hoverTimer.Stop();
    }

    private delegate IntPtr SUBCLASSPROC(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, uint dwRefData);

    [DllImport("Comctl32.dll", SetLastError = true)]
    private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("Comctl32.dll", SetLastError = true)]
    private static extern bool SetWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, uint uIdSubclass, uint dwRefData);

    private void root_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateDragSize();
        progressRoot.Width = root.ActualWidth / 4;
    }

    private void MusicControlButton_Click(object sender, RoutedEventArgs e)
    {
        MusicControlPopup.IsOpen = !MusicControlPopup.IsOpen;
    }

    private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        MusicControlPopup.VerticalOffset = -MusicControlPopup.Child.ActualSize.Y - 4;
    }

    private void MusicControlPopup_Opened(object sender, object e)
    {
        AddMusicControlEvents();
    }

    private void MusicControlPopup_Closed(object sender, object e)
    {
        RemoveMusicControlEvents();
    }

    public void AddMusicControlEvents()
    {
        App.Instance.PlayingListService.NowPlayingImageLoaded += PlayingList_NowPlayingImageLoaded;
        App.Instance.AudioService.SourceChanged += AudioService_SourceChanged1;
        App.Instance.AudioService.TimingChanged += AudioService_TimingChanged1;
    }

    private void PlayingList_NowPlayingImageLoaded(Uri imageSource, string path)
    {
        MusicControl_Image.Source = imageSource;
    }

    private void AudioService_SourceChanged1(AudioService AudioService)
    {
        if (AudioService.MusicData is null) return;
        MusicControl_TitleTb.Text = AudioService.MusicData.Title;
        MusicControl_ButtonNameTb.Text = AudioService.MusicData.ButtonName;
    }

    private void AudioService_TimingChanged1(AudioService AudioService)
    {
        MusicControl_TimeSlider.Value = AudioService.CurrentTime.Ticks;
        MusicControl_TimeSlider.Maximum = AudioService.TotalTime.Ticks;
    }

    public void RemoveMusicControlEvents()
    {
        App.Instance.PlayingListService.NowPlayingImageLoaded -= PlayingList_NowPlayingImageLoaded;
        App.Instance.AudioService.SourceChanged -= AudioService_SourceChanged1;
        App.Instance.AudioService.TimingChanged -= AudioService_TimingChanged1;
    }

    private void ResizeButton_Click(object sender, RoutedEventArgs e)
    {
        var dpi = CodeHelper.GetScaleAdjustment(this);
        AppWindow.Resize(new SizeInt32() { Width = (int)(850 * dpi), Height = (int)(132 * dpi) });
    }

    private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        var item = sender as MenuFlyoutItem;
        var tag = item.Tag as string;
        var displayArea = CodeHelper.GetDisplayArea(App.MainWindowInstance);
        switch (tag)
        {
            case "0":
                this.Move(0, 0);
                break;
            case "1":
                this.Move(0, displayArea.WorkArea.Height / 2 - AppWindow.Size.Height / 2);
                break;
            case "2":
                this.Move(0, displayArea.WorkArea.Height - AppWindow.Size.Height);
                break;
        }
    }

    private void MenuFlyoutItem_Click_1(object sender, RoutedEventArgs e)
    {
        var item = sender as MenuFlyoutItem;
        var tag = item.Tag as string;
        var displayArea = CodeHelper.GetDisplayArea(App.MainWindowInstance);
        switch (tag)
        {
            case "0":
                this.Move(displayArea.WorkArea.Width / 2 - AppWindow.Size.Width / 2, 0);
                break;
            case "1":
                this.Move(displayArea.WorkArea.Width / 2 - AppWindow.Size.Width / 2, displayArea.WorkArea.Height / 2 - AppWindow.Size.Height / 2);
                break;
            case "2":
                this.Move(displayArea.WorkArea.Width / 2 - AppWindow.Size.Width / 2, displayArea.WorkArea.Height - AppWindow.Size.Height);
                break;
        }
    }

    private void MenuFlyoutItem_Click_2(object sender, RoutedEventArgs e)
    {
        var item = sender as MenuFlyoutItem;
        var tag = item.Tag as string;
        var displayArea = CodeHelper.GetDisplayArea(App.MainWindowInstance);
        switch (tag)
        {
            case "0":
                this.Move(displayArea.WorkArea.Width - AppWindow.Size.Width, 0);
                break;
            case "1":
                this.Move(displayArea.WorkArea.Width - AppWindow.Size.Width, displayArea.WorkArea.Height / 2 - AppWindow.Size.Height / 2);
                break;
            case "2":
                this.Move(displayArea.WorkArea.Width - AppWindow.Size.Width, displayArea.WorkArea.Height - AppWindow.Size.Height);
                break;
        }
    }

    private void MenuFlyoutItem_Click_3(object sender, RoutedEventArgs e)
    {
        var item = sender as MenuFlyoutItem;
        var tag = item.Tag as string;
        var displayArea = CodeHelper.GetDisplayArea(App.MainWindowInstance);
        switch (tag)
        {
            case "0":
                this.Move(0, AppWindow.Position.Y);
                break;
            case "1":
                this.Move(displayArea.WorkArea.Width / 2 - AppWindow.Size.Width / 2, AppWindow.Position.Y);
                break;
            case "2":
                this.Move(displayArea.WorkArea.Width - AppWindow.Size.Width, AppWindow.Position.Y);
                break;
        }
    }

    private void MenuFlyoutItem_Click_4(object sender, RoutedEventArgs e)
    {
        var item = sender as MenuFlyoutItem;
        var tag = item.Tag as string;
        var displayArea = CodeHelper.GetDisplayArea(App.MainWindowInstance);
        switch (tag)
        {
            case "0":
                this.Move(AppWindow.Position.X, 0);
                break;
            case "1":
                this.Move(AppWindow.Position.X, displayArea.WorkArea.Height / 2 - AppWindow.Size.Height / 2);
                break;
            case "2":
                this.Move(AppWindow.Position.X, displayArea.WorkArea.Height - AppWindow.Size.Height);
                break;
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        App.MainWindowInstance.AppWindow.Show();
        App.MainWindowInstance.SetForegroundWindow();
        App.MainWindowInstance.SetNavViewContent(
            typeof(SettingPage),
            "open desktopLyric");
    }

    private void RadioMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        var item = sender as RadioMenuFlyoutItem;
        var tag = item.Tag as string;
        int width = 850;
        int height = 132;

        switch (tag)
        {
            case "0":
                width = 720;
                height = 102;
                break;
            case "1":
                width = 850;
                height = 132;
                break;
            case "2":
                width = 1100;
                height = 170;
                break;
            case "3":
                width = 1800;
                height = 246;
                break;
        }

        AppWindow.Resize(
            new SizeInt32()
            {
                Width = (int)(width * this.GetDpiForWindow() / 96.0),
                Height = (int)(height * (this.GetDpiForWindow() / 96.0))
            });
    }
}
