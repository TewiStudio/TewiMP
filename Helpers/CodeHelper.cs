using System;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.System;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using WinRT.Interop;
using TewiMP.Core;
using TewiMP.Core.Music;
using TewiMP.Services;

namespace TewiMP.Helpers
{
    public static class AnimateHelper
    {
        public static void AnimateColor(UIElement element, Windows.UI.Color color, double TimeSecond,
                                        float cubicBezierEasing1, float cubicBezierEasing2, float cubicBezierEasing3, float cubicBezierEasing4,
                                        out Visual elementVisual, out Compositor compositor, out ColorKeyFrameAnimation animation)
        {
            elementVisual = ElementCompositionPreview.GetElementVisual(element);
            compositor = elementVisual.Compositor;
            AnimateColor(elementVisual, color, TimeSecond, cubicBezierEasing1, cubicBezierEasing2, cubicBezierEasing3, cubicBezierEasing4,
                out animation);
        }
        
        public static void AnimateColor(Visual visual, Windows.UI.Color color, double TimeSecond,
                                        float cubicBezierEasing1, float cubicBezierEasing2, float cubicBezierEasing3, float cubicBezierEasing4,
                                        out ColorKeyFrameAnimation animation)
        {
            Visual elementVisual = visual;
            var compositor = elementVisual.Compositor;

            animation = compositor.CreateColorKeyFrameAnimation();
            var easing = compositor.CreateCubicBezierEasingFunction(new Vector2(cubicBezierEasing1, cubicBezierEasing2), new Vector2(cubicBezierEasing3, cubicBezierEasing4));

            animation.Duration = TimeSpan.FromSeconds(TimeSecond);
            animation.InsertKeyFrame(1, color, easing);
        }
        
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
                        System.Diagnostics.LogManager.Log(ex.Message);
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
                    LogService.Log("ImageFromBytes", ex.Message, LogLevel.Error);
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
            if (string.IsNullOrEmpty(strs)) return string.Empty;

            // 将字符串转换为 UTF8 字节，使用 Span 避免分配数组
            // 假设输入字符串不会超级长，计算最大字节数
            int maxByteCount = Encoding.UTF8.GetMaxByteCount(strs.Length);

            // 使用 stackalloc 在栈上分配内存
            Span<byte> inputBuffer = maxByteCount <= 1024
                ? stackalloc byte[maxByteCount]
                : new byte[maxByteCount];

            int bytesWritten = Encoding.UTF8.GetBytes(strs, inputBuffer);

            // 切片，只取实际写入的长度
            ReadOnlySpan<byte> inputSpan = inputBuffer.Slice(0, bytesWritten);

            // 计算 MD5 (MD5 固定 16 字节)
            Span<byte> hashBytes = stackalloc byte[16];
            MD5.HashData(inputSpan, hashBytes);

            // 转换为 Base64 字符串
            return Convert.ToHexString(hashBytes);
        }

        public static bool IsAccentColorDark(Windows.UI.Color c)
        {
            //var uiSettings = new UISettings();
            //var c = uiSettings.GetColorValue(UIColorType.Accent);
            bool isDark = (5 * c.G + 2 * c.R + c.B) <= 8 * 128;
            return isDark;
        }

        public static Windows.UI.Color A(this Windows.UI.Color color, byte value)
        {
            color.A = value;
            return color;
        }

        public static Windows.UI.Color R(this Windows.UI.Color color, byte value)
        {
            color.R = value;
            return color;
        }

        public static Windows.UI.Color G(this Windows.UI.Color color, byte value)
        {
            color.G = value;
            return color;
        }

        public static Windows.UI.Color B(this Windows.UI.Color color, byte value)
        {
            color.B = value;
            return color;
        }

        public static Windows.UI.Color Lighten(this Windows.UI.Color color, float amount)
        {
            return Windows.UI.Color.FromArgb(
                color.A,
                (byte)(color.R + (255 - color.R) * amount),
                (byte)(color.G + (255 - color.G) * amount),
                (byte)(color.B + (255 - color.B) * amount));
        }

        public static Windows.UI.Color Darken(this Windows.UI.Color color, float amount)
        {
            return Windows.UI.Color.FromArgb(
                color.A,
                (byte)(color.R * (1 - amount)),
                (byte)(color.G * (1 - amount)),
                (byte)(color.B * (1 - amount)));
        }

        public static Windows.UI.Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            if (value > 255)
                value = 255;
            var v = Convert.ToByte(value);
            var p = Convert.ToByte(value * (1 - saturation));
            var q = Convert.ToByte(value * (1 - f * saturation));
            var t = Convert.ToByte(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Windows.UI.Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return Windows.UI.Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return Windows.UI.Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return Windows.UI.Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return Windows.UI.Color.FromArgb(255, t, p, v);
            else
                return Windows.UI.Color.FromArgb(255, v, p, q);
        }

        public static void ColorToHSV(this Windows.UI.Color color, out double hue, out double saturation, out double value)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            float hsbB = max / 255.0f;
            float hsbS = max == 0 ? 0 : (max - min) / (float)max;

            float hsbH = 0;
            if (max == min)
            {
                hsbH = 0;
            }
            else if (max == color.R && color.G >= color.B)
            {
                hsbH = (color.G - color.B) * 60f / (max - min) + 0;
            }
            else if (max == color.R && color.G < color.B)
            {
                hsbH = (color.G - color.B) * 60f / (max - min) + 360;
            }
            else if (max == color.G)
            {
                hsbH = (color.B - color.R) * 60f / (max - min) + 120;
            }
            else if (max == color.B)
            {
                hsbH = (color.R - color.G) * 60f / (max - min) + 240;
            }
            hue = hsbH;
            saturation = hsbS;
            value = hsbB;
        }

        public static async Task<(Windows.UI.Color, Windows.UI.Color, Windows.UI.Color)> GetThemeColorAsync(string file)
        {
            if (!File.Exists(file)) return (Colors.Red, Colors.Red, Colors.Red);
            DateTime time = DateTime.Now;
            using var image = Image.FromFile(file);
            using var bitmap = new Bitmap(image.GetThumbnailImage(100, 100, () => false, nint.Zero));
            var colorThief = new ColorThiefDotNet.ColorThief();
            var qColor = await Task.Run(() => colorThief.GetColor(bitmap, 4));
            var c = qColor.Color;
            var result = Windows.UI.Color.FromArgb(c.A, c.R, c.G, c.B);
            result.ColorToHSV(out var h, out var s, out var v);

            //LogManager.Info("Album HSV before", $"h:{h}, s:{s}, v:{v}");
            ElementTheme elementTheme = App.MainWindowInstance.WindowGridBase.ActualTheme;
            var saturation = s + (elementTheme == ElementTheme.Dark ? .1 : 1);
            var value = v + (elementTheme == ElementTheme.Dark ? 1 : .01);
            var color1 = CodeHelper.ColorFromHSV(h, double.Clamp(saturation, 0, 1), double.Clamp(value, 0, 1));
            //LogManager.Info("Album HSV after", $"h:{h}, s:{saturation}, v:{value}");

            elementTheme = App.MainWindowInstance.WindowGridBase.ActualTheme == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
            saturation = s + (elementTheme == ElementTheme.Dark ? .1 : .6);
            value = v + (elementTheme == ElementTheme.Dark ? 1 : .1);
            var color2 = CodeHelper.ColorFromHSV(h, double.Clamp(saturation, 0, 1), double.Clamp(value, 0, 1));
            LogService.Elapsed("CodeHelper.GetThemeColorAsync", $"Get \"{file}\" theme color elapsed: {{0}}.", time);
            return (color1, color2, IsAccentColorDark(color1) ? Colors.White : Windows.UI.Color.FromArgb(228, 0, 0, 0));
        }

        public static async Task<bool> OpenInBrowser(Uri uri)
        {
            var result = await App.MainWindowInstance.ShowDialog("跳转外部链接", $"将会打开浏览器以跳转到外部链接：\n{uri.OriginalString}", "取消", "确认", defaultButton: Microsoft.UI.Xaml.Controls.ContentDialogButton.Close);
            if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
                return await Launcher.LaunchUriAsync(uri);
            else
                return false;
        }

        public static async Task<bool> OpenInBrowser(string url)
        {
            return await OpenInBrowser(new Uri(url));
        }

        public static T FindDescendant<T>(DependencyObject root)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);

                if (child is T t)
                    return t;

                var result = FindDescendant<T>(child);
                if (result != null)
                    return result;
            }

            return default;
        }
    }

    public static class StringSimilarity
    {
        /// <summary>
        /// 获取两个字符串的匹配程度 (0 ~ 1)
        /// 1 表示完全相同，0 表示完全不同
        /// </summary>
        public static double GetSimilarity(this string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2)) return 1.0;
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0.0;

            int distance = LevenshteinDistance(s1, s2);
            int maxLen = Math.Max(s1.Length, s2.Length);

            // 相似度 = (最大长度 - 编辑距离) / 最大长度
            return 1.0 - (double)distance / maxLen;
        }

        private static int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] dp = new int[n + 1, m + 1];

            for (int i = 0; i <= n; i++) dp[i, 0] = i;
            for (int j = 0; j <= m; j++) dp[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                    dp[i, j] = Math.Min(
                        Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                        dp[i - 1, j - 1] + cost
                    );
                }
            }
            return dp[n, m];
        }
    }
}
