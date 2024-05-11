using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NAudio.Wave;

namespace TewiMP.Controls
{
    public partial class MediaPlayStateViewer : Grid
    {
        private PlaybackState playbackState = default;
        public PlaybackState PlaybackState
        {
            get
            {
                return playbackState;
            }
            set
            {
                playbackState = value;
                if (value == PlaybackState.Playing)
                {
                    PlayIcon.Visibility = Visibility.Collapsed;
                    PauseIcon.Visibility = Visibility.Visible;
                }
                else
                {
                    PlayIcon.Visibility = Visibility.Visible;
                    PauseIcon.Visibility = Visibility.Collapsed;
                }
            }
        }

        public MediaPlayStateViewer()
        {
            InitializeComponent();
        }
    }
}
