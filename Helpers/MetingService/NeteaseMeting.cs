using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TewiMP.DataEditor;
using Kawazu;

namespace TewiMP.Helpers.MetingService
{
    public class NeteaseMeting: IMetingService
    {
        public override Meting4Net.Core.Meting Services { get; set; }
        public NeteaseMeting(Meting4Net.Core.Meting meting)
        {
            Services = meting;
        }

        public override async Task<string> GetUrl(string id, int br)
        {
            return await Task.Run(() =>
            {
                var getAddressAction = string () =>
                {
                    string address = Services.FormatMethod().Url(id, br);
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

        /// <summary>
        /// 获取网易云音乐歌词
        /// </summary>
        /// <param name="id">歌曲id</param>
        /// <returns>
        /// item1歌词
        /// item2歌词翻译
        /// </returns>
        public override async Task<Tuple<string, string>> GetLyric(string id)
        {
            return await Task.Run(() =>
            {
                var getLyricAction = Tuple<string, string> () =>
                {
                    string lyric = Services.FormatMethod().Lyric(id);

                    if (lyric != null)
                    {
                        var a = JObject.Parse(lyric);
                        if (a.ContainsKey("lyric"))
                        {
                            var l = (string)a["lyric"];
                            var t = (string)a["tlyric"];
                            return new Tuple<string, string>(l, t);
                        }
                    }

                    return null;
                };

                for (int i = 0; i <= App.metingServices.RetryCount; i++)
                {
                    var a = getLyricAction();
                    if (a != null)
                    {
                        return a;
                    }
                }

                return null;
            });
        }

        private List<string> PictureLoadingList = new();
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
                    var pic = Services.FormatMethod().PicObj(id, 5000);

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
        
        public override async Task<object> GetSearch(
            string keyword,
            int pageNumber = 1,
            int pageSize = 30,
            int type = 0)
        {
            return await Task.Run(() =>
            {
                var getSearchAction = object () =>
                {
                    string data = Services.FormatMethod(false).Search(keyword, new Meting4Net.Core.Models.Standard.Options() { page = pageNumber, limit = pageSize, type = type });

                    if (data != null)
                    {
                        var a = JObject.Parse(data);
                        if (a.ContainsKey("result"))
                        {
                            if (type == (int)SearchDataType.歌曲)
                            {
                                MusicListData ld = new(keyword, keyword, null, MusicFrom.neteaseMusic, null, null);
                                ld.Songs = UnpackMusicData(a["result"]["songs"]);
                                if (ld.Songs != null)
                                    return ld;
                                else return null;
                            }
                            else if (type == (int)SearchDataType.歌单)
                            {

                            }
                            else if (type == (int)SearchDataType.艺术家)
                            {
                                List<Artist> artists = new();
                                foreach (var artist in a["result"]["artists"])
                                {
                                    artists.Add(new()
                                    {
                                        ID = (string)artist["id"],
                                        Name = (string)artist["name"],
                                        PicturePath = (string)artist["img1v1Url"]
                                    });
                                }
                                return artists;
                            }
                            else if (type == (int)SearchDataType.用户)
                            {

                            }
                            else if (type == (int)SearchDataType.专辑)
                            {

                            }
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

        public List<MusicData> UnpackMusicData(JToken token)
        {
            if (token == null) return null;
            var datas = new List<MusicData>();
            foreach (JObject md in token)
            {
                List<Artist> artists = new();
                foreach (var artist in md["ar"])
                {
                    artists.Add(new(
                        (string)artist["name"],
                        (string)artist["id"], null
                        ));
                }

                string pic = null;
                if ((md["al"] as JObject).ContainsKey("picUrl"))
                {
                    pic = (string)md["al"]["picUrl"];
                }
                else
                {
                    pic = null;
                }

                Album album = new(
                    (string)md["al"]["name"], (string)md["al"]["id"], pic);

                bool dateTickComplete = long.TryParse((string)md["publishTime"], out var dateTick);
                DateTime? dateTime =
                    md.ContainsKey("publishTime") ? dateTickComplete ? new DateTime(dateTick) : null : null;

                MusicData data = new(
                    (string)md["name"],
                    (string)md["id"],
                    artists, album, dateTime,
                    MusicFrom.neteaseMusic);

                if (md.ContainsKey("tns"))
                    data.Title2 = (string)md["tns"].First();
                datas.Add(data);
            }
            return datas;
        }

        private List<string> PlayListLoadingList = new();
        public override async Task<MusicListData> GetPlayList(string id)
        {
            while (PlayListLoadingList.Count > 3)
            {
                await Task.Delay(250);
            }
            return await Task.Run(() =>
            {
                PlayListLoadingList.Add(id);
                var getPlayListAction = MusicListData () =>
                {
                    try
                    {
                        JObject pls = JObject.Parse(Services.FormatMethod(false).Playlist(id));
                        //System.Diagnostics.Debug.WriteLine(pls);

                        if (pls["code"].ToString() == "200")
                        {
                            MusicListData musicListData = new();

                            var pld = pls["playlist"];
                            musicListData.ListShowName = (string)pld["name"];
                            musicListData.ID = (string)pld["id"];
                            musicListData.PicturePath = (string)pld["coverImgUrl"];
                            musicListData.ListFrom = MusicFrom.neteaseMusic;
                            musicListData.ListDataType = DataType.歌单;

                            var plt = pld["tracks"];
                            musicListData.Songs = UnpackMusicData(plt);

                            musicListData.ListName = $"{musicListData.ListFrom}{musicListData.ListDataType}{musicListData.ID}";

                            //System.Diagnostics.Debug.WriteLine(musicListData.MD5);
                            if (musicListData.Songs.Any()) return musicListData;
                        }
                        else
                        {
                            //System.Diagnostics.Debug.WriteLine(pls["message"]);
                        }
                    }
                    catch(Exception err) { System.Diagnostics.Debug.WriteLine(err); }
                    return null;
                };

                for (int i = 0; i <= App.metingServices.RetryCount; i++)
                {
                    var a = getPlayListAction();
                    if (a != null)
                    {
                        PlayListLoadingList.Remove(id);
                        return a;
                    }
                }

                PlayListLoadingList.Remove(id);
                return null;
            });
        }

        public override async Task<Artist> GetArtist(string id)
        {
            return await Task.Run(() =>
            {
                var getArtistAction = Artist () =>
                {
                    var data = JObject.Parse(Services.FormatMethod(false).Artist(id));
                    //System.Diagnostics.Debug.WriteLine(data);
                    Artist artist = new();
                    if (data["code"].ToString() == "200")
                    {
                        JObject art = (JObject)data["artist"];
                        artist.Name = (string)art["name"];
                        artist.Name2 = art.ContainsKey("trans") ? (string)art["trans"] : null;
                        artist.ID = (string)art["id"];
                        artist.PicturePath = (string)art["img1v1Url"];
                        artist.Describee = (string)art["briefDesc"];

                        artist.HotSongs = new()
                        {
                            ListFrom = MusicFrom.neteaseMusic,
                            ListDataType = DataType.艺术家,
                            Songs = UnpackMusicData(data["hotSongs"]),
                            PicturePath = artist.PicturePath
                        };
                    }
                    else
                        artist = null;

                    return artist;
                };

                for (int i = 0; i <= App.metingServices.RetryCount; i++)
                {
                    Artist a = null;
                    try
                    {
                        a = getArtistAction();
                    }
                    catch { a = null; }

                    if (a != null)
                    {
                        if (!string.IsNullOrEmpty(a.ID))
                            return a;
                    }
                }

                return null;
            });
        }

        public override async Task<Album> GetAlbum(string id)
        {
            return await Task.Run(() =>
            {
                var getAlbumAction = Album () =>
                {
                    var data = JObject.Parse(Services.FormatMethod(false).Album(id));
                    
                    //System.Diagnostics.Debug.WriteLine(data);
                    Album Album = null;
                    try
                    {
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
                        }
                    }
                    catch(Exception err)
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
                    catch(Exception err) { a = null; }

                    if (a != null)
                    {
                        if (!a.IsNull())
                            return a;
                    }
                }

                return null;
            });
        }

        public override async Task<MusicData> GetMusicData(string songid)
        {
            return await Task.Run(() =>
            {
                var getSongAction = MusicData () =>
                {
                    var data = JObject.Parse(Services.FormatMethod(false).Song(songid));

                    //System.Diagnostics.Debug.WriteLine(data);
                    MusicData musicData = null;
                    try
                    {
                        if (data["code"].ToString() == "200")
                        {
                            System.Diagnostics.Debug.WriteLine(data);
                        }
                    }
                    catch(Exception err)
                    {
                        System.Diagnostics.Debug.WriteLine(err);
                    }

                    return musicData;
                };

                for (int i = 0; i <= App.metingServices.RetryCount; i++)
                {
                    MusicData a = null;
                    try
                    {
                        a = getSongAction();
                    }
                    catch(Exception err) { a = null; }

                    if (a != null)
                    {
                        return a;
                    }
                }

                return null;
            });
        }

        public override async Task<string> GetPicFromMusicData(MusicData id)
        {
            return await Task.Run(() =>
            {
                var getSongAction = string () =>
                {
                    var data = JObject.Parse(Services.FormatMethod(false).Song(id.ID));

                    //System.Diagnostics.Debug.WriteLine(data);
                    string result = null;
                    try
                    {
                        if (data["code"].ToString() == "200")
                        {
                            result = (string)data["songs"][0]["al"]["picUrl"];
                        }
                    }
                    catch(Exception err)
                    {
                        System.Diagnostics.Debug.WriteLine(err);
                    }

                    return result;
                };

                for (int i = 0; i <= App.metingServices.RetryCount; i++)
                {
                    string a = null;
                    try
                    {
                        a = getSongAction();
                    }
                    catch(Exception err) { a = null; }

                    if (a != null)
                    {
                        return a;
                    }
                }

                return null;
            });
        }
    }
}
