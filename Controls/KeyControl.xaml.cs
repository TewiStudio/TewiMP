using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Vanara.PInvoke;
using Windows.System;
using TewiMP.Background.HotKeys;

namespace TewiMP.Controls
{
    public partial class KeyControl : StackPanel
    {
        public HotKey HotKey { get; set; } = null;

        public KeyControl()
        {
            InitializeComponent();
        }

        private void Button_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is null) return;
            if (DataContext.GetType() != typeof(HotKey)) return;
            HotKey = (HotKey)DataContext;
            UpdateInterface();
        }

        HotKey lastHotkey = null;
        private void UpdateInterface()
        {
            if (HotKey is null) return;
            if (lastHotkey == HotKey) return;
            lastHotkey = HotKey;
            Children.Clear();
            var keyStrings = HotKey.GetHKMString(HotKey.HotKeyModifiers).Split(" + ");
            foreach (var keyString in keyStrings)
            {
                User32.HotKeyModifiers modifiers = User32.HotKeyModifiers.MOD_NONE;
                switch (keyString)
                {
                    case "Win":
                        modifiers = User32.HotKeyModifiers.MOD_WIN;
                        break;
                    case "Ctrl":
                        modifiers = User32.HotKeyModifiers.MOD_CONTROL;
                        break;
                    case "Shift":
                        modifiers = User32.HotKeyModifiers.MOD_SHIFT;
                        break;
                    case "Alt":
                        modifiers = User32.HotKeyModifiers.MOD_ALT;
                        break;
                }
                if (modifiers != User32.HotKeyModifiers.MOD_NONE)
                    AddKeyVisual(modifiers);
            }
            AddKeyVisual(HotKey.VirtualKey);
        }

        private void AddKeyVisual(User32.HotKeyModifiers hotKeyModifiers)
        {
            ToggleButton toggleButton = new ToggleButton()
            {
                IsChecked = true,
                IsHitTestVisible = false,
                IsTabStop = false,
                MinWidth = 38,
                MinHeight = 34,
                FontWeight = new(500)
        };
            toggleButton.Content = HotKey.GetHKMString(hotKeyModifiers);
            Children.Add(toggleButton);
        }

        private void AddKeyVisual(VirtualKey virtualKey)
        {
            ToggleButton toggleButton = new ToggleButton()
            {
                IsChecked = true,
                IsHitTestVisible = false,
                IsTabStop = false,
                MinWidth = 38,
                MinHeight = 34,
                FontWeight = new(500)
            };
            object content;
            switch (virtualKey)
            {
                case VirtualKey.Left:
                case VirtualKey.Right:
                case VirtualKey.Up:
                case VirtualKey.Down:
                    string glyph = virtualKey switch
                    {
                        VirtualKey.Left => "\uE76B",
                        VirtualKey.Right => "\uE76C",
                        VirtualKey.Up => "\uE70E",
                        _ => "\uE70D"
                    };
                    content = new FontIcon() { Glyph = glyph, FontSize = 10 };
                    break;
                case VirtualKey.NumberPad0:
                case VirtualKey.NumberPad1:
                case VirtualKey.NumberPad2:
                case VirtualKey.NumberPad3:
                case VirtualKey.NumberPad4:
                case VirtualKey.NumberPad5:
                case VirtualKey.NumberPad6:
                case VirtualKey.NumberPad7:
                case VirtualKey.NumberPad8:
                case VirtualKey.NumberPad9:
                    content = virtualKey.ToString().Replace("NumberPad", "NumPad ");
                    break;
                case VirtualKey.Number0:
                case VirtualKey.Number1:
                case VirtualKey.Number2:
                case VirtualKey.Number3:
                case VirtualKey.Number4:
                case VirtualKey.Number5:
                case VirtualKey.Number6:
                case VirtualKey.Number7:
                case VirtualKey.Number8:
                case VirtualKey.Number9:
                    content = virtualKey.ToString().Replace("Number", "");
                    break;
                case VirtualKey.Subtract: content = "-"; break;
                case VirtualKey.Add: content = "+"; break;
                case VirtualKey.Multiply: content = "×"; break;
                case VirtualKey.Divide: content = "÷"; break;
                case VirtualKey.Decimal: content = "."; break;
                case VirtualKey.CapitalLock: content = "Caps Lock"; break;
                case VirtualKey.Application: content = "Menu"; break;
                case VirtualKey.PageUp: content = "PgUp"; break;
                case VirtualKey.PageDown: content = "PgDn"; break;
                case (VirtualKey)189: content = "_"; break;
                case (VirtualKey)187: content = "="; break;
                case (VirtualKey)219: content = "["; break;
                case (VirtualKey)221: content = "]"; break;
                case (VirtualKey)220: content = "\\"; break;
                case (VirtualKey)186: content = ";"; break;
                case (VirtualKey)222: content = "'"; break;
                case (VirtualKey)188: content = "<"; break;
                case (VirtualKey)190: content = ">"; break;
                case (VirtualKey)191: content = "/"; break;
                case (VirtualKey)192: content = "~"; break;
                default:
                    content = virtualKey.ToString(); break;
            }
            toggleButton.Content = content;
            Children.Add(toggleButton);
        }
    }
}
