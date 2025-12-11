using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Vanara.PInvoke;
using WinUIEx;
using TewiMP.Services;
using TewiMP.Helpers.TransparentWindowHelper;

namespace TewiMP.UI.Windows;

public partial class BackgroundTransparentTestWindow : WindowEx
{
    public nint Handle { get; private set; }
    SUBCLASSPROC sUBCLASSPROC;

    public BackgroundTransparentTestWindow()
    {
        InitializeComponent();
        //SystemBackdrop = new TransparentTintBackdrop() { TintColor = Color.FromArgb(50, 255, 0, 0) };
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true; 
        IsAlwaysOnTop = true;
        this.CenterOnScreen(1000, 1000);
        var handle = this.GetWindowHandle();
        var style = this.GetExtendedWindowStyle();
        this.SetExtendedWindowStyle(style | ExtendedWindowStyle.Layered | ExtendedWindowStyle.Transparent);
        this.SetWindowStyle(WindowStyle.Popup);
        sUBCLASSPROC = new SUBCLASSPROC(SubClassWndProc);
        SetWindowSubclass(handle, sUBCLASSPROC, 0, 0);
        var result = User32.SetLayeredWindowAttributes(handle, new COLORREF(255, 255, 255), 255, User32.LayeredWindowAttributes.LWA_COLORKEY);
        TransparentHelper.SetTransparent(this);
        LogService.Log("Test", $"{result}");
        LogService.Log("Test", $"{((App)Application.Current)}");
    }

    private IntPtr SubClassWndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, uint dwRefData)
    {
        if (uMsg == (uint)User32.WindowMessage.WM_ERASEBKGND)
        {
            if (User32.GetClientRect(hWnd, out var rect))
            {
                using var brush = Gdi32.CreateSolidBrush(new COLORREF(255, 255, 255));
                User32.FillRect(wParam, rect, brush);
                return new IntPtr(1);
            }
        }

        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }
    private delegate IntPtr SUBCLASSPROC(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, uint dwRefData);
    [DllImport("Comctl32.dll", SetLastError = true)]
    private static extern bool SetWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, uint uIdSubclass, uint dwRefData);
    [DllImport("Comctl32.dll", SetLastError = true)]
    private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);
}
