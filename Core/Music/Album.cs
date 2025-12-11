using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using TewiMP.Services.Plugin;

namespace TewiMP.Core.Music;

public class Album : OnlyClass, IIsListPage
{
    public string Title { get; set; }
    public string Title2 { get; set; }
    public string ID { get; set; }
    public string PicturePath { get; set; }
    public string Describe { get; set; }
    public DateTime ReleaseTime { get; set; }
    public int Count { get; set; }
    public MusicFrom From { get; set; }
    public PluginInfo PluginInfo { get; set; }
    public List<Artist> Artists { get; set; }
    public MusicListData Songs { get; set; }

    private string _artistName;
    [JsonIgnore]
    public string ArtistName
    {
        get
        {
            if (Artists.Any())
            {
                if (_artistName is null)
                    SetArtistsName();
            }
            return string.IsNullOrEmpty(_artistName) ? "未知" : _artistName;
        }
    }

    public Album(string title = null, string ID = null, string picturePath = null, string describee = null, MusicListData songs = null)
    {
        Title = string.IsNullOrEmpty(title) ? "未知" : title;
        this.ID = ID == "0" || string.IsNullOrEmpty(ID) ? null : ID;
        PicturePath = picturePath;
        Describe = describee;
        Songs = songs;
    }

    public bool IsNull()
    {
        return Title == "未知" && string.IsNullOrEmpty(ID);
    }

    private void SetArtistsName()
    {
        for (int i = 0; i < Artists.Count; i++)
        {
            _artistName += $"{Artists[i].ToString()}{(i < (Artists.Count - 1) ? (i < Artists.Count - 2 ? ", " : " & ") : "")}";
        }
    }

    public override string GetMD5()
    {
        return $"{Title}{Title2}{ID}{Artists?.Count}{Describe}{ReleaseTime}";
    }

    public override string ToString()
    {
        return Title;
    }
}
