using Newtonsoft.Json;

namespace iotdotnetsdk.common.Models.Identity
{
    public class BaseIdentityCommonModel
    {
        [JsonProperty("d")]
        public IdentityCommonModel Data { get; set; }
    }

    public class IdentityCommonModel
    {
        [JsonProperty("ct")]
        public int Ct { get; set; }

        [JsonProperty("ec")]
        public int Ec { get; set; }
    }

    public class CommandFirmwareModel
    {
        [JsonProperty("ct")]
        public int Ct { get; set; }

        [JsonProperty("data")]
        public object Data { get; set; }
    }
}