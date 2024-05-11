using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace TewiMP.Background
{
    public sealed class PlayListReader
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
}
