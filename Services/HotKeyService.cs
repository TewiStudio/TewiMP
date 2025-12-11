using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using WinUIEx;
using Vanara.PInvoke;
using Microsoft.UI.Xaml;
using TewiMP.Core;
using TewiMP.UI.Windows;

namespace TewiMP.Services;

public class HotKeyService
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

    public HotKeyService()
    {
        LogService.Log("Starting", "初始化 HotKeyManager.");
    }

    public void Init(Window window)
    {
        RegisteredWindow = window;
        RegisteredWindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        RegisteredHotKeys = [.. WillRegisterHotKeysList];
        EnableHotKey = enableHotKey;
        InitCallBack();
        LogService.Log(nameof(HotKeyService), $"Window: {RegisteredWindowHandle}, EnableHotKey: {EnableHotKey}, Registered HotKey Count: {RegisteredHotKeys.Count}");
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
        //System.Diagnostics.LogManager.Log($"System Message: {uMsg}");
        if (uMsg == WM_HOTKEY)
        {
            nuint id = wParam.Value;
            HotKeyID hotKeyID = (HotKeyID)id;

            switch (hotKeyID)
            {
                case HotKeyID.PreviousSong:
                    PlayPrevious();
                    break;
                case HotKeyID.NextSong:
                    PlayNext();
                    break;
                case HotKeyID.Pause:
                    if (App.Instance.AudioService.PlaybackState == NAudio.Wave.PlaybackState.Playing)
                    {
                        App.Instance.AudioService.SetPause();
                    }
                    else
                    {
                        App.Instance.AudioService.SetPlay();
                    }
                    break;
                case HotKeyID.Stop:
                    App.Instance.AudioService.CurrentTime = TimeSpan.Zero;
                    App.Instance.AudioService.SetStop();
                    break;
                case HotKeyID.VolumeAdd:
                    App.Instance.AudioService.Volume += 1f;
                    break;
                case HotKeyID.VolumeRemove:
                    App.Instance.AudioService.Volume -= 1f;
                    break;
                case HotKeyID.OpenLyricWindow:
                    App.MainWindowInstance.OpenDesktopLyricWindow();
                    break;
                case HotKeyID.RandomPlay:
                    App.Instance.PlayingListService.PlayBehavior = App.Instance.PlayingListService.PlayBehavior == Services.PlayBehavior.随机播放 ? Services.PlayBehavior.顺序播放 : Services.PlayBehavior.随机播放;
                    break;
                case HotKeyID.OpenMainWindow:
                    App.MainWindowInstance.Restore();
                    App.MainWindowInstance.SetForegroundWindow();
                    break;
                case HotKeyID.TryActivityLyricWindow:
                    if (App.MainWindowInstance.DesktopLyricWindow != null)
                    {
                        App.MainWindowInstance.DesktopLyricWindow.Activate();
                        App.MainWindowInstance.DesktopLyricWindow.Restore();
                    }
                    break;
                case HotKeyID.ReturnToFirstSong:
                    if (App.Instance.PlayingListService.NowPlayingList.Any())
                    {
                        PlayFirst();
                    }
                    break;
                case HotKeyID.LockLyricWindow:
                    App.MainWindowInstance.DesktopLyricWindow?.Lock();/*
                    if (App.MainWindowInstance.DesktopLyricWindow != null)
                    {
                        if (!App.MainWindowInstance.DesktopLyricWindow.IsLock)
                        {
                        }
                    }*/
                    break;
                default:
                    App.MainWindowInstance.AddNotify(
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
            App.MainWindowInstance.InvokeDpiEvent();
        }

        return Windows.Win32.PInvoke.CallWindowProc(origPrc, hwnd, uMsg, wParam, lParam);
    }

    private async void PlayNext()
    {
        await App.Instance.PlayingListService.PlayNext();
    }

    private async void PlayPrevious()
    {
        await App.Instance.PlayingListService.PlayPrevious();
    }

    private async void PlayFirst()
    {
        await App.Instance.PlayingListService.Play(App.Instance.PlayingListService.NowPlayingList.First());
    }
}
