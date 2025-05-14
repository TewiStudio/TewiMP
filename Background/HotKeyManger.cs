using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using Newtonsoft.Json;
using Microsoft.UI.Xaml;

namespace TewiMP.Background.HotKeys
{
    public enum HotKeyID
    {
        PreviousSong = 1101,
        NextSong,
        Pause,
        Stop,
        VolumeAdd,
        VolumeRemove,
        OpenMainWindow,
        OpenLyricWindow,
        RandomPlay,
        TryActivityLyricWindow,
        ReturnToFirstSong,
        LockLyricWindow
    }

    public class HotKey : DataEditor.OnlyClass
    {
        public User32.HotKeyModifiers HotKeyModifiers { get; set; }
        public Windows.System.VirtualKey VirtualKey { get; set; }
        public HotKeyID HotKeyID { get; set; } = default;
        public bool IsDisabled { get; set; } = false;
        [JsonIgnore]
        public bool IsUsed { get; set; } = false;
        public HotKey(User32.HotKeyModifiers hotKeyModifiers, Windows.System.VirtualKey virtualKey, HotKeyID hotKeyID)
        {
            HotKeyModifiers = hotKeyModifiers;
            VirtualKey = virtualKey;
            HotKeyID = hotKeyID;
        }

        public override string GetMD5()
        {
            return ToString();
        }

        public override string ToString()
        {
            return $"{GetHKMString(HotKeyModifiers)} + {VirtualKey}";
        }

        public static string GetHKMString(User32.HotKeyModifiers hotKeyModifiers)
        {
            switch (hotKeyModifiers)
            {
                case User32.HotKeyModifiers.MOD_ALT:
                    return "Alt";
                case User32.HotKeyModifiers.MOD_CONTROL:
                    return "Ctrl";
                case User32.HotKeyModifiers.MOD_SHIFT:
                    return "Shift";
                case User32.HotKeyModifiers.MOD_WIN:
                    return "Win";
                default:
                    if ((User32.HotKeyModifiers.MOD_WIN | User32.HotKeyModifiers.MOD_CONTROL) == hotKeyModifiers)
                    {
                        return "Win + Ctrl";
                    }
                    else if ((User32.HotKeyModifiers.MOD_WIN | User32.HotKeyModifiers.MOD_SHIFT) == hotKeyModifiers)
                    {
                        return "Win + Shift";
                    }
                    else if ((User32.HotKeyModifiers.MOD_WIN | User32.HotKeyModifiers.MOD_ALT) == hotKeyModifiers)
                    {
                        return "Win + Alt";
                    }
                    else if ((User32.HotKeyModifiers.MOD_WIN | User32.HotKeyModifiers.MOD_CONTROL | User32.HotKeyModifiers.MOD_SHIFT) == hotKeyModifiers)
                    {
                        return "Win + Ctrl + Shift";
                    }
                    else if ((User32.HotKeyModifiers.MOD_WIN | User32.HotKeyModifiers.MOD_CONTROL | User32.HotKeyModifiers.MOD_ALT) == hotKeyModifiers)
                    {
                        return "Win + Ctrl + Alt";
                    }
                    else if ((User32.HotKeyModifiers.MOD_WIN | User32.HotKeyModifiers.MOD_CONTROL | User32.HotKeyModifiers.MOD_SHIFT | User32.HotKeyModifiers.MOD_ALT) == hotKeyModifiers)
                    {
                        return "Win + Ctrl + Shift + Alt";
                    }
                    else if ((User32.HotKeyModifiers.MOD_CONTROL | User32.HotKeyModifiers.MOD_SHIFT) == hotKeyModifiers)
                    {
                        return "Ctrl + Shift";
                    }
                    else if ((User32.HotKeyModifiers.MOD_CONTROL | User32.HotKeyModifiers.MOD_ALT) == hotKeyModifiers)
                    {
                        return "Ctrl + Alt";
                    }
                    else if ((User32.HotKeyModifiers.MOD_CONTROL | User32.HotKeyModifiers.MOD_SHIFT | User32.HotKeyModifiers.MOD_ALT) == hotKeyModifiers)
                    {
                        return "Ctrl + Shift + Alt";
                    }
                    else if ((User32.HotKeyModifiers.MOD_SHIFT | User32.HotKeyModifiers.MOD_ALT) == hotKeyModifiers)
                    {
                        return "Shift + Alt";
                    }
                    break;
            }
            return string.Empty;
        }

        public static string GetHotKeyIDString(HotKeyID hotKeyID)
        {
            switch (hotKeyID)
            {
                case HotKeyID.PreviousSong:
                    return "上一首";
                case HotKeyID.NextSong:
                    return "下一首";
                case HotKeyID.Pause:
                    return "暂停/播放";
                case HotKeyID.Stop:
                    return "停止";
                case HotKeyID.VolumeAdd:
                    return "音量加";
                case HotKeyID.VolumeRemove:
                    return "音量减";
                case HotKeyID.OpenMainWindow:
                    return "打开主窗口";
                case HotKeyID.OpenLyricWindow:
                    return "打开桌面歌词窗口";
                case HotKeyID.RandomPlay:
                    return "打开/关闭随机播放";
                case HotKeyID.TryActivityLyricWindow:
                    return "尝试使桌面歌词窗口成为前台窗口";
                case HotKeyID.ReturnToFirstSong:
                    return "返回正在播放歌单的第一首歌曲";
                case HotKeyID.LockLyricWindow:
                    return "锁定歌词窗口";
            }
            return string.Empty;
        }
    }

    public class HotKeyManager
    {
        private bool enableHotKey = false;
        public bool EnableHotKey
        {
            get => enableHotKey;
            set
            {
                enableHotKey = value;
                if (enableHotKey)
                {
                    RegisterHotKeys([.. RegisteredHotKeys]);
                }
                else
                {
                    UnregisterHotKeys([.. RegisteredHotKeys], false);
                }
            }
        }
        public Window RegisteredWindow { get; private set; }
        public nint RegisteredWindowHandle { get; private set; }
        public ObservableCollection<HotKey> RegisteredHotKeys { get; private set; } = [];
        public static List<HotKey> WillRegisterHotKeysList { get; set; } = DefaultRegisterHotKeysList;
        public static List<HotKey> DefaultRegisterHotKeysList { get; set; } =
        [
            new(User32.HotKeyModifiers.MOD_CONTROL | User32.HotKeyModifiers.MOD_SHIFT, Windows.System.VirtualKey.Left, HotKeyID.PreviousSong),
            new(User32.HotKeyModifiers.MOD_CONTROL | User32.HotKeyModifiers.MOD_SHIFT, Windows.System.VirtualKey.Right, HotKeyID.NextSong),
            new(User32.HotKeyModifiers.MOD_CONTROL | User32.HotKeyModifiers.MOD_SHIFT, Windows.System.VirtualKey.Down, HotKeyID.Pause),
            new(User32.HotKeyModifiers.MOD_CONTROL | User32.HotKeyModifiers.MOD_SHIFT, Windows.System.VirtualKey.Up, HotKeyID.Stop),
            new(User32.HotKeyModifiers.MOD_CONTROL | User32.HotKeyModifiers.MOD_SHIFT, Windows.System.VirtualKey.Subtract, HotKeyID.VolumeRemove),
            new(User32.HotKeyModifiers.MOD_CONTROL | User32.HotKeyModifiers.MOD_SHIFT, Windows.System.VirtualKey.Add, HotKeyID.VolumeAdd),
            new(User32.HotKeyModifiers.MOD_CONTROL | User32.HotKeyModifiers.MOD_SHIFT, Windows.System.VirtualKey.O, HotKeyID.OpenMainWindow),
            new(User32.HotKeyModifiers.MOD_CONTROL | User32.HotKeyModifiers.MOD_SHIFT, Windows.System.VirtualKey.L, HotKeyID.OpenLyricWindow),
            new(User32.HotKeyModifiers.MOD_CONTROL | User32.HotKeyModifiers.MOD_SHIFT, Windows.System.VirtualKey.I, HotKeyID.RandomPlay),
            new(User32.HotKeyModifiers.MOD_CONTROL | User32.HotKeyModifiers.MOD_SHIFT, Windows.System.VirtualKey.U, HotKeyID.TryActivityLyricWindow),
            new(User32.HotKeyModifiers.MOD_CONTROL | User32.HotKeyModifiers.MOD_SHIFT, Windows.System.VirtualKey.Home, HotKeyID.ReturnToFirstSong),
            new(User32.HotKeyModifiers.MOD_CONTROL | User32.HotKeyModifiers.MOD_SHIFT, Windows.System.VirtualKey.K, HotKeyID.LockLyricWindow)
        ];

        public HotKeyManager()
        {
        }

        public void Init(Window window)
        {
            RegisteredWindow = window;
            RegisteredWindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
            RegisteredHotKeys = [.. WillRegisterHotKeysList];
            EnableHotKey = enableHotKey;
            InitCallBack();
        }

        public bool RegisterHotKey(HotKey hotKey, int? insertIndex = null)
        {
            if (insertIndex == -1) insertIndex = null;
            if (hotKey.IsDisabled)
            {
                if (!RegisteredHotKeys.Contains(hotKey))
                {
                    if (insertIndex != null)
                        RegisteredHotKeys.Insert((int)insertIndex, hotKey);
                    else
                        RegisteredHotKeys.Add(hotKey);
                }
                return true;
            }

            bool r = true;
            if (EnableHotKey)
            {
                r = User32.RegisterHotKey(
                    RegisteredWindowHandle, (int)hotKey.HotKeyID, hotKey.HotKeyModifiers, (uint)hotKey.VirtualKey);
            }

            if (!r) hotKey.IsUsed = true;
            else hotKey.IsUsed = false;

            if (!RegisteredHotKeys.Contains(hotKey))
            {
                if (insertIndex != null)
                    RegisteredHotKeys.Insert((int)insertIndex, hotKey);
                else
                    RegisteredHotKeys.Add(hotKey);
            }

            return r;
        }

        public bool UnregisterHotKey(HotKeyID hotKeyID, bool removeFromList = true)
        {
            var r = User32.UnregisterHotKey(RegisteredWindowHandle, (int)hotKeyID);

            if (removeFromList)
            {
                foreach (var item in RegisteredHotKeys)
                {
                    if (item.HotKeyID == hotKeyID)
                    {
                        RegisteredHotKeys.Remove(item);
                        break;
                    }
                }
            }

            return r;
        }

        public bool ChangeHotKey(HotKey hotKey)
        {
            int index = -1;
            foreach (var item in RegisteredHotKeys)
            {
                if (item.HotKeyID == hotKey.HotKeyID)
                {
                    index = RegisteredHotKeys.IndexOf(item);
                    break;
                }
            }
            UnregisterHotKey(hotKey.HotKeyID);
            return RegisterHotKey(hotKey, index);
        }

        /// <summary>
        /// 批量注册热键
        /// </summary>
        /// <param name="willRegisterHotKeysList"></param>
        /// <returns></returns>
        public void RegisterHotKeys(List<HotKey> willRegisterHotKeysList)
        {
            // 循环列表注册热键
            foreach (HotKey key in willRegisterHotKeysList)
            {
                bool IsRegister = RegisterHotKey(key);
            }
        }
        
        /// <summary>
        /// 批量注销热键
        /// </summary>
        /// <param name="willRegisterHotKeysList"></param>
        /// <returns></returns>
        public void UnregisterHotKeys(List<HotKey> willUnregisterHotKeysList, bool removeFromList = true)
        {
            // 循环列表注册热键
            foreach (HotKey key in willUnregisterHotKeysList)
            {
                bool isRegister = UnregisterHotKey(key.HotKeyID, removeFromList);
            }
        }

        private void InitCallBack()
        {
            hotKeyPrc = HotKeyPrc;
            var hotKeyPrcPointer = Marshal.GetFunctionPointerForDelegate(hotKeyPrc);
            origPrc =
                Marshal.GetDelegateForFunctionPointer<Windows.Win32.UI.WindowsAndMessaging.WNDPROC>(
                    PInvoke.User32.SetWindowLongPtr(
                        new Windows.Win32.Foundation.HWND(RegisteredWindowHandle),
                        PInvoke.User32.WindowLongIndexFlags.GWLP_WNDPROC,
                        hotKeyPrcPointer));
        }

        private const uint WM_HOTKEY = 0x0312;
        private Windows.Win32.UI.WindowsAndMessaging.WNDPROC origPrc;
        private Windows.Win32.UI.WindowsAndMessaging.WNDPROC hotKeyPrc;
        /// <summary>
        /// 窗口获得的系统消息在这里处理
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="uMsg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private Windows.Win32.Foundation.LRESULT HotKeyPrc(Windows.Win32.Foundation.HWND hwnd,
            uint uMsg,
            Windows.Win32.Foundation.WPARAM wParam,
            Windows.Win32.Foundation.LPARAM lParam)
        {
            //System.Diagnostics.App.logManager.Log($"System Message: {uMsg}");
            if (uMsg == WM_HOTKEY)
            {
                nuint id = wParam.Value;
                HotKeyID hotKeyID = (HotKeyID)id;

                switch (hotKeyID)
                {
                    case HotKeyID.PreviousSong:
                        App.playingList.PlayPrevious();
                        break;
                    case HotKeyID.NextSong:
                        App.playingList.PlayNext();
                        break;
                    case HotKeyID.Pause:
                        if (App.audioPlayer.PlaybackState == NAudio.Wave.PlaybackState.Playing)
                        {
                            App.audioPlayer.SetPause();
                        }
                        else
                        {
                            App.audioPlayer.SetPlay();
                        }
                        break;
                    case HotKeyID.Stop:
                        App.audioPlayer.CurrentTime = TimeSpan.Zero;
                        App.audioPlayer.SetStop();
                        break;
                    case HotKeyID.VolumeAdd:
                        App.audioPlayer.Volume += 1f;
                        break;
                    case HotKeyID.VolumeRemove:
                        App.audioPlayer.Volume -= 1f;
                        break;
                    case HotKeyID.OpenLyricWindow:
                        MainWindow.OpenDesktopLyricWindow();
                        break;
                    case HotKeyID.RandomPlay:
                        App.playingList.PlayBehavior = App.playingList.PlayBehavior == Background.PlayBehavior.随机播放 ? Background.PlayBehavior.顺序播放 : Background.PlayBehavior.随机播放;
                        break;
                    case HotKeyID.OpenMainWindow:
                        MainWindow.AppWindowInstance.Show();
                        MainWindow.SOverlappedPresenter.Restore();
                        PInvoke.User32.SetForegroundWindow(MainWindow.Handle);
                        break;
                    case HotKeyID.TryActivityLyricWindow:
                        if (MainWindow.DesktopLyricWindow != null)
                        {
                            MainWindow.DesktopLyricWindow.Activate();
                            MainWindow.DesktopLyricWindow.overlappedPresenter.Restore();
                        }
                        break;
                    case HotKeyID.ReturnToFirstSong:
                        if (App.playingList.NowPlayingList.Any())
                        {
                            App.playingList.Play(App.playingList.NowPlayingList.First());
                        }
                        break;
                    case HotKeyID.LockLyricWindow:
                        MainWindow.DesktopLyricWindow?.Lock();/*
                        if (MainWindow.DesktopLyricWindow != null)
                        {
                            if (!MainWindow.DesktopLyricWindow.IsLock)
                            {
                            }
                        }*/
                        break;
                    default:
                        MainWindow.AddNotify(
                            "未知热键",
                            "未知的热键：\n" +
                                $"●uMsg：{uMsg}\n" +
                                $"●wParam.Value：{wParam.Value}\n" +
                                $"●lParam.Value：{lParam.Value}",
                            NotifySeverity.Warning);
                        break;
                }
                return (Windows.Win32.Foundation.LRESULT)IntPtr.Zero;
            }
            else if (uMsg == 0x02E0) // window dpi 改变消息，懒得再在MainWindow里再写一个 windows 信息处理了
            {
                MainWindow.InvokeDpiEvent();
            }

            return Windows.Win32.PInvoke.CallWindowProc(origPrc, hwnd, uMsg, wParam, lParam);
        }
    }
}
