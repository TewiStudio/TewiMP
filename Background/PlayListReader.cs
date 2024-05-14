using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TewiMP.Helpers;

namespace TewiMP.Background
{
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

    public class LocalMusicManager
    {
        public delegate void LocalMusicDelegate();
        public event LocalMusicDelegate DataChanging;
        public event LocalMusicDelegate DataChanged;

        public ObservableCollection<SongItemBindBase> LocalMusicItems = [];

        public LocalMusicManager() { Refresh(); }

        public async Task Refresh()
        {
            DataChanging?.Invoke();
            LocalMusicItems.Clear();

            int count = 0;
            var resultData = await DataEditor.LocalMusicHelper.GetAllAnalyzedMusicData();
            foreach (var i in resultData)
            {
                //count++;
                //i.Count = count;
                LocalMusicItems.Add(new() { MusicData = i });
            }

            DataChanged?.Invoke();
        }
    }
}
