using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using TewiMP.Helpers;
using TewiMP.Services;

namespace TewiMP.Core
{
    public enum OutApi { WaveOut, DirectSound, Wasapi, Asio, None }
    public class OutDevice : OnlyClass
    {
        public OutApi DeviceType { get; set; }
        public object Device { get; set; }
        public string DeviceName { get; set; }
        public int SampleRate { get; set; }
        public int Channels { get; set; }
        public long Latency { get; set; }
        public bool IsDefaultDevice { get; set; } = false;
        public OutDevice(OutApi deviceType, object device = null, string deviceName = "")
        {
            DeviceType = deviceType;
            Device = device;
            DeviceName = deviceName;
        }

        public override string ToString()
        {
            if (DeviceType == OutApi.None)
            {
                return "无音频输出设备";
            }
            return $"{DeviceType} - {(IsDefaultDevice ? defaultName : DeviceName)}";
        }

        public override string GetMD5()
        {
            return ToString();
        }

        public static OutDevice GetWasapiDefaultDevice(MMDeviceEnumerator enumerator)
        {
            var dout = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            var od = new OutDevice(OutApi.Wasapi, dout.ID, dout.FriendlyName) { IsDefaultDevice = true };
            od.SampleRate = dout.AudioClient.MixFormat.SampleRate;
            od.Channels = dout.AudioClient.MixFormat.Channels;
            return od;
        }

        public static OutDevice GetWasapiDefaultDevice()
        {
            var enumerator = new MMDeviceEnumerator();
            var result = GetWasapiDefaultDevice(enumerator);
            enumerator.Dispose();
            return result;
        }

        public static OutDevice GetDirectSoundOutDefaultDevice()
        {
            foreach (var dev in DirectSoundOut.Devices)
            {
                string name = dev.Description;
                OutDevice outDevice = new OutDevice(OutApi.DirectSound, dev, name) { IsDefaultDevice = name == "主声音驱动程序" };
                if (outDevice.IsDefaultDevice) return outDevice;
            }
            return null;
        }

        public static string defaultName = "默认输出设备";
        public static List<OutDevice> lastOutDevices = [];
        /// <summary>
        /// 获取可以播放的音频输出设备列表
        /// </summary>
        /// <returns><see cref="List{OutDevice}"/>OutDevice集合</returns>
        public static async Task<List<OutDevice>> GetOutDevicesAsync()
        {
            List<OutDevice> outDevices = new List<OutDevice>();
            await App.Instance.AudioService.AudioThread.InvokeAsync(() =>
            {
                // Wasapi
                var enumerator = new MMDeviceEnumerator();
                try
                {
                    // 添加默认设备
                    outDevices.Add(GetWasapiDefaultDevice(enumerator));
                }
                catch { }

                foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                {
                    OutDevice outDevice = new(OutApi.Wasapi, wasapi.ID, wasapi.FriendlyName);
                    outDevice.SampleRate = wasapi.AudioClient.MixFormat.SampleRate;
                    outDevice.Channels = wasapi.AudioClient.MixFormat.Channels;
                    outDevices.Add(outDevice);
                }
                enumerator.Dispose();
                if (outDevices.Count < 2) outDevices.Clear(); // 当只有默认播放设备时，直接认为此 api 没有输出设备

                // WaveOut
                for (int n = -1; n < WaveOut.DeviceCount; n++)
                {
                    var wocb = WaveOut.GetCapabilities(n);
                    string name = wocb.ProductName;
                    OutDevice outDevice = new(OutApi.WaveOut, n, name) { IsDefaultDevice = name == "Microsoft 声音映射器" || name == "Microsoft Sound Mapper" };
                    outDevices.Add(outDevice);
                }
                if (outDevices.Count < 2) outDevices.Clear();

                if (outDevices.Any())
                {
                    // DirectSound
                    foreach (var dev in DirectSoundOut.Devices)
                    {
                        string name = dev.Description;
                        OutDevice outDevice = new(OutApi.DirectSound, dev, name) { IsDefaultDevice = name == "主声音驱动程序" };

                        if (dev != null)
                            foreach (var device in outDevices)
                            {
                                if (device != outDevice)
                                {
                                    outDevices.Add(outDevice);
                                    break;
                                }
                            }
                    }
                    // Asio
                    var asioNames = AsioOut.GetDriverNames().ToList();
                    foreach (var asio in asioNames)
                    {
                        OutDevice outDevice = new(OutApi.Asio, asioNames.IndexOf(asio), asio);
                        outDevices.Add(outDevice);
                    }

                }

                if (outDevices.Count == 0)
                {
                    outDevices.Add(new(OutApi.None, null, "无音频输出设备"));
                }
            });
            lastOutDevices = outDevices;
            return outDevices;
        }

        public static async Task<OutDevice> GetWasapiDeviceFromOtherAPI(OutDevice outDevice)
        {
            if (outDevice.DeviceType == OutApi.Wasapi) return outDevice;
            if (outDevice.DeviceType == OutApi.Asio) return null;
            var outDevices = await GetOutDevicesAsync();
            if (outDevice.IsDefaultDevice) return outDevices[0]; // 第一个默认为 Wasapi 的默认输出设备

            OutDevice result = null;
            double lastDiff = 0;
            foreach (var device in outDevices)
            {
                if (device.DeviceType == OutApi.Wasapi)
                {
                    var diff = device.DeviceName.GetSimilarity(outDevice.DeviceName); // 比对字符串相似度，相似度最高的认为是同一个设备
                    LogService.LogDebug($"device: {device.DeviceName} / {outDevice.DeviceName}, by {diff}");
                    if (diff > lastDiff)
                    {
                        lastDiff = diff;
                        result = device;
                    }
                }
            }

            LogService.LogDebug($"Final device: {result.DeviceName}, by {lastDiff}");
            return result;
        }
    }

    public class NotificationClientImplementation : IMMNotificationClient
    {
        public delegate void OnDefaultDeviceChangedDelegate(DataFlow dataFlow, Role deviceRole, string defaultDeviceId);
        public event OnDefaultDeviceChangedDelegate OnDefaultDeviceChangedEvent;

        public delegate void OnPropertyValueChangedDelegate(string deviceId);
        public event OnPropertyValueChangedDelegate OnDeviceAddedEvent;
        public event OnPropertyValueChangedDelegate OnDeviceRemovedEvent;

        public delegate void OnDeviceStateChangedDelegate(string deviceId, DeviceState newState);
        public event OnDeviceStateChangedDelegate OnDeviceStateChangedEvent;

        public delegate void OnOnPropertyValueChangedDelegate(string deviceId, PropertyKey propertyKey);
        public event OnOnPropertyValueChangedDelegate OnPropertyValueChangedEvent;

        int defaultDeviceChangedCounter = 0;
        public async void OnDefaultDeviceChanged(DataFlow dataFlow, Role deviceRole, string defaultDeviceId)
        {
            if (deviceRole != Role.Multimedia) return;

            defaultDeviceChangedCounter++;
            await Task.Delay(100);
            defaultDeviceChangedCounter--;
            if (defaultDeviceChangedCounter != 0) return;

            LogService.Log("DeviceManager", $"系统默认设备已变更为：\"{defaultDeviceId}\"");
            OnDefaultDeviceChangedEvent?.Invoke(dataFlow, deviceRole, defaultDeviceId);
        }

        public void OnDeviceAdded(string deviceId)
        {
            LogService.Log("DeviceManager", $"新增设备：\"{deviceId}\"");
            OnDeviceAddedEvent?.Invoke(deviceId);
        }

        public void OnDeviceRemoved(string deviceId)
        {
            LogService.Log("DeviceManager", $"已移除设备：\"{deviceId}\"");
            OnDeviceRemovedEvent?.Invoke(deviceId);
        }

        public void OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            LogService.Log("DeviceManager", $"设备状态已更新。deviceId:{deviceId} / newState:{newState}");
            OnDeviceStateChangedEvent?.Invoke(deviceId, newState);
        }

        public void OnPropertyValueChanged(string deviceId, PropertyKey propertyKey)
        {
            LogService.Log("DeviceManager", $"设备属性已更新。deviceId: {deviceId} / propertyKey:{propertyKey.formatId.ToString()}");
            OnPropertyValueChangedEvent?.Invoke(deviceId, propertyKey);
        }

        public NotificationClientImplementation()
        {

        }
    }

    public class ClientDeviceEvents
    {
        private MMDeviceEnumerator deviceEnum = new MMDeviceEnumerator();
        public NotificationClientImplementation notificationClient;
        public IMMNotificationClient notifyClient;

        public ClientDeviceEvents()
        {
            notificationClient = new NotificationClientImplementation();
            notifyClient = notificationClient;
            deviceEnum.RegisterEndpointNotificationCallback(notifyClient);
        }
    }
}
