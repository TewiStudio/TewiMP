using WinRT;
using Microsoft.UI.Xaml;
using Microsoft.UI.Composition;

namespace TewiMP.Helpers.TransparentWindowHelper
{
    public static class TransparentHelper
    {
        public static void SetTransparent(Window window, bool isTransparent)
        {
            var brushHolder = window.As<ICompositionSupportsSystemBackdrop>();

            if (isTransparent)
            {
                var colorBrush = WindowsCompositionHelper.Compositor.CreateColorBrush(Windows.UI.Color.FromArgb(0, 255, 255, 255));
                brushHolder.SystemBackdrop = colorBrush;
            }
            else
            {
                brushHolder.SystemBackdrop = null;
            }
        }
    }
}
