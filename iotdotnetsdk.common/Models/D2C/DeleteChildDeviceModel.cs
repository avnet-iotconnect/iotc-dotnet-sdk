using Newtonsoft.Json;

namespace iotdotnetsdk.common.Models.D2C
{
    public class DeleteChildDeviceModel : BaseMtModel
    {
        [JsonProperty("d")]
        public DeleteChildDetails Data { get; set; }
    }

    public class DeleteChildDetails
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}