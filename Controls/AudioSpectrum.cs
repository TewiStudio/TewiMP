using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.UI;

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

        public static readonly DependencyProperty DrawDbLinesProperty = DependencyProperty.Register(
            "DrawDbLines",
            typeof(bool),
            typeof(AudioSpectrum),
            new PropertyMetadata(false, new((_,  __) =>
            {
                if (_ is null) return;
                (_ as AudioSpectrum).DrawDbLines = (bool)__.NewValue;
                
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
        
        public bool DrawDbLines
        {
            get => (bool)GetValue(DrawDbLinesProperty);
            set => SetValue(DrawDbLinesProperty, value);
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
        private float[] _pointsY;
        private float[] _smoothedPoints;
        private CanvasLinearGradientBrush _gradientBrush;
        private double _lastWidth, _lastHeight;
        private Color _lastAccentColor;
        private CanvasRenderTarget _gridCache;

        private void _spectrumCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            var ds = args.DrawingSession;
            ds.Clear(Colors.Transparent);

            if (DrawDbLines)
            {
                EnsureGrid(sender, minDb: -90, maxDb: 0, stepDb: 10);
                // 绘制缓存的网格
                if (_gridCache != null)
                    ds.DrawImage(_gridCache);
            }

            DrawSpectrum(sender, ds);
        }

        private void EnsureGrid(CanvasControl sender, float minDb, float maxDb, float stepDb)
        {
            if (_gridCache != null &&
                _gridCache.Size.Width == sender.ActualWidth &&
                _gridCache.Size.Height == sender.ActualHeight)
                return; // 大小未变化，复用

            _gridCache?.Dispose();
            _gridCache = new CanvasRenderTarget(sender, (float)sender.ActualWidth, (float)sender.ActualHeight, 96);

            using (var ds = _gridCache.CreateDrawingSession())
            {
                ds.Clear(Colors.Transparent);

                float width = (float)_gridCache.Size.Width;
                float height = (float)_gridCache.Size.Height;

                var dashStroke = new CanvasStrokeStyle { DashStyle = CanvasDashStyle.Dash };
                var textFormat = new CanvasTextFormat { FontSize = 12 };

                float range = maxDb - minDb;

                for (float db = minDb + stepDb; db < maxDb; db += stepDb)
                {
                    float normalized = (db - minDb) / range;
                    float y = height - normalized * height;

                    ds.DrawLine(0, y, width, y, Color.FromArgb(80, 255, 255, 255), 1f, dashStroke);
                    ds.DrawText($"{db} dB", 4, y - 18, Color.FromArgb(120, 255, 255, 255), textFormat);
                }
            }
        }

        private void DrawSpectrum(CanvasControl sender, CanvasDrawingSession ds)
        {
            var analyzer = App.Instance.AudioPlayer.AudioAnalyzer;
            if (analyzer?.Spectrum == null) return;

            int sampleRate = analyzer.WaveFormat.SampleRate;
            double minFreq = MinFreq;
            double maxFreq = MaxFreq;
            int barCount = SampleCount;
            var spectrum = analyzer.Spectrum;

            float w = (float)sender.ActualWidth;
            float h = (float)sender.ActualHeight;
            float stepX = w / (barCount - 1);

            // 初始化缓存数组
            if (_smoothedSpectrum == null || _smoothedSpectrum.Length != spectrum.Length)
                _smoothedSpectrum = new float[spectrum.Length];

            if (_pointsY == null || _pointsY.Length != barCount)
            {
                _pointsY = new float[barCount];
                _smoothedPoints = new float[barCount];
            }

            // 更新或重建渐变刷（仅当颜色或尺寸变化时）
            var accent = App.Instance.PlayingList.AlbumAccentColor;
            if (_gradientBrush == null || w != _lastWidth || h != _lastHeight || accent != _lastAccentColor)
            {
                _gradientBrush?.Dispose();
                _gradientBrush = new CanvasLinearGradientBrush(sender, accent, Color.FromArgb(0, 255, 255, 255))
                {
                    StartPoint = new(0, 0),
                    EndPoint = new(0, h),
                };
                _lastWidth = w;
                _lastHeight = h;
                _lastAccentColor = accent;
            }

            // 一次遍历更新平滑频谱
            float smoothingFactor = (float)SmoothingFactor;
            for (int i = 0; i < spectrum.Length; i++)
                _smoothedSpectrum[i] = _smoothedSpectrum[i] * (1 - smoothingFactor) + spectrum[i] * smoothingFactor;

            // log10 预计算
            double logMin = Math.Log10(minFreq);
            double logMax = Math.Log10(maxFreq);
            double binScale = (_smoothedSpectrum.Length - 1) / (sampleRate / 2.0);

            // 计算每个条目的 Y 坐标
            for (int i = 0; i < barCount; i++)
            {
                double logStart = logMin + (logMax - logMin) * i / barCount;
                double logEnd = logMin + (logMax - logMin) * (i + 1) / barCount;

                int binStart = (int)(Math.Pow(10, logStart) * binScale);
                int binEnd = (int)(Math.Pow(10, logEnd) * binScale);
                binStart = Math.Clamp(binStart, 0, _smoothedSpectrum.Length - 1);
                binEnd = Math.Clamp(binEnd, 0, _smoothedSpectrum.Length - 1);

                float sum = 0;
                for (int b = binStart; b <= binEnd; b++) sum += _smoothedSpectrum[b];
                float avgDb = sum / (binEnd - binStart + 1);

                float normalized = Math.Clamp((avgDb + 60) / 60f, 0f, 1f);
                _pointsY[i] = h - normalized * h - (float)StrokeWidth / 2f;
            }

            // 滑动窗口平滑（O(n) 实现）
            int win = SmoothWindow;
            float acc = 0;
            for (int i = 0; i < barCount; i++)
            {
                acc += _pointsY[i];
                if (i >= win) acc -= _pointsY[i - win];
                int len = Math.Min(i + 1, win);
                _smoothedPoints[i] = acc / len;
            }

            // 构建填充路径
            using (var fillPath = new CanvasPathBuilder(sender))
            {
                fillPath.BeginFigure(0, h);
                for (int i = 0; i < barCount; i++)
                    fillPath.AddLine(i * stepX, _smoothedPoints[i]);
                fillPath.AddLine((barCount - 1) * stepX, h);
                fillPath.EndFigure(CanvasFigureLoop.Closed);

                ds.FillGeometry(CanvasGeometry.CreatePath(fillPath), _gradientBrush);
            }

            // 绘制折线
            using (var linePath = new CanvasPathBuilder(sender))
            {
                linePath.BeginFigure(0, _smoothedPoints[0]);
                for (int i = 1; i < barCount; i++)
                    linePath.AddLine(i * stepX, _smoothedPoints[i]);
                linePath.EndFigure(CanvasFigureLoop.Open);

                ds.DrawGeometry(CanvasGeometry.CreatePath(linePath), accent, (float)StrokeWidth);
            }
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

            _gridCache?.Dispose();
            App.Instance.AudioPlayer.VolumeMeter -= AudioPlayer_VolumeMeter;
            SizeChanged -= AudioSpectrum_SizeChanged;
            Unloaded -= AutoScrollView_Unloaded;
        }
        #endregion
    }
}
