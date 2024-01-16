using Newtonsoft.Json;

using System;
using System.Collections.Generic;

namespace iotdotnetsdk.common.Models.C2D
{
    public class OTACommand : BaseCommand
    {
        [JsonProperty("cmd")]
        public string Cmd { get; set; }

        [JsonProperty("ack")]
        public Guid? Ack { get; set; }

        [JsonProperty("sw")]
        public string Sw { get; set; }

        [JsonProperty("hw")]
        public string Hw { get; set; }

        [JsonProperty("urls")]
        public List<UrlDetails> Urls { get; set; }
    }

    public class UrlDetails
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("fileName")]
        public string FileName { get; set; }

        [JsonProperty("tg", NullValueHandling = NullValueHandling.Ignore)]
        public string Tg { get; set; }
    }
}
