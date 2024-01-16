using Newtonsoft.Json;

namespace iotdotnetsdk.common.Models.C2D
{
    public class DeviceFrequencyCommand : BaseCommand
    {
        [JsonProperty("df")]
        public int Df { get; set; }
    }
}