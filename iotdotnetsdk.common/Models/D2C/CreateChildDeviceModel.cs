using Newtonsoft.Json;

using System;

namespace iotdotnetsdk.common.Models.D2C
{
    public class CreateChildDeviceModel : BaseMtModel
    {
        [JsonProperty("d")]
        public CreateChildDetails Data { get; set; }
    }

    public class CreateChildDetails
    {
        [JsonProperty("g")]
        public Guid? G { get; set; }

        [JsonProperty("dn")]
        public string Dn { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("tg")]
        public string Tg { get; set; }
    }
}