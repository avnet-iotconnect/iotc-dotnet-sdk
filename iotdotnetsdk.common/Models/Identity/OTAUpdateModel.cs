using iotdotnetsdk.common.Models.C2D;

using Newtonsoft.Json;

using System.Collections.Generic;

namespace iotdotnetsdk.common.Models.Identity
{
    public class OTAUpdateModel
    {
        [JsonProperty("d")]
        public BaseIdentityOtaModel Data { get; set; }
    }

    public class BaseIdentityOtaModel
    {
        [JsonProperty("ota")]
        public OtaDetails Ota { get; set; }

        [JsonProperty("ct")]
        public int Ct { get; set; }

        [JsonProperty("ec")]
        public int Ec { get; set; }
    }

    public class OtaDetails
    {
        [JsonProperty("cmd")]
        public string Cmd { get; set; }

        [JsonProperty("ack")]
        public string Ack { get; set; }

        [JsonProperty("ver")]
        public OtaVer Ver { get; set; }

        [JsonProperty("urls")]
        public List<UrlDetails> Urls { get; set; }
    }

    public class OtaVer
    {
        [JsonProperty("sw")]
        public string Sw { get; set; }

        [JsonProperty("hw")]
        public string Hw { get; set; }
    }
}
