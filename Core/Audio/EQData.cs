using TewiMP.Services.Media.Audio.AudioEffects;
using Windows.UI;

namespace TewiMP.Core.Audio;

public class EQData
{
    private float centreFrequency;
    public float CentreFrequency
    {
        get => centreFrequency;
        set
        {
            centreFrequency = value;
            App.Instance.AudioService.UpdateEqualizer();
        }
    }

    private float q;
    public float Q
    {
        get => q;
        set
        {
            q = value;
            App.Instance.AudioService.UpdateEqualizer();
        }
    }

    private float gain;
    public float Gain
    {
        get => gain;
        set
        {
            gain = value;
            App.Instance.AudioService.UpdateEqualizer();
        }
    }

    private int channel;
    public int Channel
    {
        get => channel;
        set
        {
            channel = value;
            App.Instance.AudioService.UpdateEqualizer();
        }
    }

    private bool isEnable;
    public bool IsEnable
    {
        get => isEnable;
        set
        {
            isEnable = value;
            App.Instance.AudioService.UpdateEqualizer();
        }
    }

    public Color Color { get; set; }
    public int Index { get; set; }
}

