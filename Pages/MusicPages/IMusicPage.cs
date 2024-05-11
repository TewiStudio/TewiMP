using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TewiMP.Pages.MusicPages
{
    public enum MusicPageViewState { View, Hidden }
    public delegate void MusicPageViewStateChangeDelegate(MusicPageViewState musicPageViewState);

    public interface IMusicPage
    {
        public MusicPageViewStateChangeDelegate MusicPageViewStateChange { get; set; }
    }
}
