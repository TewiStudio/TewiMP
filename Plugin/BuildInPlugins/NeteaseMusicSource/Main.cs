using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TewiMP.Helpers;
using TewiMP.DataEditor;
using TewiMP.Background;

namespace TewiMP.Plugin.BuildInPlugins.NeteaseMusicSource
{
    public class Main : MusicSourcePlugin
    {
        public Meting4Net.Core.Meting Services { get; private set; }
        public override PluginInfo PluginInfo => new()
        {
            Name = "neteaseMusic",
            Author = "TewiStudio",
            Version = "V0.0.1",
        };
        protected override Dictionary<string, object> PluginSettings { get; set; } = new()
        {
            { "RetryCount", 15.0 },
            { "NeteaseCookie", "" },
        };

        private readonly string defaultNeteaseCookie = "MUSIC_U=00EAB22143552A814C0124DDC82FB718A1B9A619E99B2F665D98064768CC5F03593887BA73F63C794F3BCC3B0BBD962140EA39F68F0A96EB48AF998474415464FD4BCF2D721B5CA9929534318248028734EDD0F02051EB1950815B5254F4529B24090F98637213B6AB01085540EC4C1AF854AF7DE1D1F2E0D5568D778C5400655DD34294B2756CA44A61B895AB8F05722EF425400D7F5CE48CB00727C2C9DC3BA4549946ECEC28A6E388C028D2B0BF5713F5E0F7A5B0B2236B10C92D9BDC751BBF67FF3FB630D8777707010C5121190B4DB2058CB01CE67CE9F4070862E2736CBA0EF4AEE62E05283776BE5A6F83CEA11C267EC839EFEE2F6B2F96684B802B18557F3DFFB2E880DEB0A66611A92C79AB920E040DCFC96B06511A5780F90C0718096FFF2790F07016906D04FD79A7E01544CE7D9D22CBBCF8E820958C42755A140FAC360E73BDEDC83BACA192CA648E455A45091B2F5198E0937BA612BB15CE85315EFE47845A544A23C7443B2221F46E2A";
        private int RetryCount => (int)GetSetting("RetryCount", 15.0);
        private string NeteaseCookie
        {
            get
            {
                var setting = GetSetting("NeteaseCookie", "");
                return string.IsNullOrEmpty(setting) ? defaultNeteaseCookie : setting;
            }
        }

        public override string GetUserViewPluginSettingName(string keyString)
        {
            switch (keyString)
            {
                case "NeteaseCookie":
                    return "Cookie";
                case "RetryCount":
                    return "重试次数";
                default:
                    return base.GetUserViewPluginSettingName(keyString);
            }
        }

        public override string GetUserViewPluginSettingDescribe(string keyString)
        {
            switch (keyString)
            {
                case "NeteaseCookie":
                    return "用户 Cookie 设置。";
                case "RetryCount":
                    return "从服务器获取信息失败时，程序重试的次数。";
                default:
                    return base.GetUserViewPluginSettingName(keyString);
            }
        }

        protected override void OnSettingsChanged(string key, object value)
        {
            base.OnSettingsChanged(key, value);
            if (key == "NeteaseCookie")
            {
                UpdateCookie();
            }
        }

        public override void OnEnable()
        {
            Services = new Meting4Net.Core.Meting(Meting4Net.Core.ServerProvider.Netease);
            UpdateCookie();
            base.OnEnable();
        }

        private void UpdateCookie()
        {
            Services.Cookie("os=pc;" + NeteaseCookie);
        }

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
    }
}
