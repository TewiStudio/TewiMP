using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TewiMP.DataEditor;

namespace TewiMP.Helpers
{
    public static class WebHelper
    {
        #region 属性
        public static string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/102.0.5005.124 Safari/537.36 Edg/102.0.1245.44";
        public static string NeteaseSearchAddress = "http://music.163.com/api/search/get/web?s={0}&type=1&offset={1}&limit={2}&total=true";
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
        public static async Task DownloadFileAsync(string address, string downloadPath)
        {
            if (address is null) return;
            if (downloadPath.Contains(address)) return;
            DownloadingPathCache.Add(address);
            bool err1 = false;

            await Task.Run(() =>
            {
                if (!Uri.TryCreate(address, UriKind.Absolute, out Uri uriResult)) err1 = true;
            });

            if (err1)
                throw new InvalidOperationException("无法定位到网络地址，请检查你的域名服务器是否正常工作或DNS配置是否正确。");

            var data = await Client.GetByteArrayAsync(address);
            await Task.Run(() =>
            {
                try
                {
                    var stream = File.Create(downloadPath);
                    stream.Write(data);
                    stream.Close();
                    stream.Dispose();
                }
                catch { }
            });
            DownloadingPathCache.Remove(address);
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
                //System.Diagnostics.App.logManager.Log(musicData.Title);
                await Task.Delay(400);
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
                            addressResult = await musicData.PluginSource.GetPicFromMusicData(musicData);
                            //System.Diagnostics.App.logManager.Log(addressResult);
                            /*string address = $"http://music.163.com/api/song/detail/?id={musicData.ID}&ids=%5B{musicData.ID}%5D";
                            var res = JObject.Parse(await GetStringAsync(address));*/

                        }
                        else
                        {
                            var album = await musicData.PluginSource.GetAlbum(musicData.Album.ID);
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
