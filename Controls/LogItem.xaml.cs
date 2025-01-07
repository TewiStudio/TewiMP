using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Composition;
using TewiMP.Media;
using TewiMP.Helpers;
using TewiMP.DataEditor;
using TewiMP.Background;

namespace TewiMP.Controls
{
    public sealed partial class LogItem : UserControl
    {
        public new LogData DataContext => (LogData)base.DataContext;

        public LogItem()
        {
            InitializeComponent();
        }

        private void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is null) return;
            t1.Text = $"[{DataContext.LogTime}][{DataContext.LogLevel}]";
            t2_1.Text = $" ¡ñ {DataContext.LogName}£º";
            t2_2.Text = $"{DataContext.LogContent}";
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
