using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Vanara.PInvoke;
using TewiMP.Core;
using TewiMP.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TewiMP.UI.Pages.DialogPages
{
    public sealed partial class HotKeyEditor : UserControl
    {
        public HotKeyEditor()
        {
            InitializeComponent();
        }

        HotKey hotKey = null;
        User32.HotKeyModifiers hotKeyModifiers = User32.HotKeyModifiers.MOD_NONE;
        VirtualKey normalKey = VirtualKey.A;
        private void HotKeyEditor_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.LeftWindows:
                case VirtualKey.RightWindows:
                    hotKeyModifiers = hotKeyModifiers | User32.HotKeyModifiers.MOD_WIN;
                    break;
                case VirtualKey.Control:
                    hotKeyModifiers = hotKeyModifiers | User32.HotKeyModifiers.MOD_CONTROL;
                    break;
                case VirtualKey.Shift:
                    hotKeyModifiers = hotKeyModifiers | User32.HotKeyModifiers.MOD_SHIFT;
                    break;
                case VirtualKey.Menu:
                    hotKeyModifiers = hotKeyModifiers | User32.HotKeyModifiers.MOD_ALT;
                    break;
                default:
                    normalKey = e.Key;
                    break;
            }

            hotKey = new HotKey(hotKeyModifiers, normalKey, default);
            HotKeyViewer.DataContext = hotKey;

            if (hotKeyModifiers == User32.HotKeyModifiers.MOD_NONE)
            {
                App.MainWindowInstance.AsyncDialog.IsPrimaryButtonEnabled = false;
            }
            else
            {
                App.MainWindowInstance.AsyncDialog.IsPrimaryButtonEnabled = true;
            }
        }

        private void AsyncDialog_PreviewKeyUp(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.LeftWindows:
                case VirtualKey.RightWindows:
                    hotKeyModifiers = hotKeyModifiers ^ User32.HotKeyModifiers.MOD_WIN;
                    break;
                case VirtualKey.Control:
                    hotKeyModifiers = hotKeyModifiers ^ User32.HotKeyModifiers.MOD_CONTROL;
                    break;
                case VirtualKey.Shift:
                    hotKeyModifiers = hotKeyModifiers ^ User32.HotKeyModifiers.MOD_SHIFT;
                    break;
                case VirtualKey.Menu:
                    hotKeyModifiers = hotKeyModifiers ^ User32.HotKeyModifiers.MOD_ALT;
                    break;
            }

            if (hotKey.HotKeyModifiers == User32.HotKeyModifiers.MOD_NONE)
            {
                App.MainWindowInstance.AsyncDialog.IsPrimaryButtonEnabled = false;
            }
            else
            {
                App.MainWindowInstance.AsyncDialog.IsPrimaryButtonEnabled = true;
            }
        }

        HotKey changedHotKey = null;
        bool isGettingHotKey = false;
        public async void ShowDialog(HotKey hotKey)
        {
            if (hotKey is null) return;
            changedHotKey = hotKey;

            NowHotKeyText.Text = $"当前热键：{HotKey.GetHotKeyIDString(hotKey.HotKeyID)}";
            HotKeyViewer.DataContext = changedHotKey;
            ShowDialog1();
            this.Focus(FocusState.Keyboard);
        }

        private async void ShowDialog1()
        {
            if (changedHotKey is null) return;
            App.MainWindowInstance.AsyncDialog.PreviewKeyDown += HotKeyEditor_KeyDown;
            App.MainWindowInstance.AsyncDialog.PreviewKeyUp += AsyncDialog_PreviewKeyUp;
            App.MainWindowInstance.AsyncDialog.IsPrimaryButtonEnabled = false;
            var r = await App.MainWindowInstance.ShowDialog("设置热键", this, "取消", "确定", "重置", ContentDialogButton.Primary);
            if (r == ContentDialogResult.Primary)
            {
                if (hotKey != null)
                {
                    hotKey.HotKeyID = changedHotKey.HotKeyID;
                    App.Instance.HotKeyService.ChangeHotKey(hotKey);
                }
            }
            else if (r == ContentDialogResult.Secondary)
            {
                foreach (var k in HotKeyService.DefaultRegisterHotKeysList)
                {
                    if (k.HotKeyID == changedHotKey.HotKeyID)
                    {
                        App.Instance.HotKeyService.ChangeHotKey(k);
                        break;
                    }
                }
            }
            App.MainWindowInstance.AsyncDialog.PreviewKeyDown -= HotKeyEditor_KeyDown;
            App.MainWindowInstance.AsyncDialog.PreviewKeyUp -= AsyncDialog_PreviewKeyUp;
            App.MainWindowInstance.AsyncDialog.IsPrimaryButtonEnabled = true;
        }
    }
}
