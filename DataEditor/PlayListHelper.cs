using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace TewiMP.DataEditor
{
    public static class PlayListHelper
    {
        public static async Task<List<MusicListData>> ReadAllPlayList()
        {
            var datas = new List<MusicListData>();
            return await Task.Run(() =>
            {
                JObject jdatas = JObject.Parse(File.ReadAllText(DataFolderBase.PlayListDataPath));
                foreach (var list in jdatas)
                {
                    var a = JsonNewtonsoft.FromJSON<MusicListData>(list.Value.ToString());
                    datas.Add(a);
                    a = null;
                }
                jdatas = null;
                return datas;
            });
        }

        public static async Task AddPlayList(MusicListData musicListData)
        {
            var jdata = await ReadData();
            jdata = AddPlayList(musicListData, jdata);
            await SaveData(jdata);
        }

        public static JObject AddPlayList(MusicListData musicListData, JObject addData)
        {
            addData.Add(musicListData.ListName, JObject.FromObject(musicListData));
            return addData;
        }

        public static async Task DeletePlayList(MusicListData musicListData)
        {
            var jdata = await ReadData();
            jdata.Remove(musicListData.ListName);
            await SaveData(jdata);
        }

        public static MusicListData AddMusicDataToPlayList(MusicData musicData, MusicListData musicListData)
        {
            if (musicListData.Songs == null)
            {
                musicListData.Songs = new();
            }

            if (!musicListData.Songs.Contains(musicData))
            {
                musicListData.Songs.Insert(0, musicData);
            }

            return musicListData;
        }

        public static JObject AddMusicDataToPlayList(string listName, MusicData musicData, JObject jdata)
        {
            var ml = JsonNewtonsoft.FromJSON<MusicListData>(jdata[listName].ToString());
            AddMusicDataToPlayList(musicData, ml);
            jdata[listName] = JObject.FromObject(ml);
            return jdata;
        }

        public static async Task AddMusicDataToPlayList(string listName, MusicData musicData)
        {
            JObject text = await ReadData();
            await SaveData(AddMusicDataToPlayList(listName, musicData, text));
        }

        public static JObject DeleteMusicDataFromPlayList(string listName, MusicData musicData, JObject jdata)
        {
            var ml = JsonNewtonsoft.FromJSON<MusicListData>(jdata[listName].ToString());
            for (int mc = 0; mc < ml.Songs.Count; mc++)
            {
                if (ml.Songs[mc] == musicData)
                {
                    System.Diagnostics.Debug.WriteLine(musicData.ID);
                    ml.Songs.RemoveAt(mc);
                    break;
                }
            }
            jdata[listName] = JObject.FromObject(ml);
            return jdata;
        }

        public static async Task DeleteMusicDataFromPlayList(string listName, MusicData musicData)
        {
            await SaveData(DeleteMusicDataFromPlayList(listName, musicData, await ReadData()));
        }

        public static JObject MakeJsPlayListData(MusicListData musicListData)
        {
            return new JObject() { { musicListData.ListName, JObject.FromObject(musicListData) } };
        }

        public static async Task SaveData(JObject data)
        {
            await Task.Run(() =>
            {
                File.WriteAllText(DataFolderBase.PlayListDataPath, data.ToString());
            });
        }

        public static async Task<JObject> ReadData()
        {
            return await Task.Run(() =>
            {
                return JObject.Parse(File.ReadAllText(DataFolderBase.PlayListDataPath));
            });
        }

        public static async Task<JObject> AddLocalMusicDataToPlayList(string listName, FileInfo localFile, JObject jdata)
        {
            var data = await MusicData.FromFile(localFile.FullName);
            foreach (var i in data)
            {
                AddMusicDataToPlayList(listName, i, jdata);
            }
            return jdata;
        }
    }

    public static class LocalMusicHelper
    {
        public static async Task<JObject> GetLocalMusicData()
        {
            return await Task.Run(() =>
            {
                return JObject.Parse(File.ReadAllText(DataFolderBase.LocalMusicDataPath));
            });
        }
        public static async Task SaveLocalMusicData(JObject data)
        {
            await Task.Run(() =>
            {
                File.WriteAllText(DataFolderBase.LocalMusicDataPath, data.ToString());
            });
        }
         
        public static async Task<JObject> AddLocalMusicFolder(string folderPath, JObject localMusicData)
        {
            return await Task.Run(() =>
            {
                var data = localMusicData;
                var array = data[DataFolderBase.LocalMusicDataType.LocalMusicFolderPath.ToString()] as JArray;
                array.Add(folderPath);
                data[DataFolderBase.LocalMusicDataType.LocalMusicFolderPath.ToString()] = array;
                return data;
            });
        }
        public static async Task AddLocalMusicFolder(string folderPath)
        {
            var data = await GetLocalMusicData();
            await AddLocalMusicFolder(folderPath, data);
            await SaveLocalMusicData(data);
        }

        public static async Task RemoveLocalMusicFolder(string folderPath, JObject localMusicData)
        {
            await Task.Run(() =>
            {
                var data = localMusicData;
                var array = (data[DataFolderBase.LocalMusicDataType.LocalMusicFolderPath.ToString()] as JArray).ToList();
                if (!array.Contains(folderPath)) return;
                array.Remove(folderPath);
                data[DataFolderBase.LocalMusicDataType.LocalMusicFolderPath.ToString()] = JArray.FromObject(array);
            });
        }
        public static async Task RemoveLocalMusicFolder(string folderPath)
        {
            var data = await GetLocalMusicData();
            await RemoveLocalMusicFolder(folderPath, data);
            await SaveLocalMusicData(data);
        }

        public static async Task<List<string>> GetAllMusicFolders(JObject localMusicData)
        {
            return await Task.Run(() =>
            {
                var data = localMusicData;
                List<string> localMusicFolderPath = [];
                foreach (string i in data[DataFolderBase.LocalMusicDataType.LocalMusicFolderPath.ToString()])
                {
                    localMusicFolderPath.Add(i);
                }
                return localMusicFolderPath;
            });
        }
        public static async Task<List<string>> GetAllMusicFolders()
        {
            var data = await GetLocalMusicData();
            return await GetAllMusicFolders(data);
        }

        public static async Task<List<MusicData>> AnalysisFolderMusic(string folderPath)
        {
            return await Task.Run(async () =>
            {
                List<MusicData> musicDatas = [];
                if (!Directory.Exists(folderPath)) return null;
                var folder = new DirectoryInfo(folderPath);
                foreach (var file in folder.GetFiles())
                {
                    var array = await MusicData.FromFile(file.FullName, true);
                    if (array == null) continue;
                    foreach (var item in array)
                    {
                        item.FileTime = file.LastWriteTime;
                        musicDatas.Add(item);
                    }
                }
                return musicDatas;
            });
        }

        public static async Task RemoveAllAnalyzedMusic()
        {
            var data = await GetLocalMusicData();
            await Task.Run(() => data[DataFolderBase.LocalMusicDataType.AnalyzedDatas.ToString()] = new JArray());
            await SaveLocalMusicData(data);
        }

        public static async Task AddAnalyzedMusic(MusicData[] musicDatas)
        {
            if (musicDatas == null) return;
            var data = await GetLocalMusicData();
            await Task.Run(() =>
            {
                var analyzedList = data[DataFolderBase.LocalMusicDataType.AnalyzedDatas.ToString()] as JArray;
                foreach (var item in musicDatas)
                {
                    analyzedList.Add(JObject.FromObject(item));
                }
                data[DataFolderBase.LocalMusicDataType.AnalyzedDatas.ToString()] = analyzedList;
            });
            await SaveLocalMusicData(data);
        }

        public static async Task<List<MusicData>> GetAllAnalyzedMusicData()
        {
            List<MusicData> musicDatas = [];
            var data = await GetLocalMusicData();
            await Task.Run(() =>
            {
                var analyzedList = data[DataFolderBase.LocalMusicDataType.AnalyzedDatas.ToString()] as JArray;
                foreach(var item in analyzedList)
                {
                    musicDatas.Add(JsonConvert.DeserializeObject<MusicData>(item.ToString()));
                }
                musicDatas = [.. musicDatas.OrderBy(m => m.Title)];
            });
            return musicDatas;
        }

        public static async Task ReAnalysisMusicDatas()
        {
            var list = await GetAllMusicFolders();
            await RemoveAllAnalyzedMusic();
            foreach (var path in list)
            {
                var musicDatas = await AnalysisFolderMusic(path);
                if (musicDatas == null) continue;
                await AddAnalyzedMusic(await Task.Run(musicDatas.ToArray));
            }
        }
    }
}
