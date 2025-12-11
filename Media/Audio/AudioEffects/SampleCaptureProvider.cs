namespace TewiMP.Media.Audio.AudioEffects;

using NAudio.Wave;
using System;
using System.Numerics;

public class SpectrumAnalyzer : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly int _fftSize;
    private readonly float[] _windowBuffer;
    private readonly float[] _hannWindow;
    private readonly float[] _fftBuffer;
    private readonly Complex[] _complexBuffer;
    private int _writePos;

    public float[] Spectrum { get; }

    public WaveFormat WaveFormat => _source.WaveFormat;

    public float MinDb { get; set; } = -90f;
    public float MaxDb { get; set; } = 0f;

    public SpectrumAnalyzer(ISampleProvider source, int fftSize = 2048)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _fftSize = fftSize;

        _windowBuffer = new float[_fftSize];
        _hannWindow = new float[_fftSize];
        _fftBuffer = new float[_fftSize];
        _complexBuffer = new Complex[_fftSize];
        Spectrum = new float[_fftSize / 2];

        // 预计算汉宁窗
        for (int i = 0; i < _fftSize; i++)
            _hannWindow[i] = 0.5f * (1f - (float)Math.Cos(2 * Math.PI * i / (_fftSize - 1)));
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int read = _source.Read(buffer, offset, count);
        if (read <= 0) return read;

        int channels = _source.WaveFormat.Channels;

        // 写入滑动窗口并合并多声道
        for (int i = 0; i < read; i += channels)
        {
            float sample = 0;
            for (int c = 0; c < channels; c++)
                sample += buffer[offset + i + c];
            sample /= channels;

            _windowBuffer[_writePos] = sample;
            _writePos = (_writePos + 1) % _fftSize;
        }

        return read;
    }

    public void Analyze()
    {
        // 加窗复制到 FFT 缓冲区
        for (int i = 0; i < _fftSize; i++)
        {
            int idx = (_writePos + i) % _fftSize;
            _fftBuffer[i] = _windowBuffer[idx] * _hannWindow[i];
        }

        // 转换为复数形式
        for (int i = 0; i < _fftSize; i++)
            _complexBuffer[i] = new Complex(_fftBuffer[i], 0);

        // 执行就地 FFT（无分配）
        FFTInPlace(_complexBuffer);

        // 计算分贝
        const double refValue = 1e-10;
        double norm = 2.0 / _fftSize;

        for (int i = 0; i < _fftSize / 2; i++)
        {
            double mag = _complexBuffer[i].Magnitude * norm;
            double db = 20.0 * Math.Log10(Math.Max(mag, refValue));

            // 动态范围裁剪
            db = Math.Max(MinDb, Math.Min(MaxDb, db));
            Spectrum[i] = (float)db;
        }
    }

    private static void FFTInPlace(Complex[] buffer)
    {
        int n = buffer.Length;
        int bits = (int)Math.Log2(n);

        // Bit-reversal 重排
        for (int j = 1, i = 0; j < n; j++)
        {
            int bit = n >> 1;
            for (; (i & bit) != 0; bit >>= 1)
                i &= ~bit;
            i |= bit;

            if (j < i)
            {
                var temp = buffer[j];
                buffer[j] = buffer[i];
                buffer[i] = temp;
            }
        }

        // 蝶形运算
        for (int len = 2; len <= n; len <<= 1)
        {
            double ang = -2 * Math.PI / len;
            double wlenReal = Math.Cos(ang);
            double wlenImag = Math.Sin(ang);

            for (int i = 0; i < n; i += len)
            {
                double wReal = 1;
                double wImag = 0;

                for (int j = 0; j < len / 2; j++)
                {
                    int u = i + j;
                    int v = i + j + len / 2;

                    double ur = buffer[u].Real;
                    double ui = buffer[u].Imaginary;
                    double vr = buffer[v].Real * wReal - buffer[v].Imaginary * wImag;
                    double vi = buffer[v].Real * wImag + buffer[v].Imaginary * wReal;

                    buffer[u] = new Complex(ur + vr, ui + vi);
                    buffer[v] = new Complex(ur - vr, ui - vi);

                    double nextWReal = wReal * wlenReal - wImag * wlenImag;
                    double nextWImag = wReal * wlenImag + wImag * wlenReal;
                    wReal = nextWReal;
                    wImag = nextWImag;
                }
            }
        }
    }
}

/// <summary>
/// 提供音量控制的采样提供器。
/// 可插入到任意 ISampleProvider 音频流中。
/// </summary>
public class VolumeSampleProvider : ISampleProvider
{
    private readonly ISampleProvider sourceProvider;
    private float volume = 1.0f;

    public VolumeSampleProvider(ISampleProvider source)
    {
        sourceProvider = source ?? throw new ArgumentNullException(nameof(source));
        WaveFormat = source.WaveFormat;
    }

    /// <summary>
    /// 当前音量（0.0 静音，1.0 原始音量，>1.0 可放大）
    /// </summary>
    public float Volume
    {
        get => volume;
        set => volume = Math.Clamp(value, 0f, 10f); // 限制范围防止过大失真
    }

    public WaveFormat WaveFormat { get; }

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = sourceProvider.Read(buffer, offset, count);
        if (volume != 1.0f && samplesRead > 0)
        {
            for (int n = 0; n < samplesRead; n++)
            {
                buffer[offset + n] *= volume;
            }
        }
        return samplesRead;
    }
}