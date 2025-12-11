using System;
using TewiMP.Core.Music;

namespace TewiMP.Core;

public class SongHistoryData : OnlyClass
{
    public MusicData MusicData { get; set; }
    public DateTime Time { get; set; }
    public int Count { get; set; } = 0;

    public override string GetMD5()
    {
        return $"{MusicData}{Time}";
    }
}
