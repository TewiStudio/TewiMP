using System.Linq;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TewiMP.Helpers;

namespace TewiMP.Controls
{
    public sealed partial class ScrollViewerSearchControl : Grid
    {
        public delegate void SearchItemDelegate(SongItemBindBase songItemBind);
        public event SearchItemDelegate SearchingAItem;
        public event DependencyPropertyChangedEventHandler IsOpenChanged;

        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(
                "IsOpen",
                typeof(bool),
                typeof(ScrollViewerSearchControl),
                new(false, OnIsOpenChanged)
                );
        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScrollViewerSearchControl ssc = d as ScrollViewerSearchControl;
            var value = (bool)e.NewValue;
            ssc.SetIsOpen(value);
            ssc.IsOpenChanged?.Invoke(d, e);
        }

        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value);
        }
        void SetIsOpen(bool value)
        {
            if (value)
            {
                Opacity = 1;
                IsHitTestVisible = true;
            }
            else
            {
                Opacity = 0;
                IsHitTestVisible = false;
            }
        }

        public ObservableCollection<SongItemBindBase> SongItemBinds { get; set; }
        ObservableCollection<SongItemBindBase> searchResult { get; set; } = [];

        public ScrollViewerSearchControl()
        {
            InitializeComponent();
        }

        public void FocusToSearchBox()
        {
            SearchBox.Focus(FocusState.Keyboard);
        }

        string CompareString(string str)
        {
            return (bool)LowerCheckBox.IsChecked ? str : str.ToLower();
        }

        void ChangeViewToSearchItem(bool add = true)
        {
            if (searchResult.Any())
            {
                if (add) searchNum++;
                else searchNum--;

                if (searchNum > searchResult.Count - 1) searchNum = 0;
                if (searchNum <= -1) searchNum = searchResult.Count - 1;

                var item = searchResult[searchNum];
                SearchResultTextBlock.Text = $"{searchNum + 1} of {searchResult.Count}";

                SearchingAItem?.Invoke(item);
            }
            else
            {
                SearchResultTextBlock.Text = "0 of 0";
            }
        }

        private void SearchBox_Loaded(object sender, RoutedEventArgs e)
        {
            SearchBox.ItemsSource = searchResult;
        }

        private void SearchBox_Unloaded(object sender, RoutedEventArgs e)
        {
            SearchBox.ItemsSource = null;
            SongItemBinds = null;
        }

        bool isQuery = false;
        int searchNum = -1;
        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (SongItemBinds == null) return;
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput) // сп bug
            {
                if (string.IsNullOrEmpty(SearchBox.Text)) return;
                searchResult.Clear();
                string text = CompareString(SearchBox.Text);

                switch (SearchModeComboBox.SelectedIndex)
                {
                    case 0:
                        foreach (var i in SongItemBinds)
                        {
                            if (CompareString(i.MusicData.Title).Contains(text))
                                if (!searchResult.Contains(i))
                                    searchResult.Add(i);
                            if (i.MusicData.Title2 is not null)
                            {
                                if (CompareString(i.MusicData.Title2).Contains(text))
                                    if (!searchResult.Contains(i))
                                        searchResult.Add(i);
                            }
                            if (i.MusicData.ArtistName is not null)
                            {
                                if (CompareString(i.MusicData.ArtistName).Contains(text))
                                    if (!searchResult.Contains(i))
                                        searchResult.Add(i);
                            }
                            if (CompareString(i.MusicData.Album.Title).Contains(text))
                                if (!searchResult.Contains(i))
                                    searchResult.Add(i);
                        }
                        break;
                    case 1:
                        foreach (var i in SongItemBinds)
                        {
                            if (CompareString(i.MusicData.Title).Contains(text))
                                searchResult.Add(i);
                            else if (i.MusicData.Title2 is not null)
                            {
                                if (CompareString(i.MusicData.Title2).Contains(text))
                                    searchResult.Add(i);
                            }
                        }
                        break;
                    case 2:
                        foreach (var i in SongItemBinds)
                        {
                            if (i.MusicData.ArtistName is not null)
                            {
                                if (CompareString(i.MusicData.ArtistName).Contains(text))
                                    searchResult.Add(i);
                            }
                        }
                        break;
                    case 3:
                        foreach (var i in SongItemBinds)
                        {
                            if (CompareString(i.MusicData.Album.Title).Contains(text))
                                searchResult.Add(i);
                        }
                        break;
                }
                searchNum = 0;
                SearchResultTextBlock.Text = $"All of {searchResult.Count}";
            }
            else
            {
                //SearchingAItem?.Invoke(SongItemBinds[searchNum]);
            }
        }

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            SearchBox_TextChanged(null, new() { Reason = AutoSuggestionBoxTextChangeReason.UserInput });
            ChangeViewToSearchItem();
        }

        private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (SearchModeComboBox.SelectedIndex == 0)
                SearchBox.Text = (args.SelectedItem as SongItemBindBase).MusicData.Title;
            else if (SearchModeComboBox.SelectedIndex == 1)
                SearchBox.Text = (args.SelectedItem as SongItemBindBase).MusicData.Title;
            else if (SearchModeComboBox.SelectedIndex == 2)
                SearchBox.Text = (args.SelectedItem as SongItemBindBase).MusicData.ArtistName;
            else if (SearchModeComboBox.SelectedIndex == 3)
                SearchBox.Text = (args.SelectedItem as SongItemBindBase).MusicData.Album.Title;

            searchNum = SongItemBinds.IndexOf(args.SelectedItem as SongItemBindBase);
            SearchResultTextBlock.Text = $"{searchNum + 1} of {SongItemBinds.Count}";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            switch (btn.Tag as string)
            {
                case "0":
                    ChangeViewToSearchItem(false);
                    break;
                case "1":
                    ChangeViewToSearchItem(true);
                    break;
                case "2":
                    IsOpen = !IsOpen;
                    break;
            }
        }
    }
}
