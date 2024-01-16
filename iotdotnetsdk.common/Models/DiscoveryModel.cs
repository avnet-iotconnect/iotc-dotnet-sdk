using Newtonsoft.Json;

namespace iotdotnetsdk.common.Models
{
    public class DiscoveryModel
    {
        [JsonProperty("d")]
        public D D { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public class D
    {
        [JsonProperty("ec")]
        public int Ec { get; set; }

        [JsonProperty("bu")]
        public string Bu { get; set; }

        [JsonProperty("log:mqtt")]
        public LogMqtt LogMqtt { get; set; }

        [JsonProperty("log:https")]
        public string LogHttps { get; set; }
    }

    public class LogMqtt
    {
        [JsonProperty("hn")]
        public string Hn { get; set; }

        [JsonProperty("un")]
        public string Un { get; set; }

        [JsonProperty("pwd")]
        public string Pwd { get; set; }

        [JsonProperty("topic")]
        public string Topic { get; set; }
    }
}