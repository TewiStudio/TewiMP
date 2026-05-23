using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace TewiMP.UI.Controls
{
    public partial class ScrollFootButton : UserControl
    {
        public event RoutedEventHandler PositionButtonClick;

        public bool IsPositionToNowPlayingButtonShow
        {
            get => PositionToNowPlaying_Button.Visibility == Visibility;
            set
            {
                PositionToNowPlaying_Button.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public enum ButtonType { NowPlaying, Top, Bottom }
        public ScrollFootButton()
        {
            InitializeComponent();
            PositionToNowPlaying_Button.Tag = ButtonType.NowPlaying;
            PositionToTop_Button.Tag = ButtonType.Top;
            PositionToBottom_Button.Tag = ButtonType.Bottom;
        }

        private void PositionToBottom_Button_Click(object sender, RoutedEventArgs e)
        {
            PositionButtonClick?.Invoke(ButtonType.Bottom, e);
        }

        private void PositionToTop_Button_Click(object sender, RoutedEventArgs e)
        {
            PositionButtonClick?.Invoke(ButtonType.Top, e);
        }

        private void PositionToNowPlaying_Button_Click(object sender, RoutedEventArgs e)
        {
            PositionButtonClick?.Invoke(ButtonType.NowPlaying, e);
        }
    }
}
