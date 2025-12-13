using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using TewiMP.Core.Music;

namespace TewiMP.Core.Models;

public partial class MusicDataViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    [NotifyPropertyChangedFor(nameof(Title2))]
    private MusicData _musicData;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ButtonName))]
    private MusicListData _musicListData;

    [ObservableProperty]
    private bool _showAlbumName = true;

    [ObservableProperty]
    private string _searchText = null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CountText))]
    private int _count = 0;

    public string CountText => MusicData is null ? null : (Count == 0 ? null : $"{Count}. ");
    public string Title => MusicData?.Title;
    public string Title2 => MusicData is null ? null : $" {MusicData.Title2}";
    public string ButtonName => MusicData is null ? null :
        MusicListData?.ListDataType == DataType.专辑
        ? MusicData.ArtistName
        : MusicData.ButtonName; 

    public MusicDataViewModel(MusicData musicData = default, MusicListData musicListData = default, int count = 0)
    {
        MusicData = musicData;
        MusicListData = musicListData;
        Count = count;
    }
    /*
    private static readonly Stack<MusicDataViewModel> _bindPool = new();
    public static MusicDataViewModel GetBindItem(MusicData musicData, MusicListData listData, int count)
    {
        MusicDataViewModel item;
        if (_bindPool.Count > 0)
        {
            item = _bindPool.Pop();
            item.MusicData = musicData;
            item.MusicListData = listData;
        }
        else
        {
            item = new MusicDataViewModel
            {
                MusicData = musicData,
                MusicListData = listData
            };
        }
        musicData.Count = count;
        return item;

        var item = new MusicDataViewModel
        {
            MusicData = musicData,
            MusicListData = listData
        };
        musicData.Count = count;
        return item;
    }

    public static void RecycleBindItems(IEnumerable<MusicDataViewModel> items)
    {
        foreach (var item in items)
        {
            item.MusicData = null;
            item.MusicListData = null;
            _bindPool.Push(item);
        }
    }*/
}
