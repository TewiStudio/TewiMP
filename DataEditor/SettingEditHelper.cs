using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace TewiMP.DataEditor
{
    public static class SettingEditHelper
    {
        public static void EditSetting(JObject eData, DataFolderBase.SettingParams settingParams, object data)
        {
            if (data == null)
            {
                eData[settingParams.ToString()] = null;
            }
            else
            {
                eData[settingParams.ToString()] = JValue.FromObject(data);
            }
        }

        public static async Task EditSetting(DataFolderBase.SettingParams settingParams, object data)
        {
            await Task.Run(() =>
            {
                EditSetting(DataFolderBase.JSettingData, settingParams, data);
            });
        }

        public static T GetSetting<T>(JObject eData, DataFolderBase.SettingParams settingParams)
        {
            try
            {
                return eData[settingParams.ToString()].Value<T>();
            }
            catch (ArgumentNullException ex)
            {
                var jdata = DataFolderBase.JSettingData;
                jdata.Add(settingParams.ToString(), DataFolderBase.SettingDefault[settingParams.ToString()]);
                DataFolderBase.JSettingData = jdata;
                return DataFolderBase.SettingDefault[settingParams.ToString()].Value<T>();
            }
        }

    }
}
