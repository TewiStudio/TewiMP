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
    }
}
