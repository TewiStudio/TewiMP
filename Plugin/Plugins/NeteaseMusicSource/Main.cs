using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TewiMP.Helpers;
using TewiMP.DataEditor;
using TewiMP.Background;

namespace TewiMP.Plugin.Plugins.NeteaseMusicSource
{
    public class Main : MusicSourcePlugin
    {
        public Meting4Net.Core.Meting Services { get; private set; }
        public int RetryCount { get; } = 15;
        public string NeteaseCookie = "MUSIC_U=00870CD16FF9A2C399E286D034532F5A95A6BDC5D29D6B2CD9F927E6AB57513D3DD7BBEB7082A04E2EBB367B45DD8EE96CF199ABE9670277016495018E29AD2B98C7B72C3500F161D47C2A86AE30E4DF5F3D1568CD28AD91F58C520FF3C1092B3122C79F62C7282819D166049EA1C1BED5B149166E260A5823CF33B9F2A383BEA96145083B4666A3FF372DAECF482BB0283B2CB6E7271E234BEF4BA00FD1E37E118829DCE591233A5FA118306B1EC2D46B7108FF7DCA51852A5C033EEAF15316F85B56709EC3BC1BE14EF4534634862E5617CD9372F97784ED0F729C74701482C4E612A9DB844CF8E6CCEF9FDCAB6EA9B6D7891F9E7A7112DF2DE1E658AD28DF8F67E310C190E2631780289C5CA85FC1B915F97C708697A99719B4C2E305385C80637A44A0C756D5CBE7D819E282D7D2A5CE3E177A88CE5C57C4D290F7641AA2CEDDF1AF5CC1F2B838F4DB50E84CC10E9B4F697DAB6A1016D882995E4864AEB414948218B329F61D65D37A8743686A40D9";
        public override PluginInfo PluginInfo => new()
        {
            Name = "neteaseMusic",
            Author = "TewiStudio",
            Version = "V0.0.1",
        };

        public Album GetAlbumFromJson(JToken json)
        {
            Album album = null;
            var artist = json["artist"];
            album = new()
            {
                Title = (string)json["name"],
                Title2 = json["alias"].Any() ? (string)json["alias"].First : null,
                ID = (string)json["id"],
                PicturePath = (string)json["picUrl"],
                Describe = (string)json["description"],
                ReleaseTime = (string)json["publishTime"],
                From = MusicFrom.pluginMusicSource,
                PluginInfo = PluginInfo
            };
            album.Artists = new()
            {
                new()
                {
                    Name = (string)artist["name"],
                    Name2 = (string)artist["trans"],
                    ID = (string)artist["id"],
                    PicturePath = (string)artist["picUrl"],
                    From = MusicFrom.pluginMusicSource,
                    PluginInfo = PluginInfo
                }
            };
            album.Songs = new()
            {
                ListFrom = MusicFrom.pluginMusicSource,
                PluginInfo = PluginInfo,
                ListDataType = DataType.专辑
            };
            return album;
        }

        public Artist GetArtistFromJson(JObject json)
        {
            Artist artist = new();
            artist.Name = (string)json["name"];
            artist.Name2 = json.ContainsKey("trans") ? (string)json["trans"] : null;
            artist.ID = (string)json["id"];
            artist.PicturePath = (string)json["img1v1Url"];
            artist.Describee = (string)json["briefDesc"];
            artist.From = MusicFrom.pluginMusicSource;
            artist.PluginInfo = PluginInfo;
            return artist;
        }

        public MusicListData GetMusicListDataFromJson(JToken playlistJson)
        {
            MusicListData musicListData = new();

            musicListData.ListShowName = (string)playlistJson["name"];
            musicListData.ID = (string)playlistJson["id"];
            musicListData.PicturePath = (string)playlistJson["coverImgUrl"];
            musicListData.ListFrom = MusicFrom.pluginMusicSource;
            musicListData.PluginInfo = PluginInfo;
            musicListData.ListDataType = DataType.歌单;

            var plt = playlistJson["tracks"];
            musicListData.Songs = UnpackMusicData(plt);
            musicListData.ListName = $"{musicListData.PluginInfo}{musicListData.ListDataType}{musicListData.ID}";

            return musicListData;
        }

        public List<MusicData> UnpackMusicData(JToken token)
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
                        )
                    {
                        PluginInfo = PluginInfo
                    });
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
                {
                    From = MusicFrom.pluginMusicSource,
                    PluginInfo = PluginInfo
                };

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
                    MusicFrom.pluginMusicSource)
                {
                    PluginInfo = PluginInfo
                };

                if (md.ContainsKey("tns"))
                    data.Title2 = (string)md["tns"].First();
                datas.Add(data);
            }
            return datas;
        }

        public override async Task<string> GetUrl(string id, int br)
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

                for (int i = 0; i <= RetryCount; i++)
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

                for (int i = 0; i <= RetryCount; i++)
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

                for (int i = 0; i <= RetryCount; i++)
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
                                MusicListData ld = new(keyword, keyword, null, MusicFrom.pluginMusicSource, null, null)
                                {
                                    PluginInfo = PluginInfo
                                };
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

                for (int i = 0; i <= RetryCount; i++)
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
                        //App.logManager.Log("Debug", pls.ToString());

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
                    catch (Exception err) { App.logManager.Log("NeteaseMeting", $"GetPlayList Error: {err}", LogLevel.Error); }
                    return null;
                };

                for (int i = 0; i <= RetryCount; i++)
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
                    //System.Diagnostics.App.logManager.Log(data);
                    Artist artist = new();
                    if (data["code"].ToString() == "200")
                    {
                        JObject art = (JObject)data["artist"];
                        artist = GetArtistFromJson(art);
                        artist.HotSongs = new()
                        {
                            ListFrom = MusicFrom.pluginMusicSource,
                            PluginInfo = PluginInfo,
                            ListDataType = DataType.艺术家,
                            Songs = UnpackMusicData(data["hotSongs"]),
                            PicturePath = artist.PicturePath
                        };
                    }
                    else
                        artist = null;

                    return artist;
                };

                for (int i = 0; i <= RetryCount; i++)
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
                    catch (Exception err)
                    {
                        App.logManager.Log("NeteaseMeting", $"GetAlbum Error: {err}", LogLevel.Error);
                    }

                    return album;
                };

                for (int i = 0; i <= RetryCount; i++)
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

        public override async Task<MusicData> GetMusicData(string songid)
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
                    catch (Exception err)
                    {
                        //System.Diagnostics.App.logManager.Log(err);
                    }

                    return musicData;
                };

                for (int i = 0; i <= RetryCount; i++)
                {
                    MusicData a = null;
                    try
                    {
                        a = getSongAction();
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

        public override async Task<string> GetPicFromMusicData(MusicData id)
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
                    catch (Exception err)
                    {
                        App.logManager.Log("NeteaseMeting", $"GetPicFromMusicData Error: {err}", LogLevel.Error);
                    }

                    return result;
                };

                for (int i = 0; i <= RetryCount; i++)
                {
                    string a = null;
                    try
                    {
                        a = getSongAction();
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

        public override void OnEnable()
        {
            Services = new Meting4Net.Core.Meting(Meting4Net.Core.ServerProvider.Netease).Cookie(NeteaseCookie);
            base.OnEnable();
        }
    }
}
