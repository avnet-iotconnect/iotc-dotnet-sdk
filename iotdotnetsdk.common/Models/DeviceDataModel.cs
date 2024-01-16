using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iotdotnetsdk.common.Models
{
    internal class BaseDeviceDataModel
    {
        [JsonProperty("dt")]
        public DateTime Time { get; set; }
    }

    internal class EdgeDeviceDataModel : BaseDeviceDataModel
    {
        [JsonProperty("d")]
        public List<Device2> UniqueDeviceInfo
        {
            get; set;
        }
    }

    internal class DeviceDataModel : BaseDeviceDataModel
    {
        [JsonProperty("d")]
        public List<Device2> UniqueDeviceInfo
        {
            get; set;
        }
    }

    public class Device2
    {
        [JsonProperty("id")]
        public string DeviceUniqueId { get; set; }
        [JsonProperty("uniqueId")]
        private string _DeviceUniqueId { set { DeviceUniqueId = value; } }
        [JsonProperty("d")]
        public JObject Data { get; set; }

        [JsonProperty("dt")]
        public DateTime DeviceTime { get; set; }
        [JsonProperty("time")]
        private DateTime _DeviceTime { set { DeviceTime = value; } }
        [JsonProperty("tg")]
        public string Tag { internal get; set; }
    }
}
