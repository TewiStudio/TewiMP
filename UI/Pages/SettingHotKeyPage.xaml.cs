using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Composition;
using TewiMP.Services;

namespace TewiMP.UI.Pages
{
    public partial class SettingHotKeyPage : Page
    {
        public SettingHotKeyPage()
        {
            InitializeComponent();
        }


        bool isInLoading = false;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            isInLoading = true;
            HotkeyEnableToggleSwitch.IsOn = App.Instance.HotKeyService.EnableHotKey;
            isInLoading = false;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var list = App.Instance.HotKeyService.RegisteredHotKeys.ToList();
            App.Instance.HotKeyService.UnregisterHotKeys(list);
            await Task.Delay(200);
            App.Instance.HotKeyService.RegisterHotKeys(list);
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var list = App.Instance.HotKeyService.RegisteredHotKeys.ToList();
            App.Instance.HotKeyService.UnregisterHotKeys(list);
            await Task.Delay(200);
            App.Instance.HotKeyService.RegisterHotKeys(HotKeyService.DefaultRegisterHotKeysList);
        }

        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (isInLoading) return;
            if (sender is ToggleSwitch toggleSwitch)
            {
                App.Instance.HotKeyService.EnableHotKey = toggleSwitch.IsOn;
            }
        }
    }
}
