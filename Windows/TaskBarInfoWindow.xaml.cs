using System;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Windowing;

namespace TewiMP.Windowed
{
    public partial class TaskBarInfoWindow : Window
    {
        public nint Handle { get; private set; }
        public string IconPath { get; private set; }
        public string IconPathUsing { get; private set; }

        OverlappedPresenter overlappedPresenter = null;

        public TaskBarInfoWindow()
        {
            InitializeComponent();
            Handle = WindowHelperzn.WindowHelper.GetWindowHandle(this);

            InitCallBack();
            InitTaskbarInfo();
            ShowTaskBarButtons();
            //SetTaskbarImage(Path.Combine(localPath, "icon.png"));

            MainWindow.WindowViewStateChanged += MainWindow_WindowViewStateChanged;
            App.audioPlayer.PlayStateChanged += (_) => SetTaskbarButtonIcon(_.PlaybackState);
            App.playingList.NowPlayingImageLoaded += (_, __) => IconPath = __;
            App.audioPlayer.SourceChanged += (_) =>
            {
                if (_.MusicData is null)
                    Title = App.AppName;
                else
                {
                    Title = $"{_.MusicData.Title} - {_.MusicData.ArtistName} · {App.AppName}";
                }
                Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.SetThumbnailTooltip(Handle, $"正在播放：{_.MusicData.Title} - {_.MusicData.ArtistName}");
            };
            if (App.audioPlayer.MusicData is null)
                Title = App.AppName;
            else
            {
                Title = $"{App.audioPlayer.MusicData.Title} - {App.audioPlayer.MusicData.ArtistName} · {App.AppName}";
            }
            IconPath = App.playingList.NowPlayingImagePath;

            Activated += (_, __) =>
            {
                __.Handled = true;
                App.WindowLocal.Activate();
            };
            AppWindow.Closing += (_, __) =>
            {
                __.Cancel = true;
                MainWindow.AppWindowLocal.Hide();
            };
            AppWindow.Changed += (_, __) =>
            {
                //Debug.WriteLine(__?.DidPresenterChange);
                // WinUI Bug：设置了窗口不能最大化结果还是能 >:(
                /*if (__?.DidPresenterChange == true)
                    overlappedPresenter.Minimize();*/
            };

            overlappedPresenter = OverlappedPresenter.Create();
            overlappedPresenter.IsMaximizable = false;
            overlappedPresenter.IsMinimizable = false;

            AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.BackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
            AppWindow.TitleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
            AppWindow.TitleBar.ButtonInactiveBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
            AppWindow.SetIcon(Path.Combine("Images", "Icons", "icon.ico"));
            AppWindow.MoveAndResize(new(0, 0, 0, 0));
            AppWindow.SetPresenter(overlappedPresenter);
        }

        bool lastIsBackground = false;
        private void MainWindow_WindowViewStateChanged(bool isView)
        {
            ShowTaskBarButtons();
            SetTaskbarButtonIcon(App.audioPlayer.PlaybackState);
            //TryTransparentWindow();
        }

        public async void InitTaskbarInfo()
        {
            Title = App.AppName;
            AppWindow.Hide();

            int attributeTrue = (int)NativeMethods.TRUE;
            var hresult = NativeMethods.DwmSetWindowAttribute(Handle, NativeMethods.DWMWA.HAS_ICONIC_BITMAP, ref attributeTrue, sizeof(int));
            if ((hresult != 0))
                throw Marshal.GetExceptionForHR(hresult);
            hresult = NativeMethods.DwmSetWindowAttribute(Handle, NativeMethods.DWMWA.FORCE_ICONIC_REPRESENTATION, ref attributeTrue, sizeof(int));
            if ((hresult != 0))
                throw Marshal.GetExceptionForHR(hresult);

            Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.HrInit();
            Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.RegisterTab(Handle, MainWindow.Handle);
            Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.SetTabOrder(Handle, MainWindow.Handle);
        }

        private void InitCallBack()
        {
            taskBarPrc = TaskBarPrc;
            var hotKeyPrcPointer = Marshal.GetFunctionPointerForDelegate(taskBarPrc);
            origPrc =
                Marshal.GetDelegateForFunctionPointer<Windows.Win32.UI.WindowsAndMessaging.WNDPROC>(
                    PInvoke.User32.SetWindowLongPtr(
                        new Windows.Win32.Foundation.HWND(Handle),
                        PInvoke.User32.WindowLongIndexFlags.GWL_WNDPROC,
                        hotKeyPrcPointer)
                    );
        }

        private void SetTaskbarButtonIcon(NAudio.Wave.PlaybackState playbackState)
        {
            //Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.SetProgressState(Handle, Helpers.SDKs.TaskbarProgress.TBPFLAG.TBPF_NORMAL);
            //Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.SetProgressValue(Handle, 1, 100);
            Helpers.SDKs.TaskbarProgress.THUMBBUTTON[] changer;
            if (playbackState == NAudio.Wave.PlaybackState.Playing)
            {
                changer = new[]
                {
                    new Helpers.SDKs.TaskbarProgress.THUMBBUTTON() { iId = 2, dwMask = Helpers.SDKs.TaskbarProgress.THUMBBUTTONMASK.THB_ICON, dwFlags = Helpers.SDKs.TaskbarProgress.THUMBBUTTONFLAGS.THBF_ENABLED, hIcon = pauseIconHandle, szTip = "播放" }
                };

                // 这个 api 调用似乎会慢一拍，所以这里调用两次
                Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.SetOverlayIcon(Handle, playIconHandle, null);
                Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.SetOverlayIcon(Handle, playIconHandle, null);
            }
            else
            {
                changer = new[]
                {
                    new Helpers.SDKs.TaskbarProgress.THUMBBUTTON() { iId = 2, dwMask = Helpers.SDKs.TaskbarProgress.THUMBBUTTONMASK.THB_ICON, dwFlags = Helpers.SDKs.TaskbarProgress.THUMBBUTTONFLAGS.THBF_ENABLED, hIcon = playIconHandle, szTip = "播放" }
                };
                Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.SetOverlayIcon(Handle, pauseIconHandle, null);
                Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.SetOverlayIcon(Handle, pauseIconHandle, null);
            }
            try
            {
                // 似乎在某些情况下不会起作用？
                Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.ThumbBarUpdateButtons(Handle, 3, changer);
            }
            catch(Exception ex)
            {
                DataEditor.LogHelper.WriteLog(nameof(ex), ex.ToString());
            }
        }

        static string localPath = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), "Images");
        nint pauseIconHandle = (Bitmap.FromFile(Path.Combine(localPath, "任务栏暂停.png")) as Bitmap).GetHicon();
        nint playIconHandle = (Bitmap.FromFile(Path.Combine(localPath, "任务栏播放.png")) as Bitmap).GetHicon();
        nint nextPlayIconHandle = (Bitmap.FromFile(Path.Combine(localPath, "下一首.png")) as Bitmap).GetHicon();
        nint perviousPlayIconHandle = (Bitmap.FromFile(Path.Combine(localPath, "上一首.png")) as Bitmap).GetHicon();
        private void ShowTaskBarButtons()
        {
            Helpers.SDKs.TaskbarProgress.THUMBBUTTON[] taskbarInfoButtonPauseStyle = new[]
            {
                new Helpers.SDKs.TaskbarProgress.THUMBBUTTON(){ iId = 1, dwMask = Helpers.SDKs.TaskbarProgress.THUMBBUTTONMASK.THB_ICON, dwFlags = Helpers.SDKs.TaskbarProgress.THUMBBUTTONFLAGS.THBF_ENABLED, hIcon = perviousPlayIconHandle, szTip = "上一首" },
                new Helpers.SDKs.TaskbarProgress.THUMBBUTTON(){ iId = 2, dwMask = Helpers.SDKs.TaskbarProgress.THUMBBUTTONMASK.THB_ICON, dwFlags = Helpers.SDKs.TaskbarProgress.THUMBBUTTONFLAGS.THBF_ENABLED, hIcon = App.audioPlayer.PlaybackState == NAudio.Wave.PlaybackState.Playing ? pauseIconHandle : playIconHandle, szTip = "播放" },
                new Helpers.SDKs.TaskbarProgress.THUMBBUTTON(){ iId = 3, dwMask = Helpers.SDKs.TaskbarProgress.THUMBBUTTONMASK.THB_ICON, dwFlags = Helpers.SDKs.TaskbarProgress.THUMBBUTTONFLAGS.THBF_ENABLED, hIcon = nextPlayIconHandle, szTip = "下一首" },
            };
            Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.ThumbBarAddButtons(Handle, 3, taskbarInfoButtonPauseStyle);
            Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.ThumbBarUpdateButtons(Handle, 3, taskbarInfoButtonPauseStyle);
        }

        public async void SetTaskbarImage(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = Path.Combine(localPath, "icon.png");
            }
            if (IconPathUsing == filePath) return;
            if (!await Task.Run(() => File.Exists(filePath))) return;
            bool canBreak = false;
            using var bmp = await Task.Run(() => Bitmap.FromFile(filePath));
            int size = 160;
            for (int i = 0; i < 50; i++)
            {
                if (canBreak) break;
                using var hBitmap = bmp.GetThumbnailImage(size, size, null, 0) as Bitmap;
                var hBitmapNint = hBitmap.GetHbitmap();
                try
                {
                    var a = await Task.Run(() => NativeMethods.DwmSetIconicThumbnail(Handle, hBitmapNint, NativeMethods.DWM_SIT.None));
                    if (a != 0)
                    {
                        //Debug.WriteLine($"{size}x{size} failed.");
                        size -= 2;
                        canBreak = false;
                    }
                    else
                    {
                        Debug.WriteLine($"[TaskBarInfoWindow]: TaskBar Image {size}x{size} completed.");
                        IconPathUsing = filePath;
                        canBreak = true;
                    }
                }
                catch
                {
                }
            }
        }

        nint appIconHandle = (Bitmap.FromFile(Path.Combine(localPath, "opacityMask.png")).GetThumbnailImage(1, 1, null, 0) as Bitmap).GetHbitmap();
        private const uint WM_HOTKEY = 0x0312;
        private Windows.Win32.UI.WindowsAndMessaging.WNDPROC origPrc;
        private Windows.Win32.UI.WindowsAndMessaging.WNDPROC taskBarPrc;
        /// <summary>
        /// 窗口获得的系统消息在这里处理
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="uMsg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private Windows.Win32.Foundation.LRESULT TaskBarPrc(Windows.Win32.Foundation.HWND hwnd,
            uint uMsg,
            Windows.Win32.Foundation.WPARAM wParam,
            Windows.Win32.Foundation.LPARAM lParam)
        {
            //Debug.WriteLine($"Get system message: {uMsg}\n    {wParam.Value}");
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
                    SetTaskbarImage(IconPath);
            }
            else if (uMsg == 124 || uMsg == 125)
            {/* doesn't work
                if (wParam.Value == 18446744073709551596)
                {
                    Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.SetOverlayIcon(Handle, nint.Zero, null);
                    Helpers.SDKs.TaskbarProgress.MyTaskbarInstance.SetOverlayIcon(Handle, nint.Zero, null);
                }*/
            }

            return Windows.Win32.PInvoke.CallWindowProc(origPrc, hwnd, uMsg, wParam, lParam);
        }

        /// <summary>
        /// 任务栏按钮触发时响应
        /// </summary>
        /// <param name="wParam"></param>
        private async void TaskbarButtonInvoke(Windows.Win32.Foundation.WPARAM wParam)
        {
            switch (wParam.Value)
            {
                case 402653185:
                    await App.playingList.PlayPrevious();
                    break;
                case 402653186:
                    if (App.audioPlayer.PlaybackState == NAudio.Wave.PlaybackState.Playing)
                        App.audioPlayer.SetPause();
                    else
                        App.audioPlayer.SetPlay();
                    break;
                case 402653187:
                    await App.playingList.PlayNext();
                    break;
            }
            App.playingList.NowPlayingImageLoaded += PlayingList_NowPlayingImageLoaded;
        }

        private void PlayingList_NowPlayingImageLoaded(ImageSource imageSource, string path)
        {
            App.playingList.NowPlayingImageLoaded -= PlayingList_NowPlayingImageLoaded;
            SetTaskbarImage(path);
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
            Helpers.TransparentWindowHelper.TransparentHelper.SetTransparent(this, true);
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
}
