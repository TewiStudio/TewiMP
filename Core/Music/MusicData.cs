using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using TewiMP.Helpers;
using TewiMP;
using TewiMP.Core;
using TewiMP.Services.Plugin;

namespace TewiMP.Core.Music;

public class MusicData : OnlyClass
{
    public string Title { get; set; }
    public string Title2 { get; set; }
    public string ID { get; set; }
    public List<Artist> Artists { get; set; }
    public Album Album { get; set; }
    public DateTime? ReleaseTime { get; set; }
    public DateTime? FileTime { get; set; }
    public string InLocal { get; set; }
    public CUETrackData CUETrackData { get; set; } = null;
    public int Index { get; set; } = 0;
    public int Count { get; set; }

    MusicFrom _from = MusicFrom.localMusic;
    public MusicFrom From
    {
        get => _from;
        set
        {
            if (_from == value) return;
            _from = value;
            if (Album != null)
            {
                Album.From = value;
            }
            if (Artists.Count > 0)
            {
                foreach (var artist in Artists)
                {
                    artist.From = value;
                }
            }
        }
    }

    public PluginInfo PluginInfo { get; set; }

    string _artistName = null;
    [JsonIgnore]
    public string ArtistName
    {
        get
        {
            if (Artists.Any())
            {
                if (_artistName is null)
                    SetABName();
            }
            return string.IsNullOrEmpty(_artistName) ? "未知" : _artistName;
        }
    }

    string _buttonName = null;
    [JsonIgnore]
    public string ButtonName
    {
        get
        {
            if (_buttonName is null)
            {
                SetABName();
            }
            return _buttonName;
        }
    }

    public MusicData(string title = "",
                     string ID = "",
                     List<Artist> artists = null,
                     Album album = null,
                     DateTime? releaseTime = null,
                     MusicFrom from = MusicFrom.localMusic,
                     string inLocal = null)
    {
        this.Title = title;
        this.ID = ID;
        this.Artists = artists;
        this.Album = album;
        this.ReleaseTime = releaseTime;
        this.From = from;
        this.InLocal = inLocal;

    }

    /// <summary>
    /// 设置 <see cref="ArtistName"/> 和 <see cref="ButtonName"/>
    /// </summary>
    private void SetABName()
    {
        for (int i = 0; i < Artists.Count; i++)
        {
            _artistName += $"{Artists[i].ToString()}{(i < (Artists.Count - 1) ? (i < Artists.Count - 2 ? ", " : " & ") : "")}";
        }

        _buttonName = $"{(ArtistName is null ? "未知" : ArtistName)} · {Album}";
    }

    public override string GetMD5()
    {
        return CodeHelper.ToMD5($"{Title}{(Artists.Any() ? $"{Artists[0]?.Name}{Artists[0]?.ID}" : "")}{Artists.Count}{Album?.Title}{ID}{Album?.ID}{From}{InLocal}{(CUETrackData != null ? $"{CUETrackData.StartDuration}{CUETrackData.EndDuration}" : "")}");
    }

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
