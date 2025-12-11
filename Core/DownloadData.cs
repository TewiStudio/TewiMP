namespace TewiMP.Core;

using TewiMP.Core.Music;

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
