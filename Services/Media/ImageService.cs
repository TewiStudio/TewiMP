using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using TewiMP.Core.Music;
using TewiMP.Helpers;
using TewiMP.Services.Storage;

namespace TewiMP.Services.Media;

public static class ImageService
{
    public static List<string> LoadingImages = [];
    static int loadNum = 0;
    static int maxLoadNum = 0;

    public static async Task<bool> DownloadPicAsync(string url, string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(url)) return false;

            await WebHelper.DownloadFileAsync(url, filePath);

            var fileInfo = new FileInfo(filePath);

            // 检查下载是否有效
            if (fileInfo.Exists && fileInfo.Length > 0)
            {
                return true; // 下载成功
            }

            // 下载无效（文件不存在或为空），清理垃圾文件
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }

            return false; // 下载失败（文件无效）
        }
        catch
        {
            // 发生异常（网络错误或文件占用等）
            return false; // 下载失败
        }
    }

    // 用于任务去重：Key是文件唯一标识，Value是正在进行的任务
    private static readonly ConcurrentDictionary<string, Lazy<Task<Uri>>> _pendingTasks = new();
    private static readonly SemaphoreSlim _downloadSemaphore = new(5);

    public static async Task<Uri> GetImageUri(MusicData musicData)
    {
        if (musicData is null) return default;

        // 优先从缓存获取
        string imageFullName = CacheFileHelpers.GetImageCache(musicData);
        if (File.Exists(imageFullName))
        {
            return imageFullName.ToImageUri();
        }

        var lazyTask = _pendingTasks.GetOrAdd(imageFullName, _ =>
            new Lazy<Task<Uri>>(() => DownloadAndProcessImageAsync(musicData, imageFullName),
            LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            var result = await lazyTask.Value;
            if (result is null)
            {
                LogService.Error(nameof(GetImageUri), $"Get image uri failed: {musicData} / {imageFullName}");
            }
            return result;
        }
        catch (Exception)
        {
            _pendingTasks.TryRemove(new KeyValuePair<string, Lazy<Task<Uri>>>(imageFullName, lazyTask));
            return null;
        }
    }

    /// <summary>
    /// 返回musicData对应的图像Uri
    /// </summary>
    /// <param name="musicData"></param>
    /// <returns>
    /// </returns>
    private static async Task<Uri> DownloadAndProcessImageAsync(MusicData musicData, string imageFullName)
    {
        try
        {
            // 二次检查：并发情况下，可能前一个任务刚完成，这里再查一次文件是否存在
            if (File.Exists(imageFullName))
            {
                return imageFullName.ToImageUri();
            }

            if (musicData.From == MusicFrom.localMusic)
            {
                return await SaveAndGetLocalMusicImage(musicData, imageFullName);
            }
            else
            {
                return await SaveAndGetPluginMusicImage(musicData, imageFullName);
            }
        }
        catch (Exception err)
        {
            LogService.Error(nameof(GetImageUri), $"获取歌曲封面失败。\n{err}");
            return null;
        }
        finally
        {
            // 任务完成后，从字典中移除
            _pendingTasks.TryRemove(imageFullName, out _);
        }
    }

    public static async Task<Uri> SaveAndGetLocalMusicImage(MusicData musicData, string destPath)
    {
        var imageByte = await CodeHelper.GetLocalImageByte(musicData);
        if (imageByte != null)
        {
            await File.WriteAllBytesAsync(destPath, imageByte);
            return destPath.ToImageUri();
        }
        else
        {
            FileInfo fileInfo = new(musicData.InLocal);
            string coverPath = Path.Combine(fileInfo.DirectoryName, "Cover.jpg");
            if (File.Exists(coverPath))
            {
                await Task.Run(() => File.Copy(coverPath, destPath));
                return coverPath.ToImageUri();
            }
        }
        return null;
    }

    public static async Task<Uri> SaveAndGetPluginMusicImage(MusicData musicData, string destPath)
    {
        if (!WebHelper.IsNetworkConnected) return null;

        await _downloadSemaphore.WaitAsync();
        try
        {
            string url = musicData.Album?.PicturePath != null
                ? musicData.Album.PicturePath
                : await WebHelper.GetPicturePathAsync(musicData);

            if (await DownloadPicAsync(url, destPath))
            {
                // 确保文件句柄已释放且文件可读
                //await EnsureFileReadableAsync(destPath);
                return destPath.ToImageUri();
            }
            return null;
        }
        finally
        {
            _downloadSemaphore.Release();
        }
    }

    private static async Task EnsureFileReadableAsync(string path)
    {
        int maxRetries = 5;
        int delay = 50; // ms

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                // 尝试以只读方式打开文件，不共享写入权限
                // 如果能成功打开，说明下载流已经彻底关闭了
                using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (stream.Length > 0)
                {
                    return; // 文件就绪
                }
            }
            catch (IOException)
            {
                // 文件被占用，等待一会重试
                await Task.Delay(delay);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="musicListData"></param>
    /// <param name="decodePixelWidth"></param>
    /// <param name="decodePixelHeight"></param>
    /// <param name="useBitmapImage"></param>
    /// <returns>Item1 为 ImageSource，Item2 为获取到 ImageSource 的文件路径</returns>
    public static async Task<Uri> GetImageUri(MusicListData musicListData)
    {
        if (musicListData is null) return null;

        string cachePath = CacheFileHelpers.GetImageCache(musicListData);
        if (musicListData.ListDataType == DataType.本地歌单) return cachePath.ToImageUri();
        if (File.Exists(cachePath)) return cachePath.ToImageUri();

        if (await DownloadPicAsync(musicListData.PicturePath, cachePath))
        {
            return cachePath.ToImageUri();
        }
        
        return null;
    }
}
