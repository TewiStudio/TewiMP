using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using TewiMP.Core.Models.Audio;
using TewiMP.Media.Audio;
using TewiMP.Media.Audio.AudioEffects;

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
        }

        private void UpdateData()
        {
            if (DataContext is null) return;
            inChange = true;
            QSilder.Value = DataContext.Q;
            FreSilder.Value = DataContext.CentreFrequency;
            gainSilder.Value = DataContext.Gain;
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
            if (DataContext is null) return;
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
            AudioFilterStatic.ParametricEqDatas.Remove(DataContext);
            App.Instance.AudioPlayer.UpdateEqualizer();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Menu.ShowAt(sender as FrameworkElement);
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
                        _ => -1
                    },
                    SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                };
                string chineseName = btn.Tag switch
                {
                    "Quality" => "质量",
                    "Frequency" => "中心频率",
                    "Gain" => "增益",
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
                var result = await App.MainWindowInstance.ShowDialog($"设置 \"{chineseName} {btn.Tag}{(string.IsNullOrEmpty(unit) ? "" : $"（{unit}）")}\" 值", numberBox, "取消", "确定", defaultButton: ContentDialogButton.Primary);
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
