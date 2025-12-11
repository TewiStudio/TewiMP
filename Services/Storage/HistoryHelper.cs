using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TewiMP.Core;

namespace TewiMP.Services.Storage;

public static class HistoryHelper
{
    static Lock syncLock = new();
    public delegate void HistoryDataChangedDelegate();
    public static event HistoryDataChangedDelegate HistoryDataChanged;
    public static async Task<JObject> GetHistoriesJObject()
    {
        JObject keyValuePairs = null;
        await Task.Run(() =>
        {
            try
            {
                var t = System.IO.File.ReadAllText(DataFolderBase.HistoryDataPath);
                keyValuePairs = JObject.Parse(t);
            }
            catch { }
        });
        return keyValuePairs;
    }

    public static async Task SaveHistoryJObject(JObject keyValuePairs)
    {
        await Task.Run(() =>
        {
            lock (syncLock)
            {
                try
                {
                    System.IO.File.WriteAllText(DataFolderBase.HistoryDataPath, keyValuePairs.ToString());
                }
                catch
                {
                    LogService.Error(nameof(DataFolderBase), "Failed to save history data.");
                }
            }
        });
        HistoryDataChanged?.Invoke();
    }
}

public static class SongHistoryHelper
{
    public static async Task AddHistory(SongHistoryData historyData)
    {
        var datas = await HistoryHelper.GetHistoriesJObject();
        if (datas is null) return;
        try
        {
            await Task.Run(() =>
            {
                var k = datas["Songs"] as JObject;
                if (!k.ContainsKey(historyData.MD5))
                    k.Add(historyData.MD5, JObject.FromObject(historyData));
            });
            await HistoryHelper.SaveHistoryJObject(datas);
        }
        catch (Exception ex)
        {
            LogService.Log(nameof(DataFolderBase), ex.Message, LogLevel.Error);
        }
    }
    
    public static async Task RemoveHistory(SongHistoryData historyData)
    {
        var datas = await HistoryHelper.GetHistoriesJObject();
        await Task.Run(() => (datas["Songs"] as JObject).Remove(historyData.MD5));
        await HistoryHelper.SaveHistoryJObject(datas);
    }

    public static async Task<SongHistoryData[]> GetHistories()
    {
        List<SongHistoryData> historyDatas = new();
        var datas = await HistoryHelper.GetHistoriesJObject();
        await Task.Run(() =>
        {
            int count = datas["Songs"].Count();
            foreach (var data in datas["Songs"])
            {
                var d = JsonConvert.DeserializeObject<SongHistoryData>(data.First.ToString());
                d.Count = count;
                historyDatas.Add(d);
                count--;
            }
        });
        return historyDatas.ToArray();
    }
}
