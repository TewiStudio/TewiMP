using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace TewiMP.Controls
{
    public partial class ScrollFootButton : UserControl
    {
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
    }
}
