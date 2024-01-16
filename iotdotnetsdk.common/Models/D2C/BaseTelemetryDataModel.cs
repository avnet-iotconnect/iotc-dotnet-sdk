using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace iotdotnetsdk.common.Models.D2C
{
    internal class BaseTelemetryDataModel
    {
        public ReportingModel Reporting { get; set; }
        public ReportingModel Fault { get; set; }
    }
    internal class RuleDataModel2 : BaseTelemetryDataModel
    {
        [JsonProperty("d")]
        public List<DeviceRuleInfo> Devices { get; internal set; }
    }

    internal class DeviceRuleInfo : UniqueDeviceInfo
    {
        [JsonProperty("rg")]
        public string RuleGuid { get; set; }
        [JsonProperty("ct")]
        public string ConditionText { get; internal set; }
        //[JsonProperty("cd")]
        //public List<string> ChildDevices { get; internal set; }
        [JsonProperty("cv")]
        public Dictionary<string, object> ConditionValue { get; internal set; }
        [JsonProperty("sg")]
        public string EventSubscriptionGuid { get; internal set; }


        [JsonProperty("d")]
        public JObject Data { get; set; }

    }
}
