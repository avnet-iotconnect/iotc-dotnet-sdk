using Newtonsoft.Json;

using System.Collections.Generic;

namespace iotdotnetsdk.common.Models.C2D
{
    public class ModuleCommand : BaseCommand
    {
        [JsonProperty("urls")]
        public List<UrlDetails> Urls { get; set; }
    }
}