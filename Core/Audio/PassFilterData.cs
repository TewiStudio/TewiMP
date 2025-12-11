namespace TewiMP.Core.Audio;

public class PassFilterData : EQData
{
    private PassFilterType passFilterType;
    public PassFilterType PassFilterType
    {
        get => passFilterType;
        set
        {
            passFilterType = value;
            App.Instance.AudioService.UpdateEqualizer();
        }
    }

    private int slopeDbPerOct;
    public int SlopeDbPerOct
    {
        get => slopeDbPerOct;
        set
        {
            slopeDbPerOct = value;
            App.Instance.AudioService.UpdateEqualizer();
        }
    }
}

