using Newtonsoft.Json;

using System.Collections.Generic;

namespace iotdotnetsdk.common.Models.Identity
{
    public class DevicesModel
    {
        [JsonProperty("d")]
        public BaseIdentityDeviceModel Data { get; set; }
    }

    public class BaseIdentityDeviceModel
    {
        [JsonProperty("d")]
        public List<DeviceDetails> DeviceList { get; set; }

        [JsonProperty("ct")]
        public int Ct { get; set; }

        [JsonProperty("ec")]
        public int Ec { get; set; }
    }

    public class DeviceDetails
    {
        [JsonProperty("tg")]
        public string Tg { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
    }
}