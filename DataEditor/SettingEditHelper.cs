using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace TewiMP.DataEditor
{
    public static class SettingEditHelper
    {
        public static void EditSetting(JObject eData, Enum settingParams, object data)
        {
            if (data is null)
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

        public static T GetSetting<T>(JObject eData, Enum settingParams)
        {
            try
            {
                return eData[settingParams.ToString()].Value<T>();
            }
            catch (ArgumentNullException)
            {
                var jData = DataFolderBase.JSettingData;
                jData.Add(settingParams.ToString(), DataFolderBase.SettingDefault[settingParams.ToString()]);
                DataFolderBase.JSettingData = jData;
                return DataFolderBase.SettingDefault[settingParams.ToString()].Value<T>();
            }
        }

    }
}
