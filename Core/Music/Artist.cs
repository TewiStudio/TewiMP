using TewiMP.Services.Plugin;

namespace TewiMP.Core.Music;

public record class Artist : IIsListPage
{
    public string Name { get; set; }
    public string Name2 { get; set; }
    public string ID { get; set; }
    public string PicturePath { get; set; }
    public string Describee { get; set; }
    public MusicFrom From { get; set; }
    public string PluginInfoGUID { get; set; }
    public MusicListData HotSongs { get; set; }
    public int Count { get; set; }

    public Artist(string name = null, string ID = null, string picturePath = null)
    {
        Name = name;
        this.ID = ID;
        PicturePath = picturePath;
    }

    private MusicSourcePlugin _plugin = null;
    public MusicSourcePlugin GetMusicSourcePlugin()
    {
        if (_plugin != null) return _plugin;
        if (string.IsNullOrEmpty(PluginInfoGUID)) return null;

        _plugin = PluginInfoGUID.GetMusicSourcePlugin();
        return _plugin;
    }

    public override string ToString()
    {
        return Name;
    }
}
