using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using TewiMP.DataEditor;
using TewiMP.Helpers;
using System.Diagnostics;

namespace TewiMP.Background
{
    public class CacheManager
    {
        public delegate void CachingValueMusicData(MusicData musicData, object value);
        public event CachingValueMusicData AddingCacheMusicData;
        public event CachingValueMusicData CachedMusicData;
        public event CachingValueMusicData CachingStateChangeMusicData;
        public List<MusicData> InCachingMusicData = new();

        public CacheManager() { }

        /// <summary>
        /// 获取歌曲缓存地址
        /// </summary>
        /// <param name="data"></param>
        /// <returns>null = not exists</returns>
        public async Task<string> GetCachePath(MusicData data)
        {
            return await FileHelper.GetAudioCache(data);
        }

        /// <summary>
        /// 开始缓存歌曲
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="CacheIsLoadingException"></exception>
        public async Task<string> StartCacheMusic(MusicData data)
        {
            Debug.WriteLine($"[CacheManager]: 开始缓存：\"{data}\"");
            if (data.From == MusicFrom.localMusic) throw new Exception("无法缓存本地文件。");
            AddingCacheMusicData?.Invoke(data, "正在开始缓存");
            if (InCachingMusicData.Contains(data)) throw new CacheIsLoadingException("此音频正在缓冲中。");

            string musicPathResult = null;
            musicPathResult = await GetCachePath(data);
            if (musicPathResult != null)
            {
                Debug.WriteLine($"[CacheManager]: 已存在缓存 \"{musicPathResult}\"！");
                CachedMusicData?.Invoke(data, null);
                return musicPathResult; // 当检测到已缓存时返回
            }

            if (!WebHelper.IsNetworkConnected)
            {
                InCachingMusicData.Remove(data);
                CachedMusicData?.Invoke(data, "网络未连接，请连接网络后重试。");
                throw new WebException("网络未连接，请连接网络后重试。");
            }

            InCachingMusicData.Add(data);
            string musicWebAddress = await WebHelper.GetAudioAddressAsync(data);
            if (musicWebAddress is null)
            {
                InCachingMusicData.Remove(data);
                CachedMusicData?.Invoke(data, "无法获取歌曲链接。");
                throw new WebException($"无法获取歌曲链接。");
            }

            Debug.WriteLine($"[CacheManager]: 正在下载缓存文件...");
            musicPathResult = @$"{DataFolderBase.AudioCacheFolder}\{data.From}{data.ID}";
            await Task.Run(() => File.Create(musicPathResult).Close()); // 创建缓存文件

            try
            {
                WebClient TheDownloader = new WebClient();
                TheDownloader.DownloadProgressChanged += (s, e) =>
                {
                    if (e is null) return;
                    CachingStateChangeMusicData?.Invoke(data, e.ProgressPercentage);
                };
                TheDownloader.DownloadFileCompleted += (s, e) =>
                {
                    TheDownloader?.Dispose();
                };
                var fileBytes = await TheDownloader.DownloadDataTaskAsync(new Uri(musicWebAddress));
                await Task.Run(() => File.WriteAllBytes(musicPathResult, fileBytes));
            }
            catch (Exception err)
            {
                await Task.Run(() => File.Delete(musicPathResult));
                InCachingMusicData.Remove(data);
                CachedMusicData?.Invoke(data, $"加载缓存文件失败: {err.Message}");
                throw new FileLoadException($"加载缓存文件失败: {err.Message}");
            }

            bool notDownloaded = await Task.Run(() => File.ReadAllBytes(musicPathResult).Length <= 10);
            // 当文件没有下载完成
            if (notDownloaded)
            {
                InCachingMusicData.Remove(data);
                CachedMusicData?.Invoke(data, $"加载缓存文件失败: 下载的缓存不完整。");
                await Task.Run(() =>
                {
                    if (File.Exists(musicPathResult))
                        File.Delete(musicPathResult);
                });
                throw new CacheIsIncompleteException($"加载缓存文件失败: 下载的缓存不完整。");
            }

            InCachingMusicData.Remove(data);
            CachedMusicData?.Invoke(data, null);
            Debug.WriteLine($"[CacheManager]: 缓存完成：\"{musicPathResult}\"");
            return musicPathResult;
        }
    }

    public class CacheIsLoadingException: Exception
    {
        public CacheIsLoadingException() : base() { }
        public CacheIsLoadingException(string message) : base(message) { }
        public CacheIsLoadingException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class CacheIsIncompleteException: Exception
    {
        public CacheIsIncompleteException() : base() { }
        public CacheIsIncompleteException(string message) : base(message) { }
        public CacheIsIncompleteException(string message, Exception innerException) : base(message, innerException) { }
    }
}
