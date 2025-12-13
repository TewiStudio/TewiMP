using TewiMP.Core.Music;

namespace TewiMP.Core.Models;

public class SearchItemBindBase
{
    public SearchBindDataType DataType { get; set; }
    public Artist Artist { get; set; }
    public Album Album { get; set; }
    public MusicListData PlayList { get; set; }
    public int PlayList_Count { get; set; }

    public int Count { get; set; }
}