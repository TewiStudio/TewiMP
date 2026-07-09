using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TewiMP.Core;
using TewiMP.Services.Media.Audio;
using TewiMP.Services.Media.Audio.AudioEffects;
using TewiMP.Services.Storage;

namespace TewiMP.UI.Pages.DialogPages
{
    public partial class EqualizerPage : Page
    {
        public AudioService AudioService => App.Instance.AudioService;
        public List<Slider> EqSliders { get; set; } = new();

        public bool WasapiOnly
        {
            get
            {
                if (AudioService != null)
                {
                    return AudioService.WasapiOnly;
                }
                else
                {
                    return (bool)DataFolderBase.JSettingData[DataFolderBase.SettingParams.WasapiOnly.ToString()];
                }
            }
            set
            {
                AudioService.WasapiOnly = value;

            }
        }

        public int Latency
        {
            get => AudioService.Latency;
            set
            {
                AudioService.Latency = value;
            }
        }

        public double Pitch
        {
            get => AudioService.Pitch * 10;
            set
            {
                AudioService.Pitch = value / 10;
                aSlider.Header = $"音高：{value / 10}x";
            }
        }

        public double Tempo
        {
            get => AudioService.Tempo * 10;
            set
            {
                AudioService.Tempo = value / 10;
                bSlider.Header = $"速度：{value / 10}x";
            }
        }

        public double Rate
        {
            get => AudioService.Rate * 10;
            set
            {
                AudioService.Rate = value / 10;
                cSlider.Header = $"比率：{value / 10}x";
            }
        }

        public EqualizerPage()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += EqualizerPage_Loaded;
            Unloaded += EqualizerPage_Unloaded;
        }

        private void EqualizerPage_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (StackPanel slider in SliderStackBase.Children.Cast<StackPanel>())
            {
                var a = slider.Children[0] as Slider;
                EqSliders.Add(a);
                a.ValueChanged += A_ValueChanged;
            }
            AudioService.SourceChanged += AudioService_SourceChanged;
            AudioService.EqEnableChanged += AudioService_EqEnableChanged;
            AudioService.EqBandChanged += AudioService_EqualizerBandChanged;
            AudioService.PreviewSourceChanged += AudioService_PreviewSourceChanged;
            AudioService_SourceChanged(AudioService);
            AudioService_EqEnableChanged(AudioService);
            AudioService_EqualizerBandChanged(AudioService);
            AudioService_PreviewSourceChanged(AudioService);
            aSlider.Header = $"音高：{Pitch / 10}x";
            bSlider.Header = $"速度：{Tempo / 10}x";
            cSlider.Header = $"比率：{Rate / 10}x";
        }

        private void EqualizerPage_Unloaded(object sender, RoutedEventArgs e)
        {
            //if (!IsLoaded || AudioService is null) return;
            foreach (StackPanel slider in SliderStackBase.Children.Cast<StackPanel>())
            {
                var a = slider.Children[0] as Slider;
                a.ValueChanged -= A_ValueChanged;
            }
            EqSliders.Clear();

            DataContext = null;
            AudioService.SourceChanged -= AudioService_SourceChanged;
            AudioService.EqEnableChanged -= AudioService_EqEnableChanged;
            AudioService.EqBandChanged -= AudioService_EqualizerBandChanged;
            AudioService.PreviewSourceChanged -= AudioService_PreviewSourceChanged;
        }

        private void AudioService_EqEnableChanged(AudioService AudioService)
        {
            EqEnableTS.IsOn = AudioService.EqEnabled;
        }

        private void AudioService_SourceChanged(AudioService AudioService)
        {
            OutDevicesTextBlock.Text = AudioService.NowOutDevice.ToString();
        }

        bool inComboxChange = false;
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (inChange) return;

            inComboxChange = true;
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
                ResetButton.Visibility = Visibility.Visible;
            }
            else
            {
                ResetButton.Visibility = Visibility.Collapsed;
            }
            inComboxChange = false;
        }

        bool inChange = false;
        private void AudioService_EqualizerBandChanged(AudioService AudioService)
        {
            if (!inChange)
            {
                inChange = true;
                for (int f = 0; f < AudioService.EqualizerBand.Count; f++)
                {
                    EqSliders[f].Value = AudioService.EqualizerBand[f][2] * 10;
                }
                if (!inComboxChange)
                    EqComboBox.SelectedItem = AudioEqualizerBands.NameGetCHName(AudioEqualizerBands.GetNameFromBands(AudioService.EqualizerBand));
                inChange = false;
            }
        }

        private void AudioService_PreviewSourceChanged(AudioService AudioService)
        {
            if (AudioService.WasapiOnly && AudioService.NowOutDevice.DeviceType == OutApi.Wasapi)
            {
                LatencyNumberBox.Minimum = 0;
                LatencyNumberBox.Maximum = 981;
            }
            else
            {
                LatencyNumberBox.Minimum = 50;
                LatencyNumberBox.Maximum = 1000;
            }

            WaveInfoTB.Text = AudioService.WaveInfo;
        }

        private void A_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!inChange)
            {
                var a = sender as Slider;

                EqComboBox.SelectedItem = "自定义";
                AudioEqualizerBands.CustomBands[int.Parse(a.Name.Remove(0, 2))][2] = (float)a.Value / 10;
                AudioService.EqualizerBand = AudioEqualizerBands.CustomBands;
            }
        }

        private void EqComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            AddOutDeviceToFlyOut();

            if ((EqComboBox.SelectedItem as string) == "自定义")
            {
                ResetButton.Visibility = Visibility.Visible;
            }
            else
            {
                ResetButton.Visibility = Visibility.Collapsed;
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var a in AudioEqualizerBands.CustomBands)
            {
                a[2] = 0;
            }
            AudioService.EqualizerBand = AudioEqualizerBands.CustomBands;
        }

        private void OutDevicesDropDownButton_Click(object sender, RoutedEventArgs e)
        {
            AddOutDeviceToFlyOut();
        }

        private void C_Click(object sender, RoutedEventArgs e)
        {
            var a = (OutDevice)(sender as MenuFlyoutItem).Tag;
            AudioService.NowOutDevice = a;
            OutDevicesTextBlock.Text = AudioService.NowOutDevice.ToString();

            AudioService.SetReloadAsync();
        }

        private async void AddOutDeviceToFlyOut()
        {
            var a = await OutDevice.GetOutDevicesAsync();
            OutDevicesFlyout.Items.Clear();
            foreach (var b in a)
            {
                var c = new MenuFlyoutItem() { Text = b.ToString(), Tag = b };
                c.Click += C_Click;
                OutDevicesFlyout.Items.Add(c);
            }
        }

        private async void ReloadAudio_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            await App.Instance.AudioService.Reload();
            (sender as Button).IsEnabled = true;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            AudioService_SourceChanged(App.Instance.AudioService);
        }

        private void EqEnableTS_Toggled(object sender, RoutedEventArgs e)
        {
            AudioService.EqEnabled = EqEnableTS.IsOn;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            App.MainWindowInstance.HideDialog();
            if (App.MainWindowInstance.IsMusicPageOpened) App.MainWindowInstance.OpenOrCloseMusicPage();
            App.MainWindowInstance.SetNavViewContent(typeof(SettingEqPage));
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
}