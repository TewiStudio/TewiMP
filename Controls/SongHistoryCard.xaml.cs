using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using TewiMP.DataEditor;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TewiMP.Controls
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SongHistoryCard : Button
    {
        public SongHistoryCard()
        {
            InitializeComponent();
            DataContextChanged += SongHistoryCard_DataContextChanged;
        }

        static SolidColorBrush TransparentBrush = new(Microsoft.UI.Colors.Transparent);
        SongHistoryData songHistoryData = null;
        private void SongHistoryCard_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext == null) return;
            songHistoryData = DataContext as SongHistoryData;
            if (songHistoryData.Count % 2 == 0)
            {
                BackgroundRectangle.Visibility = Visibility.Collapsed;
            }
            else
            {
                BackgroundRectangle.Visibility = Visibility.Visible;
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await App.playingList.Play(songHistoryData.MusicData);
        }
    }
}
