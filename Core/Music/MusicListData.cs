using System;
using System.Collections.Generic;
using TewiMP.Services.Plugin;

namespace TewiMP.Core.Music;

public class MusicListData : OnlyClass, IIsListPage
{
    public string ListName { get; set; }
    public string ListShowName { get; set; }
    public string PicturePath { get; set; }
    public MusicFrom ListFrom { get; set; }
    public string PluginInfoGUID { get; set; }
    public DataType ListDataType { get; set; }
    public string ID { get; set; }
    public PlaySort PlaySort { get; set; }
    public DateTime CreationTime { get; set; } = DateTime.Now;
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
        this.Songs = songs is null ? new() : songs;
        ListDataType = listDataType;
    }

    private MusicSourcePlugin _plugin = null;
    public MusicSourcePlugin GetMusicSourcePlugin()
    {
        if (_plugin != null) return _plugin;
        if (string.IsNullOrEmpty(PluginInfoGUID)) return null;

        _plugin = PluginInfoGUID.GetMusicSourcePlugin();
        return _plugin;
    }

    public override string GetMD5()
    {
        return $"{ListShowName}{ListName}{PicturePath}{ListFrom}{ListDataType}{ID}";
    }
}
