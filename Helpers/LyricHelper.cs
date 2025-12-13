using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TewiMP.Core;
using TewiMP.Services.Storage;

namespace TewiMP.Helpers;

public static class LyricHelper
{
    public static Kawazu.KawazuConverter kawazuConverter = new(DataFolderBase.KawazuDicFolder);
    public static string NoneLyricString = "·········";

    public async static Task<LyricData[]> LyricToLrcData(string lyricText, bool useRomaji = true)
    {
        if (string.IsNullOrEmpty(lyricText)) return Array.Empty<LyricData>();

        // 使用 Dictionary 去重，Key 为时间
        // 预估容量设为 50，减少扩容开销
        var lyricDict = new Dictionary<TimeSpan, LyricData>(50);

        // 解析
        using (var reader = new StringReader(lyricText))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // 找到最后一个 ']' 分离时间和歌词
                int lastBracketIndex = line.LastIndexOf(']');
                if (lastBracketIndex == -1) continue;

                // 提取歌词文本
                string content = line.Substring(lastBracketIndex + 1);
                if (string.IsNullOrWhiteSpace(content) || content == "...") content = NoneLyricString;

                // 提取时间部分
                string timePart = line.Substring(0, lastBracketIndex + 1);
                var timeTags = timePart.Split(']', StringSplitOptions.RemoveEmptyEntries);

                foreach (var tag in timeTags)
                {
                    // 去掉 '[' 并解析
                    ReadOnlySpan<char> timeSpanRaw = tag.AsSpan().TrimStart('[');

                    if (TryGetLrcTime(timeSpanRaw, out TimeSpan time))
                    {
                        if (lyricDict.TryGetValue(time, out var existingData))
                        {
                            // 处理重复时间戳：追加歌词
                            if (content != NoneLyricString)
                            {
                                existingData.Lyric.Add(content);
                            }
                        }
                        else
                        {
                            // 新增歌词
                            lyricDict.Add(time, new LyricData(new List<string> { content }, null, time));
                        }
                    }
                }
            }
        }

        // 太少则清空
        if (lyricDict.Count <= 2) lyricDict.Clear();

        // 添加结束标记
        if (lyricDict.Count > 0)
        {
            lyricDict.TryAdd(TimeSpan.MaxValue, new LyricData(null, null, TimeSpan.MaxValue));
        }

        // 排序
        var sortedLyrics = lyricDict.OrderBy(x => x.Key).Select(x => x.Value).ToList();

        // 罗马音转换阶段
        if (useRomaji)
        {
            // 配置并发选项
            var parallelOptions = new ParallelOptions
            {
                // 限制最大并发数为 CPU 核心数的一半，或者固定为 4~8
                // 留出足够的 CPU 资源给 UI 线程渲染
                MaxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount / 2)
            };

            await Parallel.ForEachAsync(sortedLyrics, parallelOptions, async (data, token) =>
            {
                // 跳过空歌词
                if (data.Lyric == null || data.Lyric.Count == 0) return;

                string firstLine = data.Lyric[0];
                if (firstLine == NoneLyricString) return;

                // 检测日文
                if (IsJapaneseEnough(firstLine))
                {
                    var romaji = await kawazuConverter.Convert(firstLine, Kawazu.To.Romaji, Kawazu.Mode.Spaced, Kawazu.RomajiSystem.Nippon);
                    data.Romaji = romaji;
                }
            });
        }

        return sortedLyrics.ToArray();
    }

    private static bool TryGetLrcTime(ReadOnlySpan<char> timeSpan, out TimeSpan result)
    {
        result = TimeSpan.Zero;

        // 查找第一个分隔符 (通常是 :)
        int firstSplit = timeSpan.IndexOf(':');
        if (firstSplit == -1) return false;

        // 解析分钟
        if (!int.TryParse(timeSpan.Slice(0, firstSplit), out int minutes)) return false;

        // 剩余部分 ss.ff 或 ss:ff
        ReadOnlySpan<char> secondPart = timeSpan.Slice(firstSplit + 1);

        // 查找第二个分隔符 (. 或 :)
        int secondSplit = secondPart.IndexOfAny('.', ':');

        int seconds = 0;
        int milliseconds = 0;

        if (secondSplit == -1)
        {
            // 只有秒，没有毫秒
            if (!int.TryParse(secondPart, out seconds)) return false;
        }
        else
        {
            // 解析秒
            if (!int.TryParse(secondPart.Slice(0, secondSplit), out seconds)) return false;

            // 解析毫秒
            ReadOnlySpan<char> millisecondPart = secondPart.Slice(secondSplit + 1);
            if (millisecondPart.Length > 0)
            {
                if (!int.TryParse(millisecondPart, out int rawMs)) return false;

                // LRC 的逻辑通常是：
                // .1 -> 100ms, .01 -> 10ms, .10 -> 100ms (取决于具体标准，通常是两位数代表 10ms 单位)
                if (millisecondPart.Length == 1) milliseconds = rawMs * 100;
                else if (millisecondPart.Length == 2) milliseconds = rawMs * 10;
                else milliseconds = rawMs;
            }
        }

        result = new TimeSpan(0, 0, minutes, seconds, milliseconds);
        return true;
    }

    /// <summary>
    /// 高性能检测日文占比，无内存分配
    /// </summary>
    private static bool IsJapaneseEnough(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        int jpCount = 0;
        int len = text.Length;

        for (int i = 0; i < len; i++)
        {
            char c = text[i];
            // 平假名 (3040-309F)
            // 片假名 (30A0-30FF)
            // 常用汉字 (4E00-9FFF)
            if ((c >= 0x3040 && c <= 0x309F) ||
                (c >= 0x30A0 && c <= 0x30FF) ||
                (c >= 0x4E00 && c <= 0x9FFF))
            {
                jpCount++;
            }
        }

        return (double)jpCount / len >= 0.15;
    }
}
