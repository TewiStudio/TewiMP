using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TewiMP.Helpers;

namespace TewiMP.Controls
{
    public sealed partial class ManageLocalMusicFolderControl : Grid
    {
        ObservableCollection<string> musicFolders = [];
        ObservableCollection<string> deletedMusicFolders = [];
        public ManageLocalMusicFolderControl()
        {
            this.InitializeComponent();
        }

        async void Init()
        {
            if (!this.IsLoaded) return;
            musicFolders.Clear();
            var folderPaths = await DataEditor.LocalMusicHelper.GetAllMusicFolders();
            foreach (string folder in folderPaths) musicFolders?.Add(folder);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Init();
            ItemsList.ItemsSource = musicFolders;
        }

        private async void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded) return;
            if (musicFolders != null)
            {/*
                foreach (string folder in deletedMusicFolders)
                {
                    await DataEditor.LocalMusicHelper.RemoveLocalMusicFolder(folder);
                }*/
                var data = await DataEditor.LocalMusicHelper.GetLocalMusicData();
                foreach (string folder in await DataEditor.LocalMusicHelper.GetAllMusicFolders(data))
                {
                    await DataEditor.LocalMusicHelper.RemoveLocalMusicFolder(folder, data);
                }
                foreach (string folderPath in musicFolders)
                {
                    await DataEditor.LocalMusicHelper.AddLocalMusicFolder(folderPath, data);
                }
                await DataEditor.LocalMusicHelper.SaveLocalMusicData(data);
            }

            musicFolders = null;
            ItemsList.ItemsSource = null;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            string folderPath = (string)btn.DataContext;
            musicFolders.Remove(folderPath);
            deletedMusicFolders.Add(folderPath);
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (musicFolders is null) return;
            var result = await FileHelper.UserSelectFolder(Windows.Storage.Pickers.PickerLocationId.MusicLibrary);
            if (musicFolders.Contains(result.Path)) return;
            musicFolders.Add(result.Path);
        }
    }
}
