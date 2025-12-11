using TewiMP.Core.Music;

namespace TewiMP.Core;

public class DownloadData
{
    public string Path = null;
    public string LrcPath = null;
    public MusicData MusicData;
    public long FileSize;
    public long DownloadedSize;
    public decimal DownloadPercent;
    public DownloadStates DownloadState;
    public string ErrorMessage = null;
}
