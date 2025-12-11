namespace TewiMP.Core.Models.Audio;

public class PassFilterData : EQData
{
    private PassFilterType passFilterType;
    public PassFilterType PassFilterType
    {
        get => passFilterType;
        set
        {
            passFilterType = value;
            App.Instance.AudioPlayer.UpdateEqualizer();
        }
    }

    private int slopeDbPerOct;
    public int SlopeDbPerOct
    {
        get => slopeDbPerOct;
        set
        {
            slopeDbPerOct = value;
            App.Instance.AudioPlayer.UpdateEqualizer();
        }
    }
}

