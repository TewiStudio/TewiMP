using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using TewiMP.Media;

namespace TewiMP.Controls
{
    public partial class PassFilterCard : Grid
    {
        public new PassFilterData DataContext
        {
            get => (PassFilterData)base.DataContext; 
            set => base.DataContext = value;
        }

        public PassFilterCard()
        {
            InitializeComponent();
            Loaded += EQCard_Loaded;
            Unloaded += EQCard_Unloaded;
            DataContextChanged += SongHistoryCard_DataContextChanged;
        }

        private void EQCard_Loaded(object sender, RoutedEventArgs e)
        {
            ColorPickerPanel.SelectedColor = DataContext.Color;
            ColorPickerPanel.LayoutUpdated += ColorPickerPanel_LayoutUpdated;
        }

        private void EQCard_Unloaded(object sender, RoutedEventArgs e)
        {
            ColorPickerPanel.LayoutUpdated -= ColorPickerPanel_LayoutUpdated;
            Loaded -= EQCard_Loaded;
            Unloaded -= EQCard_Unloaded;
            DataContextChanged -= SongHistoryCard_DataContextChanged;
        }

        private void ColorPickerPanel_LayoutUpdated(object sender, object e)
        {
            if (DataContext is null) return;
            DataContext.Color = ColorPickerPanel.SelectedColor;
            (ColoredBackground.Fill as SolidColorBrush).Color = ColorPickerPanel.SelectedColor;
        }

        private void SongHistoryCard_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is null) return;
            TypeCombo.SelectedIndex = (int)DataContext.PassFilterType;
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

        private void Grid_Holding(object sender, Microsoft.UI.Xaml.Input.HoldingRoutedEventArgs e)
        {

        }

        private void Grid_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            Menu.ShowAt(sender as FrameworkElement, new() { Position = e.GetPosition(sender as UIElement), Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Auto });
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            AudioFilterStatic.PassFilterDatas.Remove(DataContext);
            App.audioPlayer.UpdateEqualizer();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Menu.ShowAt(sender as FrameworkElement);
        }

        private void TypeCombo_Loaded(object sender, RoutedEventArgs e)
        {
            TypeCombo.ItemsSource = Enum.GetValues(typeof(PassFilterZHType));
        }

        private void TypeCombo_Unloaded(object sender, RoutedEventArgs e)
        {
            TypeCombo.ItemsSource = null;
        }

        private void TypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TypeCombo.SelectedItem == null) return;
            DataContext.PassFilterType = (PassFilterType)TypeCombo.SelectedItem;
            if (DataContext.PassFilterType is PassFilterType.LowShelf or PassFilterType.HighShelf)
            {
                dbGainRoot.Visibility = Visibility.Visible;
            }
            else
            {
                dbGainRoot.Visibility = Visibility.Collapsed;
            }
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                NumberBox numberBox = new NumberBox()
                {
                    Value = btn.Tag switch
                    {
                        "Q" => DataContext.Q / 100f,
                        "Frequency" => DataContext.Frequency,
                        "dbGain" => DataContext.Decibels / 10f
                    },
                    SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                };
                var result = await MainWindow.ShowDialog($"设置 \"{btn.Tag}\" 值", numberBox, "取消", "确定", defaultButton: ContentDialogButton.Primary);
                if (result != ContentDialogResult.Primary) return;
                switch (btn.Tag)
                {
                    case "Q":
                        DataContext.Q = (float)numberBox.Value * 100f;
                        break;
                    case "Frequency":
                        DataContext.Frequency = (float)numberBox.Value;
                        break;
                    case "dbGain":
                        DataContext.Decibels = (float)numberBox.Value * 10f;
                        break;
                }
                var dataContextTemp = DataContext;
                DataContext = null;
                DataContext = dataContextTemp;
                dataContextTemp = null;
            }
        }
    }

    public partial class PassFilterQValueConverter : Microsoft.UI.Xaml.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double)
            {
                double dValue = System.Convert.ToDouble(value) / 100;
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
