using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Controls;
using TewiMP.Background.HotKeys;

namespace TewiMP.Controls
{
    public partial class HotKeyPresenter : Page
    {
        public HotKeyPresenter()
        {
            InitializeComponent();
            Loaded += HotKeyPage_Loaded;
            Unloaded += HotKey_Unloaded;
        }

        private async void HotKeyPage_Loaded(object sender, RoutedEventArgs e)
        {
            HotKeyRoot.ItemsSource = App.hotKeyManager.RegistedHotKeys;
            await Task.Delay(100);
            if (HotKeyRoot.ItemsSource == null)
                HotKeyRoot.ItemsSource = App.hotKeyManager.RegistedHotKeys;
        }

        private void HotKey_Unloaded(object sender, RoutedEventArgs e)
        {
            HotKeyRoot.ItemsSource = null;
        }

        HotKey nowChangedHotKey = null;
        Button nowChangedButton = null;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            nowChangedHotKey = button.DataContext as HotKey;
            switch (button.Tag as string)
            {
                case "0":
                    nowChangedHotKey.IsDisabled = !nowChangedHotKey.IsDisabled;
                    App.hotKeyManager.ChangeHotKey(nowChangedHotKey);
                    break;
                case "1":
                    Pages.DialogPages.HotKeyEditor a = new();
                    a.ShowDialog(nowChangedHotKey);
                    break;
            }
        }

        private void HotKeyRoot_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
        }

        private void Button_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (sender.DataContext is null) return;
            var key = sender.DataContext as HotKey;
            if (key.IsDisabled)
            {
                ToolTipService.SetToolTip(sender, "启用");
                ((sender as Button).Content as FontIcon).Glyph = "\uE777";
            }
            else
            {
                ToolTipService.SetToolTip(sender, "禁用此热键");
                ((sender as Button).Content as FontIcon).Glyph = "\uE733";
            }
        }
    }

    public class HMConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            HotKeyID c = (HotKeyID)value;
            return HotKey.GetHotKeyIDString(c);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class IsUsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool c = (bool)value;
            return c ? "已被其它应用程序占用" : "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    
    public class IsUsedVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool c = (bool)value;
            return c ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class IsDisabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool c = (bool)value;
            return !c;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
