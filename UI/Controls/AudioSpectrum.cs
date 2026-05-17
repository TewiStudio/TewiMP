#nullable enable
using System;
using System.Linq;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using TewiMP.Core.Audio;
using TewiMP.Helpers;
using TewiMP.Services.Media.Audio;
using TewiMP.Services.Media.Audio.AudioEffects;

namespace TewiMP.UI.Controls
{
    /// <summary>
    /// 从正在播放的歌曲获取经过转换的数据，实时绘制频谱、均衡器影响曲线
    /// </summary>
    public sealed partial class AudioSpectrum : Control
    {
        private struct SpectrumBarCache
        {
            public int BinStart;      // 起始 FFT Bin 索引
            public int BinEnd;        // 结束 FFT Bin 索引
            public int Count;         // Bin 总数 (End - Start + 1)
            public float X;           // 屏幕上的 X 坐标
            public float TiltFactor;  // 预计算的 Tilt 修正系数
        }

        private CanvasControl _spectrumCanvas;

        private SpectrumBarCache[] _barCache;
        private double _cacheMinFreq, _cacheMaxFreq, _cacheTiltDbPerOct;
        private int _cacheSampleRate, _cacheBarCount;
        private float _cacheWidth;

        private float[] _smoothedSpectrum;
        private float[] _pointsX;
        private float[] _pointsY;
        private float[] _smoothedPoints;
        private CanvasLinearGradientBrush _gradientBrush;
        private CanvasRenderTarget _gridCache;
        private CanvasTextFormat canvasTextFormat;
        private double _lastWidth, _lastHeight;
        private double _lastMinFreq, _lastMaxFreq;
        private Color _lastAccentColor;
        private EQData? _draggingEQ = null;
        private PassFilterData? _draggingPass = null;
        private EQData? _hoveringEQ;
        private PassFilterData? _hoveringPass;
        private const float EQHitRadius = 10f;
        private const float MinFreqHz = 20f;
        private const float MaxFreqHz = 22000f;
        private const float MinQ = 0.3f;
        private const float MaxQ = 33.3f;
        private const float QStep = 0.1f;

        private readonly Queue<double> _frameTimes = new();   // 记录最近一段时间的帧间隔（秒）
        private double _deltaTime = 0;
        private double _lastDrawTime = 0;
        private double _targetFps = 60;
        private double _avgFps = 0;
        private double _avgFrameMs = 0;

        // 控制统计窗口大小（秒）
        private const double SampleDuration = 1.0; // 1秒内的平均值

        // 滚轮缩放
        private const double MinVisibleFreq = 20;
        private const double MaxVisibleFreq = 22000;
        private const double MinZoomRangeHz = 200;  // 最小可显示带宽
        private const double MaxZoomRangeHz = 22000 - 20; // 最大可显示带宽
        private double _animMinFreq, _animMaxFreq;
        private double _targetMinFreq, _targetMaxFreq;
        private double _zoomAnimStartTime;
        private const double ZoomAnimDuration = 0.25; // 秒
        private bool _isZoomAnimating;
        private bool _firstDraw = true;

        // 鼠标
        private float _hoverX = -1, _hoverY = -1;
        private float _hoverFreq = 0, _hoverDb = 0;
        private bool _isDragging = false;
        private float _dragStartX;
        private double _logMinStart, _logMaxStart;

        #region 依赖属性
        public static readonly DependencyProperty IsStopProperty = DependencyProperty.Register(
            "IsStop", typeof(bool), typeof(AudioSpectrum),
            new PropertyMetadata(false, OnPropertyChanged<bool>));

        public static readonly DependencyProperty SampleCountProperty = DependencyProperty.Register(
            "SampleCount", typeof(int), typeof(AudioSpectrum),
            new PropertyMetadata(128, OnPropertyChanged<int>));

        public static readonly DependencyProperty SmoothingDownFactorProperty = DependencyProperty.Register(
            "SmoothingDownFactor", typeof(double), typeof(AudioSpectrum),
            new PropertyMetadata(.15d, OnPropertyChanged<double>));

        public static readonly DependencyProperty SmoothingUpFactorProperty = DependencyProperty.Register(
            "SmoothingUpFactor", typeof(double), typeof(AudioSpectrum),
            new PropertyMetadata(.4d, OnPropertyChanged<double>));

        public static readonly DependencyProperty SmoothWindowProperty = DependencyProperty.Register(
            "SmoothWindow", typeof(int), typeof(AudioSpectrum),
            new PropertyMetadata(2, OnPropertyChanged<int>));

        public static readonly DependencyProperty TiltDbPerOctProperty = DependencyProperty.Register(
            "TiltDbPerOct", typeof(double), typeof(AudioSpectrum),
            new PropertyMetadata(-4.5d, OnPropertyChanged<double>));

        public static readonly DependencyProperty StrokeWidthProperty = DependencyProperty.Register(
            "StrokeWidth", typeof(double), typeof(AudioSpectrum),
            new PropertyMetadata(1d, OnPropertyChanged<double>));

        public static readonly DependencyProperty MinFreqProperty = DependencyProperty.Register(
            "MinFreq", typeof(double), typeof(AudioSpectrum),
            new PropertyMetadata(20d, OnPropertyChanged<double>));

        public static readonly DependencyProperty MaxFreqProperty = DependencyProperty.Register(
            "MaxFreq", typeof(double), typeof(AudioSpectrum),
            new PropertyMetadata(20000d, OnPropertyChanged<double>));

        public static readonly DependencyProperty DrawScaleProperty = DependencyProperty.Register(
            "DrawScale", typeof(double), typeof(AudioSpectrum),
            new PropertyMetadata(1.0d, OnPropertyChanged<double>));

        public static readonly DependencyProperty DrawDbLinesProperty = DependencyProperty.Register(
            "DrawDbLines", typeof(bool), typeof(AudioSpectrum),
            new PropertyMetadata(false, OnPropertyChanged<bool>));

        public static readonly DependencyProperty DrawEqLinesProperty = DependencyProperty.Register(
            "DrawEqLines", typeof(bool), typeof(AudioSpectrum),
            new PropertyMetadata(false, OnPropertyChanged<bool>));

        public static readonly DependencyProperty DrawLatencyTextProperty = DependencyProperty.Register(
            "DrawLatencyText", typeof(bool), typeof(AudioSpectrum),
            new PropertyMetadata(false, OnPropertyChanged<bool>));

        public static readonly DependencyProperty DrawEqLinesStrokeWidthProperty = DependencyProperty.Register(
            "DrawEqLinesStrokeWidth", typeof(double), typeof(AudioSpectrum),
            new PropertyMetadata(2d, OnPropertyChanged<bool>));

        public static readonly DependencyProperty DrawEqPointsProperty = DependencyProperty.Register(
            "DrawEqPoints", typeof(bool), typeof(AudioSpectrum),
            new PropertyMetadata(false, OnPropertyChanged<bool>));

        public static readonly DependencyProperty DrawEqPointsRadiusProperty = DependencyProperty.Register(
            "DrawEqPointsRadius", typeof(double), typeof(AudioSpectrum),
            new PropertyMetadata(5d, OnPropertyChanged<bool>));

        public bool IsStop { get => (bool)GetValue(IsStopProperty); set => SetValue(IsStopProperty, value); }
        public int SampleCount { get => (int)GetValue(SampleCountProperty); set => SetValue(SampleCountProperty, value); }
        public double SmoothingDownFactor { get => (double)GetValue(SmoothingDownFactorProperty); set => SetValue(SmoothingDownFactorProperty, value); }
        public double SmoothingUpFactor { get => (double)GetValue(SmoothingUpFactorProperty); set => SetValue(SmoothingUpFactorProperty, value); }
        public int SmoothWindow { get => (int)GetValue(SmoothWindowProperty); set => SetValue(SmoothWindowProperty, value); }
        public double TiltDbPerOct { get => (double)GetValue(TiltDbPerOctProperty); set => SetValue(TiltDbPerOctProperty, value); }
        public double StrokeWidth { get => (double)GetValue(StrokeWidthProperty); set => SetValue(StrokeWidthProperty, value); }
        public double MinFreq { get => (double)GetValue(MinFreqProperty); set => SetValue(MinFreqProperty, value); }
        public double MaxFreq { get => (double)GetValue(MaxFreqProperty); set => SetValue(MaxFreqProperty, value); }
        public double DrawScale { get => (double)GetValue(DrawScaleProperty); set => SetValue(DrawScaleProperty, value); }
        public bool DrawDbLines { get => (bool)GetValue(DrawDbLinesProperty); set => SetValue(DrawDbLinesProperty, value); }
        public bool DrawEqLines { get => (bool)GetValue(DrawEqLinesProperty); set => SetValue(DrawEqLinesProperty, value); }
        public bool DrawLatencyText { get => (bool)GetValue(DrawLatencyTextProperty); set => SetValue(DrawLatencyTextProperty, value); }
        public bool DrawEqPoints { get => (bool)GetValue(DrawEqPointsProperty); set => SetValue(DrawEqPointsProperty, value); }
        public double DrawEqLinesStrokeWidth { get => (double)GetValue(DrawEqLinesStrokeWidthProperty); set => SetValue(DrawEqLinesStrokeWidthProperty, value); }
        public double DrawEqPointsRadius { get => (double)GetValue(DrawEqPointsRadiusProperty); set => SetValue(DrawEqPointsRadiusProperty, value); }

        private static void OnPropertyChanged<T>(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //if (d is AudioSpectrum spectrum) spectrum._spectrumCanvas?.Invalidate();
        }
        #endregion

        public AudioSpectrum()
        {
            DefaultStyleKey = typeof(AudioSpectrum);
        }

        private double GetEqBandGainDb(double freq, EQData eq)
        {
            if (!eq.IsEnable) return 0;
            double f0 = eq.CentreFrequency;
            double Q = eq.Q;
            double gainDb = eq.Gain;

            // 带宽影响（简化高斯形状）
            double ratio = freq / f0;
            double response = Math.Exp(-0.5 * Math.Pow(Math.Log(ratio) * Q, 2.0));

            return gainDb * response;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _spectrumCanvas = GetTemplateChild("PART_SpectrumCanvas") as CanvasControl;

            if (_spectrumCanvas != null)
            {
                _spectrumCanvas.Draw += SpectrumCanvas_Draw;
                _spectrumCanvas.PointerPressed += SpectrumCanvas_PointerPressed;
                _spectrumCanvas.PointerReleased += SpectrumCanvas_PointerReleased;
                _spectrumCanvas.PointerMoved += SpectrumCanvas_PointerMoved;
                _spectrumCanvas.PointerExited += SpectrumCanvas_PointerExited;
                _spectrumCanvas.PointerWheelChanged += SpectrumCanvas_PointerWheelChanged;
                SizeChanged += AudioSpectrum_SizeChanged;

                App.Instance.AudioService.VolumeMeter -= AudioService_VolumeMeter;
                App.Instance.AudioService.VolumeMeter += AudioService_VolumeMeter;
            }
        }

        private void AudioService_VolumeMeter(AudioService AudioService, float[] sample)
        {
            if (Visibility == Visibility.Visible && !IsStop)
                _spectrumCanvas.Invalidate();
        }

        private void AudioSpectrum_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _spectrumCanvas.Width = (float)ActualWidth;
            _spectrumCanvas.Height = (float)ActualHeight;
        }

        #region 鼠标命中测试
        private EQData? HitTestEQ(float x, float y)
        {
            if (!AudioFilterStatic.ParametricEqEnable || AudioFilterStatic.ParametricEqDatas.Count == 0)
                return null;

            double logMin = Math.Log10(MinFreq);
            double logMax = Math.Log10(MaxFreq);
            float width = (float)_spectrumCanvas.ActualWidth;
            float height = (float)_spectrumCanvas.ActualHeight;

            foreach (var eq in AudioFilterStatic.ParametricEqDatas)
            {
                if (!eq.IsEnable) continue;

                float eqX = (float)((Math.Log10(eq.CentreFrequency) - logMin) / (logMax - logMin) * width);
                float eqY = height / 2 - eq.Gain / 24f * height / 2;

                if (Math.Abs(eqX - x) <= EQHitRadius && Math.Abs(eqY - y) <= EQHitRadius)
                    return eq;
            }

            return null;
        }

        private PassFilterData? HitTestPassFilter(float x, float y)
        {
            if (!AudioFilterStatic.PassFilterEqEnable || AudioFilterStatic.PassFilterDatas.Count == 0)
                return null;

            double logMin = Math.Log10(MinFreq);
            double logMax = Math.Log10(MaxFreq);
            float width = (float)_spectrumCanvas.ActualWidth;
            float height = (float)_spectrumCanvas.ActualHeight;

            foreach (var pf in AudioFilterStatic.PassFilterDatas)
            {
                if (!pf.IsEnable) continue;

                float pfX = (float)((Math.Log10(pf.CentreFrequency) - logMin) / (logMax - logMin) * width);
                float pfY = height / 2 - pf.Gain / 24f * height / 2;

                if (Math.Abs(pfX - x) <= EQHitRadius && Math.Abs(pfY - y) <= EQHitRadius)
                    return pf;
            }

            return null;
        }
        #endregion

        #region 鼠标交互
        private void SpectrumCanvas_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (!DrawDbLines) return;

            var pt = e.GetCurrentPoint(_spectrumCanvas);
            var pos = pt.Position;

            if (pt.Properties.IsLeftButtonPressed)
            {
                // 先检测是否点在 EQ / PassFilter 点上
                _draggingEQ = HitTestEQ((float)pos.X, (float)pos.Y);
                _draggingPass = HitTestPassFilter((float)pos.X, (float)pos.Y);

                if (_draggingEQ != null || _draggingPass != null)
                {
                    _spectrumCanvas.CapturePointer(e.Pointer);
                    return;
                }

                // 否则启用频谱拖动
                _isDragging = true;
                _dragStartX = (float)pos.X;
                _logMinStart = Math.Log10(MinFreq);
                _logMaxStart = Math.Log10(MaxFreq);
                _spectrumCanvas.CapturePointer(e.Pointer);
            }
        }

        private void SpectrumCanvas_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (_isDragging || _draggingEQ != null || _draggingPass != null)
            {
                _isDragging = false;
                _draggingEQ = null;
                _draggingPass = null;
                _spectrumCanvas.ReleasePointerCapture(e.Pointer);
            }
        }

        private void SpectrumCanvas_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (!DrawDbLines) return;

            var pos = e.GetCurrentPoint(sender as UIElement).Position;
            _hoverX = (float)pos.X;
            _hoverY = (float)pos.Y;
            UpdateHoverData();

            // 更新悬停 EQ 或 PassFilter
            _hoveringEQ = HitTestEQ(_hoverX, _hoverY);
            _hoveringPass = HitTestPassFilter(_hoverX, _hoverY);

            if (!_isDragging && _draggingEQ == null && _draggingPass == null)
                return;

            float width = (float)_spectrumCanvas.ActualWidth;
            float height = (float)_spectrumCanvas.ActualHeight;
            double logMin = Math.Log10(MinFreq);
            double logMax = Math.Log10(MaxFreq);

            // 拖动 EQ 点
            if (_draggingEQ != null)
            {
                double logFreq = logMin + (_hoverX / width) * (logMax - logMin);
                float freq = (float)Math.Pow(10, logFreq);
                _draggingEQ.CentreFrequency = Math.Clamp(float.Round(freq, 2), MinFreqHz, MaxFreqHz);

                float gain = (height / 2 - _hoverY) / (height / 2) * 24f;
                _draggingEQ.Gain = Math.Clamp(float.Round(gain, 2), -24f, 24f);
                return;
            }

            // 拖动 PassFilter 点
            if (_draggingPass != null)
            {
                double logFreq = logMin + (_hoverX / width) * (logMax - logMin);
                float freq = (float)Math.Pow(10, logFreq);
                _draggingPass.CentreFrequency = Math.Clamp(float.Round(freq, 2), MinFreqHz, MaxFreqHz);

                if (_draggingPass.PassFilterType is PassFilterType.LowShelf or PassFilterType.HighShelf)
                {
                    float gain = (height / 2 - _hoverY) / (height / 2) * 24f;
                    _draggingPass.Gain = Math.Clamp(float.Round(gain, 2), -24f, 24f);
                }
                else
                {
                    if (_draggingPass.Gain != 0) _draggingPass.Gain = 0;
                }

                return;
            }

            // 拖动频谱
            if (!_isDragging) return;

            float dx = (float)pos.X - _dragStartX;
            double logRange = _logMaxStart - _logMinStart;
            double deltaLog = -dx / width * logRange;

            double newLogMin = _logMinStart + deltaLog;
            double newLogMax = newLogMin + logRange;

            double minBound = Math.Log10(20);
            double maxBound = Math.Log10(22000);

            if (newLogMin < minBound)
            {
                newLogMin = minBound;
                newLogMax = minBound + logRange;
            }
            else if (newLogMax > maxBound)
            {
                newLogMax = maxBound;
                newLogMin = maxBound - logRange;
            }

            MinFreq = Math.Pow(10, newLogMin);
            MaxFreq = Math.Pow(10, newLogMax);
        }

        private void SpectrumCanvas_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            _hoverX = _hoverY = -1;
        }

        private void SpectrumCanvas_PointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            int delta = e.GetCurrentPoint(_spectrumCanvas).Properties.MouseWheelDelta;
            bool ctrlDown = App.MainWindowInstance.isControlDown;

            // 如果 Ctrl 按下，则执行频谱缩放
            if (ctrlDown && DrawDbLines)
            {
                var pos = e.GetCurrentPoint(_spectrumCanvas).Position;
                ZoomSpectrum((float)pos.X, delta);
                return;
            }

            // === EQ 调整模式 ===
            if (_hoveringEQ != null)
            {
                float newQ = _hoveringEQ.Q + (delta > 0 ? QStep : -QStep);
                _hoveringEQ.Q = Math.Clamp(float.Round(newQ, 2), MinQ, MaxQ);
                return;
            }

            // === PassFilter 调整模式 ===
            if (_hoveringPass != null)
            {
                if (_hoveringPass.PassFilterType is PassFilterType.LowPass or PassFilterType.HighPass)
                {
                    int newQ = _hoveringPass.SlopeDbPerOct + (delta > 0 ? 12 : -12);
                    _hoveringPass.SlopeDbPerOct = int.Clamp(newQ, 12, 96);
                }
                else
                {
                    float newQ = _hoveringPass.Q + (delta > 0 ? QStep : -QStep);
                    _hoveringPass.Q = Math.Clamp(float.Round(newQ, 2), MinQ, MaxQ);
                }
                return;
            }

            if (!DrawDbLines) return;
            // 默认行为：频谱缩放
            var pointerPos = e.GetCurrentPoint(_spectrumCanvas).Position;
            ZoomSpectrum((float)pointerPos.X, delta);
        }
        #endregion

        #region 缩放 & hover
        private void ZoomSpectrum(float mouseX, int delta)
        {
            if (_spectrumCanvas == null || _spectrumCanvas.ActualWidth <= 0) return;

            // 缩放强度
            double baseFactor = 1.35;
            double step = Math.Abs(delta) / 120.0;
            double scaleFactor = Math.Pow(baseFactor, step);
            if (delta < 0) scaleFactor = 1.0 / scaleFactor;

            // 当前频率范围
            double logMin = Math.Log10(MinFreq);
            double logMax = Math.Log10(MaxFreq);
            double logRange = logMax - logMin;

            // 鼠标所在频率
            double mouseRatio = mouseX / _spectrumCanvas.ActualWidth;
            double logMouse = logMin + mouseRatio * logRange;
            double mouseFreq = Math.Pow(10, logMouse);

            // 新范围
            double newLogRange = logRange / scaleFactor;

            // 缩放限制
            double curRangeHz = Math.Pow(10, logMax) - Math.Pow(10, logMin);
            if (curRangeHz < MinZoomRangeHz && delta > 0) return;
            if (curRangeHz > MaxZoomRangeHz && delta < 0) return;

            // 保证锚点频率不偏移
            double newLogMin = logMouse - mouseRatio * newLogRange;
            double newLogMax = newLogMin + newLogRange;

            // 限制边界
            double minLimit = Math.Log10(MinVisibleFreq);
            double maxLimit = Math.Log10(MaxVisibleFreq);

            // 如果超出边界则整体偏移回来
            if (newLogMin < minLimit)
            {
                double diff = minLimit - newLogMin;
                newLogMin += diff;
                newLogMax += diff;
            }
            if (newLogMax > maxLimit)
            {
                double diff = newLogMax - maxLimit;
                newLogMin -= diff;
                newLogMax -= diff;
            }

            // 应用动画目标
            _targetMinFreq = Math.Pow(10, newLogMin);
            _targetMaxFreq = Math.Pow(10, newLogMax);
            _animMinFreq = MinFreq;
            _animMaxFreq = MaxFreq;
            _zoomAnimStartTime = Environment.TickCount64 / 1000.0;
            _isZoomAnimating = true;
        }

        private void UpdateHoverData()
        {
            if (_hoverX < 0 || _smoothedSpectrum == null) return;

            var analyzer = App.Instance.AudioService.AudioAnalyzer;
            if (analyzer?.Spectrum == null) return;

            double logMin = Math.Log10(MinFreq);
            double logMax = Math.Log10(MaxFreq);
            double freq = Math.Pow(10, logMin + (_hoverX / _spectrumCanvas.ActualWidth) * (logMax - logMin));

            _hoverFreq = (float)freq;

            double binScale = (_smoothedSpectrum.Length - 1) / (analyzer.WaveFormat.SampleRate / 2.0);
            double bin = freq * binScale;
            int i0 = Math.Clamp((int)Math.Floor(bin), 0, _smoothedSpectrum.Length - 1);
            int i1 = Math.Clamp((int)Math.Ceiling(bin), 0, _smoothedSpectrum.Length - 1);

            double t = bin - i0;
            _hoverDb = (float)(_smoothedSpectrum[i0] * (1 - t) + _smoothedSpectrum[i1] * t);
        }
        #endregion

        #region 绘制
        private void SpectrumCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            var ds = args.DrawingSession;
            ds.Clear(Colors.Transparent);
            sender.DpiScale = (float)DrawScale;

            // 计算帧时间
            double now = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
            if (_lastDrawTime > 0)
            {
                _deltaTime = now - _lastDrawTime;

                if (DrawLatencyText)
                {
                    _frameTimes.Enqueue(_deltaTime);
                    // 移除超出统计窗口的旧数据
                    while (_frameTimes.Sum() > SampleDuration)
                        _frameTimes.Dequeue();

                    // 计算平均帧时间和FPS
                    if (_frameTimes.Count > 0)
                    {
                        double avgDelta = _frameTimes.Average();
                        _avgFps = 1.0 / avgDelta;
                        _avgFrameMs = avgDelta * 1000.0;
                    }
                }
            }
            _lastDrawTime = now;

            double logMin = Math.Log10(MinFreq);
            double logMax = Math.Log10(MaxFreq);
            float width = (float)sender.ActualWidth;
            float height = (float)sender.ActualHeight;

            DrawSpectrum(sender, ds, width, height);

            if (DrawEqLines)
            {
                DrawEqResponseCurve(ds, (float)sender.ActualWidth, (float)sender.ActualHeight);
            }
            if (DrawEqPoints)
            {
                DrawEQPoints(ds, (float)sender.ActualWidth, (float)sender.ActualHeight, logMin, logMax);
            }
            if (DrawDbLines)
            {
                DrawGrid(sender, ds, width, height);
            }

            if (DrawLatencyText)
            {
                // 绘制平均 FPS 信息
                string info = $"FPS: {_avgFps:0.0} ({_avgFrameMs:0.0} ms)";
                ds.DrawText(info, 10, 10, App.Instance.PlayingListService.TextColor, canvasTextFormat ??= new CanvasTextFormat { FontSize = 14 });
            }
        }

        // 内部插值函数
        static double Lerp(double a, double b, double t) => a + (b - a) * t;

        private void DrawSpectrum(CanvasControl sender, CanvasDrawingSession ds, float w, float h)
        {
            var analyzer = App.Instance.AudioService.AudioAnalyzer;
            if (analyzer?.Spectrum == null) return;

            // 获取基础参数
            int sampleRate = analyzer.WaveFormat.SampleRate;
            int barCount = SampleCount;

            // 更新缓存。如果正在缩放，这里会每帧执行计算；如果不缩放，这里耗时为0
            UpdateSpectrumCache(sampleRate, barCount, MinFreq, MaxFreq, w, h, analyzer);

            // 初始化数组
            if (_smoothedSpectrum == null || _smoothedSpectrum.Length != analyzer.Spectrum.Length)
                _smoothedSpectrum = new float[analyzer.Spectrum.Length];

            if (_pointsY == null || _pointsY.Length != barCount)
            {
                _pointsY = new float[barCount];
                _smoothedPoints = new float[barCount];
                _pointsX = new float[barCount];
            }

            // 渐变刷
            var accent = ((SolidColorBrush)Foreground).Color;// ?? App.Instance.PlayingListService.AlbumAccentColor;
            if (_gradientBrush == null || w != _lastWidth || h != _lastHeight || accent != _lastAccentColor)
            {
                _gradientBrush?.Dispose();
                _gradientBrush = new CanvasLinearGradientBrush(sender, accent, Color.FromArgb(0, accent.R, accent.G, accent.B))
                {
                    StartPoint = new(0, 0),
                    EndPoint = new(0, h),
                };
                _lastWidth = w;
                _lastHeight = h;
                _lastAccentColor = accent;
            }

            // 时间平滑
            float adjustedDownFactor = (float)(1.0 - Math.Pow(1.0 - SmoothingDownFactor, _deltaTime * _targetFps));
            float adjustedUpFactor = (float)(1.0 - Math.Pow(1.0 - SmoothingUpFactor, _deltaTime * _targetFps));
            var spectrum = analyzer.Spectrum;
            for (int i = 0; i < spectrum.Length; i++)
            {
                float target = spectrum[i];
                float current = _smoothedSpectrum[i];
                if (target >= current)
                    _smoothedSpectrum[i] = current + (target - current) * adjustedUpFactor;
                else
                    _smoothedSpectrum[i] = current + (target - current) * adjustedDownFactor;
            }

            // 计算 Bar 高度
            float range = analyzer.MaxDb - analyzer.MinDb;
            float minDb = analyzer.MinDb;
            float strokeW = (float)StrokeWidth / 2f;

            for (int i = 0; i < barCount; i++)
            {
                var cache = _barCache[i];

                float sum = 0;
                // 边界保护，防止 SampleRate 突变导致的越界
                int end = Math.Min(cache.BinEnd, _smoothedSpectrum.Length - 1);
                for (int b = cache.BinStart; b <= end; b++)
                    sum += _smoothedSpectrum[b];

                float avgDb = sum / cache.Count;

                // 归一化
                float normalized = (avgDb - minDb) / range;

                // 应用预计算的 Tilt
                normalized = normalized + cache.TiltFactor * normalized;

                normalized = Math.Clamp(normalized, 0f, 1f);

                _pointsY[i] = h - normalized * h - strokeW;
                _pointsX[i] = cache.X;
            }

            // 滑动窗口平滑
            if (SmoothWindow < 2)
            {
                _smoothedPoints = _pointsY;
            }
            else
            {
                for (int i = 0; i < barCount; i++)
                {
                    double freqRatio = (double)i / barCount;
                    int win = (int)(SmoothWindow * (1 + (1 - freqRatio) * 8)); // 低频平滑更强
                    float sum = 0;
                    int count = 0;
                    for (int j = Math.Max(0, i - win / 2); j < Math.Min(barCount, i + win / 2); j++)
                    {
                        sum += _pointsY[j];
                        count++;
                    }
                    _smoothedPoints[i] = sum / count;
                }
            }

            // 绘制填充
            using (var fillPath = new CanvasPathBuilder(sender))
            {
                fillPath.BeginFigure(-1, h);
                fillPath.AddLine(0, _smoothedPoints[0]);
                for (int i = 1; i < barCount - 1; i++)
                    fillPath.AddLine(_pointsX[i], _smoothedPoints[i]);
                fillPath.AddLine(w, _smoothedPoints[^1]);
                fillPath.AddLine(w + 1, h);
                fillPath.EndFigure(CanvasFigureLoop.Closed);

                ds.FillGeometry(CanvasGeometry.CreatePath(fillPath), _gradientBrush);
            }

            // 绘制曲线
            using (var linePath = new CanvasPathBuilder(sender))
            {
                linePath.BeginFigure(0, _smoothedPoints[0]);
                for (int i = 1; i < barCount; i++)
                    linePath.AddLine(_pointsX[i], _smoothedPoints[i]);
                linePath.AddLine(w, _smoothedPoints[^1]);
                linePath.EndFigure(CanvasFigureLoop.Open);

                ds.DrawGeometry(CanvasGeometry.CreatePath(linePath), accent, (float)StrokeWidth);
            }

            // Hover 提示
            DrawHoverInfo(ds, w, h);
            if (_isZoomAnimating)
            {
                double t = (Environment.TickCount64 / 1000.0 - _zoomAnimStartTime) / ZoomAnimDuration;

                if (t >= 1.0)
                {
                    _isZoomAnimating = false;
                    MinFreq = _targetMinFreq;
                    MaxFreq = _targetMaxFreq;
                }
                else
                {
                    // ease-out 平滑算法
                    double easedT = 1 - Math.Pow(1 - t, 3);
                    MinFreq = Lerp(_animMinFreq, _targetMinFreq, easedT);
                    MaxFreq = Lerp(_animMaxFreq, _targetMaxFreq, easedT);
                }
            }
        }

        private CanvasTextLayout _hoverTextLayout;
        private string _lastHoverText;
        private void DrawHoverInfo(CanvasDrawingSession ds, float w, float h)
        {
            if (_hoverX < 0) return;

            UpdateHoverData();
            string hoverText = $"{_hoverFreq:0.#} Hz / {_hoverDb:0.0} dB";

            // 只有文字变了才重新创建 Layout
            if (_hoverTextLayout == null || hoverText != _lastHoverText)
            {
                _hoverTextLayout?.Dispose();
                var textFormat = new CanvasTextFormat { FontSize = 14 }; // 可以提为成员变量
                _hoverTextLayout = new CanvasTextLayout(ds, hoverText, textFormat, w, h);
                _lastHoverText = hoverText;
            }

            var brush = App.Instance.PlayingListService.TextColor;
            float textWidth = (float)_hoverTextLayout.DrawBounds.Width;

            float textX = _hoverX + 10;
            if (textX + textWidth > w) textX = w - textWidth - 2;

            float textY = _hoverY - 20;
            if (textY < 0) textY = 0;

            ds.DrawTextLayout(_hoverTextLayout, textX, textY, brush);
            ds.DrawLine(_hoverX, 0, _hoverX, h, brush, .5f);
        }

        CanvasStrokeStyle gridDash;
        CanvasTextFormat gridTextFormat;
        private void DrawGrid(CanvasControl sender, CanvasDrawingSession ds, float w, float h)
        {
            bool isFreqChanged = Math.Abs(_lastMinFreq - MinFreq) > 0.0001 ||
                                 Math.Abs(_lastMaxFreq - MaxFreq) > 0.0001;
            if (_gridCache != null && _gridCache.Size.Width == w && _gridCache.Size.Height == h
                && !isFreqChanged)
            {
                ds.DrawImage(_gridCache);
                return;
            }

            _gridCache?.Dispose();
            _gridCache = new CanvasRenderTarget(sender, (float)sender.ActualWidth, (float)sender.ActualHeight, sender.Dpi);
            _lastMinFreq = MinFreq; _lastMaxFreq = MaxFreq;

            using var gds = _gridCache.CreateDrawingSession();
            gds.Clear(Colors.Transparent);

            // dB 网格
            float minDb = -90, maxDb = 0, stepDb = 10;
            float width = (float)_gridCache.Size.Width;
            float height = (float)_gridCache.Size.Height;
            var dash = gridDash ??= new CanvasStrokeStyle { DashStyle = CanvasDashStyle.Dash };
            var textFormat = gridTextFormat ??= new CanvasTextFormat { FontSize = 12 };
            var textColor = App.Instance.PlayingListService.TextColor;
            float range = maxDb - minDb;
            for (float db = minDb + stepDb; db < maxDb; db += stepDb)
            {
                float y = height - (db - minDb) / range * height;
                gds.DrawLine(0, y, width, y, textColor.A(30), 1f, dash);
                gds.DrawText($"{db} dB", 4, y - 18, textColor.A(150), textFormat);
            }

            // 对数频率网格
            double logMin = Math.Log10(MinFreq);
            double logMax = Math.Log10(MaxFreq);
            double visibleRange = logMax - logMin;

            float lastLabelRight = -9999;
            float baseFont = 11f + (float)Math.Clamp((1.2 - visibleRange) * 4f, -1f, 5f);

            double[] baseMultipliers = { 1, 2, 3, 5, 7 };

            for (int oct = (int)Math.Floor(logMin); oct <= (int)Math.Ceiling(logMax); oct++)
            {
                double baseFreq = Math.Pow(10, oct);

                double[] subMultipliers;
                if (visibleRange < 0.5)
                    subMultipliers = new[] { 1, 1.1, 1.25, 1.5, 1.75, 2, 2.5, 3, 4, 5, 6, 7, 8, 9 };
                else if (visibleRange < 1.0)
                    subMultipliers = new[] { 1, 1.25, 1.5, 2, 3, 5, 7, 9 };
                else if (visibleRange < 2.0)
                    subMultipliers = new[] { 1d, 2, 3, 5, 7 };
                else
                    subMultipliers = baseMultipliers;

                foreach (var m in subMultipliers)
                {
                    double f = baseFreq * m;
                    if (f < MinFreq || f > MaxFreq) continue;

                    float x = (float)((Math.Log10(f) - logMin) / (logMax - logMin) * width);

                    bool isMain = Math.Abs(m - 1) < 0.001;
                    float alpha = isMain ? 60f : 40f;
                    float thickness = isMain ? 1.5f : 0.7f;

                    // 高频淡化
                    if (f > 10000)
                    {
                        float reduce = (float)Math.Clamp(1.0 - (f - 10000) / 15000.0, 0.3, 1.0);
                        alpha *= reduce;
                    }

                    if (f != 20)
                        gds.DrawLine(x, 0, x, height, textColor.A((byte)alpha), thickness, dash);

                    // 绘制文字标签
                    string label = f >= 1000 ? $"{f / 1000:0.#}kHz" : $"{(int)f}Hz";
                    using var layout = new CanvasTextLayout(gds, label, textFormat, 100, 20);
                    float labelWidth = (float)layout.DrawBounds.Width;

                    float textX = x + 4;
                    float textY = height - (16 + baseFont * 0.4f);

                    // 若文字超出右边界，则左移回画布内
                    bool nearRightEdge = textX + labelWidth > width - 4;
                    if (nearRightEdge)
                        textX = width - labelWidth - 4;

                    // 如果与前一个标签重叠，跳过绘制
                    if (textX < lastLabelRight + labelWidth * 0.6f)
                        continue;

                    gds.DrawText(label, textX, textY, textColor.A(180), textFormat);
                    lastLabelRight = textX + labelWidth;
                }
            }

            ds.DrawImage(_gridCache);
        }

        private void DrawEQPoints(CanvasDrawingSession ds, float w, float h, double logMin, double logMax)
        {
            // 绘制 Parametric EQ 点
            if (AudioFilterStatic.ParametricEqEnable)
            {
                foreach (var eq in AudioFilterStatic.ParametricEqDatas)
                {
                    if (!eq.IsEnable) continue;
                    float x = (float)((Math.Log10(eq.CentreFrequency) - logMin) / (logMax - logMin) * w);
                    float y = h / 2 - eq.Gain / 24f * h / 2;
                    ds.FillCircle(x, y, (float)DrawEqPointsRadius, eq.Color.A(200));
                }
            }

            // 绘制 PassFilter 点
            if (AudioFilterStatic.PassFilterEqEnable)
            {
                foreach (var pf in AudioFilterStatic.PassFilterDatas)
                {
                    if (!pf.IsEnable) continue;
                    float x = (float)((Math.Log10(pf.CentreFrequency) - logMin) / (logMax - logMin) * w);
                    float y = h / 2 - pf.Gain / 24f * h / 2;
                    ds.FillCircle(x, y, (float)DrawEqPointsRadius, pf.Color.A(200));
                }
            }
        }

        private void DrawEqResponseCurve(CanvasDrawingSession ds, float w, float h)
        {
            if (!AudioFilterStatic.ParametricEqEnable && !AudioFilterStatic.PassFilterEqEnable)
                return;

            int points = 512; // 曲线精度
            Vector2[] curve = new Vector2[points];
            double logMin = Math.Log10(MinFreq);
            double logMax = Math.Log10(MaxFreq);

            for (int i = 0; i < points; i++)
            {
                double logFreq = logMin + (double)i / (points - 1) * (logMax - logMin);
                float freq = (float)Math.Pow(10, logFreq);

                double totalDb = 0.0;

                // PassFilter（dB叠加，避免数值溢出）
                if (AudioFilterStatic.PassFilterEqEnable)
                {
                    double fs = App.Instance.AudioService?.FileReader?.WaveFormat.SampleRate ?? 44100;

                    foreach (var pass in AudioFilterStatic.PassFilterDatas)
                    {
                        if (!pass.IsEnable) continue;

                        int slope = pass.SlopeDbPerOct;
                        int stages = Math.Max(1, slope / 12);
                        double f0 = pass.CentreFrequency;
                        double Q = pass.Q;
                        double gainDb = pass.Gain;

                        for (int s = 0; s < stages; s++)
                        {
                            double width = freq / f0;
                            double db = 0.0;

                            switch (pass.PassFilterType)
                            {
                                case PassFilterType.LowPass:
                                    {
                                        double num = 1.0;
                                        double den = Math.Sqrt(Math.Pow(1 - Math.Pow(width, 2), 2) + Math.Pow(width / Q, 2));
                                        double mag = num / den;
                                        db = 20 * Math.Log10(mag);
                                    }
                                    break;

                                case PassFilterType.HighPass:
                                    {
                                        double num = Math.Pow(width, 2);
                                        double den = Math.Sqrt(Math.Pow(1 - Math.Pow(width, 2), 2) + Math.Pow(width / Q, 2));
                                        double mag = num / den;
                                        db = 20 * Math.Log10(mag);
                                    }
                                    break;

                                case PassFilterType.LowShelf:
                                    {
                                        double x1 = freq / f0;
                                        double mag = gainDb * (1 / (1 + Math.Pow(x1, 2 * Q)));
                                        db = mag;
                                    }
                                    break;

                                case PassFilterType.HighShelf:
                                    {
                                        double x1 = freq / f0;
                                        double mag = gainDb * (1 - 1 / (1 + Math.Pow(x1, 2 * Q)));
                                        db = mag;
                                    }
                                    break;

                                case PassFilterType.BandPassPeak:
                                    {
                                        double num = width / Q;
                                        double den = Math.Sqrt(Math.Pow(1 - Math.Pow(width, 2), 2) + Math.Pow(width / Q, 2));
                                        double mag = num / den;
                                        db = 20 * Math.Log10(mag);
                                    }
                                    break;

                                case PassFilterType.BandPassSkirt:
                                    {
                                        double num = Q * width;
                                        double den = Math.Sqrt(Math.Pow(1 - Math.Pow(width, 2), 2) + Math.Pow(width * Q, 2));
                                        double mag = num / den;
                                        db = 20 * Math.Log10(mag);
                                    }
                                    break;

                                case PassFilterType.Notch:
                                    {
                                        double num = Math.Abs(1 - Math.Pow(width, 2));
                                        double den = Math.Sqrt(Math.Pow(1 - Math.Pow(width, 2), 2) + Math.Pow(width / Q, 2));
                                        double mag = num / den;
                                        db = 20 * Math.Log10(mag);
                                    }
                                    break;

                                case PassFilterType.AllPass:
                                    db = 0.0;
                                    break;

                                default:
                                    db = 0.0;
                                    break;
                            }

                            totalDb += db;
                        }
                    }
                }

                // ParametricEq（dB直接相加）
                if (AudioFilterStatic.ParametricEqEnable)
                {
                    double parametricDb = 0;
                    foreach (var eq in AudioFilterStatic.ParametricEqDatas)
                    {
                        if (!eq.IsEnable) continue;
                        parametricDb += GetEqBandGainDb(freq, eq);
                    }
                    totalDb += parametricDb;
                }

                float x = (float)(i / (double)(points - 1) * w);
                float y = h / 2 - (float)(totalDb / 24.0 * (h / 2));
                curve[i] = new Vector2(x, y);
            }

            // curve 是 List<Vector2> 或 Vector2[]，包含连续点
            if (curve == null || curve.Length < 2)
                return;

            // 创建路径
            using (var pathBuilder = new CanvasPathBuilder(ds))
            {
                pathBuilder.BeginFigure(curve[0]);

                for (int i = 1; i < curve.Length; i++)
                    pathBuilder.AddLine(curve[i]);

                pathBuilder.EndFigure(CanvasFigureLoop.Open);

                // 绘制曲线
                using (var geometry = CanvasGeometry.CreatePath(pathBuilder))
                {
                    ds.DrawGeometry(geometry, App.Instance.PlayingListService.TextColor.A(180), (float)DrawEqLinesStrokeWidth);
                }
            }
        }

        private void UpdateSpectrumCache(int sampleRate, int barCount, double minFreq, double maxFreq, float w, float h, SpectrumAnalyzer analyzer)
        {
            // 检查缓存是否有效
            if (_barCache != null &&
                _barCache.Length == barCount &&
                _cacheSampleRate == sampleRate &&
                Math.Abs(_cacheWidth - w) < 0.1f &&
                Math.Abs(_cacheMinFreq - minFreq) < 0.001 &&
                Math.Abs(_cacheMaxFreq - maxFreq) < 0.001 &&
                Math.Abs(_cacheTiltDbPerOct - TiltDbPerOct) < 0.001)
            {
                return;
            }

            if (_barCache == null || _barCache.Length != barCount)
                _barCache = new SpectrumBarCache[barCount];

            // 开始数学计算
            double logMin = Math.Log10(minFreq);
            double logMax = Math.Log10(maxFreq);
            double binScale = (analyzer.Spectrum.Length - 1) / (sampleRate / 2.0);
            double tiltDbPerOct = TiltDbPerOct;
            double slopeDbPerDec = -tiltDbPerOct * 3.32;
            const double refFreq = 1000.0;

            double a = minFreq == 20 ? 0.6 : 1;

            double prevEdgeFreq = Math.Pow(10, logMin); // Start with i=0
            for (int i = 0; i < barCount; i++)
            {
                // 计算频率边界
                double tNext = (double)(i + 1) / barCount;
                double curvedTNext = Math.Pow(tNext, a);
                double nextEdgeFreq = Math.Pow(10, logMin + (logMax - logMin) * curvedTNext);

                double fStart = prevEdgeFreq;
                double fEnd = nextEdgeFreq;
                prevEdgeFreq = nextEdgeFreq; // 为下一次循环做准备

                // 计算 Bin 索引
                _barCache[i].BinStart = Math.Clamp((int)(fStart * binScale), 0, analyzer.Spectrum.Length - 1);
                _barCache[i].BinEnd = Math.Clamp((int)(fEnd * binScale), 0, analyzer.Spectrum.Length - 1);
                _barCache[i].Count = _barCache[i].BinEnd - _barCache[i].BinStart + 1;

                // 计算中心频率用于 X 坐标和 Tilt
                double freqCenter = Math.Sqrt(fStart * fEnd);

                // 计算 X 坐标
                _barCache[i].X = (float)((Math.Log10(freqCenter) - logMin) / (logMax - logMin) * w);

                // 计算 Tilt 修正值 (预先算好系数)
                // 原始公式：normalized + tiltOffset * normalized
                // tiltOffset = (slope / range) * decades
                double decadesFromRef = Math.Log10(freqCenter / refFreq);
                double dbRange = analyzer.MaxDb - analyzer.MinDb;
                _barCache[i].TiltFactor = (float)((slopeDbPerDec / dbRange) * decadesFromRef);
            }

            // 修正首尾 X 坐标
            _barCache[0].X = 0;
            _barCache[^1].X = w;

            // 更新缓存标记
            _cacheSampleRate = sampleRate;
            _cacheWidth = w;
            _cacheMinFreq = minFreq;
            _cacheMaxFreq = maxFreq;
            _cacheTiltDbPerOct = TiltDbPerOct;
        }
        #endregion
    }
}
