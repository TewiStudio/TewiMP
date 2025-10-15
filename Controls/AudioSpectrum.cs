#nullable enable
using Meting4Net.Core.Models.Netease;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Numerics;
using TewiMP.Helpers;
using TewiMP.Media;
using Windows.UI;
using static Vanara.PInvoke.Gdi32;

namespace TewiMP.Controls
{
    public sealed partial class AudioSpectrum : Control
    {
        private CanvasControl _spectrumCanvas;

        // ===== 缓存 =====
        private float[] _smoothedSpectrum;
        private float[] _pointsX;
        private float[] _pointsY;
        private float[] _smoothedPoints;
        private CanvasLinearGradientBrush _gradientBrush;
        private CanvasRenderTarget _gridCache;
        private double _lastWidth, _lastHeight;
        private double _lastMinFreq, _lastMaxFreq;
        private Color _lastAccentColor;
        private EQData? _draggingEQ = null;
        private PassFilterData? _draggingPass = null;
        private const float EQHitRadius = 10f;
        private const float MinFreqHz = 20f;
        private const float MaxFreqHz = 22000f;
        private const float MinQ = 0.3f;
        private const float MaxQ = 33.3f;
        private const float QStep = 0.1f;

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

        // ===== 鼠标 =====
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

        public static readonly DependencyProperty SmoothingFactorProperty = DependencyProperty.Register(
            "SmoothingFactor", typeof(double), typeof(AudioSpectrum),
            new PropertyMetadata(.13d, OnPropertyChanged<double>));

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

        public static readonly DependencyProperty DrawDbLinesProperty = DependencyProperty.Register(
            "DrawDbLines", typeof(bool), typeof(AudioSpectrum),
            new PropertyMetadata(false, OnPropertyChanged<bool>));

        public static readonly DependencyProperty DrawEqLinesProperty = DependencyProperty.Register(
            "DrawEqLines", typeof(bool), typeof(AudioSpectrum),
            new PropertyMetadata(false, OnPropertyChanged<bool>));

        public static readonly DependencyProperty DrawEqPointsProperty = DependencyProperty.Register(
            "DrawEqPoints", typeof(bool), typeof(AudioSpectrum),
            new PropertyMetadata(false, OnPropertyChanged<bool>));

        public bool IsStop { get => (bool)GetValue(IsStopProperty); set => SetValue(IsStopProperty, value); }
        public int SampleCount { get => (int)GetValue(SampleCountProperty); set => SetValue(SampleCountProperty, value); }
        public double SmoothingFactor { get => (double)GetValue(SmoothingFactorProperty); set => SetValue(SmoothingFactorProperty, value); }
        public int SmoothWindow { get => (int)GetValue(SmoothWindowProperty); set => SetValue(SmoothWindowProperty, value); }
        public double TiltDbPerOct { get => (double)GetValue(TiltDbPerOctProperty); set => SetValue(TiltDbPerOctProperty, value); }
        public double StrokeWidth { get => (double)GetValue(StrokeWidthProperty); set => SetValue(StrokeWidthProperty, value); }
        public double MinFreq { get => (double)GetValue(MinFreqProperty); set => SetValue(MinFreqProperty, value); }
        public double MaxFreq { get => (double)GetValue(MaxFreqProperty); set => SetValue(MaxFreqProperty, value); }
        public bool DrawDbLines { get => (bool)GetValue(DrawDbLinesProperty); set => SetValue(DrawDbLinesProperty, value); }
        public bool DrawEqLines { get => (bool)GetValue(DrawEqLinesProperty); set => SetValue(DrawEqLinesProperty, value); }
        public bool DrawEqPoints { get => (bool)GetValue(DrawEqPointsProperty); set => SetValue(DrawEqPointsProperty, value); }

        private static void OnPropertyChanged<T>(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AudioSpectrum spectrum) spectrum._spectrumCanvas?.Invalidate();
        }
        #endregion

        public AudioSpectrum()
        {
            DefaultStyleKey = typeof(AudioSpectrum);
        }

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

                App.Instance.AudioPlayer.VolumeMeter -= AudioPlayer_VolumeMeter;
                App.Instance.AudioPlayer.VolumeMeter += AudioPlayer_VolumeMeter;
            }
        }

        private void AudioPlayer_VolumeMeter(Media.AudioPlayer audioPlayer, float[] sample)
        {
            if (Visibility == Visibility.Visible)
                _spectrumCanvas.Invalidate();
        }

        private void AudioSpectrum_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _spectrumCanvas.Width = (float)ActualWidth;
            _spectrumCanvas.Height = (float)ActualHeight;
        }

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

        private EQData? _hoveringEQ;
        private PassFilterData? _hoveringPass;
        private void SpectrumCanvas_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (!DrawDbLines) return;

            var pos = e.GetCurrentPoint(sender as UIElement).Position;
            _hoverX = (float)pos.X;
            _hoverY = (float)pos.Y;
            UpdateHoverData();

            // --- 更新悬停 EQ 或 PassFilter ---
            _hoveringEQ = HitTestEQ(_hoverX, _hoverY);
            _hoveringPass = HitTestPassFilter(_hoverX, _hoverY);

            if (!_isDragging && _draggingEQ == null && _draggingPass == null)
                return;

            float width = (float)_spectrumCanvas.ActualWidth;
            float height = (float)_spectrumCanvas.ActualHeight;
            double logMin = Math.Log10(MinFreq);
            double logMax = Math.Log10(MaxFreq);

            // === 拖动 EQ 点 ===
            if (_draggingEQ != null)
            {
                double logFreq = logMin + (_hoverX / width) * (logMax - logMin);
                float freq = (float)Math.Pow(10, logFreq);
                _draggingEQ.CentreFrequency = Math.Clamp(freq, MinFreqHz, MaxFreqHz);

                float gain = (height / 2 - _hoverY) / (height / 2) * 24f;
                _draggingEQ.Gain = Math.Clamp(gain, -24f, 24f);
                return;
            }

            // === 拖动 PassFilter 点 ===
            if (_draggingPass != null)
            {
                double logFreq = logMin + (_hoverX / width) * (logMax - logMin);
                float freq = (float)Math.Pow(10, logFreq);
                _draggingPass.CentreFrequency = Math.Clamp(freq, MinFreqHz, MaxFreqHz);

                if (_draggingPass.PassFilterType is PassFilterType.LowShelf or PassFilterType.HighShelf)
                {
                    float gain = (height / 2 - _hoverY) / (height / 2) * 24f;
                    _draggingPass.Gain = Math.Clamp(gain, -24f, 24f);
                }
                else
                {
                    if (_draggingPass.Gain != 0 ) _draggingPass.Gain = 0;
                }

                return;
            }

            // === 拖动频谱 ===
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
            if (ctrlDown)
            {
                var pos = e.GetCurrentPoint(_spectrumCanvas).Position;
                ZoomSpectrum((float)pos.X, delta);
                return;
            }

            // === EQ 调整模式 ===
            if (_hoveringEQ != null)
            {
                float newQ = _hoveringEQ.Q + (delta > 0 ? QStep : -QStep);
                _hoveringEQ.Q = Math.Clamp(newQ, MinQ, MaxQ);
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
                    _hoveringPass.Q = Math.Clamp(newQ, MinQ, MaxQ);
                }
                return;
            }

            // === 默认行为：频谱缩放 ===
            var pointerPos = e.GetCurrentPoint(_spectrumCanvas).Position;
            ZoomSpectrum((float)pointerPos.X, delta);
        }
        #endregion

        #region 缩放 & hover
        private void ZoomSpectrum(float mouseX, int delta)
        {
            if (_spectrumCanvas == null || _spectrumCanvas.ActualWidth <= 0) return;

            // --- 缩放强度 ---
            double baseFactor = 1.35;
            double step = Math.Abs(delta) / 120.0;
            double scaleFactor = Math.Pow(baseFactor, step);
            if (delta < 0) scaleFactor = 1.0 / scaleFactor;

            // --- 当前频率范围 ---
            double logMin = Math.Log10(MinFreq);
            double logMax = Math.Log10(MaxFreq);
            double logRange = logMax - logMin;

            // 鼠标所在频率
            double mouseRatio = mouseX / _spectrumCanvas.ActualWidth;
            double logMouse = logMin + mouseRatio * logRange;
            double mouseFreq = Math.Pow(10, logMouse);

            // --- 新范围 ---
            double newLogRange = logRange / scaleFactor;

            // 缩放限制
            double curRangeHz = Math.Pow(10, logMax) - Math.Pow(10, logMin);
            if (curRangeHz < MinZoomRangeHz && delta > 0) return;
            if (curRangeHz > MaxZoomRangeHz && delta < 0) return;

            // 保证锚点频率不偏移
            double newLogMin = logMouse - mouseRatio * newLogRange;
            double newLogMax = newLogMin + newLogRange;

            // --- 限制边界 ---
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

            // --- 应用动画目标 ---
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

            var analyzer = App.Instance.AudioPlayer.AudioAnalyzer;
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

            double logMin = Math.Log10(MinFreq);
            double logMax = Math.Log10(MaxFreq);

            DrawSpectrum(sender, ds);

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
                DrawGrid(sender, ds);
            }
        }

        // 内部插值函数
        static double Lerp(double a, double b, double t) => a + (b - a) * t;

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

            // ===== 初始化缓存 =====
            if (_smoothedSpectrum == null || _smoothedSpectrum.Length != spectrum.Length)
                _smoothedSpectrum = new float[spectrum.Length];

            if (_pointsY == null || _pointsY.Length != barCount)
            {
                _pointsY = new float[barCount];
                _smoothedPoints = new float[barCount];
                _pointsX = new float[barCount];
            }

            // ===== 渐变刷 =====
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

            // ===== 平滑频谱值 =====
            float smoothingFactor = (float)SmoothingFactor;
            for (int i = 0; i < spectrum.Length; i++)
                _smoothedSpectrum[i] = _smoothedSpectrum[i] * (1 - smoothingFactor) + spectrum[i] * smoothingFactor;

            // ===== 对数频率映射参数 =====
            double logMin = Math.Log10(minFreq);
            double logMax = Math.Log10(maxFreq);
            double binScale = (_smoothedSpectrum.Length - 1) / (sampleRate / 2.0);

            // ===== Tilt 参数 =====
            double tiltDbPerOct = TiltDbPerOct;
            double slopeDbPerDec = -tiltDbPerOct * 3.32; // 每十倍频变化斜率
            const double refFreq = 1000.0; // 参考频率

            // ===== 低频加密映射函数 =====
            double a = minFreq == 20 ? 0.6 : 1; // <1 表示低频更密
            Func<double, double> curve = t => Math.Pow(t, a);

            // ===== 生成边界频率 =====
            double[] freqEdges = new double[barCount + 1];
            for (int i = 0; i <= barCount; i++)
            {
                double t = (double)i / barCount;
                double curvedT = curve(t);
                freqEdges[i] = Math.Pow(10, logMin + (logMax - logMin) * curvedT);
            }

            // ===== 计算每个条目 =====
            for (int i = 0; i < barCount; i++)
            {
                double fStart = freqEdges[i];
                double fEnd = freqEdges[i + 1];
                double freqCenter = Math.Sqrt(fStart * fEnd);

                int binStart = Math.Clamp((int)(fStart * binScale), 0, _smoothedSpectrum.Length - 1);
                int binEnd = Math.Clamp((int)(fEnd * binScale), 0, _smoothedSpectrum.Length - 1);

                float sum = 0;
                for (int b = binStart; b <= binEnd; b++)
                    sum += _smoothedSpectrum[b];
                float avgDb = sum / (binEnd - binStart + 1);

                // 归一化 + tilt 修正
                float normalized = Math.Clamp((avgDb - analyzer.MinDb) / (analyzer.MaxDb - analyzer.MinDb), 0f, 1f);
                double decadesFromRef = Math.Log10(freqCenter / refFreq);
                float tiltOffset = (float)((slopeDbPerDec / (analyzer.MaxDb - analyzer.MinDb)) * decadesFromRef);
                normalized = Math.Clamp(normalized + tiltOffset * normalized, 0f, 1f);

                _pointsY[i] = h - normalized * h - (float)StrokeWidth / 2f;
                _pointsX[i] = (float)((Math.Log10(freqCenter) - logMin) / (logMax - logMin) * w);
            }

            // ===== 固定边界坐标 =====
            _pointsX[0] = 0;
            _pointsX[^1] = w;

            // ===== 滑动窗口平滑 =====
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

            // ===== 绘制填充 =====
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

            // ===== 绘制曲线 =====
            using (var linePath = new CanvasPathBuilder(sender))
            {
                linePath.BeginFigure(0, _smoothedPoints[0]);
                for (int i = 1; i < barCount; i++)
                    linePath.AddLine(_pointsX[i], _smoothedPoints[i]);
                linePath.AddLine(w, _smoothedPoints[^1]);
                linePath.EndFigure(CanvasFigureLoop.Open);

                ds.DrawGeometry(CanvasGeometry.CreatePath(linePath), accent, (float)StrokeWidth);
            }

            // ===== Hover 提示 =====
            if (_hoverX >= 0)
            {
                UpdateHoverData();
                string hoverText = $"{_hoverFreq:0.#} Hz / {_hoverDb:0.0} dB";
                var textFormat = new CanvasTextFormat { FontSize = 14 };
                var brush = App.Instance.PlayingList.TextColor;

                float textWidth = (float)new CanvasTextLayout(ds, hoverText, textFormat, w, h).DrawBounds.Width;
                float textX = _hoverX + 10;
                if (textX + textWidth > w)
                    textX = w - textWidth - 2;

                float textY = _hoverY - 20;
                if (textY < 0) textY = 0;

                ds.DrawText(hoverText, textX, textY, brush, textFormat);
                ds.DrawLine(_hoverX, 0, _hoverX, h, brush, .5f);
            }
            if (_firstDraw)
            {
                _animMinFreq = MinFreq;
                _animMaxFreq = MaxFreq;
                _targetMinFreq = MinFreq;
                _targetMaxFreq = MaxFreq;
                _firstDraw = false;
            }

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
                    // ease-out 平滑
                    double easedT = 1 - Math.Pow(1 - t, 3);

                    MinFreq = Lerp(_animMinFreq, _targetMinFreq, easedT);
                    MaxFreq = Lerp(_animMaxFreq, _targetMaxFreq, easedT);

                    // 在动画中不断触发重绘
                    _spectrumCanvas.Invalidate();
                }
            }
        }

        private void DrawGrid(CanvasControl sender, CanvasDrawingSession ds)
        {
            if (_gridCache != null && _gridCache.Size.Width == sender.ActualWidth && _gridCache.Size.Height == sender.ActualHeight
                && _lastMinFreq == MinFreq && _lastMaxFreq == MaxFreq)
            {
                ds.DrawImage(_gridCache);
                return;
            }

            _gridCache?.Dispose();
            _gridCache = new CanvasRenderTarget(sender, (float)sender.ActualWidth, (float)sender.ActualHeight, 96);
            _lastMinFreq = MinFreq; _lastMaxFreq = MaxFreq;

            using var gds = _gridCache.CreateDrawingSession();
            gds.Clear(Colors.Transparent);

            // ===== 水平 dB 网格 =====
            float minDb = -90, maxDb = 0, stepDb = 10;
            float width = (float)_gridCache.Size.Width;
            float height = (float)_gridCache.Size.Height;
            var dash = new CanvasStrokeStyle { DashStyle = CanvasDashStyle.Dash };
            var textFormat = new CanvasTextFormat { FontSize = 12 };
            var textColor = App.Instance.PlayingList.TextColor;
            float range = maxDb - minDb;
            for (float db = minDb + stepDb; db < maxDb; db += stepDb)
            {
                float y = height - (db - minDb) / range * height;
                gds.DrawLine(0, y, width, y, textColor.A(30), 1f, dash);
                gds.DrawText($"{db} dB", 4, y - 18, textColor.A(150), textFormat);
            }

            // ===== 对数频率网格 =====
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

        private void DrawEQPoints(CanvasDrawingSession ds, float width, float height, double logMin, double logMax)
        {
            // 绘制 Parametric EQ 点
            if (AudioFilterStatic.ParametricEqEnable)
            {
                foreach (var eq in AudioFilterStatic.ParametricEqDatas)
                {
                    if (!eq.IsEnable) continue;
                    float x = (float)((Math.Log10(eq.CentreFrequency) - logMin) / (logMax - logMin) * width);
                    float y = height / 2 - eq.Gain / 24f * height / 2;
                    ds.FillCircle(x, y, 5f, eq.Color);
                }
            }

            // 绘制 PassFilter 点
            if (AudioFilterStatic.PassFilterEqEnable)
            {
                foreach (var pf in AudioFilterStatic.PassFilterDatas)
                {
                    if (!pf.IsEnable) continue;
                    float x = (float)((Math.Log10(pf.CentreFrequency) - logMin) / (logMax - logMin) * width);
                    float y = height / 2 - pf.Gain / 24f * height / 2;
                    ds.FillCircle(x, y, 6f, pf.Color.A(200));
                }
            }
        }

        private void DrawEqResponseCurve(CanvasDrawingSession ds, float width, float height)
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

                // PassFilter（dB叠加，避免数值溢出）if (AudioFilterStatic.PassFilterEqEnable)
                {
                    double fs = App.Instance.AudioPlayer?.FileReader?.WaveFormat.SampleRate ?? 44100;

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
                            double w = freq / f0;
                            double db = 0.0;

                            switch (pass.PassFilterType)
                            {
                                case PassFilterType.LowPass:
                                    {
                                        double num = 1.0;
                                        double den = Math.Sqrt(Math.Pow(1 - Math.Pow(w, 2), 2) + Math.Pow(w / Q, 2));
                                        double mag = num / den;
                                        db = 20 * Math.Log10(mag);
                                    }
                                    break;

                                case PassFilterType.HighPass:
                                    {
                                        double num = Math.Pow(w, 2);
                                        double den = Math.Sqrt(Math.Pow(1 - Math.Pow(w, 2), 2) + Math.Pow(w / Q, 2));
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
                                        double num = w / Q;
                                        double den = Math.Sqrt(Math.Pow(1 - Math.Pow(w, 2), 2) + Math.Pow(w / Q, 2));
                                        double mag = num / den;
                                        db = 20 * Math.Log10(mag);
                                    }
                                    break;

                                case PassFilterType.BandPassSkirt:
                                    {
                                        double num = Q * w;
                                        double den = Math.Sqrt(Math.Pow(1 - Math.Pow(w, 2), 2) + Math.Pow(w * Q, 2));
                                        double mag = num / den;
                                        db = 20 * Math.Log10(mag);
                                    }
                                    break;

                                case PassFilterType.Notch:
                                    {
                                        double num = Math.Abs(1 - Math.Pow(w, 2));
                                        double den = Math.Sqrt(Math.Pow(1 - Math.Pow(w, 2), 2) + Math.Pow(w / Q, 2));
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

                float x = (float)(i / (double)(points - 1) * width);
                float y = height / 2 - (float)(totalDb / 24.0 * (height / 2));
                curve[i] = new Vector2(x, y);
            }

            // === 用连续线段绘制曲线 ===
            for (int i = 1; i < points; i++)
            {
                ds.DrawLine(curve[i - 1], curve[i], App.Instance.PlayingList.TextColor.A(180), 2f);
            }
        }
        #endregion
    }
}
