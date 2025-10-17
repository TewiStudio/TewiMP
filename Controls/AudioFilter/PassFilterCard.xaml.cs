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
        }

        private void UpdateData()
        {
            if (DataContext is null) return;
            inChange = true;
            QSilder.Value = DataContext.Q;
            FreSilder.Value = DataContext.CentreFrequency;
            gainSilder.Value = DataContext.Gain;
            slopeSilder.Value = DataContext.SlopeDbPerOct;
            TypeCombo.SelectedIndex = (int)DataContext.PassFilterType;
            inChange = false;
        }

        bool inChange = false;
        private void EQCard_Loaded(object sender, RoutedEventArgs e)
        {
            App.Instance.AudioPlayer.EqBandChanged -= AudioPlayer_EqBandChanged;
            App.Instance.AudioPlayer.EqBandChanged += AudioPlayer_EqBandChanged;
            ColorPickerPanel.SelectedColor = DataContext.Color;
            UpdateData();
        }

        private void EQCard_Unloaded(object sender, RoutedEventArgs e)
        {
            if (IsLoaded) return;
            App.Instance.AudioPlayer.EqBandChanged -= AudioPlayer_EqBandChanged;
        }

        private void AudioPlayer_EqBandChanged(AudioPlayer audioPlayer)
        {
            if (isUserChange) return;
            UpdateData();
        }

        private void ColorPickerPanel_LayoutUpdated(object sender, object e)
        {
            if (DataContext is null || !IsLoaded) return;
            DataContext.Color = ColorPickerPanel.SelectedColor;
            (ColoredBackground.Fill as SolidColorBrush).Color = ColorPickerPanel.SelectedColor;
        }

        private void SongHistoryCard_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            UpdateData();
        }

        bool isUserChange = false;
        private void Silder_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (inChange || DataContext is null) return;
            isUserChange = true;
            DataContext.Q = (float)QSilder.Value;
            DataContext.CentreFrequency = (float)FreSilder.Value;
            DataContext.Gain = (float)gainSilder.Value;
            DataContext.SlopeDbPerOct = (int)slopeSilder.Value;
            isUserChange = false;
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
            App.Instance.AudioPlayer.UpdateEqualizer();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Menu.ShowAt(sender as FrameworkElement);
        }

        private void TypeCombo_Loaded(object sender, RoutedEventArgs e)
        {
            TypeCombo.ItemsSource = Enum.GetValues(typeof(PassFilterZHType));
            TypeCombo.SelectedIndex = (int)DataContext.PassFilterType;
        }

        private void TypeCombo_Unloaded(object sender, RoutedEventArgs e)
        {
            // 会导致第二时间加载时不显示内容
            //TypeCombo.ItemsSource = null;
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
            if (DataContext.PassFilterType is PassFilterType.LowPass or PassFilterType.HighPass)
            {
                DataContext.Q = 1;
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
                        "Quality" => DataContext.Q,
                        "Frequency" => DataContext.CentreFrequency,
                        "Gain" => DataContext.Gain,
                        "Slope" => DataContext.SlopeDbPerOct,
                        _ => -1
                    },
                    SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                };
                string chineseName = btn.Tag switch
                {
                    "Quality" => "质量",
                    "Frequency" => "中心频率",
                    "Gain" => "增益",
                    "Slope" => "斜率",
                    _ => "未知"
                };
                string unit = btn.Tag switch
                {
                    "Quality" => "",
                    "Frequency" => "Hz",
                    "Gain" => "dB",
                    "Slope" => "dB/Oct",
                    _ => ""
                };
                var result = await App.MainWindowInstance.ShowDialog($"设置 \"{chineseName} {btn.Tag}{(string.IsNullOrEmpty(unit) ? "" : $" ({unit})")}\" 值", numberBox, "取消", "确定", defaultButton: ContentDialogButton.Primary);
                if (result != ContentDialogResult.Primary) return;
                switch (btn.Tag)
                {
                    case "Quality":
                        DataContext.Q = (float)numberBox.Value;
                        QSilder.Value = DataContext.Q;
                        break;
                    case "Frequency":
                        DataContext.CentreFrequency = (float)numberBox.Value;
                        FreSilder.Value = DataContext.CentreFrequency;
                        break;
                    case "Gain":
                        DataContext.Gain = (float)numberBox.Value;
                        gainSilder.Value = DataContext.Gain;
                        break;
                    case "Slope":
                        DataContext.SlopeDbPerOct = (int)numberBox.Value;
                        gainSilder.Value = DataContext.SlopeDbPerOct;
                        break;
                }
            }
        }

        private void Grid_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            MoveIcon.Opacity = .6;
        }

        private void Grid_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            MoveIcon.Opacity = 0;
        }
    }
}
