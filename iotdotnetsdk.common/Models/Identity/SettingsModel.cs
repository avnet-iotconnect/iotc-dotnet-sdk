using Newtonsoft.Json;

using System.Collections.Generic;

namespace iotdotnetsdk.common.Models.Identity
{
    public class SettingsModel
    {
        [JsonProperty("d")]
        public BaseIdentitySettingModel Data { get; set; }
    }

    public class BaseIdentitySettingModel
    {
        [JsonProperty("set")]
        public List<SettingDetails> SettingList { get; set; }

        [JsonProperty("ct")]
        public int Ct { get; set; }

        [JsonProperty("ec")]
        public int Ec { get; set; }
    }


    public class SettingDetails
    {
        [JsonProperty("ln")]
        public string Ln { get; set; }

        [JsonProperty("dt")]
        public int Dt { get; set; }

        [JsonProperty("dv")]
        public string Dv { get; set; }
    }
}