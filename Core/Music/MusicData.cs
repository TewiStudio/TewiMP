using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using TewiMP.Helpers;
using TewiMP.Services.Plugin;

namespace TewiMP.Core.Music;

public class MusicData : IEquatable<MusicData>
{
    public string Title { get; set; }
    public string Title2 { get; set; }
    public string ID { get; set; }

    private List<Artist> _artists;
    public List<Artist> Artists
    {
        get => _artists;
        set
        {
            if (_artists != value)
            {
                _artists = value;
                // 当列表变动时，立即使缓存失效
                _artistName = null;
                _buttonName = null;
            }
        }
    }

    private Album _album;
    public Album Album
    {
        get => _album;
        set
        {
            if (_album != value)
            {
                _album = value;
                // 当专辑变动时，ButtonName 需要重新计算
                _buttonName = null;
            }
        }
    }

    public DateTime? ReleaseTime { get; set; }
    public DateTime? FileTime { get; set; }
    public string InLocal { get; set; }
    public CUETrackData CUETrackData { get; set; }
    public int Index { get; set; } = 0;
    public string PluginInfoGUID { get; set; }

    private MusicFrom _from = MusicFrom.localMusic;
    public MusicFrom From
    {
        get => _from;
        set
        {
            if (_from == value) return;
            _from = value;

            // 级联更新子对象的状态
            if (_album != null) _album.From = value;

            if (_artists != null && _artists.Count > 0)
            {
                foreach (var artist in _artists)
                {
                    artist.From = value;
                }
            }
        }
    }

    // 缓存字段
    private string _artistName;
    private string _buttonName;

    [JsonIgnore]
    public string ArtistName
    {
        get
        {
            // 只有为 null 时才计算
            if (_artistName == null)
            {
                if (_artists == null || _artists.Count == 0)
                {
                    _artistName = "未知";
                }
                else if (_artists.Count == 1)
                {
                    // 单歌手直接 ToString，无需 Join
                    _artistName = _artists[0].ToString();
                }
                else
                {
                    _artistName = string.Join(", ", _artists.Select(a => a.ToString()));
                }
            }
            return _artistName;
        }
    }

    [JsonIgnore]
    public string ButtonName
    {
        get
        {
            if (_buttonName == null)
            {
                // 确保依赖的属性已计算
                var albStr = _album?.ToString() ?? string.Empty;
                _buttonName = $"{ArtistName} · {albStr}";
            }
            return _buttonName;
        }
    }

    public MusicData(string title = "",
                     string id = "",
                     List<Artist> artists = null,
                     Album album = null,
                     DateTime? releaseTime = null,
                     MusicFrom from = MusicFrom.localMusic,
                     string inLocal = null)
    {
        Title = title;
        ID = id;
        _artists = artists;
        _album = album;
        ReleaseTime = releaseTime;
        _from = from;
        InLocal = inLocal;
    }

    private MusicSourcePlugin _plugin = null;
    public MusicSourcePlugin GetMusicSourcePlugin(bool throwError = true)
    {
        if (_plugin != null) return _plugin;
        if (string.IsNullOrEmpty(PluginInfoGUID)) return null;

        _plugin = PluginInfoGUID.GetMusicSourcePlugin(throwError);
        return _plugin;
    }

    /// <summary>
    /// 判断是否是同一首歌。
    /// </summary>
    public bool Equals(MusicData other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        // 如果源不一样，直接返回
        if (From != other.From) return false;

        if (GetMusicSourcePlugin(false) is not null && other.GetMusicSourcePlugin(false) is not null)
        {
            if (GetMusicSourcePlugin() != other.GetMusicSourcePlugin()) return false;
        }

        // 如果有 ID，优先比对 ID
        if (!string.IsNullOrEmpty(ID) && !string.IsNullOrEmpty(other.ID))
        {
            return ID == other.ID;
        }

        // 如果 ID 为空，则回退到比较文件路径或标题
        if (!string.IsNullOrEmpty(InLocal) && !string.IsNullOrEmpty(other.InLocal))
        {
            return InLocal == other.InLocal;
        }

        return Title == other.Title && ArtistName == other.ArtistName;
    }

    public override bool Equals(object obj) => Equals(obj as MusicData);

    public override int GetHashCode()
    {
        if (!string.IsNullOrEmpty(ID)) return ID.GetHashCode();

        // 只有当 ID 为空时组合其他字段
        return HashCode.Combine(Title, InLocal);
    }

    public static bool operator ==(MusicData left, MusicData right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(MusicData left, MusicData right) => !(left == right);

    public override string ToString()
    {
        return $"{Title} - {ButtonName}";
    }

    public static async Task<MusicData[]> FromFile(string file, bool extensionCheck = false)
    {
        return await Task.Run(() =>
        {
            FileInfo localFile = new(file);
            if (extensionCheck)
            {
                if (!App.SupportedMediaFormats.Contains(localFile.Extension)) return null;
            }
            if (localFile.Extension == ".cue")
            {
                var encoding = CodeHelper.GetEncoding(localFile.FullName, System.Text.Encoding.Default);
                CueSharp.CueSheet cueSheet = new CueSharp.CueSheet(localFile.FullName, encoding);
                string path = Path.Combine(localFile.DirectoryName, cueSheet.Tracks.First().DataFile.Filename);
                TimeSpan duration = default;

                var track = new ATL.Track(path);
                duration = TimeSpan.FromMilliseconds(track.DurationMs);

                List<MusicData> data = new List<MusicData>();
                foreach (var t in cueSheet.Tracks)
                {
                    //开始的时间
                    CueSharp.Index startIndex = t.Indices.Last();
                    TimeSpan startTime = new(0, 0, startIndex.Minutes, startIndex.Seconds, startIndex.Frames * 10);

                    //结束的时间
                    int endCount = t.TrackNumber;
                    CueSharp.Index endIndex = default;
                    TimeSpan endTime = default;
                    if (endCount < cueSheet.Tracks.Length)
                    {
                        endIndex = cueSheet.Tracks[t.TrackNumber].Indices.Last();
                        endTime = new(0, 0, endIndex.Minutes, endIndex.Seconds, endIndex.Frames * 10);
                    }
                    else
                    {
                        endTime = duration;
                    }

                    if (startTime >= endTime) endTime = TimeSpan.Zero;

                    string finalPath = string.IsNullOrEmpty(t.DataFile.Filename) ? path : Path.Combine(localFile.DirectoryName, t.DataFile.Filename);

                    MusicData musicData = new(
                        t.Title, null,
                        new List<Artist>() { new(string.IsNullOrEmpty(t.Performer) ? cueSheet.Performer : t.Performer) },
                        new(cueSheet.Title))
                    {
                        From = MusicFrom.localMusic,
                        InLocal = finalPath,
                        CUETrackData = new()
                        {
                            Index = t.TrackNumber,
                            StartDuration = startTime,
                            EndDuration = endTime,
                            Path = localFile.FullName
                        },
                        Index = t.TrackNumber
                    };
                    data.Add(musicData);
                }
                return data.ToArray();
            }
            else
            {
                MusicData localAudioData;
                TagLib.File tagFile = null;
                TagLib.Tag tag = null;
                //Track track = null;
                bool isError = false;

                try
                {
                    /*
                    await Task.Run(() =>
                    {
                        track = new(localFile.FullName);
                    });*/
                    if (localFile.Extension != ".mid")
                    {
                        try
                        {
                            tagFile = TagLib.File.Create(localFile.FullName);
                            tag = tagFile.Tag;
                        }
                        catch { }
                        if (tag is null) isError = true;
                        if (!isError)
                        {
                            if (tag.IsEmpty) isError = true;
                            if (string.IsNullOrEmpty(tag.Title)) isError = true;
                            if (tag.Performers is null) isError = true;
                        }
                    }
                    else isError = true;
                }
                catch
                {
                    isError = true;
                }

                if (!isError)
                {
                    List<Artist> artists = new();
                    foreach (var art in tag.Performers)
                    {
                        artists.Add(new(art));
                    }

                    localAudioData = new MusicData(
                    tag.Title, null, artists, new(tag.Album),
                        inLocal: localFile.FullName, from: MusicFrom.localMusic
                        )
                    { Index = (int)tag.Track };
                    localAudioData.ReleaseTime = tag.DateTagged ?? new DateTime(localFile.CreationTime.Ticks);
                }
                else
                {
                    localAudioData = new MusicData(
                    localFile.Name, null, new(), new(null),
                        inLocal: localFile.FullName, from: MusicFrom.localMusic
                    );
                }
                tagFile?.Dispose();
                return [localAudioData];
            }
        });
    }
}
