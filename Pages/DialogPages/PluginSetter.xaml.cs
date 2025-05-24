using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using TewiMP.Plugin;

namespace TewiMP.Pages.DialogPages
{
    public sealed partial class PluginSetter : UserControl
    {
        public Plugin.Plugin Plugin { get; set; }
        public PluginSetter()
        {
            InitializeComponent();
        }

        ObservableCollection<PluginSetting> values = [];
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var item in Plugin.PluginSettings)
            {
                values.Add(new(Plugin, item));
            }
            SettingsListRoot.ItemsSource = values;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            SettingsListRoot.ItemsSource = null;
        }

        private void SettingsCard_Loading(FrameworkElement sender, object args)
        {

        }

        bool isDataContentChange = false;
        private void SettingsCard_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            isDataContentChange = true;
            if (sender is SettingsCard card && card.Content is Grid grid && grid.DataContext is PluginSetting setting)
            {
                foreach (var item in grid.Children)
                {
                    item.Visibility = Visibility.Collapsed;
                }
                if (setting.Value is string value && grid.Children[0] is TextBox textBox)
                {
                    textBox.Visibility = Visibility.Visible;
                    textBox.Text = value;
                }
                else if (setting.Value is int or double or float or decimal && grid.Children[1] is NumberBox numberBox)
                {
                    numberBox.Visibility = Visibility.Visible;
                    if (setting.Value is int inter) numberBox.Value = inter;
                    else if (setting.Value is double doub) numberBox.Value = doub;
                    else if (setting.Value is float flo) numberBox.Value = flo;
                    else if (setting.Value is decimal dec) numberBox.Value = (double)dec; // NumberBox does not support decimal directly
                }
                else if (setting.Value is bool boolean && grid.Children[2] is ToggleSwitch toggleSwitch)
                {
                    toggleSwitch.Visibility = Visibility.Visible;
                    toggleSwitch.IsOn = boolean;
                }
            }
            isDataContentChange = false;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isDataContentChange && sender is TextBox textBox && textBox.DataContext is PluginSetting pluginSetting)
            {
                pluginSetting.Plugin.SetSetting(pluginSetting.Key, textBox.Text);
            }
        }

        private void NumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (!isDataContentChange && sender is NumberBox numberBox && numberBox.DataContext is PluginSetting pluginSetting)
            {/*
                if (pluginSetting.Value is int)
                {
                    pluginSetting.Plugin.SetSetting(pluginSetting.Key, (int)numberBox.Value);
                }
                else if (pluginSetting.Value is double)
                {
                }
                else if (pluginSetting.Value is float)
                {
                    pluginSetting.Plugin.SetSetting(pluginSetting.Key, (float)numberBox.Value);
                }
                else if (pluginSetting.Value is decimal)
                {
                    pluginSetting.Plugin.SetSetting(pluginSetting.Key, (decimal)numberBox.Value);
                }*/
                pluginSetting.Plugin.SetSetting(pluginSetting.Key, numberBox.Value);
            }
        }

        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isDataContentChange && sender is ToggleSwitch toggleSwitch && toggleSwitch.DataContext is PluginSetting pluginSetting)
            {
                pluginSetting.Plugin.SetSetting(pluginSetting.Key, toggleSwitch.IsOn);
            }
        }
    }

    internal class PluginSetting(Plugin.Plugin plugin, KeyValuePair<string, object> keyValue)
    {
        public Plugin.Plugin Plugin => plugin;
        public string Key => keyValue.Key;
        public object Value => keyValue.Value;
        public string DisplayName => plugin.GetUserViewPluginSettingName(Key);
        public string Describe => plugin.GetUserViewPluginSettingDescribe(Key);
    }
}
