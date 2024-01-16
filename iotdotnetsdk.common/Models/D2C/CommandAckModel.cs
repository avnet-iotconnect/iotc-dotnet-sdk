using iotdotnetsdk.common.Enums;

using Newtonsoft.Json;

using System;

namespace iotdotnetsdk.common.Models.D2C
{
    public class CommandAckModel : BaseDtModel
    {
        [JsonProperty("d")]
        public CommandAckDetails Data { get; set; }

        internal override bool HasData => Data != null;
    }

    public class CommandAckDetails
    {
        [JsonProperty("ack")]
        public Guid? Ack { get; set; }

        [JsonProperty("type")]
        public CommandAckType Type { get; set; }

        [JsonProperty("st")]
        public int St { get; set; }

        [JsonProperty("msg")]
        public string Msg { get; set; }

        [JsonProperty("cid", NullValueHandling = NullValueHandling.Ignore)]
        public string Cid { get; set; }
    }
}