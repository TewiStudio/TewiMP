namespace TewiMP.Core.Models;

using Vanara.PInvoke;
using Newtonsoft.Json;

public class HotKey : OnlyClass
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
