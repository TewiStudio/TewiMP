namespace TewiMP.Services.Storage;

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using TewiMP.Core.Models.Music;

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
                if (array is null) continue;
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
        if (musicDatas is null) return;
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
            if (musicDatas is null) continue;
            await AddAnalyzedMusic(await Task.Run(musicDatas.ToArray));
        }
    }
}
