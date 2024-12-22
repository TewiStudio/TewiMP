using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TewiMP.Media;

namespace TewiMP.Controls
{
    public partial class EQCard : Grid
    {
        public new EQData DataContext
        {
            get => (EQData)base.DataContext; 
            set => base.DataContext = value;
        }

        public EQCard()
        {
            InitializeComponent();
            DataContextChanged += SongHistoryCard_DataContextChanged;
        }

        private void SongHistoryCard_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is null) return;

        }

        private void Silder_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
        }

        private void Segmented_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void ToggleOnButton_Checked(object sender, RoutedEventArgs e)
        {
        }
    }

    public partial class GainThumbToolTipValueConverter : Microsoft.UI.Xaml.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double)
            {
                double dValue = System.Convert.ToDouble(value) / 10;
                return dValue;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public partial class ThumbToolTipValueConverter : Microsoft.UI.Xaml.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double)
            {
                double dValue = System.Convert.ToDouble(value);
                return dValue;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
}
