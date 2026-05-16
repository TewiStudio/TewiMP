using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TewiMP.Services;
using TewiMP.Core.Music;

namespace TewiMP.Helpers
{
    public static class WebHelper
    {
        #region 属性
        public static string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/102.0.5005.124 Safari/537.36 Edg/102.0.1245.44";
        public static HttpClient Client = new HttpClient();
        private static bool IsRequested = false;
        #endregion

        #region 联网检测
        [DllImport("wininet.dll", EntryPoint = "InternetGetConnectedState")]
        public extern static bool InternetGetConnectedState(out int conState, int reader);
        public static bool IsNetworkConnected
        {
            get
            {
                var n = 0;
                if (!InternetGetConnectedState(out n, 0)) return false;
                return true;
            }
        }
        #endregion/// <summary>

        private static List<string> DownloadingPathCache = new();
        /// <summary>
        /// 下载文件 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="downloadPath"></param>
        /// <returns></returns>
        /// <exception cref="WebException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public static async Task DownloadFileAsync(
        string address,
        string downloadPath,
        IProgress<double>? progress = null,
        CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentNullException(nameof(address));

            if (!Uri.TryCreate(address, UriKind.Absolute, out Uri uri))
                throw new InvalidOperationException("无法定位到网络地址，请检查域名服务器或DNS配置。");

            DownloadingPathCache.Add(address);

            try
            {
                using var response = await Client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var canReportProgress = totalBytes > 0 && progress != null;

                await using var contentStream = await response.Content.ReadAsStreamAsync(token);
                await using var fileStream = File.Create(downloadPath);

                var buffer = new byte[81920]; // 80KB buffer
                long totalRead = 0;
                int read;

                while ((read = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length), token)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, read), token);
                    totalRead += read;

                    if (canReportProgress)
                    {
                        double pct = (double)totalRead / totalBytes;
                        progress!.Report(pct);
                    }
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException or TaskCanceledException)
            {
                LogService.Info("DownloadFileAsync", "下载已取消。");
                // 取消时删除未完成文件
                if (File.Exists(downloadPath)) File.Delete(downloadPath);
                throw;
            }
            catch (Exception ex)
            {
                LogService.Error("DownloadFileAsync", $"下载失败：{ex.Message}");
                throw;
            }
            finally
            {
                DownloadingPathCache.Remove(address);
            }
        }

        /// <summary>
        /// 获取网址返回字符串
        /// </summary>
        /// <param name="address"></param>
        /// <param name="timeOutSec"></param>
        /// <returns></returns>
        /// <exception cref="System.Net.WebException"></exception>
        public static async Task<string> GetStringAsync(string address, int timeOutSec = 7)
        {
            if (!IsNetworkConnected) throw new WebException("网络未连接。");

            return await Client.GetStringAsync(address);
        }

        static List<MusicData> loadingImages = [];
        public static async Task<string> GetPicturePathAsync(MusicData musicData)
        {
            while (loadingImages.Count > 1)
            {
                //System.Diagnostics.LogManager.Log(musicData.Title);
                await Task.Delay(300);
            }
            loadingImages.Add(musicData);

            string addressResult = null;

            try
            {
                switch (musicData.From)
                {
                    case MusicFrom.pluginMusicSource:
                        if (musicData.Album.ID is null)
                        {
                            addressResult = await musicData.GetMusicSourcePlugin().GetPicFromMusicData(musicData);
                            //System.Diagnostics.LogManager.Log(addressResult);
                            /*string address = $"http://music.163.com/api/song/detail/?id={musicData.ID}&ids=%5B{musicData.ID}%5D";
                            var res = JObject.Parse(await GetStringAsync(address));*/

                        }
                        else
                        {
                            var album = await musicData.GetMusicSourcePlugin().GetAlbum(musicData.Album.ID);
                            addressResult = album?.PicturePath;
                        }
                        break;
                    default:
                        addressResult = musicData.Album.PicturePath;
                        break;
                }
            }
            catch { addressResult = null; }

            loadingImages.Remove(musicData);
            return addressResult;
        }
    }
}
