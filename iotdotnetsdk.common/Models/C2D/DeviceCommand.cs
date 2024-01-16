using Newtonsoft.Json;

using System;

namespace iotdotnetsdk.common.Models.C2D
{
    public class DeviceCommand : BaseCommand
    {
        [JsonProperty("cmd")]
        public string Cmd { get; set; }

        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string ChildId { get; set; }

        [JsonProperty("ack", NullValueHandling = NullValueHandling.Ignore)]
        public Guid? Ack { get; set; }
    }
}
