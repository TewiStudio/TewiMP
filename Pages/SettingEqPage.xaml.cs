using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using System;
using System.Numerics;
using System.Threading.Tasks;
using TewiMP.Media;
using Windows.UI;

namespace TewiMP.Pages
{
    public partial class SettingEqPage : Page
    {
        public AudioPlayer AudioPlayer => App.Instance.AudioPlayer;
        public bool EqEnabled
        {
            get
            {
                return App.Instance.AudioPlayer.EqEnabled;
            }
            set
            {
                App.Instance.AudioPlayer.EqEnabled = value;
            }
        }

        public SettingEqPage()
        {
            InitializeComponent();
            DataContext = this;
        }

        void Init()
        {
            InitVisual();
            InitShyHeader();
        }

        ScrollViewer scrollViewer;
        CompositionPropertySet scrollerPropertySet;
        Compositor compositor;
        Visual headerVisual;
        Visual backgroundVisual;
        Visual logoVisual;
        Visual headerFootRootVisual;
        void InitVisual()
        {
            // 设置header为顶层
            var headerPresenter = (UIElement)VisualTreeHelper.GetParent((UIElement)ListViewBase.Header);
            var headerContainer = (UIElement)VisualTreeHelper.GetParent(headerPresenter);
            Canvas.SetZIndex(headerContainer, 1);

            scrollViewer = (VisualTreeHelper.GetChild(ListViewBase, 0) as Border).Child as ScrollViewer;
            scrollViewer.CanContentRenderOutsideBounds = true;
            scrollViewer.ViewChanging -= ScrollViewer_ViewChanging;
            scrollViewer.ViewChanging += ScrollViewer_ViewChanging;

            scrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
            compositor = scrollerPropertySet.Compositor;
            headerVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseGrid);
            logoVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseTextBlock);
            backgroundVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseRectangle);
            headerFootRootVisual = ElementCompositionPreview.GetElementVisual(HeaderFootRoot);
        }


        ExpressionAnimation offsetExpression;
        ExpressionAnimation logoHeaderScaleAnimation;
        ExpressionAnimation logoVisualOffsetYAnimation;
        ExpressionAnimation logoVisualOffsetXAnimation;
        ExpressionAnimation backgroundVisualOpacityAnimation;
        ExpressionAnimation headerFootRootVisualOffsetAnimation;
        void InitShyHeader()
        {
            if (!IsLoaded) return;
            if (scrollViewer is null) return;

            var paddingSize = 40;
            var progress = $"Clamp(-scroller.Translation.Y / {paddingSize}, 0, 1.0)";

            offsetExpression?.Dispose();
            offsetExpression = compositor.CreateExpressionAnimation($"-scroller.Translation.Y - Round({progress} * {paddingSize})");
            offsetExpression.SetReferenceParameter("scroller", scrollerPropertySet);
            headerVisual.StartAnimation("Offset.Y", offsetExpression);

            logoHeaderScaleAnimation?.Dispose();
            logoHeaderScaleAnimation = compositor.CreateExpressionAnimation("Lerp(Vector2(1,1), Vector2(0.7, 0.7), " + progress + ")");
            logoHeaderScaleAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            logoVisual.StartAnimation("Scale.xy", logoHeaderScaleAnimation);

            logoVisualOffsetYAnimation?.Dispose();
            logoVisualOffsetYAnimation = compositor.CreateExpressionAnimation($"Lerp(0, 24, {progress})");
            logoVisualOffsetYAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            logoVisual.StartAnimation("Offset.Y", logoVisualOffsetYAnimation);

            logoVisualOffsetXAnimation?.Dispose();
            logoVisualOffsetXAnimation = compositor.CreateExpressionAnimation($"Lerp(0, -12, {progress})");
            logoVisualOffsetXAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            logoVisual.StartAnimation("Offset.X", logoVisualOffsetXAnimation);

            backgroundVisualOpacityAnimation?.Dispose();
            backgroundVisualOpacityAnimation = compositor.CreateExpressionAnimation($"Lerp(0, 1, {progress})");
            backgroundVisualOpacityAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            backgroundVisual.StartAnimation("Opacity", backgroundVisualOpacityAnimation);
            /*
                        headerFootRootVisualOffsetAnimation?.Dispose();
                        headerFootRootVisualOffsetAnimation = compositor.CreateExpressionAnimation(
                            $"Lerp(" +
                                $"Vector3(" +
                                    $"-16," +
                                    $"{ActualHeight} - {headerFootRootVisual.Size.Y} - 8," +
                                    $"0)," +
                                $"Vector3(" +
                                    $"-16," +
                                    $"{paddingSize} + {ActualHeight} - {headerFootRootVisual.Size.Y} - 8," +
                                    $"0)," +
                                $"{progress})");
                        headerFootRootVisualOffsetAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
                        headerFootRootVisual.StartAnimation("Offset", headerFootRootVisualOffsetAnimation);*/
        }

        void DisposeVisuals()
        {
            offsetExpression?.Dispose();
            logoHeaderScaleAnimation?.Dispose();
            logoVisualOffsetYAnimation?.Dispose();
            logoVisualOffsetXAnimation?.Dispose();
            backgroundVisualOpacityAnimation?.Dispose();
            headerFootRootVisualOffsetAnimation?.Dispose();

            scrollViewer = null;
            scrollerPropertySet = null;
            compositor = null;
            headerVisual = null;
            backgroundVisual = null;
            logoVisual = null;
            headerFootRootVisual = null;
            offsetExpression = null;
            logoHeaderScaleAnimation = null;
            logoVisualOffsetYAnimation = null;
            logoVisualOffsetXAnimation = null;
            backgroundVisualOpacityAnimation = null;
            headerFootRootVisualOffsetAnimation = null;
        }

        private void ScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            headerVisual.IsPixelSnappingEnabled = true;
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
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            isInLoaded = true;
            GraphicEqToggleButton.IsOn = AudioFilterStatic.GraphicEqEnable;
            ParametricToggleButton.IsOn = AudioFilterStatic.ParametricEqEnable;
            PassFilterToggleButton.IsOn = AudioFilterStatic.PassFilterEqEnable;
            isInLoaded = false;

            AudioPlayer.EqEnableChanged -= AudioPlayer_EqEnableChanged;
            AudioPlayer.EqEnableChanged += AudioPlayer_EqEnableChanged;
            AudioPlayer.EqBandChanged -= AudioPlayer_EqualizerBandChanged;
            AudioPlayer.EqBandChanged += AudioPlayer_EqualizerBandChanged;

            EQList.ItemsSource = AudioFilterStatic.ParametricEqDatas;
            PassFilterList.ItemsSource = AudioFilterStatic.PassFilterDatas;

            AddOutDeviceToFlyOut();
            AudioPlayer_EqEnableChanged(AudioPlayer);
            AudioPlayer_EqualizerBandChanged(AudioPlayer);

            Init();
            await Task.Delay(10);
            InitShyHeader();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            DisposeVisuals();
            AudioPlayer.EqEnableChanged -= AudioPlayer_EqEnableChanged;
            AudioPlayer.EqBandChanged -= AudioPlayer_EqualizerBandChanged;
            EQList.ItemsSource = null;
            PassFilterList.ItemsSource = null;
            App.Instance.SaveSettings();
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
            InitShyHeader();
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
            App.Instance.AudioPlayer.UpdateEqualizer();
        }

        private void GraphicEqToggleButton_Toggled(object sender, RoutedEventArgs e)
        {
            if (isInLoaded) return;
            AudioFilterStatic.GraphicEqEnable = GraphicEqToggleButton.IsOn;
            App.Instance.AudioPlayer.UpdateEqualizer();
        }

        private void ParametricToggleButton_Toggled(object sender, RoutedEventArgs e)
        {
            if (isInLoaded) return;
            AudioFilterStatic.ParametricEqEnable = ParametricToggleButton.IsOn;
            App.Instance.AudioPlayer.UpdateEqualizer();
        }

        private float[] _smoothedSpectrum;
        private void eqCanvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedDrawEventArgs args)
        {
            var ds = args.DrawingSession;
            ds.Clear(Colors.Black);

            var analyzer = AudioPlayer.AudioAnalyzer;
            if (analyzer == null) return;
            analyzer.Analyze();
            if (analyzer.Spectrum == null) return;

            int fftSize = analyzer.Spectrum.Length * 2;
            int sampleRate = analyzer.WaveFormat.SampleRate;

            double minFreq = 20;
            double maxFreq = 20000;
            int barCount = 128; // 可自定义矩形条数

            int w = (int)sender.Size.Width;
            int h = (int)sender.Size.Height;
            float barWidth = w / (float)barCount;

            // 初始化平滑数组
            if (_smoothedSpectrum == null || _smoothedSpectrum.Length != analyzer.Spectrum.Length)
                _smoothedSpectrum = new float[analyzer.Spectrum.Length];

            float smoothingFactor = 0.2f; // 越小越平滑
            for (int i = 0; i < analyzer.Spectrum.Length; i++)
                _smoothedSpectrum[i] = _smoothedSpectrum[i] * (1 - smoothingFactor) + analyzer.Spectrum[i] * smoothingFactor;

            double logMin = Math.Log10(minFreq);
            double logMax = Math.Log10(maxFreq);

            for (int i = 0; i < barCount; i++)
            {
                // 对数均分
                double logStart = logMin + (logMax - logMin) * i / barCount;
                double logEnd = logMin + (logMax - logMin) * (i + 1) / barCount;

                double freqStart = Math.Pow(10, logStart);
                double freqEnd = Math.Pow(10, logEnd);

                // FFT bin 索引，保证至少覆盖一个 bin
                int binStart = Math.Max(0, (int)Math.Round(freqStart / (sampleRate / 2.0) * (analyzer.Spectrum.Length - 1)));
                int binEnd = Math.Min(analyzer.Spectrum.Length - 1, (int)Math.Round(freqEnd / (sampleRate / 2.0) * (analyzer.Spectrum.Length - 1)));
                if (binEnd < binStart) binEnd = binStart;

                // 平均该条所有 bin
                float sum = 0;
                int count = binEnd - binStart + 1;
                for (int b = binStart; b <= binEnd; b++)
                    sum += _smoothedSpectrum[b];

                float db = sum / count;
                float normalized = (db + 60) / 60f;
                float barHeight = normalized * h;

                // 渐变颜色
                Color color;
                if (normalized < 0.5f)
                {
                    float t = normalized / 0.5f;
                    color = Color.FromArgb(255, (byte)(t * 255), 255, 0);
                }
                else
                {
                    float t = (normalized - 0.5f) / 0.5f;
                    color = Color.FromArgb(255, 255, (byte)((1 - t) * 255), 0);
                }

                ds.FillRectangle(i * barWidth, h - barHeight, barWidth, barHeight, color);
            }
        }

        private void eqCanvas_CreateResources(Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {

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
