using WinRT;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Composition;
using Microsoft.UI;

namespace TewiMP.Helpers.TransparentWindowHelper
{
    public static class TransparentHelper
    {
        public static void SetTransparent(Window window, Color? color = null)
        {
            var brushHolder = window.As<ICompositionSupportsSystemBackdrop>();
            var colorBrush = WindowsCompositionHelper.Compositor.CreateColorBrush(color ?? Colors.Transparent);
            brushHolder.SystemBackdrop = colorBrush;
        }
    }
}
