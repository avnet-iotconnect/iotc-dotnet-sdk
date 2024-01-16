using Newtonsoft.Json;

using System.Collections.Generic;

namespace iotdotnetsdk.common.Models.Identity
{
    public class AttributesModel
    {
        [JsonProperty("d")]
        public BaseAttrIdentityModel Data { get; set; }
    }

    public class BaseAttrIdentityModel
    {
        [JsonProperty("att")]
        public List<BaseAttrDetails> AttrList { get; set; }

        [JsonProperty("ct")]
        public int Ct { get; set; }

        [JsonProperty("ec")]
        public int Ec { get; set; }
    }

    public class BaseAttrDetails
    {
        [JsonProperty("p")]
        public string P { get; set; }

        [JsonProperty("dt")]
        public int? Dt { get; set; }

        [JsonProperty("tg")]
        public string Tg { get; set; }

        [JsonProperty("tw")]
        public string Tw { get; set; }

        [JsonProperty("agt")]
        public AggregateTypeFlags AggregateType { get; set; }
        [JsonProperty("aggregateType")]
        private AggregateTypeFlags _AggregateType { set { AggregateType = value; } }

        [JsonProperty("d")]
        public List<AttrDetails> D { get; set; }
    }

    public class AttrDetails
    {
        [JsonProperty("tg")]
        public string Tg { get; set; }

        [JsonProperty("ln")]
        public string Ln { get; set; }

        [JsonProperty("dt")]
        public int Dt { get; set; }

        [JsonProperty("dv")]
        public string Dv { get; set; }

        [JsonProperty("sq")]
        public int Sq { get; set; }

        [JsonProperty("tw")]
        public string Tw { get; set; }

        [JsonProperty("agt")]
        public AggregateTypeFlags AggregateType { get; set; }
        [JsonProperty("aggregateType")]
        public AggregateTypeFlags _AggregateType { get; set; }
    }
}