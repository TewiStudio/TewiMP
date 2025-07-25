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
using TewiMP.Windowed;

namespace TewiMP.Controls
{
    public sealed partial class LogItem : UserControl
    {
        public new LogData DataContext => (LogData)base.DataContext;

        public LogItem()
        {
            InitializeComponent();
        }

        private void t1_2_ColorManager()
        {
            if (DataContext is null) return;
            if (DataContext.LogLevel == LogLevel.Info)
            {
                t2.Foreground = App.Current.Resources["TextFillColorPrimaryBrush"] as SolidColorBrush;
                t1_2.Foreground = App.Current.Resources["TextFillColorPrimaryBrush"] as SolidColorBrush;
            }
            else if (DataContext.LogLevel == LogLevel.Warning)
            {
                t2.Foreground = App.Current.Resources["TextOnAccentFillColorPrimaryBrush"] as SolidColorBrush;
                t1_2.Foreground = App.Current.Resources["SystemFillColorCautionBackgroundBrush"] as SolidColorBrush;
            }
            else if (DataContext.LogLevel == LogLevel.Error)
            {
                t2.Foreground = App.Current.Resources["TextOnAccentFillColorPrimaryBrush"] as SolidColorBrush;
                t1_2.Foreground = App.Current.Resources["SystemFillColorCriticalBackgroundBrush"] as SolidColorBrush;
            }
        }

        private void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is null) return;
            t1_1.Text = $"[{DataContext.LogTime}]";
            t1_2.Text = $"[{DataContext.LogLevel}]";
            t2_1.Text = $" ¡ñ {DataContext.LogName}£º";
            t2_2.Text = $"{DataContext.LogContent}";
            t1_2_ColorManager();
            swp.Value = DataContext.LogLevel;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private void UserControl_ActualThemeChanged(FrameworkElement sender, object args)
        {
            t1_2_ColorManager();
        }
    }
}
