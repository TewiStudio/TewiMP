using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;

namespace TewiMP.Controls
{
    public sealed partial class LoadingTip : Grid
    {
        public LoadingTip()
        {
            InitializeComponent();
        }

        public void ShowLoading()
        {
            LoadingRingBaseGrid.Visibility = Visibility.Visible;
            LoadingRingBaseGrid.Opacity = 1;
            LoadingRing.IsIndeterminate = true;
        }

        public async void UnShowLoading()
        {
            LoadingRingBaseGrid.Opacity = 0;
            await Task.Delay(500);
            LoadingRing.IsIndeterminate = false;
            LoadingRingBaseGrid.Visibility = Visibility.Collapsed;
        }

    }
}
