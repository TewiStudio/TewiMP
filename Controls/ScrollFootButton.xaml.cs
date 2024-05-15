using Microsoft.UI.Xaml.Controls;

namespace TewiMP.Controls
{
    public partial class ScrollFootButton : UserControl
    {
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
