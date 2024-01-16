using Newtonsoft.Json;

namespace iotdotnetsdk.common.Models.C2D
{
    public class ModuleCommand : BaseCommand
    {
        [JsonProperty("urls")]
        public List<UrlDetails> Urls { get; set; }
    }
}