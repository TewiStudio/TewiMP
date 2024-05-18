using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using TewiMP.Helpers;

namespace TewiMP.DataEditor
{
    public enum MusicFrom { kwMusic, kgMusic, qqMusic, neteaseMusic, miguMusic, localMusic, otherMusic }
    public enum DataType { 歌曲, 歌单, 本地歌单, 专辑, 用户, 艺术家 }
    public enum SearchDataType { 歌曲 = 1, 歌单 = 1000, 专辑 = 10, 用户 = 1002, 艺术家 = 100 }
    public enum MusicKbps { aac, wma, Kbps128, Kbps192, Kbps320, Kbps1000 }
    public enum PlaySort { 默认升序, 默认降序, 名称升序, 名称降序, 艺术家升序, 艺术家降序, 专辑升序, 专辑降序, 时间升序, 时间降序, 索引升序, 索引降序 }

    public abstract class OnlyClass
    {
        string md5;
        [JsonIgnore]
        public string MD5
        {
            get
            {
                if (md5 == null)
                    md5 = GetMD5();
                return md5;
            }
        }

        public abstract string GetMD5();

        public static bool operator ==(OnlyClass left, OnlyClass right)
        {
            if (left is null && right is null) return true;
            if (left is null || right is null) return false;
            return left.MD5 == right.MD5;
        }

        public static bool operator !=(OnlyClass left, OnlyClass right)
        {
            if (left is null && right is null) return false;
            if (left is null || right is null) return true;
            return !(left.MD5 == right.MD5);
        }

        public override bool Equals(object other)
        {
            if (!(other is OnlyClass)) return false;
            return string.Equals(MD5, (other as OnlyClass).MD5, StringComparison.InvariantCulture);
        }

        public override int GetHashCode()
        {
            return (MD5 != null ? StringComparer.InvariantCulture.GetHashCode(MD5) : 0);
        }
    }
    public interface IIsListPage { }

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

    public class SearchData : OnlyClass, IIsListPage
    {
        public string Key { get; set; }
        public MusicFrom From { get; set; }
        public SearchDataType SearchDataType { get; set; }
        public override string GetMD5()
        {
            return $"{Key}{From}{SearchDataType}";
        }
    }

    public class Artist : OnlyClass, IIsListPage
    {
        public string Name { get; set; }
        public string Name2 { get; set; }
        public string ID { get; set; }
        public string PicturePath { get; set; }
        public string Describee { get; set; }
        public MusicFrom From { get; set; }
        public MusicListData HotSongs { get; set; }
        public int Count { get; set; }

        public Artist(string name = null, string ID = null, string picturePath = null)
        {
            Name = name;
            this.ID = ID;
            PicturePath = picturePath;
        }

        public override string GetMD5()
        {
            return $"{Name}{Name2}{ID}{PicturePath}";
        }

        public new string ToString()
        {
            return Name;
        }
    }

    public class Album : OnlyClass, IIsListPage
    {
        public string Title { get; set; }
        public string Title2 { get; set; }
        public string ID { get; set; }
        public string PicturePath { get; set; }
        public string Describee { get; set; }
        public string RelaseTime { get; set; }
        public int Count { get; set; }
        public MusicFrom From { get; set; }
        public List<Artist> Artists { get; set; }
        public MusicListData Songs { get; set; }
        public Album(string title = null, string ID = null, string picturePath = null, string describee = null, MusicListData songs = null)
        {
            Title = string.IsNullOrEmpty(title) ? "未知" : title;
            this.ID = ID == "0" || string.IsNullOrEmpty(ID) ? null : ID;
            PicturePath = picturePath;
            Describee = describee;
            Songs = songs;
        }

        public bool IsNull()
        {
            return Title == "未知" && string.IsNullOrEmpty(ID);
        }

        public override string GetMD5()
        {
            return $"{Title}{Title2}{ID}{Artists?.Count}{Describee}{RelaseTime}";
        }

        public override string ToString()
        {
            return Title;
        }
    }

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

        string _artistName = null;
        [JsonIgnore]
        public string ArtistName
        {
            get
            {
                if (Artists.Any())
                {
                    if (_artistName == null)
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
                if (_buttonName == null)
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


        private void SetABName()
        {
            for (int i = 0; i < Artists.Count; i++)
            {
                _artistName += $"{Artists[i].ToString()}{(i < (Artists.Count - 1) ? (i < Artists.Count - 2 ? ", " : " & ") : "")}";
            }

            _buttonName = $"{(ArtistName == null ? "未知" : ArtistName)} · {Album}";
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
                    string path = $"{localFile.DirectoryName}\\{cueSheet.Tracks.First().DataFile.Filename}";
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

                        string finalPath;
                        if (string.IsNullOrEmpty(path))
                        {
                            finalPath = Path.Combine(localFile.DirectoryName, t.DataFile.Filename);
                        }
                        else
                        {
                            finalPath = path;
                        }
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
                    data.Reverse();
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
                            if (tag == null) isError = true;
                            if (!isError)
                            {
                                if (tag.IsEmpty) isError = true;
                                if (string.IsNullOrEmpty(tag.Title)) isError = true;
                                if (tag.Performers == null) isError = true;
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

    public class MusicListData : OnlyClass, IIsListPage
    {
        public string ListName { get; set; }
        public string ListShowName { get; set; }
        public string PicturePath { get; set; }
        public MusicFrom ListFrom { get; set; }
        public DataType ListDataType { get; set; }
        public string ID { get; set; }
        public PlaySort PlaySort { get; set; }
        public List<MusicData> Songs { get; set; }

        public MusicListData(string listName = null, string listShowName = null, string picturePath = null,
            MusicFrom listFrom = default, string ID = null, List<MusicData> songs = null, DataType listDataType = default)
        {
            this.ListName = listName;
            this.ListShowName = listShowName;
            this.PicturePath = picturePath;
            this.ListFrom = listFrom;
            this.ListDataType = listDataType;
            this.ID = ID;
            this.Songs = songs == null ? new() : songs;
            ListDataType = listDataType;
        }

        public override string GetMD5()
        {
            return $"{ListShowName}{ListName}{PicturePath}{ListFrom}{ListDataType}{ID}";
        }
    }

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
                if (lyricAllString == null && Lyric != null)
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
            if (Lyric == null) return null;
            return $"{string.Join(' ', Lyric)}{Lyric.Count}{LyricTimeSpan.Ticks}";
        }
    }

    public static class Music
    {
        public static List<string> GetArtistStrings(List<Artist> artists)
        {
            List<string> a = new();
            foreach (Artist artist in artists)
            {
                a.Add(artist.ToString());
            }
            return a;
        }

        public static MusicFrom MusicFromFromString(this string text)
        {
            MusicFrom musicFrom = MusicFrom.localMusic;

            try
            {
                switch (text)
                {
                    case "kwMusic":
                        musicFrom = MusicFrom.kwMusic;
                        break;
                    case "neteaseMusic":
                        musicFrom = MusicFrom.neteaseMusic;
                        break;
                    case "kgMusic":
                        musicFrom = MusicFrom.kgMusic;
                        break;
                    case "qqMusic":
                        musicFrom = MusicFrom.qqMusic;
                        break;
                    case "miguMusic":
                        musicFrom = MusicFrom.miguMusic;
                        break;
                    case "localMusic":
                        musicFrom = MusicFrom.localMusic;
                        break;
                    default:
                        musicFrom = MusicFrom.otherMusic;
                        break;
                }
            }
            catch
            {
                musicFrom = MusicFrom.kwMusic;
            }

            return musicFrom;
        }
    }
}
