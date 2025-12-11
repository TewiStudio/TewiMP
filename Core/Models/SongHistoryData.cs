namespace TewiMP.Core.Models;

using System;
using TewiMP.Core.Models.Music;

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
