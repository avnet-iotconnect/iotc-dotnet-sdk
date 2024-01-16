using Newtonsoft.Json;

using System.Collections.Generic;

namespace iotdotnetsdk.common.Models.Identity
{
    public class RulesModel
    {
        [JsonProperty("d")]
        public BaseIdentityRuleModel Data { get; set; }
    }

    public class BaseIdentityRuleModel
    {
        [JsonProperty("r")]
        public List<RuleDetails> RuleList { get; set; }

        [JsonProperty("ct")]
        public int Ct { get; set; }

        [JsonProperty("ec")]
        public int Ec { get; set; }
    }

    public class RuleDetails
    {
        [JsonProperty("g")]
        public string G { get; set; }

        [JsonProperty("es")]
        public string Es { get; set; }

        [JsonProperty("con")]
        public string Con { get; set; }

        [JsonProperty("cmd")]
        public string Cmd { get; set; }
    }
}