using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Windowing;
using Microsoft.UI.Composition;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.UI.ViewManagement;
using TewiMP.DataEditor;
using WinRT.Interop;
using Windows.System;

namespace TewiMP.Helpers
{
    public static class AnimateHelper
    {
        public static void AnimateScalar(UIElement element, float scalar, double TimeSecond,
                                         float cubicBezierEasing1, float cubicBezierEasing2, float cubicBezierEasing3, float cubicBezierEasing4,
                                         out Visual elementVisual, out Compositor compositor, out ScalarKeyFrameAnimation animation)
        {
            elementVisual = ElementCompositionPreview.GetElementVisual(element);
            compositor = elementVisual.Compositor;
            AnimateScalar(elementVisual, scalar, TimeSecond, cubicBezierEasing1, cubicBezierEasing2, cubicBezierEasing3, cubicBezierEasing4,
                out animation);
        }
        
        public static void AnimateScalar(Visual visual, float scalar, double TimeSecond,
                                         float cubicBezierEasing1, float cubicBezierEasing2, float cubicBezierEasing3, float cubicBezierEasing4,
                                         out ScalarKeyFrameAnimation animation)
        {
            Visual elementVisual = visual;
            var compositor = elementVisual.Compositor;

            animation = compositor.CreateScalarKeyFrameAnimation();
            var easing = compositor.CreateCubicBezierEasingFunction(new Vector2(cubicBezierEasing1, cubicBezierEasing2), new Vector2(cubicBezierEasing3, cubicBezierEasing4));

            animation.Duration = TimeSpan.FromSeconds(TimeSecond);
            animation.InsertKeyFrame(1, scalar, easing);
        }

        public static void AnimateOffset(UIElement element, float offsetX, float offsetY, float offsetZ, double TimeSecond,
                                         float cubicBezierEasing1, float cubicBezierEasing2, float cubicBezierEasing3, float cubicBezierEasing4,
                                         out Visual elementVisual, out Compositor compositor, out Vector3KeyFrameAnimation animation)
        {
            elementVisual = ElementCompositionPreview.GetElementVisual(element);
            compositor = elementVisual.Compositor;

            var easing = compositor.CreateCubicBezierEasingFunction(new Vector2(cubicBezierEasing1, cubicBezierEasing2), new Vector2(cubicBezierEasing3, cubicBezierEasing4));
            animation = compositor.CreateVector3KeyFrameAnimation();

            animation.Duration = TimeSpan.FromSeconds(TimeSecond);
            animation.InsertKeyFrame(1, new Vector3(offsetX, offsetY, offsetZ), easing);
        }
    }

    public static class CodeHelper
    {
        #region 取字符中间
        public static string StringBetween(string str, string leftstr, string rightstr)
        {
            Regex rg = new Regex("(?<=(" + leftstr + "))[.\\s\\S]*?(?=(" + rightstr + "))", RegexOptions.Multiline | RegexOptions.Singleline);
            return rg.Match(str).Value;
        }
        #endregion

        #region 设置目标窗体大小，位置
        /// <summary>
        /// 设置目标窗体大小，位置
        /// </summary>
        /// <param name="hWnd">目标句柄</param>
        /// <param name="x">目标窗体新位置X轴坐标</param>
        /// <param name="y">目标窗体新位置Y轴坐标</param>
        /// <param name="nWidth">目标窗体新宽度</param>
        /// <param name="nHeight">目标窗体新高度</param>
        /// <param name="BRePaint">是否刷新窗体</param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool BRePaint);

        [DllImport("Shcore.dll", SetLastError = true)]
        internal static extern int GetDpiForMonitor(IntPtr hmonitor, Monitor_DPI_Type dpiType, out uint dpiX, out uint dpiY);

        internal enum Monitor_DPI_Type : int
        {
            MDT_Effective_DPI = 0,
            MDT_Angular_DPI = 1,
            MDT_Raw_DPI = 2,
            MDT_Default = MDT_Effective_DPI
        }

        public static DisplayArea GetDisplayArea(Window window)
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(window);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return DisplayArea.GetFromWindowId(wndId, DisplayAreaFallback.Primary);
        }

        public static double GetScaleAdjustment(Window window)
        {
            DisplayArea displayArea = GetDisplayArea(window);
            IntPtr hMonitor = Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);

            // Get DPI.
            int result = GetDpiForMonitor(hMonitor, Monitor_DPI_Type.MDT_Default, out uint dpiX, out uint _);
            if (result != 0)
            {
                throw new Exception("Could not get DPI for monitor.");
            }

            uint scaleFactorPercent = (uint)(((long)dpiX * 100 + (96 >> 1)) / 96);
            return scaleFactorPercent / 100.0;
        }
        #endregion

        #region 获取文件夹大小
        public static async Task<double> GetDirctoryLength(string dir)
        {
            if (Directory.Exists(dir))
            {
                double totalFileSize = 0;

                DirectoryInfo directoryInfo = new DirectoryInfo(dir);
                FileInfo[] fileInfos = directoryInfo.GetFiles();

                await Task.Run(() =>
                {
                    foreach (FileInfo fileInfo in fileInfos)
                    {
                        totalFileSize += fileInfo.Length;
                    }
                });

                return totalFileSize;
            }
            else
            {
                //throw new DirectoryNotFoundException($"找不到路径 \"{dir}\"。");
                return 0;
            }
        }
        #endregion

        #region 计算文件大小
        private const double KBCount = 1024;
        private const double MBCount = KBCount * 1024;
        private const double GBCount = MBCount * 1024;
        private const double TBCount = GBCount * 1024;

        /// <summary>
        /// 得到适应的大小
        /// </summary>
        /// <param name="path"></param>
        /// <returns>string</returns>
        public static string GetAutoSizeString(double size, int roundCount)
        {
            if (KBCount > size)
            {
                return Math.Round(size, roundCount) + "B";
            }
            else if (MBCount > size)
            {
                return Math.Round(size / KBCount, roundCount) + "KB";
            }
            else if (GBCount > size)
            {
                return Math.Round(size / MBCount, roundCount) + "MB";
            }
            else if (TBCount > size)
            {
                return Math.Round(size / GBCount, roundCount) + "GB";
            }
            else
            {
                return Math.Round(size / TBCount, roundCount) + "TB";
            }
        }
        #endregion

        #region 检查是否最小化
        [DllImport("user32")]
        public static extern bool IsIconic(IntPtr hwnd);
        #endregion

        #region 合法文件名
        public static string ReplaceBadCharOfFileName(string fileName)
        {
            string str = fileName;
            str = str.Replace("\\", string.Empty);
            str = str.Replace("/", string.Empty);
            str = str.Replace(":", string.Empty);
            str = str.Replace("*", string.Empty);
            str = str.Replace("?", string.Empty);
            str = str.Replace("\"", string.Empty);
            str = str.Replace("<", string.Empty);
            str = str.Replace(">", string.Empty);
            str = str.Replace("|", string.Empty);
            return str;
        }
        #endregion

        /*
                public static async Task<ImageSource> GetCover(string path)
                {
                    try
                    {
                        Track track = null;
                        IList<PictureInfo> embeddedPictures = null;
                        await Task.Run(() => { track = new(path); embeddedPictures = track.EmbeddedPictures; });
                        if (track.EmbeddedPictures.Any())
                        {
                            await ImageFromBytes((track.EmbeddedPictures.First().PictureData));
                        }

                    }
                    catch { }
                    return null;
                }

                public async static System.Threading.Tasks.Task<BitmapImage> ImageFromBytes(byte[] bytes)
                {
                    var image = new BitmapImage();

                    try
                    {
                        var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                        await stream.WriteAsync(bytes.AsBuffer());
                        stream.Seek(0);
                        await image.SetSourceAsync(stream);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.App.logManager.Log(ex.Message);
                    }

                    return image;
                }
                public static async Task<ImageSource> SaveToImageSource(this MemoryStream stream)
                {
                    ImageSource imageSource = null;
                    try
                    {
                        var ras = stream.AsRandomAccessStream();
                        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(ras);
                        var provider = await decoder.GetPixelDataAsync();
                        byte[] buffer = provider.DetachPixelData();
                        WriteableBitmap bitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                        await bitmap.PixelBuffer.AsStream().WriteAsync(buffer, 0, buffer.Length);
                        imageSource = bitmap;
                    }
                    catch { }
                    return imageSource;
                }
        */

        #region 判断文件编码
        /// <summary>
        /// 根据文件尝试返回字符编码
        /// </summary>
        /// <param name="file">文件路径</param>
        /// <param name="defEnc">没有BOM返回的默认编码</param>
        /// <returns>如果文件无法读取，返回null。否则，返回根据BOM判断的编码或者缺省编码（没有BOM）。</returns>
        public static Encoding GetEncoding(string file, Encoding defEnc)
        {
            using (var stream = File.OpenRead(file))
            {
                //判断流可读？
                if (!stream.CanRead)
                    return null;
                //字节数组存储BOM
                var bom = new byte[4];
                //实际读入的长度
                int readc;

                readc = stream.Read(bom, 0, 4);

                if (readc >= 2)
                {
                    if (readc >= 4)
                    {
                        //UTF32，Big-Endian
                        if (CheckBytes(bom, 4, 0x00, 0x00, 0xFE, 0xFF))
                            return new UTF32Encoding(true, true);
                        //UTF32，Little-Endian
                        if (CheckBytes(bom, 4, 0xFF, 0xFE, 0x00, 0x00))
                            return new UTF32Encoding(false, true);
                    }
                    //UTF8
                    if (readc >= 3 && CheckBytes(bom, 3, 0xEF, 0xBB, 0xBF))
                        return new UTF8Encoding(true);

                    //UTF16，Big-Endian
                    if (CheckBytes(bom, 2, 0xFE, 0xFF))
                        return new System.Text.UnicodeEncoding(true, true);
                    //UTF16，Little-Endian
                    if (CheckBytes(bom, 2, 0xFF, 0xFE))
                        return new System.Text.UnicodeEncoding(false, true);
                }

                return defEnc;
            }
        }

        //辅助函数，判断字节中的值
        public static bool CheckBytes(byte[] bytes, int count, params int[] values)
        {
            for (int i = 0; i < count; i++)
                if (bytes[i] != values[i])
                    return false;
            return true;
        }
        #endregion

        #region 时间戳转DateTime
        /// <summary>
        /// 指定时间戳转为时间。
        /// </summary>
        /// <param name="timeStamp">需要被反转的时间戳</param>
        /// <param name="accurateToMilliseconds">是否精确到毫秒</param>
        /// <returns>返回时间戳对应的DateTime</returns>
        public static DateTime UnixGetTime(long timeStamp, bool accurateToMilliseconds = false)
        {
            if (accurateToMilliseconds)
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(timeStamp).LocalDateTime;
            }
            else
            {
                return DateTimeOffset.FromUnixTimeSeconds(timeStamp).LocalDateTime;
            }
        }
        #endregion

        public async static Task<BitmapImage> ImageFromBytes(byte[] bytes, int width = 0, int height = 0)
        {
            var image = new BitmapImage();
            InMemoryRandomAccessStream stream = null;
            image.DecodePixelWidth = width;
            image.DecodePixelHeight = height;

            await Task.Run(async () =>
            {
                try
                {
                    stream = new();
                    await stream.WriteAsync(bytes.AsBuffer());
                    stream.Seek(0);
                }
                catch (Exception ex)
                {
                    App.logManager.Log("ImageFromBytes", ex.Message, Background.LogLevel.Error);
                }
            });
            await image.SetSourceAsync(stream);
            await Task.Run(() =>
            {
                stream.Dispose();
            });

            return image;
        }

        public static async Task<ImageSource> GetCover(string path, int width = 0, int height = 0)
        {
            ImageSource result = null;
            try
            {
                TagLib.File f = null;
                var a = await Task.Run(() =>
                {
                    try
                    {
                        f = TagLib.File.Create(path);
                    }
                    catch { }
                    if (f is null) return null;
                    if (f.Tag.Pictures is null) return null;
                    if (f.Tag.Pictures.Length == 0) return null;

                    foreach (var data in f.Tag.Pictures)
                    {
                        switch (data.Type)
                        {
                            case TagLib.PictureType.FrontCover:
                            case TagLib.PictureType.BackCover:
                                if (data.Data.Data.Length == 0) continue;
                                f.Dispose();
                                return data.Data.Data;
                        }
                    }

                    var bin = f.Tag.Pictures[0].Data.Data;
                    f.Dispose();
                    return bin;
                });
                if (a is null) return null;
                result = await ImageFromBytes(a, width, height);
            }
            catch { result = null; }

            return result;
        }

        public static async Task<byte[]> GetLocalImageByte(MusicData musicData)
        {
            byte[] result;
            TagLib.File f = null;
            if (musicData is null) return null;
            if (musicData.From != MusicFrom.localMusic) return null;
            if (string.IsNullOrEmpty(musicData.InLocal)) return null;

            try
            {
                var a = await Task.Run(() =>
                {
                    try
                    {
                        f = TagLib.File.Create(musicData.InLocal);
                    }
                    catch
                    {
                        return null;
                    }
                    if (f is null) return null;
                    if (f.Tag.Pictures is null) return null;
                    if (f.Tag.Pictures.Length == 0) return null;

                    foreach (var data in f.Tag.Pictures)
                    {
                        switch (data.Type)
                        {
                            case TagLib.PictureType.FrontCover:
                            case TagLib.PictureType.BackCover:
                                if (data.Data.Data.Length == 0) continue;
                                var imageData = data.Data.Data;
                                f.Dispose();
                                return imageData;
                        }
                    }

                    var bin = f.Tag.Pictures[0].Data.Data;
                    f.Dispose();
                    return bin;
                });
                if (a is null) return null;
                result = a;
            }
            catch { result = null; }
            return result;
        }

        public static async Task<ImageSource> SaveToImageSource(this byte[] imageBuffer)
        {
            ImageSource imageSource = null;
            MemoryStream stream = null;
            IRandomAccessStream ras = null;
            await Task.Run(() =>
            {
                stream = new MemoryStream(imageBuffer);
                ras = stream.AsRandomAccessStream();
            });

            try
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(ras);
                var provider = await decoder.GetPixelDataAsync();
                byte[] buffer = await Task.Run(() => provider.DetachPixelData());
                WriteableBitmap bitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                await bitmap.PixelBuffer.AsStream().WriteAsync(buffer, 0, buffer.Length);
                imageSource = bitmap;
                await Task.Run(() => { stream.Dispose(); ras.Dispose(); });
            }
            catch { }

            return imageSource;
        }
       
        public static string ToMD5(string strs)
        {
            using MD5 md5 = MD5.Create();
            byte[] bytes = Encoding.Default.GetBytes(strs);//将要加密的字符串转换为字节数组
            byte[] encryptData = md5.ComputeHash(bytes);//将字符串加密后也转换为字符数组
            return Convert.ToBase64String(encryptData);//将加密后的字节数组转换为加密字符串
        }

        public static bool IsAccentColorDark()
        {
            var uiSettings = new UISettings();
            var c = uiSettings.GetColorValue(UIColorType.Accent);
            bool isDark = (5 * c.G + 2 * c.R + c.B) <= 8 * 128;
            return isDark;
        }

        public static async Task<bool> OpenInBrowser(Uri uri)
        {
            var result = await MainWindow.ShowDialog("跳转外部链接", $"将会打开浏览器以跳转到外部链接：\n{uri.OriginalString}", "取消", "确认", defaultButton: Microsoft.UI.Xaml.Controls.ContentDialogButton.Close);
            if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
                return await Launcher.LaunchUriAsync(uri);
            else
                return false;
        }

        public static async Task<bool> OpenInBrowser(string url)
        {
            return await OpenInBrowser(new Uri(url));
        }
    }

    public static class LyricHelper
    {
        public static string NoneLyricString = "·········";
        public async static Task<LyricData[]> LyricToLrcData(string lyricText, bool romaji = true)
        {
            Dictionary<TimeSpan, LyricData> lyricDictionary = new();
            IOrderedEnumerable<KeyValuePair<TimeSpan, LyricData>> sorter = null;
            await Task.Run(() =>
            {
                foreach (var lyric in lyricText.Split('\n'))
                {
                    if (string.IsNullOrEmpty(lyric)) continue;
                    var timesAndLyric = lyric.Split(']');
                    //当一句歌词在不同时间段时
                    if (timesAndLyric.Count() > 2)
                    {
                        for (int i = timesAndLyric.Count(); i > 0; i--)
                        {
                            var timef = timesAndLyric[i - 1].Replace("[", "");
                            TimeSpan? timesResultBackup = GetLrcTimeStringTimeSpan(timef);
                            if (timesResultBackup is null) continue;
                            TimeSpan timesResult = (TimeSpan)timesResultBackup;

                            if (!lyricDictionary.ContainsKey(timesResult))
                            {
                                string text = timesAndLyric.Last();
                                if (text == "") text = NoneLyricString;
                                lyricDictionary.Add(timesResult, new(new() { text }, null, timesResult));
                            }
                        }
                    }
                    //当一句歌词在只在同一时间段时
                    else
                    {
                        var timef = timesAndLyric[0].Replace("[", "");
                        TimeSpan? timesResultBackup = GetLrcTimeStringTimeSpan(timef);
                        if (timesResultBackup is null) continue;
                        TimeSpan timesResult = (TimeSpan)timesResultBackup;
                        
                        var text = timesAndLyric[1];
                        if (text == "" || text == "...") text = NoneLyricString;

                        //当有相同时间的歌词时
                        if (lyricDictionary.ContainsKey(timesResult))
                        {
                            var l = lyricDictionary[timesResult];
                            if (text != NoneLyricString)
                            {
                                l.Lyric.Add(text);
                            }
                        }
                        //当没有相同时间的歌词时
                        else
                        {
                            lyricDictionary.Add(timesResult, new(new() { text }, null, timesResult));
                        }
                    }
                }

                if (lyricDictionary.Count <= 2)
                {
                    lyricDictionary.Clear();
                }
                if (lyricDictionary.Any())
                {
                    //lastLyric
                    lyricDictionary.Add(TimeSpan.MaxValue,
                        new(null, null, TimeSpan.MaxValue));
                }
                sorter = from pair in lyricDictionary orderby pair.Key ascending select pair;
            });

            List<LyricData> lyricList = new();
            Kawazu.KawazuConverter converter = new();
            foreach (var l in sorter)
            {
                if (l.Value.Lyric?.First() != null)
                {
                    var percent = await Task.Run(() =>
                    {
                        int count = 0;
                        foreach (var c in l.Value.Lyric.First())
                        {
                            //if (((byte)c) % 2 == 0) count++;
                            if (Regex.IsMatch(c.ToString(), @"^[\u0800-\u4e00]+$")) count++;
                        }
                        return (double)count / l.Value.Lyric.First().Length;
                    });
                    if (percent >= 0.15)
                    {
                        var romaji = await converter.Convert(l.Value.Lyric.First(), Kawazu.To.Romaji, Kawazu.Mode.Spaced, Kawazu.RomajiSystem.Nippon);
                        l.Value.Romaji = romaji;
                    }

                }
                lyricList.Add(l.Value);
            }
            converter.Dispose();
            return lyricList.ToArray();
        }

        static TimeSpan? GetLrcTimeStringTimeSpan(string timeString)
        {
            if (timeString.Contains('.'))
            {
                var times = timeString.Split('.');
                var timesa = TimeSpan.TryParse($"00:{times[0]}", null, out TimeSpan timesb);

                if (times.Length == 1) return null;
                if (!timesa) return null;

                var timeMillsStr = times[1];
                var parse = int.TryParse(timeMillsStr, out int iparse);
                if (!parse) return null;

                switch (timeMillsStr.Length)
                {
                    case 1: timeMillsStr += "00"; break;
                    case 2: timeMillsStr += "0"; break;
                    case 3: break;
                    default: App.logManager.Log("LyricManager", "歌词源文件时间精度较低。", Background.LogLevel.Warning); break;
                }
                var timeMills = TimeSpan.FromMilliseconds(int.Parse(timeMillsStr));
                return timesb + timeMills;
            }
            else
            {
                var times = timeString.Split(':');
                if (times.Length == 0 || times.Length == 1) return null;
                var timesa = TimeSpan.TryParse($"00:{times[0]}:{times[1]}", null, out TimeSpan timesb);

                var timeMillsStr = "0";
                if (times.Length == 3)
                {
                    timeMillsStr = times[2];
                    var parse = int.TryParse(timeMillsStr, out int iparse);
                    if (!parse) return null;

                    switch (timeMillsStr.Length)
                    {
                        case 1: timeMillsStr += "00"; break;
                        case 2: timeMillsStr += "0"; break;
                        case 3: break;
                        default: App.logManager.Log("LyricManager", "歌词源文件时间精度较低。", Background.LogLevel.Warning); break;
                    }
                }
                var timeMills = TimeSpan.FromMilliseconds(int.Parse(timeMillsStr));
                return timesb + timeMills;
            }

        }
    }
}
