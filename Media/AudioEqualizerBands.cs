using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TagLib.IIM;
using Windows.UI;

namespace TewiMP.Media
{
    public enum Channel { Left, Dual, Right };

    public class EQData
    {
        private float centreFrequency;
        public float CentreFrequency
        {
            get => centreFrequency;
            set
            {
                centreFrequency = value;
                App.audioPlayer.UpdateEqualizer();
            }
        }

        private float q;
        public float Q
        {
            get => q;
            set
            {
                q = value;
                App.audioPlayer.UpdateEqualizer();
            }
        }

        private float decibels;
        public float Decibels
        {
            get => decibels;
            set
            {
                decibels = value;
                App.audioPlayer.UpdateEqualizer();
            }
        }

        private int channel;
        public int Channel
        {
            get => channel;
            set
            {
                channel = value;
                App.audioPlayer.UpdateEqualizer();
            }
        }

        private bool isEnable;
        public bool IsEnable
        {
            get => isEnable;
            set
            {
                isEnable = value;
                App.audioPlayer.UpdateEqualizer();
            }
        }

        public Color Color { get; set; }
    }

    public struct LowPassData
    {
        public float CutoffFrequency { get; set; }
        public float Q { get; set; }
    }
    
    public struct HighPassData
    {
        public float CutoffFrequency { get; set; }
        public float Q { get; set; }
    }

    public static class AudioFilterStatic
    {
        public static ObservableCollection<EQData> EQDatas = 
        [
            new() { CentreFrequency = 20, Channel = (int)Channel.Left, Decibels = -20, Q = 18f, IsEnable = true },
            new() { CentreFrequency = 1512, Channel = (int)Channel.Dual, Decibels = 60, Q = 12f, IsEnable = true },
            new() { CentreFrequency = 12000, Channel = (int)Channel.Right, Decibels = 110, Q = 1f, IsEnable = false },
        ];
    }

    public static class AudioEqualizerBands
    {
        public static Tuple<string, string>[] BandNames = new Tuple<string, string>[]
        {
            new Tuple<string, string>(nameof(CustomBands), "自定义"),
            new Tuple<string, string>(nameof(HighBands), "高音增强"),
            new Tuple<string, string>(nameof(LowBands), "低音增强"),
            new Tuple<string, string>(nameof(HeadsetBands), "头戴式耳机"),
            new Tuple<string, string>(nameof(LaptopBands), "笔记本电脑"),
            new Tuple<string, string>(nameof(PortableBands), "便携式扬声器"),
            new Tuple<string, string>(nameof(StereoBands), "家庭立体声"),
            new Tuple<string, string>(nameof(TVBands), "电视"),
            new Tuple<string, string>(nameof(CarBands), "汽车")
        };

        public static List<float[]> GetBandFromString(string name)
        {
            switch (name)
            {
                case nameof(CustomBands):
                    return CustomBands;
                case nameof(HighBands):
                    return HighBands;
                case nameof(LowBands):
                    return LowBands;
                case nameof(HeadsetBands):
                    return HeadsetBands;
                case nameof(LaptopBands):
                    return LaptopBands;
                case nameof(PortableBands):
                    return PortableBands;
                case nameof(StereoBands):
                    return StereoBands;
                case nameof(CarBands):
                    return CarBands;
                case nameof(TVBands):
                    return TVBands;
                default:
                    return CustomBands; //throw new ArgumentOutOfRangeException(name);
            }
        }
        
        public static string GetNameFromBands(List<float[]> floats)
        {
            string a = GetBandsMD5(floats);

            foreach (var b in BandNames)
            {
                if (GetBandsMD5(GetBandFromString(b.Item1)) == a)
                {
                    return b.Item1;
                }
            }

            return null;
        }

        public static string NameGetCHName(string name)
        {
            foreach (var a in BandNames)
            {
                if (a.Item1 == name)
                {
                    return a.Item2;
                }
            }

            return null;
        }

        public static string GetBandsMD5(List<float[]> floats)
        {
            string strs = "";
            foreach (var a in floats)
            {
                foreach (var b in a)
                {
                    strs += b;
                }
            }

            return strs;
        }

        public static List<float[]> NormalBands = new()
        {
            new float[3] { 31, 1, 0 }, //   0
            new float[3] { 62, 1, 0 }, //   1
            new float[3] { 125, 1, 0 }, //  2
            new float[3] { 250, 1, 0 }, //  3
            new float[3] { 500, 1, 0 }, //  4
            new float[3] { 1000, 1, 0 }, // 5
            new float[3] { 2000, 1, 0 }, // 6
            new float[3] { 4000, 1, 0 }, // 7
            new float[3] { 8000, 1, 0 }, // 8
            new float[3] { 16000, 1, 0 } // 9
        };
        
        public static List<float[]> CustomBands = new()
        {
            new float[3] { 31, 1, 0 }, //   0
            new float[3] { 62, 1, 0 }, //   1
            new float[3] { 125, 1, 0 }, //  2
            new float[3] { 250, 1, 0 }, //  3
            new float[3] { 500, 1, 0 }, //  4
            new float[3] { 1000, 1, 0 }, // 5
            new float[3] { 2000, 1, 0 }, // 6
            new float[3] { 4000, 1, 0 }, // 7
            new float[3] { 8000, 1, 0 }, // 8
            new float[3] { 16000, 1, 0 } // 9
        };

        public static List<float[]> HighBands = new()
        {
            new float[3] { 31, 1, 0 }, //      0
            new float[3] { 62, 1, 0 }, //      1
            new float[3] { 125, 1, 0 }, //     2
            new float[3] { 250, 1, 0 }, //     3
            new float[3] { 500, 1, 0 }, //     4
            new float[3] { 1000, 1, 0 }, //    5
            new float[3] { 2000, 1, 1 }, //    6
            new float[3] { 4000, 1, 4 }, //    7
            new float[3] { 8000, 1, 4.8f }, // 8
            new float[3] { 16000, 1, 7 } //    9
        };

        public static List<float[]> LowBands = new()
        {
            new float[3] { 31, 1, 3 }, //     0
            new float[3] { 62, 1, 7 }, //     1
            new float[3] { 125, 1, 4.8f }, // 2
            new float[3] { 250, 1, 4 }, //    3
            new float[3] { 500, 1, 1 }, //    4
            new float[3] { 1000, 1, 0 }, //   5
            new float[3] { 2000, 1, 0 }, //   6
            new float[3] { 4000, 1, 0 }, //   7
            new float[3] { 8000, 1, 0 }, //   8
            new float[3] { 16000, 1, 0 } //   9
        };

        public static List<float[]> HeadsetBands = new()
        {
            new float[3] { 31, 1, 9 }, //      0
            new float[3] { 62, 1, 7 }, //      1
            new float[3] { 125, 1, 4.4f }, //  2
            new float[3] { 250, 1, 3 }, //     3
            new float[3] { 500, 1, 0.2f }, //  4
            new float[3] { 1000, 1, 0 }, //    5
            new float[3] { 2000, 1, 0.5f }, // 6
            new float[3] { 4000, 1, 3 }, //    7
            new float[3] { 8000, 1, 2.9f }, // 8
            new float[3] { 16000, 1, 4 } //    9
        };

        public static List<float[]> LaptopBands = new()
        {
            new float[3] { 31, 1, 6 }, //      0
            new float[3] { 62, 1, 6 }, //      1
            new float[3] { 125, 1, 5 }, //     2
            new float[3] { 250, 1, 6 }, //     3
            new float[3] { 500, 1, 2.6f }, //  4
            new float[3] { 1000, 1, 2 }, //    5
            new float[3] { 2000, 1, 2.5f }, // 6
            new float[3] { 4000, 1, 6 }, //    7
            new float[3] { 8000, 1, 5.5f }, // 8
            new float[3] { 16000, 1, 7 } //    9
        };

        public static List<float[]> PortableBands = new()
        {
            new float[3] { 31, 1, 8 }, //      0
            new float[3] { 62, 1, 8 }, //      1
            new float[3] { 125, 1, 5.4f }, //  2
            new float[3] { 250, 1, 5 }, //     3
            new float[3] { 500, 1, 2.7f }, //  4
            new float[3] { 1000, 1, 3 }, //    5
            new float[3] { 2000, 1, 2.3f }, // 6
            new float[3] { 4000, 1, 4 }, //    7
            new float[3] { 8000, 1, 3.6f }, // 8
            new float[3] { 16000, 1, 5 } //    9
        };

        public static List<float[]> StereoBands = new()
        {
            new float[3] { 31, 1, 6 }, //      0
            new float[3] { 62, 1, 6 }, //      1
            new float[3] { 125, 1, 4.1f }, //  2
            new float[3] { 250, 1, 4 }, //     3
            new float[3] { 500, 1, 1.7f }, //  4
            new float[3] { 1000, 1, 2 }, //    5
            new float[3] { 2000, 1, 1.7f }, // 6
            new float[3] { 4000, 1, 4 }, //    7
            new float[3] { 8000, 1, 4.1f }, // 8
            new float[3] { 16000, 1, 6 } //    9
        };

        public static List<float[]> TVBands = new()
        {
            new float[3] { 31, 1, 3 }, //      0
            new float[3] { 62, 1, 3 }, //      1
            new float[3] { 125, 1, 4.5f }, //  2
            new float[3] { 250, 1, 8 }, //     3
            new float[3] { 500, 1, 2.8f }, //  4
            new float[3] { 1000, 1, 0 }, //    5
            new float[3] { 2000, 1, 1.3f }, // 6
            new float[3] { 4000, 1, 6 }, //    7
            new float[3] { 8000, 1, 6.1f }, // 8
            new float[3] { 16000, 1, 8 } //    9
        };

        public static List<float[]> CarBands = new()
        {
            new float[3] { 31, 1, 8 }, //      0
            new float[3] { 62, 1, 8 }, //      1
            new float[3] { 125, 1, 4.8f }, //  2
            new float[3] { 250, 1, 3 }, //     3
            new float[3] { 500, 1, 0.1f }, //  4
            new float[3] { 1000, 1, 0 }, //    5
            new float[3] { 2000, 1, 0.7f }, // 6
            new float[3] { 4000, 1, 4 }, //    7
            new float[3] { 8000, 1, 4.8f }, // 8
            new float[3] { 16000, 1, 7 } //    9
        };
    }
}
