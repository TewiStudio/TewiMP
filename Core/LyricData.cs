using System;
using System.Collections.Generic;

namespace TewiMP.Core;

public class LyricData : OnlyClass
{
    public List<string> Lyric { get; set; }
    public string Romaji { get; set; }
    public TimeSpan LyricTimeSpan { get; set; }
    string lyricAllString = null;
    public string LyricAllString
    {
        get
        {
            if (lyricAllString is null && Lyric != null)
            {
                lyricAllString = string.Join("\n", Lyric);
            }
            return lyricAllString;
        }
    }
    public LyricData(List<string> lyric, string translate, TimeSpan timeSpan)
    {
        Lyric = lyric;
        LyricTimeSpan = timeSpan;
    }

    public override string GetMD5()
    {
        if (Lyric is null) return null;
        return $"{string.Join(' ', Lyric)}{Lyric.Count}{LyricTimeSpan.Ticks}";
    }
}
