using System;
using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TewiMP.Controls
{
    public sealed partial class AudioSpectrum : Control
    {
        public static readonly DependencyProperty SampleCountProperty = DependencyProperty.Register(
            "SampleCount",
            typeof(int),
            typeof(AudioSpectrum),
            new PropertyMetadata(128, new((_,  __) =>
            {
                if (_ is null) return;
                (_ as AudioSpectrum).SampleCount = (int)__.NewValue;
            })
        ));

        public static readonly DependencyProperty SmoothingFactorProperty = DependencyProperty.Register(
            "SmoothingFactor",
            typeof(double),
            typeof(AudioSpectrum),
            new PropertyMetadata(.13d, new((_,  __) =>
            {
                if (_ is null) return;
                (_ as AudioSpectrum).SmoothingFactor = (double)__.NewValue;
            })
        ));

        
        public static readonly DependencyProperty SmoothWindowProperty = DependencyProperty.Register(
            "SmoothWindow",
            typeof(int),
            typeof(AudioSpectrum),
            new PropertyMetadata(2, new((_,  __) =>
            {
                if (_ is null) return;
                (_ as AudioSpectrum).SmoothWindow = (int)__.NewValue;
            })
        ));

        public static readonly DependencyProperty StrokeWidthProperty = DependencyProperty.Register(
            "StrokeWidth",
            typeof(double),
            typeof(AudioSpectrum),
            new PropertyMetadata(1d, new((_,  __) =>
            {
                if (_ is null) return;
                (_ as AudioSpectrum).StrokeWidth = (double)__.NewValue;
            })
        ));

        public static readonly DependencyProperty MinFreqProperty = DependencyProperty.Register(
            "MinFreq",
            typeof(double),
            typeof(AudioSpectrum),
            new PropertyMetadata(20d, new((_,  __) =>
            {
                if (_ is null) return;
                (_ as AudioSpectrum).MinFreq = (double)__.NewValue;
            })
        ));

        public static readonly DependencyProperty MaxFreqProperty = DependencyProperty.Register(
            "MaxFreq",
            typeof(double),
            typeof(AudioSpectrum),
            new PropertyMetadata(16000d, new((_,  __) =>
            {
                if (_ is null) return;
                (_ as AudioSpectrum).MaxFreq = (double)__.NewValue;
            })
        ));

        public static readonly DependencyProperty IsStopProperty = DependencyProperty.Register(
            "IsStop",
            typeof(bool),
            typeof(AudioSpectrum),
            new PropertyMetadata(false, new((_,  __) =>
            {
                if (_ is null) return;
                var audioSpectrum = (AudioSpectrum)_;
                audioSpectrum.IsStop = (bool)__.NewValue;
                if (audioSpectrum.IsStop)
                {
                    App.Instance.AudioPlayer.VolumeMeter -= audioSpectrum.AudioPlayer_VolumeMeter;
                }
                else
                {
                    App.Instance.AudioPlayer.VolumeMeter -= audioSpectrum.AudioPlayer_VolumeMeter;
                    App.Instance.AudioPlayer.VolumeMeter += audioSpectrum.AudioPlayer_VolumeMeter;
                }
            })
        ));

        public int SampleCount
        {
            get => (int)GetValue(SampleCountProperty);
            set => SetValue(SampleCountProperty, value);
        }
        
        public double SmoothingFactor
        {
            get => (double)GetValue(SmoothingFactorProperty);
            set => SetValue(SmoothingFactorProperty, value);
        }
        
        public int SmoothWindow
        {
            get => (int)GetValue(SmoothWindowProperty);
            set => SetValue(SmoothWindowProperty, value);
        }
        
        public double StrokeWidth
        {
            get => (double)GetValue(StrokeWidthProperty);
            set => SetValue(StrokeWidthProperty, value);
        }
        
        public double MinFreq
        {
            get => (double)GetValue(MinFreqProperty);
            set => SetValue(MinFreqProperty, value);
        }
        
        public double MaxFreq
        {
            get => (double)GetValue(MaxFreqProperty);
            set => SetValue(MaxFreqProperty, value);
        }
        
        public bool IsStop
        {
            get => (bool)GetValue(IsStopProperty);
            set => SetValue(IsStopProperty, value);
        }

        public AudioSpectrum()
        {
            DefaultStyleKey = typeof(AudioSpectrum);
        }

        #region Events
        private CanvasControl _spectrumCanvas;
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Loaded -= AutoScrollView_Loaded;
            Loaded += AutoScrollView_Loaded;
            Unloaded -= AutoScrollView_Unloaded;
            Unloaded += AutoScrollView_Unloaded;
            _spectrumCanvas = GetTemplateChild("PART_SpectrumCanvas") as CanvasControl;

            if (_spectrumCanvas != null)
            {
                if (!IsStop) // 如果状态不为停止，则订阅事件
                {
                    App.Instance.AudioPlayer.VolumeMeter -= AudioPlayer_VolumeMeter;
                    App.Instance.AudioPlayer.VolumeMeter += AudioPlayer_VolumeMeter;
                }

                SizeChanged -= AudioSpectrum_SizeChanged;
                SizeChanged += AudioSpectrum_SizeChanged;

                _spectrumCanvas.Draw -= _spectrumCanvas_Draw;
                _spectrumCanvas.Draw += _spectrumCanvas_Draw;
            }
        }

        public void AudioPlayer_VolumeMeter(Media.AudioPlayer audioPlayer, float[] sample)
        {
            if (Visibility == Visibility.Collapsed) return;
            _spectrumCanvas.Invalidate();
        }

        private void AudioSpectrum_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _spectrumCanvas.Width = ActualWidth;
            _spectrumCanvas.Height = ActualHeight;
        }

        private float[] _smoothedSpectrum;
        private void _spectrumCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            var ds = args.DrawingSession;

            // 背景透明
            ds.Clear(Colors.Transparent);

            var analyzer = App.Instance.AudioPlayer.AudioAnalyzer;
            if (analyzer == null) return;
            if (analyzer.Spectrum == null) return;

            int sampleRate = analyzer.WaveFormat.SampleRate;
            double minFreq = MinFreq;
            double maxFreq = MaxFreq;
            int barCount = SampleCount;

            float w = (float)sender.ActualWidth;
            float h = (float)sender.ActualHeight;
            float stepX = w / (barCount - 1);

            if (_smoothedSpectrum == null || _smoothedSpectrum.Length != analyzer.Spectrum.Length)
                _smoothedSpectrum = new float[analyzer.Spectrum.Length];

            // 平滑频谱数据
            float smoothingFactor = (float)SmoothingFactor;
            for (int i = 0; i < analyzer.Spectrum.Length; i++)
                _smoothedSpectrum[i] = _smoothedSpectrum[i] * (1 - smoothingFactor) + analyzer.Spectrum[i] * smoothingFactor;

            // 计算每个条的 Y 坐标
            float[] pointsY = new float[barCount];
            double logMin = Math.Log10(minFreq);
            double logMax = Math.Log10(maxFreq);

            for (int i = 0; i < barCount; i++)
            {
                double logStart = logMin + (logMax - logMin) * i / barCount;
                double logEnd = logMin + (logMax - logMin) * (i + 1) / barCount;

                double freqStart = Math.Pow(10, logStart);
                double freqEnd = Math.Pow(10, logEnd);

                int binStart = Math.Max(0, (int)Math.Round(freqStart / (sampleRate / 2.0) * (_smoothedSpectrum.Length - 1)));
                int binEnd = Math.Min(_smoothedSpectrum.Length - 1, (int)Math.Round(freqEnd / (sampleRate / 2.0) * (_smoothedSpectrum.Length - 1)));
                if (binEnd < binStart) binEnd = binStart;

                float sum = 0;
                int count = binEnd - binStart + 1;
                for (int b = binStart; b <= binEnd; b++)
                    sum += _smoothedSpectrum[b];

                float db = sum / count;
                float normalized = Math.Clamp((db + 60) / 60f, 0f, 1f);
                float offset = (float)StrokeWidth / 2f;
                pointsY[i] = Math.Clamp(h - normalized * h - offset, 0, h - 1);
            }

            // 平滑折线
            int smoothWindow = SmoothWindow;
            float[] smoothedPoints = new float[pointsY.Length];
            for (int i = 0; i < pointsY.Length; i++)
            {
                float sum = 0;
                int count = 0;
                for (int j = -smoothWindow; j <= smoothWindow; j++)
                {
                    int idx = i + j;
                    if (idx >= 0 && idx < pointsY.Length)
                    {
                        sum += pointsY[idx];
                        count++;
                    }
                }
                smoothedPoints[i] = sum / count;
            }
            pointsY = smoothedPoints;

            // 构建折线下方连续路径
            var fillPath = new CanvasPathBuilder(sender);
            fillPath.BeginFigure(0, h); // 左下
            for (int i = 0; i < barCount; i++)
            {
                fillPath.AddLine(i * stepX, pointsY[i]);
            }
            fillPath.AddLine((barCount - 1) * stepX, h); // 右下
            fillPath.EndFigure(CanvasFigureLoop.Closed);

            // 垂直渐变刷子：上半透明白色，下完全透明
            var gradient = new CanvasLinearGradientBrush(sender, App.Instance.PlayingList.AlbumAccentColor, Color.FromArgb(0, 255, 255, 255))
            {
                StartPoint = new(0, 0),
                EndPoint = new(0, h),
            };

            // 填充折线下方
            ds.FillGeometry(CanvasGeometry.CreatePath(fillPath), gradient);

            // 绘制折线
            var linePath = new CanvasPathBuilder(sender);
            linePath.BeginFigure(0, pointsY[0]);
            for (int i = 1; i < barCount; i++)
                linePath.AddLine(i * stepX, pointsY[i]);
            linePath.EndFigure(CanvasFigureLoop.Open);

            ds.DrawGeometry(CanvasGeometry.CreatePath(linePath), App.Instance.PlayingList.AlbumAccentColor, (float)StrokeWidth);
        }

        private void AutoScrollView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= AutoScrollView_Loaded;
        }

        private void AutoScrollView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_spectrumCanvas != null)
            {
                _spectrumCanvas.Draw -= _spectrumCanvas_Draw;
            }

            App.Instance.AudioPlayer.VolumeMeter -= AudioPlayer_VolumeMeter;
            SizeChanged -= AudioSpectrum_SizeChanged;
            Unloaded -= AutoScrollView_Unloaded;
        }
        #endregion
    }
}
