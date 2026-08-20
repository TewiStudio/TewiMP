using System;
using NAudio.Wave;

namespace TewiMP.Services.Media.Audio.AudioEffects;

public class SpectrumAnalyzer : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly int _fftSize;
    private readonly int _fftSizeHalf;
    private readonly int _logN;

    // 缓存区
    private readonly float[] _windowBuffer; // 环形缓冲区
    private readonly float[] _hannWindow;   // 汉宁窗缓存

    // FFT 计算专用数组
    // 分离实部和虚部比 Complex[] 更快，且利于 SIMD 优化
    private readonly float[] _re;
    private readonly float[] _im;

    // 预计算查找表
    private readonly int[] _bitReverseTable;
    private readonly float[] _sinTable;
    private readonly float[] _cosTable;

    private int _writePos;
    private readonly object _bufferLock = new object(); // 简单的线程同步

    public float[] Spectrum { get; }
    public WaveFormat WaveFormat => _source.WaveFormat;

    // 预计算常数
    private readonly float _dbScale;

    public float MinDb { get; set; } = -90f;
    public float MaxDb { get; set; } = 0f;

    public SpectrumAnalyzer(ISampleProvider source, int fftSize = 2048)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _fftSize = fftSize;
        _fftSizeHalf = fftSize / 2;
        _logN = (int)Math.Log2(fftSize);

        _windowBuffer = new float[_fftSize];
        _hannWindow = new float[_fftSize];

        _re = new float[_fftSize];
        _im = new float[_fftSize];

        Spectrum = new float[_fftSizeHalf];

        // 预计算归一化系数 (2/N)
        // Log10(Mag * 2/N) -> Log10(Mag) + Log10(2/N)
        // 在最后计算 DB 时统一处理

        // 预计算汉宁窗
        for (int i = 0; i < _fftSize; i++)
        {
            _hannWindow[i] = 0.5f * (1f - (float)Math.Cos(2 * Math.PI * i / (_fftSize - 1)));
        }

        // 预计算位反转表 (Bit-Reversal)
        _bitReverseTable = new int[_fftSize];
        for (int i = 0; i < _fftSize; i++)
        {
            int rev = 0;
            int n = i;
            for (int j = 0; j < _logN; j++)
            {
                rev = (rev << 1) | (n & 1);
                n >>= 1;
            }
            _bitReverseTable[i] = rev;
        }

        // 预计算三角函数表
        // FFT 需要计算 logN 层
        // 每一层需要的旋转因子预先算好，这里为了通用性，缓存最大的旋转因子
        // 实际上只需要计算 [0..N/2] 的 sin/cos
        _sinTable = new float[_fftSize / 2];
        _cosTable = new float[_fftSize / 2];
        for (int i = 0; i < _fftSize / 2; i++)
        {
            double angle = -2 * Math.PI * i / _fftSize;
            _cosTable[i] = (float)Math.Cos(angle);
            _sinTable[i] = (float)Math.Sin(angle);
        }
    }

    public int Read(Span<float> buffer)
    {
        int read = _source.Read(buffer);
        if (read <= 0) return read;

        int channels = _source.WaveFormat.Channels;

        // 简单的锁，防止读取和写入发生严重的重叠
        // 虽然在音频处理中锁是禁忌，但这里的拷贝操作极快，且仅在 Analyze 时竞争
        lock (_bufferLock)
        {
            // 只有当 buffer 写到尽头时才回绕
            int sourceIdx = 0;
            while (sourceIdx < read)
            {
                // 混合声道
                float sample = 0;
                // 手动展开常用声道数循环
                if (channels == 2)
                {
                    sample = (buffer[sourceIdx] + buffer[sourceIdx + 1]) * 0.5f;
                    sourceIdx += 2;
                }
                else
                {
                    for (int c = 0; c < channels; c++)
                        sample += buffer[sourceIdx + c];
                    sample /= channels;
                    sourceIdx += channels;
                }

                _windowBuffer[_writePos++] = sample;
                if (_writePos >= _fftSize) _writePos = 0;
            }
        }

        return read;
    }

    public void Analyze()
    {
        // 拷贝并加窗
        lock (_bufferLock)
        {
            int pos = _writePos;
            // 分两段拷贝，避免使用取模运算 %
            // 第一段：从 writePos 到 结尾
            int part1Len = _fftSize - pos;
            for (int i = 0; i < part1Len; i++)
            {
                _re[i] = _windowBuffer[pos + i] * _hannWindow[i];
                _im[i] = 0; // 清空虚部
            }
            // 第二段：从 0 到 writePos
            for (int i = 0; i < pos; i++)
            {
                int destIdx = part1Len + i;
                _re[destIdx] = _windowBuffer[i] * _hannWindow[destIdx];
                _im[destIdx] = 0;
            }
        }

        FFT();

        // 计算分贝
        // Magnitude = Sqrt(re^2 + im^2)
        // dB = 20 * Log10(Mag * Norm)
        //    = 10 * Log10((re^2 + im^2) * Norm^2)
        // 这样避免了 Sqrt 开方运算

        double norm = 2.0 / _fftSize;
        double normSq = norm * norm;
        double minMagsSq = Math.Pow(10, MinDb / 10.0) / normSq; // 预先转换 MinDb 到线性能量值

        // 只需要遍历一半
        for (int i = 0; i < _fftSizeHalf; i++)
        {
            float re = _re[i];
            float im = _im[i];

            // 计算能量
            float magSq = re * re + im * im;

            if (magSq <= 1e-10f) // 极小值保护
            {
                Spectrum[i] = MinDb;
            }
            else
            {
                // 使用 10 * Log10(mag^2)
                double db = 10.0 * Math.Log10(magSq * normSq);
                Spectrum[i] = (float)Math.Clamp(db, MinDb, MaxDb);
            }
        }
    }

    private void FFT()
    {
        // 位反转重排
        // 利用预计算表，避免所有位运算
        for (int i = 0; i < _fftSize; i++)
        {
            int j = _bitReverseTable[i];
            if (j > i)
            {
                float tempRe = _re[j];
                float tempIm = _im[j];
                _re[j] = _re[i];
                _im[j] = _im[i];
                _re[i] = tempRe;
                _im[i] = tempIm;
            }
        }

        // 蝶形运算
        int step = 1;
        for (int level = 0; level < _logN; level++)
        {
            int jump = step << 1;
            // 这一层的旋转因子步长
            int tableStep = _fftSize / jump;

            for (int i = 0; i < step; i++)
            {
                // 查表
                int tableIdx = i * tableStep;
                float wRe = _cosTable[tableIdx];
                float wIm = _sinTable[tableIdx];

                for (int j = i; j < _fftSize; j += jump)
                {
                    int k = j + step;

                    float tRe = _re[k] * wRe - _im[k] * wIm;
                    float tIm = _re[k] * wIm + _im[k] * wRe;

                    float uRe = _re[j];
                    float uIm = _im[j];

                    _re[k] = uRe - tRe;
                    _im[k] = uIm - tIm;
                    _re[j] = uRe + tRe;
                    _im[j] = uIm + tIm;
                }
            }
            step = jump;
        }
    }
}
