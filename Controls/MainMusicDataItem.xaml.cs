using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TewiMP.DataEditor;
using Windows.ApplicationModel.Appointments.AppointmentsProvider;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TewiMP.Controls
{
    internal static class MainMusicDataItemStatic
    {
        internal static List<MainMusicDataItem> InstancesList = new List<MainMusicDataItem>();

        static bool isInited = false;
        internal static void InitListen(this MainMusicDataItem item)
        {
            if (isInited) return;
            isInited = true;
            App.audioPlayer.SourceChanged += (_) =>
            {
                foreach (MainMusicDataItem item in InstancesList)
                {
                    item.IsPlaying = item.MusicData == _.MusicData;
                }
            };
        }

        internal static void StaticAdd(this MainMusicDataItem item)
        {
            InstancesList.Add(item);
        }

        internal static void StaticRemove(this MainMusicDataItem item)
        {
            InstancesList.Remove(item);
        }
    }

    public sealed partial class MainMusicDataItem : UserControl
    {
        private bool _isPlaying = false;

        public MusicData MusicData => DataContext as MusicData;
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                _isPlaying = value;
                if (value)
                {
                    BackgroundFill.Opacity = 1;
                    IsPlayingFill.Opacity = 1;
                }
                else
                {
                    BackgroundFill.Opacity = 0;
                    IsPlayingFill.Opacity = 0;
                }
            }
        }

        public MainMusicDataItem()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            App.playingList.NowPlayingList.Remove(DataContext as MusicData);
        }

        private void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext == null) return;
            IsPlaying = MusicData == App.audioPlayer.MusicData;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.InitListen();
            this.StaticAdd();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            this.StaticRemove();
        }

        private async void Grid_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (IsPlaying) return;
            await App.playingList.Play(MusicData);
        }

        private void UserControl_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            BackgroundFill.Opacity = 1;
        }

        private void UserControl_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (IsPlaying) return;
            BackgroundFill.Opacity = 0;
        }
    }
}
