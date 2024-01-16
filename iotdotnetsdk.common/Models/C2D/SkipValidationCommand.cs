using Newtonsoft.Json;

namespace iotdotnetsdk.common.Models.C2D
{
    internal class SkipValidationCommand : BaseCommand
    {
        [JsonProperty("d")]
        public bool SkipValidation { get; set; }
    }
}
