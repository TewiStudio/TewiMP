namespace TewiMP.Services;

using System.Threading.Tasks;
using System.Collections.ObjectModel;
using TewiMP.Core.Music;

/// <summary>
/// 用于读取播放列表
/// </summary>
public class PlayListReader
{
    public delegate void PlayListChanged();
    public event PlayListChanged Updated;

    ObservableCollection<MusicListData> nowMusicListData;
    public ObservableCollection<MusicListData> NowMusicListData
    {
        get => nowMusicListData;
        private set
        {
            nowMusicListData = value;
        }
    }

    public PlayListReader()
    {
        LogService.Log("Starting", "初始化 PlayListReader.");
    }

    bool inRefresh = false;
    public async Task Refresh()
    {
        if (inRefresh) return;
        inRefresh = true;
        NowMusicListData = [.. await Storage.PlayListHelper.ReadAllPlayList()];
        Updated?.Invoke();
        inRefresh = false;
    }
}
