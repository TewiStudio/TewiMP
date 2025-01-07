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
        public Meting4Net.Core.Meting Services { get; set; }
        public NeteaseMeting(Meting4Net.Core.Meting meting)
        {
            Services = meting;
        }

        public static Album GetAlbumFromJson(JToken json)
        {
            Album album = null;
            var artist = json["artist"];
            album = new()
            {
                Title = (string)json["name"],
                Title2 = json["alias"].Any() ? (string)json["alias"].First : null,
                ID = (string)json["id"],
                PicturePath = (string)json["picUrl"],
                Describee = (string)json["description"],
                RelaseTime = (string)json["publishTime"],
                From = MusicFrom.neteaseMusic
            };
            album.Artists = new()
            {
                new()
                {
                    Name = (string)artist["name"],
                    Name2 = (string)artist["trans"],
                    ID = (string)artist["id"],
                    PicturePath = (string)artist["picUrl"],
                    From = MusicFrom.neteaseMusic
                }
            };
            album.Songs = new()
            {
                ListFrom = MusicFrom.neteaseMusic,
                ListDataType = DataType.专辑
            };
            return album;
        }

        public static Artist GetArtistFromJson(JObject json)
        {
            Artist artist = new();
            artist.Name = (string)json["name"];
            artist.Name2 = json.ContainsKey("trans") ? (string)json["trans"] : null;
            artist.ID = (string)json["id"];
            artist.PicturePath = (string)json["img1v1Url"];
            artist.Describee = (string)json["briefDesc"];
            artist.From = MusicFrom.neteaseMusic;
            return artist;
        }

        public static MusicListData GetMusicListDataFromJson(JToken playlistJson)
        {
            MusicListData musicListData = new();

            musicListData.ListShowName = (string)playlistJson["name"];
            musicListData.ID = (string)playlistJson["id"];
            musicListData.PicturePath = (string)playlistJson["coverImgUrl"];
            musicListData.ListFrom = MusicFrom.neteaseMusic;
            musicListData.ListDataType = DataType.歌单;

            var plt = playlistJson["tracks"];
            musicListData.Songs = UnpackMusicData(plt);
            musicListData.ListName = $"{musicListData.ListFrom}{musicListData.ListDataType}{musicListData.ID}";

            return musicListData;
        }

        public static List<MusicData> UnpackMusicData(JToken token)
        {
            if (token is null) return null;
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
                    (string)md["al"]["name"], (string)md["al"]["id"], pic)
                { From = MusicFrom.neteaseMusic };

                DateTime? dateTime = null;
                bool dateTickComplete = long.TryParse((string)md["publishTime"], out var dateTick);
                if (dateTick != 0)
                {
                    DateTime? resultDateTime = dateTickComplete ? CodeHelper.UnixGetTime(dateTick, true) : null;
                    dateTime =
                        md.ContainsKey("publishTime") ? resultDateTime : null;
                }

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

        public async Task<string> GetUrl(string id, int br)
        {
            return await Task.Run(() =>
            {
                var getAddressAction = string () =>
                {
                    string address = Services.FormatMethod().Url(id, br);

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
        public async Task<Tuple<string, string>> GetLyric(string id)
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
        public async Task<string> GetPic(string id)
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
        
        public async Task<object> GetSearch(
            string keyword,
            int pageNumber = 1,
            int pageSize = 30,
            int type = 0)
        {
            SearchDataType dataType = (SearchDataType)type;
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
                            if (dataType == SearchDataType.歌曲)
                            {
                                MusicListData ld = new(keyword, keyword, null, MusicFrom.neteaseMusic, null, null);
                                ld.Songs = UnpackMusicData(a["result"]["songs"]);
                                if (ld.Songs != null)
                                    return ld;
                                else return null;
                            }
                            else if (dataType == SearchDataType.歌单)
                            {
                                List<object[]> list = new();
                                foreach (var musicList in a["result"]["playlists"])
                                {
                                    list.Add([GetMusicListDataFromJson(musicList), (int)musicList["trackCount"]]);
                                }
                                return list;
                            }
                            else if (dataType == SearchDataType.专辑)
                            {
                                List<Album> list = new();
                                foreach (var albumjson in a["result"]["albums"])
                                {
                                    list.Add(GetAlbumFromJson(albumjson));
                                }
                                return list;
                            }
                            else if (dataType == SearchDataType.艺术家)
                            {
                                List<Artist> artists = new();
                                foreach (var artist in a["result"]["artists"])
                                {
                                    artists.Add(GetArtistFromJson((JObject)artist));
                                }
                                return artists;
                            }
                            else if (dataType == SearchDataType.用户)
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

        private List<string> PlayListLoadingList = new();
        public async Task<MusicListData> GetPlayList(string id)
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
                        //System.Diagnostics.App.logManager.Log(pls);

                        if (pls["code"].ToString() == "200")
                        {
                            MusicListData musicListData = GetMusicListDataFromJson(pls["playlist"]);
                            if (musicListData.Songs.Any()) return musicListData;
                        }
                        else
                        {
                            //System.Diagnostics.App.logManager.Log(pls["message"]);
                        }
                    }
                    catch(Exception err) { App.logManager.Log("NeteaseMeting", $"GetPlayList Error: {err}", Background.LogLevel.Error); }
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

        public async Task<Artist> GetArtist(string id)
        {
            return await Task.Run(() =>
            {
                var getArtistAction = Artist () =>
                {
                    var data = JObject.Parse(Services.FormatMethod(false).Artist(id));
                    //System.Diagnostics.App.logManager.Log(data);
                    Artist artist = new();
                    if (data["code"].ToString() == "200")
                    {
                        JObject art = (JObject)data["artist"];
                        artist = GetArtistFromJson(art);
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

        public async Task<Album> GetAlbum(string id)
        {
            return await Task.Run(() =>
            {
                var getAlbumAction = Album () =>
                {
                    string jsonStr = Services.FormatMethod(false).Album(id);
                    var data = JObject.Parse(jsonStr);

                    Album album = null;
                    //System.Diagnostics.App.logManager.Log(data);
                    try
                    {
                        if (data["code"].ToString() == "200")
                        {
                            JObject json = (JObject)data["album"];
                            album = GetAlbumFromJson(json);
                            album.Songs.Songs = UnpackMusicData(data["songs"]);
                        }
                    }
                    catch(Exception err)
                    {
                        App.logManager.Log("NeteaseMeting", $"GetAlbum Error: {err}", Background.LogLevel.Error);
                    }

                    return album;
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

        public async Task<MusicData> GetMusicData(string songid)
        {
            return await Task.Run(() =>
            {
                var getSongAction = MusicData () =>
                {
                    var data = JObject.Parse(Services.FormatMethod(false).Song(songid));

                    //System.Diagnostics.App.logManager.Log(data);
                    MusicData musicData = null;
                    try
                    {
                        if (data["code"].ToString() == "200")
                        {
                            //System.Diagnostics.App.logManager.Log(data);
                        }
                    }
                    catch(Exception err)
                    {
                        //System.Diagnostics.App.logManager.Log(err);
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

        public async Task<string> GetPicFromMusicData(MusicData id)
        {
            return await Task.Run(() =>
            {
                var getSongAction = string () =>
                {
                    var data = JObject.Parse(Services.FormatMethod(false).Song(id.ID));

                    //System.Diagnostics.App.logManager.Log(data);
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
                        App.logManager.Log("NeteaseMeting", $"GetPicFromMusicData Error: {err}", Background.LogLevel.Error);
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
