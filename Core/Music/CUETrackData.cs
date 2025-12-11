namespace TewiMP.Core.Music;

using System;

public class CUETrackData
{
    public string Path { get; set; } = null;
    public int Index { get; set; } = 0;
    public TimeSpan Duration
    {
        get => EndDuration - StartDuration;
    }
    public TimeSpan StartDuration { get; set; } = default;
    public TimeSpan EndDuration { get; set; } = default;
}
