using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Composition;
using TewiMP.Services.Storage;
using TewiMP.UI.Controls;
using TewiMP.Core;

namespace TewiMP.UI.Pages
{
    public partial class HistoryPage : Page
    {
        bool isLeavedPage = false;
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            isLeavedPage = true;
        }

        ObservableCollection<SongHistoryData> songHistories = new();
        public HistoryPage()
        {
            InitializeComponent();
            ListViewBase.ItemsSource = songHistories;
            HistoryHelper.HistoryDataChanged += HistoryHelper_HistoryDataChanged;
        }

        private void HistoryHelper_HistoryDataChanged()
        {
            if (HeaderSelectBase.SelectedItem == HeaderSelectBase.Items[1])
                Init();
        }

        private async void Init()
        {
            if (isLeavedPage) return;
            var scrollOffset = StickContentHeader.CachedScrollViewer.VerticalOffset;
            var datas = await SongHistoryHelper.GetHistories();
            List<SongHistoryData> d = [.. datas];
            d = d.OrderByDescending(m => m.Time).ToList();
            if (isLeavedPage) return;
            songHistories.Clear();
            foreach (var data in d)
            {
                songHistories.Add(data);
            }
            await Task.Delay(10);
            if (IsLoaded)
                StickContentHeader.CachedScrollViewer.ScrollToVerticalOffset(scrollOffset);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await App.Instance.PlayingListService.Play(((sender as Button).DataContext as SongHistoryData).MusicData);
        }

        private void HeaderSelectBase_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            if (HeaderSelectBase.SelectedItem == HeaderSelectBase.Items[1])
            {
                HeaderText.Visibility = Visibility.Visible;
                ListViewBase.ItemsSource = songHistories;
                ListViewBase.ItemTemplate = this.Resources["HistoryDataTemplate"] as DataTemplate;
                Init();
            }
            else
            {
                HeaderText.Visibility = Visibility.Collapsed;
                songHistories.Clear();
                ListViewBase.ItemsSource = null;
                ListViewBase.ItemTemplate = null;
                ListViewBase.Items.Clear();
                ListViewBase.Items.Add(new SongHistoryInfo() { Margin = new(0, 12, 0, 0) });
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ListViewBase.ItemTemplate = this.Resources["HistoryDataTemplate"] as DataTemplate;
            Init();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            HistoryHelper.HistoryDataChanged -= HistoryHelper_HistoryDataChanged;
            songHistories.Clear();
            ListViewBase.ItemsSource = null;
            ListViewBase.Items.Clear();
        }
    }
}
