using Newtonsoft.Json;

namespace iotdotnetsdk.common.Models.C2D
{
    public class BaseCommand
    {
        [JsonProperty("ct")]
        public int Ct { get; set; }

        [JsonProperty("v")]
        public double V { get; set; }
    }
}