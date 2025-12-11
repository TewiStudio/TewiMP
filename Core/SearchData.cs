namespace TewiMP.Core;

using TewiMP.Services.Plugin;

public class SearchData : OnlyClass, IIsListPage
{
    public string Key { get; set; }
    public MusicSourcePlugin SourcePlugin { get; set; }
    public SearchDataType SearchDataType { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 30;
    public override string GetMD5()
    {
        return $"{Key}{SourcePlugin.PluginInfo}{SearchDataType}";
    }
}
