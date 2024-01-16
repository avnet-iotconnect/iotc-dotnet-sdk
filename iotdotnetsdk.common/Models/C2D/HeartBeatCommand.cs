using Newtonsoft.Json;

namespace iotdotnetsdk.common.Models.C2D
{
    public class HeartBeatCommand : BaseCommand
    {
        [JsonProperty("f")]
        public int Freq { get; set; }
    }
}