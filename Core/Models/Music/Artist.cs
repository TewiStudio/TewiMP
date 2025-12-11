namespace TewiMP.Core.Models.Music;

using TewiMP.Plugin;

public class Artist : OnlyClass, IIsListPage
{
    public string Name { get; set; }
    public string Name2 { get; set; }
    public string ID { get; set; }
    public string PicturePath { get; set; }
    public string Describee { get; set; }
    public MusicFrom From { get; set; }
    public PluginInfo PluginInfo { get; set; }
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
