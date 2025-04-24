using System;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Composition;
using TewiMP.Media;

namespace TewiMP.Pages
{
    public partial class SettingEqPage : Page
    {
        public AudioPlayer AudioPlayer => App.audioPlayer;
        public bool EqEnabled
        {
            get
            {
                return App.audioPlayer.EqEnabled;
            }
            set
            {
                App.audioPlayer.EqEnabled = value;
            }
        }

        public SettingEqPage()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void UpdateShyHeader()
        {
            // 设置header为顶层
            var headerPresenter = (UIElement)VisualTreeHelper.GetParent((UIElement)ListViewBase.Header);
            var headerContainer = (UIElement)VisualTreeHelper.GetParent(headerPresenter);
            Canvas.SetZIndex(headerContainer, 1);

            var scrollViewer = (VisualTreeHelper.GetChild(ListViewBase, 0) as Border).Child as ScrollViewer;
            scrollViewer.CanContentRenderOutsideBounds = true;

            CompositionPropertySet scrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
            Compositor compositor = scrollerPropertySet.Compositor;

            var padingSize = 40;
            // Get the visual that represents our HeaderTextBlock 
            // And define the progress animation string
            var headerVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseGrid);
            String progress = $"Clamp(-scroller.Translation.Y / {padingSize}, 0, 1.0)";

            // Shift the header by 50 pixels when scrolling down
            var offsetExpression = compositor.CreateExpressionAnimation($"-scroller.Translation.Y - {progress} * {padingSize}");
            offsetExpression.SetReferenceParameter("scroller", scrollerPropertySet);
            headerVisual.StartAnimation("Offset.Y", offsetExpression);

            /*
            Visual textVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseTextBlock);
            Vector3 finalOffset = new Vector3(0, 10, 0);
            var headerOffsetAnimation = compositor.CreateExpressionAnimation($"Lerp(Vector3(0,0,0), finalOffset, {progress})");
            headerOffsetAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            headerOffsetAnimation.SetVector3Parameter("finalOffset", finalOffset);
            textVisual.StartAnimation(nameof(Visual.Offset), headerOffsetAnimation);
            */

            // Logo scale and transform                                          from               to
            var logoHeaderScaleAnimation = compositor.CreateExpressionAnimation("Lerp(Vector2(1,1), Vector2(0.7, 0.7), " + progress + ")");
            logoHeaderScaleAnimation.SetReferenceParameter("scroller", scrollerPropertySet);

            var logoVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseTextBlock);
            logoVisual.StartAnimation("Scale.xy", logoHeaderScaleAnimation);

            var logoVisualOffsetYAnimation = compositor.CreateExpressionAnimation($"Lerp(0, 24, {progress})");
            logoVisualOffsetYAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            logoVisual.StartAnimation("Offset.Y", logoVisualOffsetYAnimation);

            var logoVisualOffsetXAnimation = compositor.CreateExpressionAnimation($"Lerp(0, -12, {progress})");
            logoVisualOffsetXAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            logoVisual.StartAnimation("Offset.X", logoVisualOffsetXAnimation);

            var backgroundVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseRectangle);
            var backgroundVisualOpacityAnimation = compositor.CreateExpressionAnimation($"Lerp(0, 1, {progress})");
            backgroundVisualOpacityAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            backgroundVisual.StartAnimation("Opacity", backgroundVisualOpacityAnimation);
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

        private void AudioPlayer_EqEnableChanged(AudioPlayer audioPlayer)
        {
            EqEnableSwitcher.IsOn = audioPlayer.EqEnabled;
        }

        bool inEqBandChange = false;
        private void AudioPlayer_EqualizerBandChanged(AudioPlayer audioPlayer)
        {
            if (!inEqBandChange)
            {
                inEqBandChange = true;
                for (int f = 0; f < audioPlayer.EqualizerBand.Count; f++)
                {
                    ((SliderStackBase.Children[f] as StackPanel).Children[0] as Slider).Value = audioPlayer.EqualizerBand[f][2] * 10;
                }
                if (!inComboChange)
                    GraphicEqComboBox.SelectedItem = AudioEqualizerBands.NameGetCHName(AudioEqualizerBands.GetNameFromBands(audioPlayer.EqualizerBand));
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

            AudioPlayer.EqEnableChanged += AudioPlayer_EqEnableChanged;
            AudioPlayer.EqBandChanged += AudioPlayer_EqualizerBandChanged;
            EQList.ItemsSource = AudioFilterStatic.ParametricEqDatas;
            PassFilterList.ItemsSource = AudioFilterStatic.PassFilterDatas;

            AddOutDeviceToFlyOut();
            AudioPlayer_EqEnableChanged(AudioPlayer);
            AudioPlayer_EqualizerBandChanged(AudioPlayer);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            AudioPlayer.EqEnableChanged -= AudioPlayer_EqEnableChanged;
            AudioPlayer.EqBandChanged -= AudioPlayer_EqualizerBandChanged;
            EQList.ItemsSource = null;
            PassFilterList.ItemsSource = null;
            App.SaveSettings();
        }

        private void C_Click(object sender, RoutedEventArgs e)
        {
            var a = (OutDevice)(sender as MenuFlyoutItem).Tag;
            AudioPlayer.NowOutDevice = a;
            //OutDevicesTextBlock.Text = AudioPlayer.NowOutDevice.ToString();
            AudioPlayer.SetReloadAsync();
        }

        private void C_Unloaded(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem c)
            {
                c.Click -= C_Click;
                c.Unloaded -= C_Unloaded;
            }
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateShyHeader();
        }

        private void EqEnableSwitcher_Toggled(object sender, RoutedEventArgs e)
        {
            AudioPlayer.EqEnabled = EqEnableSwitcher.IsOn;
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
                    AudioPlayer.EqualizerBand = AudioEqualizerBands.GetBandFromString(b.Item1);
                    AudioPlayer.NameOfBand = b.Item1;
                    AudioPlayer.NameOfBandCH = b.Item2;
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
                AudioPlayer.EqualizerBand = AudioEqualizerBands.CustomBands;
            }
        }

        private void GraphicResetButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var a in AudioEqualizerBands.CustomBands)
            {
                a[2] = 0;
            }
            AudioPlayer.EqualizerBand = AudioEqualizerBands.CustomBands;
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
            App.audioPlayer.UpdateEqualizer();
        }

        private void GraphicEqToggleButton_Toggled(object sender, RoutedEventArgs e)
        {
            if (isInLoaded) return;
            AudioFilterStatic.GraphicEqEnable = GraphicEqToggleButton.IsOn;
            App.audioPlayer.UpdateEqualizer();
        }

        private void ParametricToggleButton_Toggled(object sender, RoutedEventArgs e)
        {
            if (isInLoaded) return;
            AudioFilterStatic.ParametricEqEnable = ParametricToggleButton.IsOn;
            App.audioPlayer.UpdateEqualizer();
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
