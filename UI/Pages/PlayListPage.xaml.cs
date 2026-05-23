using System.Collections;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Composition;
using TewiMP.Core.Music;
using TewiMP.UI.Controls;

namespace TewiMP.UI.Pages
{
    public partial class PlayListPage : Page
    {
        private ScrollViewer scrollViewer => StickTitleHeader.CachedScrollviewer;
        private static double verticalOffset = 0;
        ArrayList arrayList;
        public PlayListPage()
        {
            InitializeComponent();
            //arrayList = new ArrayList(100000000);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (scrollViewer is not null)
                verticalOffset = scrollViewer.VerticalOffset;
        }

        ObservableCollection<MusicListData> playListCards = new();
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ItemsViewer.ItemsSource = playListCards;
            Init();
            UpdatePlayList();

            await Task.Delay(10);
            scrollViewer.ScrollToVerticalOffset(verticalOffset);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            playListCards.Clear();
            ItemsViewer.ItemsSource = null;
            BaseGridView.Items.Clear();
            RemoveEvent();
        }

        private void MainWindow_MainViewStateChanged(bool isView)
        {/*
            if (isView)
                ItemsViewer.ItemsSource = playListCards;
            else
            {
                ItemsViewer.ItemsSource = null;
            }*/
        }

        bool isInUpdate = false;
        public async void UpdatePlayList()
        {
            if (isInUpdate) return;
            isInUpdate = true;
            playListCards.Clear();

            if (App.Instance.PlayListReader.NowMusicListData is null)
                await App.Instance.PlayListReader.Refresh();

            int count = 0;
            foreach (var item in App.Instance.PlayListReader.NowMusicListData)
            {
                count++;
                playListCards.Add(item);
            }
            isInUpdate = false;
        }

        void Init()
        {
            InitEvent();
        }

        void InitEvent()
        {
            if (!IsLoaded) return;
            App.Instance.PlayListReader.Updated -= PlayListReader_Updated;
            App.Instance.PlayListReader.Updated += PlayListReader_Updated;
            App.MainWindowInstance.MainViewStateChanged -= MainWindow_MainViewStateChanged;
            App.MainWindowInstance.MainViewStateChanged += MainWindow_MainViewStateChanged;
        }

        void RemoveEvent()
        {
            App.Instance.PlayListReader.Updated -= PlayListReader_Updated;
            App.MainWindowInstance.MainViewStateChanged -= MainWindow_MainViewStateChanged;
        }

        private void PositionToButton_Click(object sender, RoutedEventArgs e)
        {
            switch ((ScrollFootButton.ButtonType)sender)
            {
                case ScrollFootButton.ButtonType.Top:
                    scrollViewer.ChangeView(null, 0, null);
                    break;
                case ScrollFootButton.ButtonType.Bottom:
                    scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null);
                    break;
            }
        }

        private void PlayListReader_Updated()
        {
            UpdatePlayList();
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            await App.Instance.PlayListReader.Refresh();
        }

        private async void AppBarButton_Click_1(object sender, RoutedEventArgs e)
        {
            await DialogPages.AddPlayListPage.ShowDialog();
        }

        private async void AppBarButton_Click_2(object sender, RoutedEventArgs e)
        {
            await DialogPages.InsertPlayListPage.ShowDialog();
        }
    }
}
