using System;
using System.IO;
using System.Threading.Tasks;
using TewiMP.Core.Music;
using TewiMP.Services.Storage;
using static System.Net.Mime.MediaTypeNames;

namespace TewiMP.Helpers
{
    public static class CacheFileHelpers
    {
        /// <summary>
        /// 查询或获取音频缓存文件路径。
        /// </summary>
        /// <param name="musicData"></param>
        /// <returns>
        /// !=null - 音频缓存文件路径 /
        /// null - 未查询到音频缓存
        /// </returns>
        public static async Task<string> GetAudioCache(MusicData musicData)
        {
            return await Task.Run(() =>
            {
                DirectoryInfo directory = new DirectoryInfo(DataFolderBase.AudioCacheFolder);
                FileInfo[] fileInfo = directory.GetFiles();
                foreach (FileInfo file in fileInfo)
                {
                    string name = file.Name;
                    if (name == $"{musicData.PluginInfoGUID}{musicData.ID}")
                    {
                        return file.FullName;
                    }
                }
                return null;
            });
        }

        /// <summary>
        /// 查询或获取图片缓存文件路径。
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>
        /// !=null - 图片缓存文件路径 /
        /// null - 未查询到图片缓存
        /// </returns>
        public static string GetImageCache(string imageCacheFileName)
        {
            var path = Path.Combine(DataFolderBase.ImageCacheFolder, imageCacheFileName);
            return path;
        }

        /// <summary>
        /// 查询或获取图片缓存文件路径。
        /// </summary>
        /// <param name="musicListData"></param>
        /// <returns>
        /// !=null - 图片缓存文件路径 /
        /// null - 未查询到图片缓存。如果为本地歌单会返回歌单记录的图片文件地址
        /// </returns>
        public static string GetImageCache(MusicListData musicListData)
        {
            var filename = musicListData.ListDataType == DataType.LocalPlaylist
                ? musicListData.PicturePath
                : GetImageCache(GetImageCacheFileName(musicListData));
            return filename;
        }

        /// <summary>
        /// 从 <paramref name="musicData"/> 获取缓存文件路径。
        /// </summary>
        /// <param name="musicData"></param>
        /// <returns>
        /// </returns>
        public static string GetImageCache(MusicData musicData)
        {
            var filename = GetImageCacheFileName(musicData);
            return GetImageCache(filename);
        }

        /// <summary>
        /// 根据 <paramref name="musicData"/> 获取图片缓存文件名。
        /// </summary>
        /// <param name="musicData"></param>
        /// <returns></returns>
        public static string GetImageCacheFileName(MusicData musicData)
        {
            return musicData.From == MusicFrom.localMusic
                ? $"{musicData.From}{CodeHelper.ToMD5(musicData.Album.Title)}"
                : $"{musicData.PluginInfoGUID}" +
                  $"{(string.IsNullOrEmpty(musicData.Album?.ID)
                        ? musicData.ID.Replace(@"/", "#")
                        : musicData.Album.ID)}";
        }

        /// <summary>
        /// 根据 <paramref name="musicData"/> 获取图片缓存文件名。
        /// </summary>
        /// <param name="musicData"></param>
        /// <returns></returns>
        public static string GetImageCacheFileName(MusicListData musicListData)
        {
            return $"{musicListData.PluginInfoGUID}{musicListData.ListDataType}{musicListData.ID}";
        }

        /// <summary>
        /// 查询或获取歌词缓存文件路径。
        /// </summary>
        /// <param name="musicData"></param>
        /// <returns>
        /// !=null - 歌词缓存文件路径 /
        /// null - 未查询到歌词缓存
        /// </returns>
        public static async Task<string> GetLyricCache(MusicData musicData)
        {
            return await Task.Run(() =>
            {
                if (musicData.From == MusicFrom.localMusic)
                {
                    var file = new FileInfo(musicData.InLocal);
                    string lrcPath = $"{(string.IsNullOrEmpty(file.Extension) ? file.FullName : file.FullName.Replace(file.Extension, ""))}.lrc";
                    if (File.Exists(lrcPath)) return lrcPath;
                }
                else
                {
                    DirectoryInfo directory = new DirectoryInfo(DataFolderBase.LyricCacheFolder);
                    FileInfo[] fileInfo = directory.GetFiles();
                    foreach (FileInfo file in fileInfo)
                    {
                        if (file.Name == $"{musicData.PluginInfoGUID}{musicData.ID}")
                        {
                            return file.FullName;
                        }
                    }
                }
                return null;
            });
        }

        public static Uri ToImageUri(this string filePath)
        {
            //System.Diagnostics.LogManager.Log(filePath);

            if (!Uri.TryCreate(filePath, UriKind.Absolute, out var uri))
                return new(DataFolderBase.IconPNGPath);

            if (uri.IsFile && !File.Exists(uri.LocalPath))
                return new(DataFolderBase.IconPNGPath);

            return uri;
        }
    }
}