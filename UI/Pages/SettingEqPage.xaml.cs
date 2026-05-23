using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using System;
using System.Numerics;
using System.Threading.Tasks;
using TewiMP.UI.Controls;
using TewiMP.Services.Media.Audio;
using TewiMP.Services.Media.Audio.AudioEffects;
using Windows.UI;
using TewiMP.Core;

namespace TewiMP.UI.Pages
{
    public partial class SettingEqPage : Page
    {
        public AudioService AudioService => App.Instance.AudioService;

        public static readonly DependencyProperty EqEnabledProperty = DependencyProperty.Register(
            "EqEnabled", typeof(bool), typeof(AudioSpectrum),
            new PropertyMetadata(false, new((_, __) =>
            {
                App.Instance.AudioService.EqEnabled = (bool)__.NewValue;
            })));

        public bool EqEnabled
        {
            get
            {
                return (bool)GetValue(EqEnabledProperty);
            }
            set
            {
                SetValue(EqEnabledProperty, value);
            }
        }

        public SettingEqPage()
        {
            InitializeComponent();
            DataContext = this;
        }

        void Init()
        {
        }

        private async void AddOutDeviceToFlyOut()
        {/*
            var a = await OutDevice.GetOutDevicesAsync();
            OutDevicesFlyout.Items.Clear();
            foreach (var b in a)
            {
                var c = new MenuFlyoutItem() { Text = b.ToString(), Tag = b };
                c.Click += C_Click;
                c.Unloaded += C_Unloaded;
                OutDevicesFlyout.Items.Add(c);
            }*/
        }

        private void AudioService_EqEnableChanged(AudioService AudioService)
        {
            EqEnableSwitcher.IsOn = AudioService.EqEnabled;
        }

        bool inEqBandChange = false;
        private void AudioService_EqualizerBandChanged(AudioService AudioService)
        {
            if (!inEqBandChange)
            {
                inEqBandChange = true;
                for (int f = 0; f < AudioService.EqualizerBand.Count; f++)
                {
                    ((SliderStackBase.Children[f] as StackPanel).Children[0] as Slider).Value = AudioService.EqualizerBand[f][2] * 10;
                }
                if (!inComboChange)
                    GraphicEqComboBox.SelectedItem = AudioEqualizerBands.NameGetCHName(AudioEqualizerBands.GetNameFromBands(AudioService.EqualizerBand));
                inEqBandChange = false;
            }
        }

        bool isInLoaded = false;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            isInLoaded = true;
            GraphicEqToggleButton.IsOn = AudioFilterStatic.GraphicEqEnable;
            ParametricToggleButton.IsOn = AudioFilterStatic.ParametricEqEnable;
            PassFilterToggleButton.IsOn = AudioFilterStatic.PassFilterEqEnable;
            isInLoaded = false;

            AudioService.EqEnableChanged -= AudioService_EqEnableChanged;
            AudioService.EqEnableChanged += AudioService_EqEnableChanged;
            AudioService.EqBandChanged -= AudioService_EqualizerBandChanged;
            AudioService.EqBandChanged += AudioService_EqualizerBandChanged;
            App.MainWindowInstance.MainViewStateChanged -= MainWindowInstance_MainViewStateChanged;
            App.MainWindowInstance.MainViewStateChanged += MainWindowInstance_MainViewStateChanged;

            EQList.ItemsSource = AudioFilterStatic.ParametricEqDatas;
            PassFilterList.ItemsSource = AudioFilterStatic.PassFilterDatas;

            AddOutDeviceToFlyOut();
            AudioService_EqEnableChanged(AudioService);
            AudioService_EqualizerBandChanged(AudioService);

            Init();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            AudioService.EqEnableChanged -= AudioService_EqEnableChanged;
            AudioService.EqBandChanged -= AudioService_EqualizerBandChanged;
            App.MainWindowInstance.MainViewStateChanged -= MainWindowInstance_MainViewStateChanged;
            EQList.ItemsSource = null;
            PassFilterList.ItemsSource = null;
            //if (!App.IsExited) App.Instance.SaveSettings();
        }

        private void MainWindowInstance_MainViewStateChanged(bool isView)
        {
            spectrumCanvas.IsStop = !isView;
            AudioSpectrum.IsStop = !isView;
        }

        private void C_Click(object sender, RoutedEventArgs e)
        {
            var a = (OutDevice)(sender as MenuFlyoutItem).Tag;
            AudioService.NowOutDevice = a;
            //OutDevicesTextBlock.Text = AudioService.NowOutDevice.ToString();
            AudioService.SetReloadAsync();
        }

        private void C_Unloaded(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem c)
            {
                c.Click -= C_Click;
                c.Unloaded -= C_Unloaded;
            }
        }

        private void EqEnableSwitcher_Toggled(object sender, RoutedEventArgs e)
        {
            AudioService.EqEnabled = EqEnableSwitcher.IsOn;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Random r = new();
            AudioFilterStatic.ParametricEqDatas.Add(new()
            {
                CentreFrequency = 31,
                Q = 1,
                Gain = 0,
                Channel = 1,
                IsEnable = true,
                Color = Color.FromArgb(255, (byte)r.Next(0, 255), (byte)r.Next(0, 255), (byte)r.Next(0, 255))
            });
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Random r = new();
            AudioFilterStatic.PassFilterDatas.Add(new()
            {
                CentreFrequency = 2048,
                Q = 1,
                Channel = 1,
                IsEnable = true,
                Color = Color.FromArgb(255, (byte)r.Next(0, 255), (byte)r.Next(0, 255), (byte)r.Next(0, 255))
            });
        }

        private void GraphicEqComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if ((GraphicEqComboBox.SelectedItem as string) == "自定义")
            {
                GraphicResetButton.Visibility = Visibility.Visible;
            }
            else
            {
                GraphicResetButton.Visibility = Visibility.Collapsed;
            }
        }

        bool inComboChange = false;
        private void GraphicEqComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (inEqBandChange) return;

            inComboChange = true;
            var a = sender as ComboBox;
            foreach (var b in AudioEqualizerBands.BandNames)
            {
                if (b.Item2 == (a.SelectedItem as string))
                {
                    AudioService.EqualizerBand = AudioEqualizerBands.GetBandFromString(b.Item1);
                    AudioService.NameOfBand = b.Item1;
                    AudioService.NameOfBandCH = b.Item2;
                    break;
                }
            }
            if ((a.SelectedItem as string) == "自定义")
            {
                GraphicResetButton.Visibility = Visibility.Visible;
            }
            else
            {
                GraphicResetButton.Visibility = Visibility.Collapsed;
            }
            inComboChange = false;
        }

        private void GraphicEQSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!inEqBandChange)
            {
                var a = sender as Slider;

                GraphicEqComboBox.SelectedItem = "自定义";
                AudioEqualizerBands.CustomBands[int.Parse(a.Name.Remove(0, 2))][2] = (float)a.Value / 10;
                AudioService.EqualizerBand = AudioEqualizerBands.CustomBands;
            }
        }

        private void GraphicResetButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var a in AudioEqualizerBands.CustomBands)
            {
                a[2] = 0;
            }
            AudioService.EqualizerBand = AudioEqualizerBands.CustomBands;
        }

        private void EqComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ResetEButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void EqComboBox_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void PassFilterToggleButton_Toggled(object sender, RoutedEventArgs e)
        {
            if (isInLoaded) return;
            AudioFilterStatic.PassFilterEqEnable = PassFilterToggleButton.IsOn;
            App.Instance.AudioService.UpdateEqualizer();
        }

        private void GraphicEqToggleButton_Toggled(object sender, RoutedEventArgs e)
        {
            if (isInLoaded) return;
            AudioFilterStatic.GraphicEqEnable = GraphicEqToggleButton.IsOn;
            App.Instance.AudioService.UpdateEqualizer();
        }

        private void ParametricToggleButton_Toggled(object sender, RoutedEventArgs e)
        {
            if (isInLoaded) return;
            AudioFilterStatic.ParametricEqEnable = ParametricToggleButton.IsOn;
            App.Instance.AudioService.UpdateEqualizer();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            AudioSpectrumRoot.Visibility = AudioSpectrumRoot.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    public partial class ThumbToolTipValueConverter : Microsoft.UI.Xaml.Data.IValueConverter
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

    public partial class EqIconOpacityValueConverter : Microsoft.UI.Xaml.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool v)
            {
                return v ? 1 : .5f;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
}
