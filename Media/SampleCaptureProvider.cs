namespace TewiMP.Media;

using NAudio.Wave;
using System;
using System.Numerics;

public class AudioAnalyzer : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly int _fftSize;
    private readonly float[] _windowBuffer;
    private int _writePos;
    private readonly float[] _hannWindow;
    private readonly float[] _fftBuffer;

    public float[] Spectrum { get; private set; }

    public AudioAnalyzer(ISampleProvider source, int fftSize = 2048)
    {
        _source = source;
        _fftSize = fftSize;
        _windowBuffer = new float[fftSize];
        Spectrum = new float[fftSize / 2];

        // 汉宁窗
        _hannWindow = new float[fftSize];
        for (int i = 0; i < fftSize; i++)
            _hannWindow[i] = 0.5f - 0.5f * (float)Math.Cos(2 * Math.PI * i / fftSize);

        _fftBuffer = new float[_fftSize];
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int read = _source.Read(buffer, offset, count);
        if (read <= 0) return read;

        int channels = _source.WaveFormat.Channels;

        // 写入滑动窗口并合并立体声
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
        // 先清空
        Array.Clear(_fftBuffer, 0, _fftSize);

        // 将最新样本顺序复制到 fftBuffer
        for (int i = 0; i < _fftSize; i++)
        {
            int idx = (_writePos + i) % _fftSize;
            _fftBuffer[i] = _windowBuffer[idx] * _hannWindow[i]; // 同时应用汉宁窗
        }

        // 汉宁窗
        for (int i = 0; i < _fftSize; i++)
            _fftBuffer[i] *= _hannWindow[i];

        // FFT
        Complex[] complex = new Complex[_fftSize];
        for (int i = 0; i < _fftSize; i++)
            complex[i] = new Complex(_fftBuffer[i], 0);
        FFT(complex);

        // RMS 归一化
        double rms = 0;
        for (int i = 0; i < _fftSize; i++)
            rms += _fftBuffer[i] * _fftBuffer[i];
        rms = Math.Sqrt(rms / _fftSize);
        if (rms < 1e-10) rms = 1e-10;

        // 计算 dB
        for (int i = 0; i < _fftSize / 2; i++)
        {
            double mag = complex[i].Magnitude / (_fftSize / 2.0);
            double db = 20 * Math.Log10(mag / rms);
            db = Math.Max(-60, Math.Min(0, db));
            Spectrum[i] = (float)db;
        }
    }

    private void FFT(Complex[] buffer)
    {
        int n = buffer.Length;
        if (n <= 1) return;

        Complex[] even = new Complex[n / 2];
        Complex[] odd = new Complex[n / 2];
        for (int i = 0; i < n / 2; i++)
        {
            even[i] = buffer[i * 2];
            odd[i] = buffer[i * 2 + 1];
        }

        FFT(even);
        FFT(odd);

        for (int k = 0; k < n / 2; k++)
        {
            Complex t = Complex.Exp(-Complex.ImaginaryOne * 2 * Math.PI * k / n) * odd[k];
            buffer[k] = even[k] + t;
            buffer[k + n / 2] = even[k] - t;
        }
    }

    public WaveFormat WaveFormat => _source.WaveFormat;
}
