using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media;
using TewiMP.Helpers;
using TewiMP.DataEditor;

namespace TewiMP.Media
{
    public static class ImageManage
    {
        public static List<MusicData> LoadingImages = new();
        static int loadNum = 0;
        static int maxLoadNum = 0;

        public static async Task<bool> DownloadPic(string a, string b)
        {
            try
            {
                await WebHelper.DownloadFileAsync(a, b);
            }
            catch { }

            bool error = await Task.Run(() =>
            {
                if (File.Exists(b))
                {
                    try
                    {
                        if (File.ReadAllBytes(b).Length == 0)
                        {
                            File.Delete(b);
                            return true;
                        }
                    }
                    catch { }
                }
                return false;
            });

            return error;
        }

        public static Dictionary<OnlyClass, Tuple<ImageSource, string>> localImageCache { get; set; } = new();
        /// <summary>
        /// 返回musicData对应的图像对象和图像所在的本地路径
        /// </summary>
        /// <param name="musicData"></param>
        /// <param name="decodePixelWidth"></param>
        /// <param name="decodePixelHeight"></param>
        /// <param name="useBitmapImage"></param>
        /// <returns>
        /// <list type="table">
        /// <item>T1: ImageSource，图像对象</item>
        /// <item>T2: string，图像在本地的路径</item>
        /// </list>
        /// </returns>
        public static async Task<Tuple<ImageSource, string>> GetImageSource(MusicData musicData, int decodePixelWidth = 0, int decodePixelHeight = 0, bool useBitmapImage = false)
        {
            foreach (var imageCache in localImageCache)
            {
                if (imageCache.Key.GetType() != typeof(MusicData)) continue;
                if (imageCache.Key == musicData)
                {
                    return new(imageCache.Value.Item1, imageCache.Value.Item2);
                }
            }

            ImageSource source = null;
            string resultPath = null;
            resultPath = await FileHelper.GetImageCache(musicData);

            if (musicData.From == MusicFrom.localMusic)
            {
                if (string.IsNullOrEmpty(resultPath))
                {
                    var imageByte = await CodeHelper.GetLocalImageByte(musicData);
                    if (imageByte != null)
                    {
                        string b = $@"{DataFolderBase.ImageCacheFolder}\{musicData.From}{musicData.MD5.Replace(@"/", "#")}";
                        await Task.Run(() =>
                        {
                            var f = File.Create(b);
                            f.Write(imageByte);
                            f.Close();
                            f.Dispose();
                        });
                        source = await FileHelper.GetImageSource(b, decodePixelWidth, decodePixelHeight, useBitmapImage);
                    }
                    else
                    {
                        string coverPath = await Task.Run(() =>
                        {
                            FileInfo fileInfo = new FileInfo(musicData.InLocal);
                            string coverPath = $"{fileInfo.DirectoryName}\\Cover.jpg";
                            if (File.Exists(coverPath)) return coverPath;
                            else return null;
                        });
                        if (coverPath != null)
                        {
                            source = await FileHelper.GetImageSource(coverPath, decodePixelWidth, decodePixelHeight, useBitmapImage);
                        }
                    }
                }
                else
                {
                    source = await FileHelper.GetImageSource(resultPath, decodePixelWidth, decodePixelHeight, useBitmapImage);
                }
            }
            else
            {
                while (LoadingImages.Contains(musicData))
                {
                    await Task.Delay(1000);
                }
                if (resultPath is null)
                {
                    while (loadNum > maxLoadNum)
                    {
                        await Task.Delay(400);
                    }
                    loadNum++;
                    LoadingImages.Add(musicData);

                    if (WebHelper.IsNetworkConnected)
                    {
                        string b = $@"{DataFolderBase.ImageCacheFolder}\{musicData.PluginSource}{(string.IsNullOrEmpty(musicData.Album?.ID) ? musicData.MD5.Replace(@"/", "#") : musicData.Album.ID)}";
                        string a;
                        if (musicData.Album?.PicturePath != null)
                        {
                            a = musicData.Album.PicturePath;
                        }
                        else
                        {
                            a = await WebHelper.GetPicturePathAsync(musicData);
                        }
                        bool error = await DownloadPic(a, b);
                        if (!error) resultPath = b;
                    }
                }

                try
                {
                    source = await FileHelper.GetImageSource(resultPath, decodePixelWidth, decodePixelHeight, useBitmapImage);
                }
                finally
                {
                    LoadingImages.Remove(musicData);
                    loadNum--;
                }
            }

            Tuple<ImageSource, string> resultTuple = new(source, resultPath);
            //localImageCache.Add(musicData, resultTuple);
            return resultTuple;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="musicListData"></param>
        /// <param name="decodePixelWidth"></param>
        /// <param name="decodePixelHeight"></param>
        /// <param name="useBitmapImage"></param>
        /// <returns>Item1 为 ImageSource，Item2 为获取到 ImageSource 的文件路径</returns>
        public static async Task<Tuple<ImageSource, string>> GetImageSource(MusicListData musicListData, int decodePixelWidth = 0, int decodePixelHeight = 0, bool useBitmapImage = false)
        {
            if (musicListData is null) return null;

            foreach (var imageCache in localImageCache)
            {
                if (imageCache.Key.GetType() != typeof(MusicListData)) continue;
                if (imageCache.Key == musicListData)
                {
                    return new(imageCache.Value.Item1, imageCache.Value.Item2);
                }
            }

            string cachePath = await FileHelper.GetImageCache(musicListData);
            string resultPath = null;

            if (cachePath != null)
            {
                resultPath = cachePath;
            }
            else
            {
                if (WebHelper.IsNetworkConnected)
                {
                    string b = $@"{DataFolderBase.ImageCacheFolder}\{musicListData.ListFrom}{musicListData.ListDataType}{musicListData.ID}";
                    await Task.Run(() =>
                    {
                        if (!File.Exists(b))
                            File.Create(b).Close();
                    });
                    await WebHelper.DownloadFileAsync(musicListData.PicturePath, b);
                    resultPath = b;
                }
                else
                {
                    resultPath = "/Images/icon.png";
                }
            }

            var source = await FileHelper.GetImageSource(resultPath, decodePixelWidth, decodePixelHeight, useBitmapImage);

            Tuple<ImageSource, string> resultTuple = new(source, resultPath);
            //localImageCache.Add(musicListData, resultTuple);
            return resultTuple;
        }
    }
}
