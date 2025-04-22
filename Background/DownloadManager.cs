using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using TewiMP.Pages;
using TewiMP.Helpers;
using TewiMP.DataEditor;

namespace TewiMP.Background
{
    public enum DownloadStates { Waiting, Downloading, DownloadedSaving, DownloadedPreview, Downloaded, Error }
    public class DownloadData
    {
        public string Path = null;
        public string LrcPath = null;
        public MusicData MusicData;
        public long FileSize;
        public long DownloadedSize;
        public decimal DownloadPercent;
        public DownloadStates DownloadState;
        public string ErrorMessage = null;
    }

    public class DownloadManager
    {
        public DownloadPage NowDownloadPage { get; set; }

        public List<DownloadData> AllDownloadData { get; set; } = [];
        public List<DownloadData> WaitingDownloadData { get; set; } = [];
        public List<DownloadData> DownloadingData { get; set; } = [];
        public List<DownloadData> DownloadedData { get; set; } = [];
        public List<DownloadData> DownloadErrorData { get; set; } = [];

        public delegate void DownloadHandler(DownloadData data);
        public event DownloadHandler AddDownload;
        public event DownloadHandler OnDownloading;
        public event DownloadHandler OnDownloadedSaving;
        public event DownloadHandler OnDownloadedPreview;
        public event DownloadHandler OnDownloaded;
        public event DownloadHandler OnDownloadError;

        public DataFolderBase.DownloadQuality DownloadQuality { get; set; } = DataFolderBase.DownloadQuality.lossless;
        public DataFolderBase.DownloadNamedMethod DownloadNamedMethod { get; set; } = DataFolderBase.DownloadNamedMethod.t_ar_al;
        public int DownloadingMaximum { get; set; } = 3;
        public bool IDv3WriteImage { get; set; } = true;
        public bool IDv3WriteArtistImage { get; set; } = true;
        public bool IDv3WriteLyric { get; set; } = true;
        public bool SaveLyricToLrcFile { get; set; } = true;

        bool _pauseDownload = false;
        public bool PauseDownload
        {
            get => _pauseDownload;
            set
            {
                _pauseDownload = value;
                UpdateDownload();
            }
        }

        public DownloadManager()
        {
            OnDownloaded += (_) =>
            {
                UpdateDownload();
            };
            OnDownloadError += (_) =>
            {
                UpdateDownload();
            };
        }

        /// <summary>
        /// 添加下载歌曲
        /// </summary>
        /// <param name="musicData"></param>
        public void Add(MusicData musicData)
        {
            var dm = new DownloadData() { MusicData = musicData };
            if (!DownloadingData.Contains(dm) || !DownloadedData.Contains(dm) || !WaitingDownloadData.Contains(dm))
            {
                dm.DownloadState = DownloadStates.Waiting;
                AllDownloadData.Add(dm);
                WaitingDownloadData.Add(dm);
                AddDownload?.Invoke(dm);
                try
                {
                    UpdateDownload();
                }
                catch (Exception err)
                {
                    dm.ErrorMessage = err.Message;
                    OnDownloadError?.Invoke(dm);
#if DEBUG
                    App.logManager.Log("DownloadManager", err.Message, LogLevel.Error);
#endif
                }
            }
        }

        public void UpdateDownload()
        {
            while (DownloadingData.Count < DownloadingMaximum && WaitingDownloadData.Any() && !PauseDownload)
            {
                var dm = WaitingDownloadData[0];
                WaitingDownloadData.Remove(dm);
                DownloadingData.Add(dm);
                try
                {
#if DEBUG
                    App.logManager.Log("DownloadManager", $"下载中：{dm.MusicData.Title}");
#endif
                    StartDownload(dm);
                }
                catch (Exception err)
                {
#if DEBUG
                    App.logManager.Log("DownloadManager", err.Message);
#endif
                    DownloadingData.Remove(dm);
                    DownloadErrorData.Add(dm);
                    dm.DownloadState = DownloadStates.Error;
                    dm.ErrorMessage = err.Message;
                    OnDownloadError?.Invoke(dm);
                }
            }
        }

        public async void StartDownload(DownloadData dm)
        {
            dm.DownloadState = DownloadStates.Downloading;
            bool exists = await Task.Run(() => Directory.Exists(DataFolderBase.DownloadFolder));
            if (!exists)
            {
                dm.DownloadState = DownloadStates.Error;
                dm.ErrorMessage = "下载路径不存在";
                DownloadingData.Remove(dm);
                DownloadErrorData.Add(dm);
                OnDownloadError?.Invoke(dm);
                return;
            }

            string downloadPath1 = null;
            switch (DownloadNamedMethod)
            {
                case DataFolderBase.DownloadNamedMethod.t_ar_al:
                    downloadPath1 = Path.Combine(
                        $"{DataFolderBase.DownloadFolder}",
                        $"{CodeHelper.ReplaceBadCharOfFileName(dm.MusicData.Title)} - {CodeHelper.ReplaceBadCharOfFileName(dm.MusicData.ButtonName)}");
                    break;
                case DataFolderBase.DownloadNamedMethod.t_ar:
                    downloadPath1 = Path.Combine(
                        $"{DataFolderBase.DownloadFolder}",
                        $"{CodeHelper.ReplaceBadCharOfFileName(dm.MusicData.Title)} - {CodeHelper.ReplaceBadCharOfFileName(dm.MusicData.ArtistName)}");
                    break;
                case DataFolderBase.DownloadNamedMethod.t_al_ar:
                    downloadPath1 = Path.Combine(
                        $"{DataFolderBase.DownloadFolder}",
                        $"{CodeHelper.ReplaceBadCharOfFileName(dm.MusicData.Title)} - {CodeHelper.ReplaceBadCharOfFileName(dm.MusicData.Album.Title)} · {CodeHelper.ReplaceBadCharOfFileName(dm.MusicData.ArtistName)}");
                    break;
                case DataFolderBase.DownloadNamedMethod.t_al:
                    downloadPath1 = Path.Combine(
                        $"{DataFolderBase.DownloadFolder}",
                        $"{CodeHelper.ReplaceBadCharOfFileName(dm.MusicData.Title)} - {CodeHelper.ReplaceBadCharOfFileName(dm.MusicData.Album.Title)}");
                    break;
                case DataFolderBase.DownloadNamedMethod.t:
                    downloadPath1 = Path.Combine(
                        $"{DataFolderBase.DownloadFolder}",
                        $"{CodeHelper.ReplaceBadCharOfFileName(dm.MusicData.Title)}");
                    break;
            }
                
            string addressPath = null;
            bool isErr = false;

            var downloadQuality = DownloadQuality;
            var writeImage = IDv3WriteImage;
            var writeArtistImage = IDv3WriteArtistImage;
            var writeLyric = IDv3WriteLyric;
            var saveLyric = SaveLyricToLrcFile;
            try
            {
                addressPath = await App.metingServices.NeteaseServices.GetUrl(dm.MusicData.ID, (int)downloadQuality);
            }
            catch
            {
                isErr = true;
            }
            if (string.IsNullOrEmpty(addressPath) || isErr)
            {
                dm.DownloadState = DownloadStates.Error;
                dm.ErrorMessage = "找不到音频的服务器地址";
                DownloadingData.Remove(dm);
                DownloadErrorData.Add(dm);
                OnDownloadError?.Invoke(dm);
                return;
            }

            string lastName = Path.GetExtension(addressPath.Split("?").First());
            string downloadPath = downloadPath1 + lastName;
            string lyricPath = downloadPath1 + ".lrc";
            dm.Path = downloadPath;
            dm.LrcPath = lyricPath;
            //await WebHelper.DownloadFileAsync(addressPath, downloadPath);

            System.Net.WebClient TheDownloader = new System.Net.WebClient();
            TheDownloader.DownloadProgressChanged += (s, e) =>
            {
                if (e is null) return;
                dm.DownloadPercent = e.ProgressPercentage;
                dm.FileSize = e.TotalBytesToReceive;
                dm.DownloadedSize = e.BytesReceived;
                dm.DownloadState = DownloadStates.Downloading;
                OnDownloading?.Invoke(dm);
                //System.Diagnostics.App.logManager.Log(e.ProgressPercentage);
                //Set1(e.ProgressPercentage, (Convert.ToDouble(e.BytesReceived) / Convert.ToDouble(e.TotalBytesToReceive) * 100).ToString("0.0") + "%", zilongcn.Others.GetAutoSizeString(e.BytesReceived, 2) + "/" + zilongcn.Others.GetAutoSizeString(e.TotalBytesToReceive, 2));
            };
            TheDownloader.DownloadFileCompleted += (s, e) =>
            {
                TheDownloader?.Dispose();
            };
            var bytes = await TheDownloader.DownloadDataTaskAsync(new Uri(addressPath));
            dm.DownloadState = DownloadStates.DownloadedSaving;
            OnDownloadedSaving?.Invoke(dm);
            await Task.Run(() =>
            {
                if (!File.Exists(downloadPath))
                {
                    File.Create(downloadPath).Dispose();
                }
                File.WriteAllBytes(downloadPath, bytes);
            });


            dm.DownloadState = DownloadStates.DownloadedPreview;
            OnDownloadedPreview?.Invoke(dm);

            List<Tuple<string, byte[]>> artistsPictureData = new();
            byte[] picDatas = null;
            if (writeImage)
            {
                try
                {
                    picDatas = await WebHelper.Client.GetByteArrayAsync(await WebHelper.GetPicturePathAsync(dm.MusicData));
                }
                catch (Exception err)
                {
                    App.logManager.Log("DownloadManager", err.ToString(), LogLevel.Error);
                }
                if (IDv3WriteArtistImage)
                {
                    try
                    {
                        foreach (var a in dm.MusicData.Artists)
                        {
                            string result = a.PicturePath;
                            if (a.PicturePath is null)
                            {
                                result =
                                    (await App.metingServices.NeteaseServices.GetArtist(a.ID)).PicturePath;
                            }
                            var data = await WebHelper.Client.GetByteArrayAsync(result);
                            artistsPictureData.Add(new(a.Name, data));
                        }
                    }
                    catch (Exception err)
                    {
                        App.logManager.Log("DownloadManager", err.ToString(), LogLevel.Error);
                    }
                }
            }

            TagLib.File tagFile = null;
            TagLib.Tag tag = null;
            await Task.Run(() =>
            {
                tagFile = TagLib.File.Create(downloadPath);
                tag = tagFile.Tag;

                List<TagLib.IPicture> pictures = new() { };
                if (picDatas != null)
                {
                    var cover = new TagLib.Id3v2.AttachmentFrame
                    {
                        Type = TagLib.PictureType.FrontCover,
                        Description = "Cover",
                        Data = new TagLib.ByteVector(picDatas),
                        TextEncoding = TagLib.StringType.UTF16
                    };
                    pictures.Add(cover);
                }

                if (artistsPictureData.Any())
                {
                    foreach (var a in artistsPictureData)
                    {
                        TagLib.Id3v2.AttachmentFrame artistImage = new()
                        {
                            Type = TagLib.PictureType.Artist,
                            Description = a.Item1,
                            Data = new TagLib.ByteVector(a.Item2),
                            TextEncoding = TagLib.StringType.UTF16
                        };
                        pictures.Add(artistImage);
                    }
                }

                tag.Pictures = pictures.ToArray();
                tag.Title = dm.MusicData.Title;
                tag.Album = dm.MusicData.Album.Title;
                tag.DateTagged = dm.MusicData.ReleaseTime;
                tag.Comment = $"Download with {App.AppName}";
                tag.Description = tag.Comment;

                List<string> artists = new();
                foreach (var a in dm.MusicData.Artists)
                {
                    artists.Add(a.Name);
                }
                tag.Performers = artists.ToArray();
                tag.AlbumArtists = tag.Performers;
            });

            var lyric = await App.metingServices.NeteaseServices.GetLyric(dm.MusicData.ID);
            if (lyric != null)
            {
                if (!lyric.Item1.Contains("纯音乐，请欣赏"))
                {
                    if (lyric.Item1.Length >= 10)
                    {
                        await Task.Run(() =>
                        {
                            if (saveLyric)
                            {
                                File.Create(lyricPath).Dispose();
                                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                                File.WriteAllText(lyricPath, $"{lyric.Item1}\n{lyric.Item2}", Encoding.GetEncoding("GB2312"));
                            }

                            if (writeLyric) tag.Lyrics = $"{lyric.Item1}\n{lyric.Item2}";
                        });
                    }
                    else
                    {

                    }
                }
            }

            await Task.Run(() =>
            {
                tagFile.Save();
                tagFile.Dispose();
            });
            dm.DownloadState = DownloadStates.Downloaded;
            DownloadingData.Remove(dm);
            DownloadedData.Add(dm);
            OnDownloaded?.Invoke(dm);
            App.logManager.Log("DownloadManager", $"下载完成：{dm.MusicData.Title}");
        }

        public void CallOnDownloadingEvent(DownloadData dm)
        {
            OnDownloading?.Invoke(dm);
        }
        public void CallOnDownloadedEvent(DownloadData dm)
        {
            OnDownloaded?.Invoke(dm);
        }
        public void CallOnDownloadErrorEvent(DownloadData dm)
        {
            OnDownloadError?.Invoke(dm);
        }
    }
}
