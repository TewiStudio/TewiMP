using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Composition;
using CommunityToolkit.WinUI.Controls;
using TewiMP.Helpers;
using TewiMP.Services.Storage;
using System.Threading.Tasks;
using TewiMP.Services.Plugin;

namespace TewiMP.UI.Pages
{
    public partial class SettingPlugin : Page
    {
        public SettingPlugin()
        {
            InitializeComponent();
        }


        bool isInLoading = false;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            isInLoading = true;
            PluginMusicSource.ItemsSource = PluginService.MusicSourcePlugins;
            PluginOther.ItemsSource = PluginService.Plugins;
            isInLoading = false;
        }

        private void SettingsCard_Click(object sender, RoutedEventArgs e)
        {
            PluginService.Init();
        }

        private async void SettingsCard_Click_1(object sender, RoutedEventArgs e)
        {
            if (sender is SettingsCard card)
            {
                if (card.DataContext is Services.Plugin.Plugin plugin)
                {
                    await plugin.ShowSettingsDialog();
                    PluginService.SavePluginInfoSettings();
                }
            }
        }

        private void SettingsCard_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (sender is SettingsCard card && args.NewValue is Services.Plugin.Plugin dataContent)
            {
                card.Header = $"{dataContent.PluginInfo.Name} ({dataContent.PluginInfo.Version})";
                card.Description = string.IsNullOrEmpty(dataContent.PluginInfo.Describe) ?
                    $"by {dataContent.PluginInfo.Author}" :
                    $"{dataContent.PluginInfo.Describe}\nby {dataContent.PluginInfo.Author}";
            }
        }

        private async void SettingsCard_Click_2(object sender, RoutedEventArgs e)
        {
            await FileHelper.ExploreFolder(DataFolderBase.PluginFolder);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await CodeHelper.OpenInBrowser("https://github.com/TewiStudio/TewiMP/tree/master/Plugin/BuildInPlugins/BuildInPluginSample");
        }
    }
}
