using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TewiMP.Pages.DialogPages
{
    public enum TimingEndEvent { 暂停播放, 退出程序, 注销, 关机 }
    public sealed partial class TimeEventPage : Page
    {
        public DispatcherTimer TimingTimer;
        public TimingEndEvent TimingEndEvent;
        public TimeSpan LeftTime;

        public TimeEventPage()
        {
            InitializeComponent();
        }

        private void TimeEventPage_Loaded(object sender, RoutedEventArgs e)
        {
            StartButton.IsEnabled = TimingTimePicker.Time >= TimeSpan.Zero;
        }

        private void TimeEventPage_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (TimingTimer is null)
            {
                if (TimingTimePicker.Time <= TimeSpan.Zero)
                {
                    return;
                }

                TimingEndEvent = (TimingEndEvent)TimingEndEventComboBox.SelectedIndex;
                LeftTime = TimingTimePicker.Time;
                //LeftTime = TimeSpan.FromSeconds(2);
                //System.Diagnostics.App.logManager.Log(LeftTime);
                TimingTimer = new()
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                TimingTimer.Tick += TimingTimer_Tick;
                TimingTimer.Start();
            }
            else
            {
                CancelTiming();
            }
            SetStartButtonStyle();
        }

        private void SetStartButtonStyle()
        {
            if (TimingTimer is null)
            {
                StartButton.Style = App.Current.Resources["DefaultButtonStyle"] as Style;
                StartButton.Content = "开始定时";
            }
            else
            {
                StartButton.Style = App.Current.Resources["AccentButtonStyle"] as Style;
                StartButton.Content = $"取消定时（剩余时间：{LeftTime}）";
            }
            TimingEnable.IsEnabled = TimingTimer is null;
        }

        private void TimingTimer_Tick(object sender, object e)
        {
            LeftTime -= TimeSpan.FromSeconds(1);
            StartButton.Content = $"取消定时（{(LeftTime < TimeSpan.Zero ? "等待歌曲播放结束" : $"剩余时间：{LeftTime}")}）";
            if (LeftTime >= TimeSpan.Zero) return;
            TimingEndDo();
        }

        private void TimingEndDo()
        {
            if (WaitPlayEndCheckBox.IsChecked == true)
            {
                App.audioPlayer.SourceChanged += AudioPlayer_SourceChanged;
            }
            else
                TimingEndEventDo();
        }

        private async void TimingEndEventDo()
        {
            CancelTiming();
            SetStartButtonStyle();
            switch (TimingEndEvent)
            {
                case TimingEndEvent.暂停播放:
                    if (WaitPlayEndCheckBox.IsChecked == true) await System.Threading.Tasks.Task.Delay(100);
                    App.audioPlayer.SetPause();
                    break;
                case TimingEndEvent.退出程序:
                    App.ExitApp();
                    break;
                case TimingEndEvent.注销:
                    break;
                case TimingEndEvent.关机:
                    break;
            }
        }

        private void CancelTiming()
        {
            App.audioPlayer.SourceChanged -= AudioPlayer_SourceChanged;
            if (TimingTimer is not null)
            {
                TimingTimer.Tick -= TimingTimer_Tick;
                TimingTimer.Stop();
            }
            TimingTimer = null;
        }

        private void TimingTimePicker_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
        {
            StartButton.IsEnabled = TimingTimePicker.Time >= TimeSpan.Zero;
        }

        private void AudioPlayer_SourceChanged(Media.AudioPlayer audioPlayer)
        {
            App.audioPlayer.PlayEnd -= AudioPlayer_SourceChanged;
            TimingEndEventDo();
        }
    }
}
