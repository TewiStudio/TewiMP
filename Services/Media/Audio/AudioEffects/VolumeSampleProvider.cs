using System;
using NAudio.Wave;

namespace TewiMP.Services.Media.Audio.AudioEffects;

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