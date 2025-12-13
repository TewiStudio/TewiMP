using System.Threading.Tasks;
using System.Collections.ObjectModel;
using TewiMP.Core.Models;

namespace TewiMP.Services;

/// <summary>
/// 本地音乐解析
/// </summary>
public class LocalMusicManagerService
{
    public delegate void LocalMusicDelegate();
    public event LocalMusicDelegate DataChanging;
    public event LocalMusicDelegate DataChanged;
    public event LocalMusicDelegate DataAnalyzing;
    public event LocalMusicDelegate DataAnalyzed;

    public ObservableCollection<MusicDataViewModel> LocalMusicItems { get; set; } = [];

    public LocalMusicManagerService()
    {
        LogService.Log("Starting", "初始化 LocalMusicManager.");
    }

    bool isAnalyzingData = false;
    public async Task ReAnalysisMusicDatas()
    {
        if (isAnalyzingData) return;
        isAnalyzingData = true;
        DataAnalyzing?.Invoke();
        await Storage.LocalMusicHelper.ReAnalysisMusicDatas();
        isAnalyzingData = false;
        DataAnalyzed?.Invoke();
    }

    public async Task Refresh()
    {
        if (isAnalyzingData) return;
        DataChanging?.Invoke();

        var resultData = await Storage.LocalMusicHelper.GetAllAnalyzedMusicData();
        LocalMusicItems.Clear();
        foreach (var i in resultData)
        {
            LocalMusicItems.Add(new() { MusicData = i });
        }

        DataChanged?.Invoke();
    }
}
