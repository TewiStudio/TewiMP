using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace TewiMP.Controls
{
    public partial class ScrollFootButton : UserControl
    {
        public enum ButtonType { NowPlaying, Top, Bottom }
        public ScrollFootButton()
        {
            InitializeComponent();
            PositionToNowPlaying_Button.Tag = ButtonType.NowPlaying;
            PositionToTop_Button.Tag = ButtonType.Top;
            PositionToBottom_Button.Tag = ButtonType.Bottom;
        }
    }
}
