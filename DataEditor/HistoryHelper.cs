using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace TewiMP.DataEditor
{
    public class SongHistoryData : OnlyClass
    {
        public MusicData MusicData { get; set; }
        public DateTime Time { get; set; }
        public int Count { get; set; } = 0;

        public override string GetMD5()
        {
            return $"{MusicData}{Time}";
        }
    }

    public static class HistoryHelper
    {
        static object syncLock = new();
        public delegate void HistoryDataChangedDelegate();
        public static event HistoryDataChangedDelegate HistoryDataChanged;
        public static async Task<JObject> GetHistoriesJObject()
        {
            JObject keyValuePairs = null;
            await Task.Run(() =>
            {
                var t = System.IO.File.ReadAllText(DataFolderBase.HistoryDataPath);
                keyValuePairs = JObject.Parse(t);
            });
            return keyValuePairs;
        }

        public static async Task SaveHistoryJObject(JObject keyValuePairs)
        {
            await Task.Run(() =>
            {
                lock (syncLock)
                    System.IO.File.WriteAllText(DataFolderBase.HistoryDataPath, keyValuePairs.ToString());
            });
            HistoryDataChanged?.Invoke();
        }
    }

    public static class SongHistoryHelper
    {
        public static async Task AddHistory(SongHistoryData historyData)
        {
            var datas = await HistoryHelper.GetHistoriesJObject();
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
                System.Diagnostics.Debug.WriteLine($"SongHistoryHelper: {ex.Message}");
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
                    var d = JsonNewtonsoft.FromJSON<SongHistoryData>(data.First.ToString());
                    d.Count = count;
                    historyDatas.Add(d);
                    count--;
                }
            });
            return historyDatas.ToArray();
        }
    }
}
