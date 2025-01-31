using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using TewiMP.Background;

namespace TewiMP.Controls
{
    public partial class NotifyItem : Grid
    {
        private NotifyItemData notifyItemData { get; set; }

        public NotifyItem()
        {
            InitializeComponent();
        }

        string normalBackgroundIcon = "\uF136";
        string waringBackgroundIcon = "\uF139";
        string informationIcon = "\uF13F";
        string completeIcon = "\uF13E";
        string errorIcon = "\uF13D";
        string waringIcon = "\uF13B";
        public void SetNotifyItemData(NotifyItemData notifyItemData)
        {
            this.notifyItemData = notifyItemData;
            if (notifyItemData is null) return;
            var logLevel = notifyItemData.Severity.ToLogLevel();
            if (logLevel is not null)
            {
                App.logManager.Log(notifyItemData.Title, notifyItemData.Message, (LogLevel)logLevel);
            }

            MessageTextBlock.Visibility = string.IsNullOrEmpty(notifyItemData.Message) ?
                Visibility.Collapsed : Visibility.Visible;
            ClickableButton.Visibility = string.IsNullOrEmpty(notifyItemData.ButtonMessage) ?
                Visibility.Collapsed : Visibility.Visible;

            ClickableButton.Content = notifyItemData.ButtonMessage;

            foreach (FrameworkElement element in BackgroundColorControl.Children)
            {
                element.Visibility = Visibility.Collapsed;
            }
            if (notifyItemData.Severity == NotifySeverity.Info || notifyItemData.Severity == NotifySeverity.Error
                || notifyItemData.Severity == NotifySeverity.Complete)
            {
                IconControlBackground.Glyph = normalBackgroundIcon;
                switch (notifyItemData.Severity)
                {
                    case NotifySeverity.Info:
                        IconControl.Glyph = informationIcon;
                        IconControl.Foreground = App.Current.Resources["SystemFillColorCriticalBackgroundBrush"] as Brush;
                        IconControlBackground.Foreground = App.Current.Resources["SystemFillColorAttentionBrush"] as Brush;
                        break;
                    case NotifySeverity.Error:
                        IconControl.Glyph = errorIcon;
                        IconControl.Foreground = App.Current.Resources["SystemFillColorCriticalBackgroundBrush"] as Brush;
                        IconControlBackground.Foreground = App.Current.Resources["SystemFillColorCriticalBrush"] as Brush;
                        BackgroundColorControl.Children[2].Visibility = Visibility.Visible;
                        break;
                    case NotifySeverity.Complete:
                        IconControl.Glyph = completeIcon;
                        IconControl.Foreground = App.Current.Resources["SystemFillColorSuccessBackgroundBrush"] as Brush;
                        IconControlBackground.Foreground = App.Current.Resources["SystemFillColorSuccessBrush"] as Brush;
                        BackgroundColorControl.Children[0].Visibility = Visibility.Visible;
                        break;
                }
            }
            else if (notifyItemData.Severity == NotifySeverity.Warning)
            {
                IconControlBackground.Glyph = waringBackgroundIcon;
                IconControl.Glyph = waringIcon;
                IconControl.Foreground = App.Current.Resources["SystemFillColorCautionBackgroundBrush"] as Brush;
                IconControlBackground.Foreground = App.Current.Resources["SystemFillColorCautionBrush"] as Brush;
                BackgroundColorControl.Children[1].Visibility = Visibility.Visible;
            }
            
            if (notifyItemData.Severity == NotifySeverity.Loading)
            {
                IconLoading.Visibility = Visibility.Visible;
                IconControlBackground.Visibility = Visibility.Collapsed;
                IconControl.Visibility = Visibility.Collapsed;
            }
            else
            {
                IconLoading.Visibility = Visibility.Collapsed;
                IconControlBackground.Visibility = Visibility.Visible;
                IconControl.Visibility = Visibility.Visible;
            }

            TitleTextBlox.Text = notifyItemData.Title;
            MessageTextBlock.Text = notifyItemData.Message;
        }

        public void SetNotifyItemData(string title, string message, NotifySeverity severity = NotifySeverity.Info, string buttonMessage = null, Action buttonAction = null)
        {
            SetNotifyItemData(new(title, message, severity, null, buttonMessage, buttonAction));
        }

        public NotifyItemData GetNotifyItemData()
        {
            return notifyItemData;
        }

        public void SetProcess(int maxValue, int value)
        {
            IconLoading.IsIndeterminate = value == 0;
            IconLoading.Maximum = maxValue;
            IconLoading.Value = value;
        }

        private void sizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void unloaded(object sender, RoutedEventArgs e)
        {

        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Grid_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {

        }

        private void ClickableButton_Click(object sender, RoutedEventArgs e)
        {
            notifyItemData.ButtonAction.Invoke();
        }
    }
}
