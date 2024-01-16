using iotcdotnetsdk.common.Internals;

using iotdotnetsdk.common.Enums;
using iotdotnetsdk.common.Models;
using iotdotnetsdk.common.Models.D2C;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace iotdotnetsdk.common.Internals.Devices
{
    internal class Gateway : BaseGateway
    {
        public Gateway(SyncData syncInfo, Func<string, Task> deviceCallBack, Func<string, Task> twinUpdateCallBack, SDKOptions options, DiscoveryCommon discoveryCommon, string platform)
            : base(syncInfo, deviceCallBack, twinUpdateCallBack, options, discoveryCommon, platform)
        {

        }

        internal override void SendData(string data)
        {
            if (SyncData == null || SyncData == null || SyncData.Has == null || !SyncData.Has.Attr)
                throw new SDKInitializationException("Device sync is not initialized or No attributes defined");

            SDKCommon.Console_WriteLine($"Deserializing data for UniqueId:{UniqueId}");
            List<DeviceTelemetryModel> devices;
            try
            {
                devices = JsonConvert.DeserializeObject<List<DeviceTelemetryModel>>(data);
                SDKCommon.Console_WriteLine($"Deserializing data for UniqueId:{UniqueId} - Success");
            }
            catch (Exception ex)
            {
                SDKCommon.Console_WriteLine($"Deserializing data for UniqueId:{UniqueId} - Failed");
                SDKCommon.Console_WriteError($"Serialization exception for UniqueId:{UniqueId} Data:{data} Exception:{ex.Message}");
                return;
            }

            BaseTelemetryDataModel telemetryData = null;

            try
            {
                SDKCommon.Console_WriteLine($"Preparing IoTConnect message for UniqueId:{UniqueId}");
                telemetryData = base.PrepareMessage(devices);
                SDKCommon.Console_WriteLine($"Preparing IoTConnect message for UniqueId:{UniqueId} - Success");
            }
            catch (Exception ex)
            {
                SDKCommon.Console_WriteLine($"Preparing IoTConnect message for UniqueId:{UniqueId} - Failed");
                SDKCommon.Console_WriteError($"Error while preparing message for UniqueId:{UniqueId} Data:{data} Exception:{ex.Message}");
                return;
            }

            try
            {
                if (SyncData.Meta.IsDataFreqApplicable)
                {
                    var currentTime = DateTime.UtcNow;
                    if (LastDataSentTime.HasValue && LastDataSentTime >= currentTime)
                        return;
                    LastDataSentTime = currentTime.AddSeconds(SyncData.Meta.DataFreq.HasValue ? SyncData.Meta.DataFreq.Value : 0);
                }


                if (telemetryData.Fault.HasData)
                {
                    SendD2C(telemetryData.Fault, new KeyValuePair<string, string>("mt", ((int)MessageTypes.FLT).ToString()));
                }
                if (telemetryData.Reporting.HasData)
                {
                    SendD2C(telemetryData.Reporting, new KeyValuePair<string, string>("mt", ((int)MessageTypes.RPT).ToString()));
                }
            }
            catch (Exception ex)
            {
                SDKCommon.Console_WriteError($"Error while sending telemetry data for UniqueId:{UniqueId} Data:{data} Exception:{ex.Message}");
            }
        }
    }
}
