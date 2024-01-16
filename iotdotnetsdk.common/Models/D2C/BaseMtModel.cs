using iotdotnetsdk.common.Enums;

using Newtonsoft.Json;

using System;

namespace iotdotnetsdk.common.Models.D2C
{
    public class BaseMtModel
    {
        [JsonProperty("mt")]
        public MessageTypes Mt { get; set; }

        [JsonProperty("sid", NullValueHandling = NullValueHandling.Ignore)]
        public string sId { get; set; }
    }

    public abstract class BaseDtModel
    {
        [JsonProperty("dt")]
        public DateTime Dt { get; set; }
        abstract internal bool HasData { get; }
    }
}