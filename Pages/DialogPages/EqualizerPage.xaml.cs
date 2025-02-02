using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace TewiMP.Pages.DialogPages
{
    public partial class EqualizerPage : Page
    {
        public Media.AudioPlayer AudioPlayer => App.audioPlayer;
        public List<Slider> EqSliders { get; set; } = new();

        public bool WasapiOnly
        {
            get
            {
                if (AudioPlayer != null)
                {
                    return AudioPlayer.WasapiOnly;
                }
                else
                {
                    return (bool)DataEditor.DataFolderBase.JSettingData[DataEditor.DataFolderBase.SettingParams.WasapiOnly.ToString()];
                }
            }
            set
            {
                AudioPlayer.WasapiOnly = value;

            }
        }

        public int Latency
        {
            get => AudioPlayer.Latency;
            set
            {
                AudioPlayer.Latency = value;
            }
        }

        public double Pitch
        {
            get => AudioPlayer.Pitch * 10;
            set
            {
                AudioPlayer.Pitch = value / 10;
                aSlider.Header = $"音高：{value / 10}x";
            }
        }

        public double Tempo
        {
            get => AudioPlayer.Tempo * 10;
            set
            {
                AudioPlayer.Tempo = value / 10;
                bSlider.Header = $"速度：{value / 10}x";
            }
        }

        public double Rate
        {
            get => AudioPlayer.Rate * 10;
            set
            {
                AudioPlayer.Rate = value / 10;
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
            AudioPlayer.SourceChanged += AudioPlayer_SourceChanged;
            AudioPlayer.EqEnableChanged += AudioPlayer_EqEnableChanged;
            AudioPlayer.EqBandChanged += AudioPlayer_EqualizerBandChanged;
            AudioPlayer.PreviewSourceChanged += AudioPlayer_PreviewSourceChanged;
            AudioPlayer_SourceChanged(AudioPlayer);
            AudioPlayer_EqEnableChanged(AudioPlayer);
            AudioPlayer_EqualizerBandChanged(AudioPlayer);
            AudioPlayer_PreviewSourceChanged(AudioPlayer);
            aSlider.Header = $"音高：{Pitch / 10}x";
            bSlider.Header = $"速度：{Tempo / 10}x";
            cSlider.Header = $"比率：{Rate / 10}x";
        }

        private void EqualizerPage_Unloaded(object sender, RoutedEventArgs e)
        {
            //if (!IsLoaded || AudioPlayer is null) return;
            foreach (StackPanel slider in SliderStackBase.Children.Cast<StackPanel>())
            {
                var a = slider.Children[0] as Slider;
                a.ValueChanged -= A_ValueChanged;
            }
            EqSliders.Clear();

            DataContext = null;
            AudioPlayer.SourceChanged -= AudioPlayer_SourceChanged;
            AudioPlayer.EqEnableChanged -= AudioPlayer_EqEnableChanged;
            AudioPlayer.EqBandChanged -= AudioPlayer_EqualizerBandChanged;
            AudioPlayer.PreviewSourceChanged -= AudioPlayer_PreviewSourceChanged;
        }

        private void AudioPlayer_EqEnableChanged(Media.AudioPlayer audioPlayer)
        {
            EqEnableTS.IsOn = audioPlayer.EqEnabled;
        }

        private void AudioPlayer_SourceChanged(Media.AudioPlayer audioPlayer)
        {
            OutDevicesTextBlock.Text = audioPlayer.NowOutDevice.ToString();
        }

        bool inComboxChange = false;
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (inChange) return;

            inComboxChange = true;
            var a = sender as ComboBox;
            foreach (var b in Media.AudioEqualizerBands.BandNames)
            {
                if (b.Item2 == (a.SelectedItem as string))
                {
                    AudioPlayer.EqualizerBand = Media.AudioEqualizerBands.GetBandFromString(b.Item1);
                    AudioPlayer.NameOfBand = b.Item1;
                    AudioPlayer.NameOfBandCH = b.Item2;
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
        private void AudioPlayer_EqualizerBandChanged(Media.AudioPlayer audioPlayer)
        {
            if (!inChange)
            {
                inChange = true;
                for (int f = 0; f < audioPlayer.EqualizerBand.Count; f++)
                {
                    EqSliders[f].Value = audioPlayer.EqualizerBand[f][2] * 10;
                }
                if (!inComboxChange)
                    EqComboBox.SelectedItem = Media.AudioEqualizerBands.NameGetCHName(Media.AudioEqualizerBands.GetNameFromBands(audioPlayer.EqualizerBand));
                inChange = false;
            }
        }

        private void AudioPlayer_PreviewSourceChanged(Media.AudioPlayer audioPlayer)
        {
            if (audioPlayer.WasapiOnly && audioPlayer.NowOutDevice.DeviceType == Media.AudioPlayer.OutApi.Wasapi)
            {
                LatencyNumberBox.Minimum = 0;
                LatencyNumberBox.Maximum = 981;
            }
            else
            {
                LatencyNumberBox.Minimum = 50;
                LatencyNumberBox.Maximum = 1000;
            }

            WaveInfoTB.Text = audioPlayer.WaveInfo;
        }

        private void A_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (!inChange)
            {
                var a = sender as Slider;

                EqComboBox.SelectedItem = "自定义";
                Media.AudioEqualizerBands.CustomBands[int.Parse(a.Name.Remove(0, 2))][2] = (float)a.Value / 10;
                AudioPlayer.EqualizerBand = Media.AudioEqualizerBands.CustomBands;
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
            foreach (var a in Media.AudioEqualizerBands.CustomBands)
            {
                a[2] = 0;
            }
            AudioPlayer.EqualizerBand = Media.AudioEqualizerBands.CustomBands;
        }

        private void OutDevicesDropDownButton_Click(object sender, RoutedEventArgs e)
        {
            AddOutDeviceToFlyOut();
        }

        private void C_Click(object sender, RoutedEventArgs e)
        {
            var a = (Media.OutDevice)(sender as MenuFlyoutItem).Tag;
            AudioPlayer.NowOutDevice = a;
            OutDevicesTextBlock.Text = AudioPlayer.NowOutDevice.ToString();

            AudioPlayer.SetReloadAsync();
        }

        private async void AddOutDeviceToFlyOut()
        {
            var a = await Media.OutDevice.GetOutDevicesAsync();
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
            await App.audioPlayer.Reload();
            (sender as Button).IsEnabled = true;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            AudioPlayer_SourceChanged(App.audioPlayer);
        }

        private void EqEnableTS_Toggled(object sender, RoutedEventArgs e)
        {
            AudioPlayer.EqEnabled = EqEnableTS.IsOn;
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