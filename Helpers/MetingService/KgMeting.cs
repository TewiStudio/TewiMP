using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TewiMP.DataEditor;

namespace TewiMP.Helpers.MetingService
{
    class KgMeting : IMetingService
    {
        public override Meting4Net.Core.Meting Services { get; set; }
        public KgMeting(Meting4Net.Core.Meting meting)
        {
            Services = meting;
        }

        public override async Task<object> GetSearch(string keyword, int pageNumber = 1, int pageSize = 30, int type = 0)
        {
            return await Task.Run(() =>
            {
                var getSearchAction = object () =>
                {
                    string data = Services.FormatMethod(false).Search(keyword, new Meting4Net.Core.Models.Standard.Options() { page = pageNumber, limit = pageSize, type = type});

                    System.Diagnostics.Debug.WriteLine(data.ToString());
                    if (data != null)
                    {
                         var a = JObject.Parse(data);
                        if (a.ContainsKey("data"))
                        {
                            if (type == 0)
                            {
                                MusicListData ld = new(keyword, keyword, null, MusicFrom.kgMusic, null, null);
                                foreach (var item in a["data"]["info"])
                                {
                                    ld.Songs.Add(new(
                                        (string)item["songname"],
                                        (string)item["hash"],
                                        new() { new((string)item["singername"]) { From = MusicFrom.kgMusic } },
                                        new((string)item["album_name"], (string)item["album_id"]),
                                        from: MusicFrom.kgMusic
                                        )
                                    { });
                                }
                                return ld;
                            }
                            System.Diagnostics.Debug.WriteLine(data);
                        }
                    }

                    return null;
                };

                for (int i = 0; i <= App.metingServices.RetryCount; i++)
                {
                    var a = getSearchAction();
                    if (a != null)
                    {
                        return a;
                    }
                }

                return null;
            });
        }

        public override async Task<string> GetUrl(string id, int br)
        {
            return await Task.Run(() =>
            {
                var getAddressAction = string () =>
                {
                    string address = Services.FormatMethod().Url(id);
                    System.Diagnostics.Debug.WriteLine(address);

                    if (address != null)
                    {
                        var a = JObject.Parse(address);
                        if (a.ContainsKey("url"))
                        {
                            if (a["url"].ToString() != "")
                            {
                                address = a["url"].ToString();
                                return address;
                            }
                        }
                    }

                    return null;
                };

                for (int i = 0; i <= App.metingServices.RetryCount; i++)
                {
                    var a = getAddressAction();
                    if (a != null)
                    {
                        return a;
                    }
                }

                return null;
            });
        }

        public class KgMusicData : MusicData
        {
            public string Lyric { get; set; }
            public KgMusicData() { }
        }
        public override async Task<MusicData> GetMusicData(string id)
        {
            return await Task.Run(() =>
            {
                var getPicAction = MusicData () =>
                {
                    MusicData musicData = null;
                    dynamic song = JsonConvert.DeserializeObject<dynamic>(Services.FormatMethod(false).Song(id));

                    var url = $"https://wwwapi.kugou.com/yy/index.php?r=play/getdata&hash={id}&album_id={song.albumid.ToString()}";
                    WebClient webClient = new WebClient() { Encoding = Encoding.UTF8 };
                    webClient.Headers.Add("cookie", "kg_mid=c1470a3e02dbaa42f2d0f9ee79320cf0; kg_dfid=4WPBwo1Rm8lo3XLx8X0i4I0b; kg_dfid_collect=d41d8cd98f00b204e9800998ecf8427e; Hm_lvt_aedee6983d4cfc62f509");
                    string result = webClient.DownloadString(url);
                    webClient.Dispose();

                    dynamic obj = JsonConvert.DeserializeObject<dynamic>(result);
                    dynamic data = obj?.data;
                    if (data is null) return null;
                    if (data.song_name is null) return null;

                    List<Artist> artists = new List<Artist>();
                    foreach (var artist in data.authors)
                    {
                        artists.Add(new(artist.author_name.ToString(), artist.author_id.ToString(), artist.avatar.ToString()));
                    }

                    musicData = new()
                    {
                        Title = data.song_name.ToString(),
                        ID = id,
                        Artists = artists,
                        Album = new Album(data.album_name.ToString(), data.album_id.ToString(), data.img.ToString()) { From = MusicFrom.kgMusic },
                        From = MusicFrom.kgMusic
                    };

                    return musicData;
                };

                for (int i = 0; i <= App.metingServices.RetryCount; i++)
                {
                    MusicData a = null;
                    try
                    {
                        a = getPicAction();
                    }
                    catch (Exception err) { a = null; }

                    if (a != null)
                    {
                        return a;
                    }
                }

                return null;
            });
        }

        private List<string> PictureLoadingList = new();
        /// <summary>
        /// 获取到的是歌手图片，如果要获取专辑图片使用 GetPicFromMusicData
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override async Task<string> GetPic(string id)
        {
            while (PictureLoadingList.Count > 3)
            {
                await Task.Delay(250);
            }
            return await Task.Run(() =>
            {
                PictureLoadingList.Add(id);
                var getPicAction = string () =>
                {
                    // 获取的是歌手图片
                    var pic = Services.FormatMethod().PicObj(id);

                    if (pic != null)
                    {
                        return pic.url;
                    }

                    return null;
                };

                for (int i = 0; i <= App.metingServices.RetryCount; i++)
                {
                    var a = getPicAction();
                    if (a != null)
                    {
                        PictureLoadingList.Remove(id);
                        return a;
                    }
                }

                PictureLoadingList.Remove(id);
                return null;
            });
        }

        public override async Task<string> GetPicFromMusicData(MusicData musicData)
        {
            return await Task.Run(async () =>
            {
                var getPicAction = async Task<string> () =>
                {
                    return null;
                    try
                    {
                        return (await GetMusicData(musicData.ID)).Album?.PicturePath;
                    }
                    catch
                    {
                        return null;
                    }
                };

                for (int i = 0; i <= App.metingServices.RetryCount; i++)
                {
                    string a = null;
                    try
                    {
                        a = await getPicAction();
                    }
                    catch (Exception err) { a = null; }

                    if (a != null)
                    {
                        return a;
                    }
                }

                return null;
            });
        }

        public override Task<Artist> GetArtist(string id)
        {
            throw new NotImplementedException();
        }

        public override async Task<Album> GetAlbum(string id)
        {
            return await Task.Run(() =>
            {
                var getAlbumAction = Album () =>
                {
                    var a = Services.FormatMethod(false).Album(id);
                    var data = JObject.Parse(a);

                    System.Diagnostics.Debug.WriteLine(a);
                    Album Album = null;
                    try
                    {/*
                        if (data["code"].ToString() == "200")
                        {
                            JObject album = (JObject)data["album"];
                            var artist = album["artist"];
                            Album = new()
                            {
                                Title = (string)album["name"],
                                Title2 = album["alias"].Any() ? (string)album["alias"].First : null,
                                ID = id,
                                PicturePath = (string)album["picUrl"],
                                Describee = (string)album["description"],
                                RelaseTime = (string)album["publishTime"]
                            };
                            Album.Artists = new()
                            {
                                new()
                                {
                                    Name = (string)artist["name"],
                                    Name2 = (string)artist["trans"],
                                    ID = (string)artist["id"],
                                    PicturePath = (string)artist["picUrl"],
                                }
                            };
                            Album.Songs = new()
                            {
                                ListFrom = MusicFrom.neteaseMusic,
                                ListDataType = DataType.专辑
                            };
                            Album.Songs.Songs = UnpackMusicData(data["songs"]);
                        }*/
                    }
                    catch (Exception err)
                    {
                        System.Diagnostics.Debug.WriteLine(err);
                    }

                    return Album;
                };

                for (int i = 0; i <= App.metingServices.RetryCount; i++)
                {
                    Album a = null;
                    try
                    {
                        a = getAlbumAction();
                    }
                    catch (Exception err) { a = null; }

                    if (a != null)
                    {
                        if (!a.IsNull())
                            return a;
                    }
                }

                return null;
            });
        }

        public override Task<Tuple<string, string>> GetLyric(string id)
        {
            throw new NotImplementedException ();
        }

        public override Task<MusicListData> GetPlayList(string id)
        {
            throw new NotImplementedException();
        }
    }
}
