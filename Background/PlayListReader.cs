using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TewiMP.Helpers;

namespace TewiMP.Background
{
    /// <summary>
    /// 用于读取播放列表
    /// </summary>
    public class PlayListReader
    {
        public delegate void PlayListChanged();
        public event PlayListChanged Updated;

        ObservableCollection<DataEditor.MusicListData> nowMusicListData;
        public ObservableCollection<DataEditor.MusicListData> NowMusicListData
        {
            get => nowMusicListData;
            private set
            {
                nowMusicListData = value;
            }
        }

        public PlayListReader() { }

        bool inRefresh = false;
        public async Task Refresh()
        {
            if (inRefresh) return;
            inRefresh = true;
            NowMusicListData = [.. await DataEditor.PlayListHelper.ReadAllPlayList()];
            Updated?.Invoke();
            inRefresh = false;
        }
    }

    /// <summary>
    /// 本地音乐解析
    /// </summary>
    public class LocalMusicManager
    {
        public delegate void LocalMusicDelegate();
        public event LocalMusicDelegate DataChanging;
        public event LocalMusicDelegate DataChanged;
        public event LocalMusicDelegate DataAnalyzing;
        public event LocalMusicDelegate DataAnalyzed;

        public ObservableCollection<SongItemBindBase> LocalMusicItems { get; set; } = [];

        public LocalMusicManager() => Refresh();

        bool isAnalyzingData = false;
        public async Task ReAnalysisMusicDatas()
        {
            if (isAnalyzingData) return;
            isAnalyzingData = true;
            DataAnalyzing?.Invoke();
            await DataEditor.LocalMusicHelper.ReAnalysisMusicDatas();
            isAnalyzingData = false;
            DataAnalyzed?.Invoke();
        }

        public async Task Refresh()
        {
            if (isAnalyzingData) return;
            DataChanging?.Invoke();

            var resultData = await DataEditor.LocalMusicHelper.GetAllAnalyzedMusicData();
            LocalMusicItems.Clear();
            foreach (var i in resultData)
            {
                LocalMusicItems.Add(new() { MusicData = i });
            }

            DataChanged?.Invoke();
        }
    }
}
