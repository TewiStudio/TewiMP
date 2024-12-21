using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace TewiMP.Controls
{
    public partial class EQCard : Grid
    {
        public EQCard()
        {
            InitializeComponent();
            DataContextChanged += SongHistoryCard_DataContextChanged;
        }

        private void SongHistoryCard_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is null) return;
            //songHistoryData = DataContext as SongHistoryData;
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
