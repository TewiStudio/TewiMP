using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TewiMP.Helpers;
using Windows.Foundation;
using Windows.Foundation.Collections;

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
            if (isUnloaded) return;
            musicFolders.Clear();
            var folderPaths = await DataEditor.LocalMusicHelper.GetAllMusicFolders();
            foreach (string folder in folderPaths) musicFolders?.Add(folder);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Init();
            ItemsList.ItemsSource = musicFolders;
        }

        bool isUnloaded = false;
        private async void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            isUnloaded = true;
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
            if (musicFolders == null) return;
            var result = await FileHelper.UserSelectFolder(Windows.Storage.Pickers.PickerLocationId.MusicLibrary);
            if (musicFolders.Contains(result.Path)) return;
            musicFolders.Add(result.Path);
        }
    }
}
