using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

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
}
