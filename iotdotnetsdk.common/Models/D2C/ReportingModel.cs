using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace iotdotnetsdk.common.Models.D2C
{
    public class ReportingModel : BaseDtModel
    {
        [JsonProperty("d")]
        public List<DeviceTelemetryModel> Data { get; set; }
        internal override bool HasData => (Data != null && Data.Count > 0);
    }

    public class DeviceTelemetryModel
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("tg")]
        public string Tg { get; set; }

        [JsonProperty("dt")]
        public DateTime Dt { get; set; }

        [JsonProperty("d")]
        public JObject Data { get; set; }
    }
}