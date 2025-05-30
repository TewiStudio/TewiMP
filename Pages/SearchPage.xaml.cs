﻿using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using TewiMP.Plugin;
using TewiMP.DataEditor;

namespace TewiMP.Pages
{
    public partial class SearchPage : Page
    {
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var navToString = e.Parameter as string;
            if (string.IsNullOrEmpty(navToString)) return;
            SearchTextBox.Text = navToString;
        }

        public SearchPage()
        {
            InitializeComponent();
        }

        public void StartSearch(string title)
        {
            ListViewPages.ListViewPage.SetPageToListViewPage(new()
            {
                PageType = ListViewPages.PageType.Search,
                Param = new SearchData
                {
                    Key = title,
                    SourcePlugin = SearchSourceComboBox.SelectedItem as MusicSourcePlugin,
                    SearchDataType = (SearchDataType)Enum.Parse(typeof(SearchDataType), SearchTypeComboBox.SelectedItem as string)
                }
            });
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            string a;

            if (args.ChosenSuggestion != null)
            {
                a = args.ChosenSuggestion.ToString();
            }
            else
            {
                a = sender.Text;
            }

            if (a == "") return;

            //ContentFrame.Navigate(typeof(SearchPage));

            // 防止触发 NavView.SelectionChanged 事件
            //IsBackRequest = true;
            //NavView.SelectedItem = null;
            //IsBackRequest = false;
            StartSearch(a);
        }

        private void AutoSuggestBox_AccessKeyInvoked(UIElement sender, Microsoft.UI.Xaml.Input.AccessKeyInvokedEventArgs args)
        {
            (sender as AutoSuggestBox).Focus(FocusState.Programmatic);
        }

        private void AutoSuggestBox_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void AutoSuggestBox_LostFocus(object sender, RoutedEventArgs e)
        {
        }

        private void Page_PreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchTextBox.Text))
            {
                MainWindow.CanKeyDownBack = true;
            }
            else
            {
                MainWindow.CanKeyDownBack = false;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SearchSourceComboBox.ItemsSource = PluginManager.MusicSourcePlugins;
#if !DEBUG
            if (PluginManager.MusicSourcePlugins.Count > 0)
                SearchSourceComboBox.SelectedIndex = 0;
#endif

            var b = Enum.GetNames(typeof(SearchDataType)).ToList();
            b.RemoveAt(b.IndexOf(b.Last()));
            SearchTypeComboBox.ItemsSource = b;
            SearchTypeComboBox.SelectedIndex = 0;
            SearchTextBox.Focus(FocusState.Keyboard);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            SearchSourceComboBox.ItemsSource = null;
        }
    }
}
