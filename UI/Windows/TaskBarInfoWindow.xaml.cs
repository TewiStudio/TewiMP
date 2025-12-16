using System;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using TewiMP.Services;
using TewiMP.Services.Storage;
using Vanara.PInvoke;

namespace TewiMP.UI.Windows;

public partial class TaskBarInfoWindow : Window
{
    private int _maxThumbWidth = 200;
    private int _maxThumbHeight = 109;
    public nint Handle { get; private set; }
    public string IconPath { get; private set; }
    public string IconPathUsing { get; private set; }

    OverlappedPresenter overlappedPresenter = null;

    public TaskBarInfoWindow()
    {
        LogService.Log("Starting", "初始化 TaskBarInfoWindow.");

        InitializeComponent();
        Handle = WindowHelpers.WindowHelper.GetWindowHandle(this);

        InitCallBack();
        InitTaskbarInfo();
        ShowTaskBarButtons();
        //SetTaskbarImage(Path.Combine(localPath, "icon.png"));

        App.MainWindowInstance.WindowViewStateChanged += MainWindow_WindowViewStateChanged;
        App.Instance.AudioService.PlayStateChanged += (_) => SetTaskbarButtonIcon(_.PlaybackState);
        App.Instance.PlayingListService.NowPlayingImageLoaded += (_, __) => IconPath = __;
        App.Instance.AudioService.SourceChanged += (_) =>
        {
            if (!User32.IsWindow(Handle)) return;
            if (_.MusicData is null)
            {
                Title = App.Instance.AppName;
                return;
            }
            else
            {
                Title = $"{_.MusicData.Title} - {_.MusicData.ArtistName} · {App.Instance.AppName}";
            }
            Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.SetThumbnailTooltip(Handle, $"正在播放：{_.MusicData.Title} - {_.MusicData.ArtistName}");
        };
        if (App.Instance.AudioService.MusicData is null)
            Title = App.Instance.AppName;
        else
        {
            Title = $"{App.Instance.AudioService.MusicData.Title} - {App.Instance.AudioService.MusicData.ArtistName} · {App.Instance.AppName}";
        }
        IconPath = App.Instance.PlayingListService.NowPlayingImagePath;

        Activated += (_, __) =>
        {
            __.Handled = true;
            App.MainWindowInstance.Activate();
        };
        AppWindow.Closing += (_, __) =>
        {
            __.Cancel = true;
            App.MainWindowInstance.AppWindow.Hide();
        };
        AppWindow.Changed += (_, __) =>
        {
            //LogManager.Log(__?.DidPresenterChange);
            // WinUI Bug：设置了窗口不能最大化结果还是能 >:(
            /*if (__?.DidPresenterChange == true)
                overlappedPresenter.Minimize();*/
        };

        overlappedPresenter = OverlappedPresenter.Create();
        overlappedPresenter.IsMaximizable = false;
        overlappedPresenter.IsMinimizable = false;

        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.BackgroundColor = global::Windows.UI.Color.FromArgb(0, 0, 0, 0);
        AppWindow.TitleBar.ButtonBackgroundColor = global::Windows.UI.Color.FromArgb(0, 0, 0, 0);
        AppWindow.TitleBar.ButtonInactiveBackgroundColor = global::Windows.UI.Color.FromArgb(0, 0, 0, 0);
        AppWindow.SetIcon(DataFolderBase.IconICOPath);
        AppWindow.MoveAndResize(new(0, 0, 0, 0));
        AppWindow.SetPresenter(overlappedPresenter);
    }

    bool lastIsBackground = false;
    private void MainWindow_WindowViewStateChanged(bool isView)
    {
        if (!User32.IsWindow(Handle)) return;
        ShowTaskBarButtons();
        SetTaskbarButtonIcon(App.Instance.AudioService.PlaybackState);
        //TryTransparentWindow();
    }

    public void InitTaskbarInfo()
    {
        Title = App.Instance.AppName;
        AppWindow.Hide();

        int attributeTrue = (int)NativeMethods.TRUE;
        var hresult = NativeMethods.DwmSetWindowAttribute(Handle, NativeMethods.DWMWA.HAS_ICONIC_BITMAP, ref attributeTrue, sizeof(int));
        if ((hresult != 0))
            throw Marshal.GetExceptionForHR(hresult);
        hresult = NativeMethods.DwmSetWindowAttribute(Handle, NativeMethods.DWMWA.FORCE_ICONIC_REPRESENTATION, ref attributeTrue, sizeof(int));
        if ((hresult != 0))
            throw Marshal.GetExceptionForHR(hresult);

        Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.HrInit();
        Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.RegisterTab(Handle, App.MainWindowInstance.Handle);
        Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.SetTabOrder(Handle, App.MainWindowInstance.Handle);
        UpdateTaskbarCover(DataFolderBase.IconPNGPath);
    }

    private void InitCallBack()
    {
        taskBarPrc = TaskBarPrc;
        var hotKeyPrcPointer = Marshal.GetFunctionPointerForDelegate(taskBarPrc);
        origPrc =
            Marshal.GetDelegateForFunctionPointer<global::Windows.Win32.UI.WindowsAndMessaging.WNDPROC>(
                PInvoke.User32.SetWindowLongPtr(
                    new global::Windows.Win32.Foundation.HWND(Handle),
                    PInvoke.User32.WindowLongIndexFlags.GWL_WNDPROC,
                    hotKeyPrcPointer)
                );
    }

    private void SetTaskbarButtonIcon(NAudio.Wave.PlaybackState playbackState)
    {
        //Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.SetProgressState(Handle, Helpers.SDKs.TaskbarProgress.TBPFLAG.TBPF_NORMAL);
        //Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.SetProgressValue(Handle, 1, 100);
        if (!User32.IsWindow(Handle)) return;
        Helpers.SDKs.TaskbarProgress.THUMBBUTTON[] changer;
        if (playbackState == NAudio.Wave.PlaybackState.Playing)
        {
            changer =
            [
                new Helpers.SDKs.TaskbarProgress.THUMBBUTTON() { iId = 2, dwMask = Helpers.SDKs.TaskbarProgress.THUMBBUTTONMASK.THB_ICON, dwFlags = Helpers.SDKs.TaskbarProgress.THUMBBUTTONFLAGS.THBF_ENABLED, hIcon = pauseIconHandle, szTip = "播放" }
            ];

            // 这个 api 调用似乎会慢一拍，所以这里调用两次
            Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.SetOverlayIcon(Handle, playIconHandle, null);
            Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.SetOverlayIcon(Handle, playIconHandle, null);
        }
        else
        {
            changer =
            [
                new Helpers.SDKs.TaskbarProgress.THUMBBUTTON() { iId = 2, dwMask = Helpers.SDKs.TaskbarProgress.THUMBBUTTONMASK.THB_ICON, dwFlags = Helpers.SDKs.TaskbarProgress.THUMBBUTTONFLAGS.THBF_ENABLED, hIcon = playIconHandle, szTip = "播放" }
            ];
            Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.SetOverlayIcon(Handle, pauseIconHandle, null);
            Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.SetOverlayIcon(Handle, pauseIconHandle, null);
        }
        try
        {
            // 似乎在某些情况下不会起作用？
            Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.ThumbBarUpdateButtons(Handle, 3, changer);
        }
        catch (Exception ex)
        {
            LogService.Error("SetTaskbarButtonIcon", ex.ToString());
        }
    }

    nint pauseIconHandle = (Bitmap.FromFile(DataFolderBase.TaskbarAssetPausePath) as Bitmap).GetHicon();
    nint playIconHandle = (Bitmap.FromFile(DataFolderBase.TaskbarAssetPlayPath) as Bitmap).GetHicon();
    nint nextPlayIconHandle = (Bitmap.FromFile(DataFolderBase.TaskbarAssetNextPath) as Bitmap).GetHicon();
    nint perviousPlayIconHandle = (Bitmap.FromFile(DataFolderBase.TaskbarAssetPreviousPath) as Bitmap).GetHicon();
    private void ShowTaskBarButtons()
    {
        Helpers.SDKs.TaskbarProgress.THUMBBUTTON[] taskbarInfoButtonPauseStyle = new[]
        {
            new Helpers.SDKs.TaskbarProgress.THUMBBUTTON(){ iId = 1, dwMask = Helpers.SDKs.TaskbarProgress.THUMBBUTTONMASK.THB_ICON, dwFlags = Helpers.SDKs.TaskbarProgress.THUMBBUTTONFLAGS.THBF_ENABLED, hIcon = perviousPlayIconHandle, szTip = "上一首" },
            new Helpers.SDKs.TaskbarProgress.THUMBBUTTON(){ iId = 2, dwMask = Helpers.SDKs.TaskbarProgress.THUMBBUTTONMASK.THB_ICON, dwFlags = Helpers.SDKs.TaskbarProgress.THUMBBUTTONFLAGS.THBF_ENABLED, hIcon = App.Instance.AudioService.PlaybackState == NAudio.Wave.PlaybackState.Playing ? pauseIconHandle : playIconHandle, szTip = "播放" },
            new Helpers.SDKs.TaskbarProgress.THUMBBUTTON(){ iId = 3, dwMask = Helpers.SDKs.TaskbarProgress.THUMBBUTTONMASK.THB_ICON, dwFlags = Helpers.SDKs.TaskbarProgress.THUMBBUTTONFLAGS.THBF_ENABLED, hIcon = nextPlayIconHandle, szTip = "下一首" },
        };
        Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.ThumbBarAddButtons(Handle, 3, taskbarInfoButtonPauseStyle);
        Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.ThumbBarUpdateButtons(Handle, 3, taskbarInfoButtonPauseStyle);
    }

    public void UpdateTaskbarCover(string newPath)
    {
        if (IconPathUsing == newPath) return;
        IconPath = newPath;

        SetTaskbarImage(IconPath, _maxThumbWidth, _maxThumbHeight);
    }

    public void SetTaskbarImage(string filePath, int maxWidth, int maxHeight)
    {
        if (string.IsNullOrEmpty(filePath)) filePath = DataFolderBase.IconPNGPath;
        if (!File.Exists(filePath)) return;
        try
        {
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var originalBmp = new Bitmap(fileStream);

            // 计算保持纵横比的目标尺寸
            float ratioX = (float)maxWidth / originalBmp.Width;
            float ratioY = (float)maxHeight / originalBmp.Height;
            float ratio = Math.Min(ratioX, ratioY); // 取较小的比例，确保塞得进缩略图框

            int newWidth = (int)(originalBmp.Width * ratio);
            int newHeight = (int)(originalBmp.Height * ratio);

            LogService.Info(nameof(TaskBarInfoWindow), $"Taskbar thumbnail size：{newWidth}x{newHeight}");

            // 创建一个新的 32位 ARGB 位图
            using var targetBmp = new Bitmap(newWidth, newHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            using (var g = Graphics.FromImage(targetBmp))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(originalBmp, 0, 0, newWidth, newHeight);
            }

            // 获取 HBITMAP
            IntPtr hBitmap = targetBmp.GetHbitmap();

            try
            {
                int result = NativeMethods.DwmSetIconicThumbnail(Handle, hBitmap, NativeMethods.DWM_SIT.None);
                if (result != 0)
                {
                    LogService.Log(nameof(SetTaskbarImage), $"DwmSetIconicThumbnail failed: {result:X}, size: {newWidth}x{newHeight}");
                    NativeMethods.DwmInvalidateIconicBitmaps(Handle);
                }
                else
                {
                    IconPathUsing = filePath;
                }
            }
            finally
            {
                NativeMethods.DeleteObject(hBitmap); // 释放 GDI 对象
            }
        }
        catch (Exception ex)
        {
            LogService.Error(nameof(TaskBarInfoWindow), ex.ToString());
        }
    }

    static nint appIconHandle = (Bitmap.FromFile(DataFolderBase.TaskbarAssetPlayPath).GetThumbnailImage(1, 1, null, 0) as Bitmap).GetHbitmap();
    private const uint WM_HOTKEY = 0x0312;
    private global::Windows.Win32.UI.WindowsAndMessaging.WNDPROC origPrc;
    private global::Windows.Win32.UI.WindowsAndMessaging.WNDPROC taskBarPrc;
    /// <summary>
    /// 窗口获得的系统消息在这里处理
    /// </summary>
    /// <param name="hwnd"></param>
    /// <param name="uMsg"></param>
    /// <param name="wParam"></param>
    /// <param name="lParam"></param>
    /// <returns></returns>
    private global::Windows.Win32.Foundation.LRESULT TaskBarPrc(global::Windows.Win32.Foundation.HWND hwnd,
        uint uMsg,
        global::Windows.Win32.Foundation.WPARAM wParam,
        global::Windows.Win32.Foundation.LPARAM lParam)
    {
        //LogManager.Log($"Get system message: {uMsg}\n    {wParam.Value}");
        if (uMsg == 806)
        {
            // 到了屏幕外面就看不见了 :-)
            NativeMethods.NativePoint offset = new(-5000, -5000);
            // 只知道设置1x1的缩略图进去后看起来是正常的...
            var a = NativeMethods.DwmSetIconicLivePreviewBitmap(Handle, appIconHandle, ref offset, 0);
        }
        else if (uMsg == 273)
        {
            TaskbarButtonInvoke(wParam);
        }
        else if (uMsg == 127)
        {
            if (wParam.Value == 2)
            {
                UpdateTaskbarCover(IconPath);
                return new global::Windows.Win32.Foundation.LRESULT(0);
            }
        }
        else if (uMsg is 0x0323)
        {
            // 高16位是宽，低16位是高
            _maxThumbWidth = (short)((lParam >> 16) & 0xFFFF);
            _maxThumbHeight = (short)(lParam & 0xFFFF);

            LogService.Log(nameof(TaskBarInfoWindow), $"System accept taskbar thumbnail size: {_maxThumbWidth}x{_maxThumbHeight}");
            SetTaskbarImage(IconPath, _maxThumbWidth, _maxThumbHeight);
            return new global::Windows.Win32.Foundation.LRESULT(0);
        }
        else if (uMsg == 124 || uMsg == 125)
        {/* doesn't work
            if (wParam.Value == 18446744073709551596)
            {
                Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.SetOverlayIcon(Handle, nint.Zero, null);
                Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.SetOverlayIcon(Handle, nint.Zero, null);
            }*/
        }

        return global::Windows.Win32.PInvoke.CallWindowProc(origPrc, hwnd, uMsg, wParam, lParam);
    }

    /// <summary>
    /// 任务栏按钮触发时响应
    /// </summary>
    /// <param name="wParam"></param>
    private async void TaskbarButtonInvoke(global::Windows.Win32.Foundation.WPARAM wParam)
    {
        switch (wParam.Value)
        {
            case 402653185:
                await App.Instance.PlayingListService.PlayPrevious();
                break;
            case 402653186:
                if (App.Instance.AudioService.PlaybackState == NAudio.Wave.PlaybackState.Playing)
                    App.Instance.AudioService.SetPause();
                else
                    App.Instance.AudioService.SetPlay();
                break;
            case 402653187:
                await App.Instance.PlayingListService.PlayNext();
                break;
        }
        App.Instance.PlayingListService.NowPlayingImageLoaded += PlayingList_NowPlayingImageLoaded;
    }

    private void PlayingList_NowPlayingImageLoaded(Uri imageSource, string path)
    {
        App.Instance.PlayingListService.NowPlayingImageLoaded -= PlayingList_NowPlayingImageLoaded;
        UpdateTaskbarCover(path);
    }

    #region Transparent Window Method
    private SUBCLASSPROC subClassProc;
    public void TryTransparentWindow()
    {
        subClassProc = new SUBCLASSPROC(SubClassWndProc);
        var windowHandle = new IntPtr((long)this.AppWindow.Id.Value);
        SetWindowSubclass(windowHandle, subClassProc, 0, 0);

        var exStyle = Vanara.PInvoke.User32.GetWindowLongAuto(windowHandle, Vanara.PInvoke.User32.WindowLongFlags.GWL_EXSTYLE).ToInt32();
        if ((exStyle & (int)Vanara.PInvoke.User32.WindowStylesEx.WS_EX_LAYERED) == 0)
        {
            exStyle |= (int)Vanara.PInvoke.User32.WindowStylesEx.WS_EX_LAYERED;
            exStyle |= (int)Vanara.PInvoke.User32.WindowStylesEx.WS_EX_TRANSPARENT;
            Vanara.PInvoke.User32.SetWindowLong(windowHandle, Vanara.PInvoke.User32.WindowLongFlags.GWL_EXSTYLE, exStyle);
            Vanara.PInvoke.User32.SetLayeredWindowAttributes(
                windowHandle,
                (uint)System.Drawing.ColorTranslator.ToWin32(System.Drawing.Color.FromArgb(255, 99, 99, 99)), 255,
                Vanara.PInvoke.User32.LayeredWindowAttributes.LWA_COLORKEY);
        }
        Helpers.TransparentWindowHelper.TransparentHelper.SetTransparent(this);
    }

    private IntPtr SubClassWndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, uint dwRefData)
    {
        if (uMsg == (uint)Vanara.PInvoke.User32.WindowMessage.WM_ERASEBKGND)
        {
            if (Vanara.PInvoke.User32.GetClientRect(hWnd, out var rect))
            {
                using var brush = Vanara.PInvoke.Gdi32.CreateSolidBrush((uint)System.Drawing.ColorTranslator.ToWin32(System.Drawing.Color.FromArgb(255, 99, 99, 99)));
                Vanara.PInvoke.User32.FillRect(wParam, rect, brush);
                return new IntPtr(1);
            }
        }

        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    private delegate IntPtr SUBCLASSPROC(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, uint dwRefData);

    [DllImport("Comctl32.dll", SetLastError = true)]
    private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("Comctl32.dll", SetLastError = true)]
    private static extern bool SetWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, uint uIdSubclass, uint dwRefData);
    #endregion
}

internal static class NativeMethods
{
    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteObject(IntPtr hObject);
    [DllImport("dwmapi.dll")]
    public static extern int DwmInvalidateIconicBitmaps(IntPtr hwnd);
    [DllImport("dwmapi.dll")]
    public static extern int DwmSetIconicThumbnail(IntPtr hwnd, IntPtr hbmp, DWM_SIT dwSITFlags);

    [DllImport("dwmapi.dll")]
    internal static extern int DwmSetIconicLivePreviewBitmap(
        IntPtr hwnd,
        IntPtr hbitmap,
        ref NativePoint ptClient,
        uint flags);

    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWA dwAttribute, ref int pvAttribute, int cbAttribute);

    public enum DWM_SIT
    {
        None,
        DISPLAYFRAME = 1
    }

    public enum DWMWA
    {
        NCRENDERING_ENABLED = 1,
        NCRENDERING_POLICY,
        TRANSITIONS_FORCEDISABLED,
        ALLOW_NCPAINT,
        CAPTION_BUTTON_BOUNDS,
        NONCLIENT_RTL_LAYOUT,
        FORCE_ICONIC_REPRESENTATION,
        FLIP3D_POLICY,
        EXTENDED_FRAME_BOUNDS,
        // New to Windows 7:
        HAS_ICONIC_BITMAP,
        DISALLOW_PEEK,
        EXCLUDED_FROM_PEEK
        // LAST
    }

    public const uint TRUE = 1;
    /// <summary>
    /// A wrapper for the native POINT structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct NativePoint
    {
        /// <summary>
        /// Initialize the NativePoint
        /// </summary>
        /// <param name="x">The x coordinate of the point.</param>
        /// <param name="y">The y coordinate of the point.</param>
        public NativePoint(int x, int y)
            : this()
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// The X coordinate of the point
        /// </summary>        
        public int X { get; set; }

        /// <summary>
        /// The Y coordinate of the point
        /// </summary>                                
        public int Y { get; set; }

        /// <summary>
        /// Determines if two NativePoints are equal.
        /// </summary>
        /// <param name="first">First NativePoint</param>
        /// <param name="second">Second NativePoint</param>
        /// <returns>True if first NativePoint is equal to the second; false otherwise.</returns>
        public static bool operator ==(NativePoint first, NativePoint second)
        {
            return first.X == second.X
                && first.Y == second.Y;
        }

        /// <summary>
        /// Determines if two NativePoints are not equal.
        /// </summary>
        /// <param name="first">First NativePoint</param>
        /// <param name="second">Second NativePoint</param>
        /// <returns>True if first NativePoint is not equal to the second; false otherwise.</returns>
        public static bool operator !=(NativePoint first, NativePoint second)
        {
            return !(first == second);
        }

        /// <summary>
        /// Determines if this NativePoint is equal to another.
        /// </summary>
        /// <param name="obj">Another NativePoint to compare</param>
        /// <returns>True if this NativePoint is equal obj; false otherwise.</returns>
        public override bool Equals(object obj)
        {
            return (obj != null && obj is NativePoint) ? this == (NativePoint)obj : false;
        }

        /// <summary>
        /// Gets a hash code for the NativePoint.
        /// </summary>
        /// <returns>Hash code for the NativePoint</returns>
        public override int GetHashCode()
        {
            int hash = X.GetHashCode();
            hash = hash * 31 + Y.GetHashCode();
            return hash;
        }
    }
}
