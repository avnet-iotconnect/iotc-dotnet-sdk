using iotcdotnetsdk.common.Internals;

using iotdotnetsdk.common.Enums;
using iotdotnetsdk.common.Internals;

using Newtonsoft.Json;

using System;
using System.Xml.Serialization;

namespace iotdotnetsdk.common.Models
{
    public class SyncResponse
    {
        [JsonProperty("d")]
        public SyncData Data { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("pf")]
        public string Platform { get; set; }
    }

    public class SyncData
    {
        [JsonProperty("ec")]
        public ErrorCodes Ec { get; set; }

        [JsonProperty("ct")]
        public int Ct { get; set; }

        [JsonProperty("meta")]
        public Meta Meta { get; set; }

        [JsonProperty("has")]
        public Has Has { get; set; }

        [JsonProperty("p")]
        public Protocol Protocol { get; set; }

        [JsonProperty("r")]
        public List<RuleLocalDb> Rules { get; set; }

        [JsonProperty("dt")]
        public DateTime Dt { get; set; }
    }

    public class RuleLocalDb
    {
        [JsonProperty("g")]
        [XmlElement(ElementName = "g")]
        public string Guid { get; set; }

        [JsonProperty("es")]
        [XmlElement(ElementName = "es")]
        public string EventSubscriptionGuid { get; set; }

        [JsonProperty("con")]
        [XmlElement(ElementName = "con")]
        public string ConditionText { get; set; }

        [JsonProperty("cmd")]
        [XmlElement(ElementName = "cmd")]
        public string CommandText { get; set; }
    }

    public class Meta
    {
        [JsonProperty("at")]
        public int At { get; set; }

        [JsonProperty("df")]
        public int? DataFreq { get; set; }

        public bool IsDataFreqApplicable
        {
            get
            {
                return DiscoveryCommon.IsDataFreqEnable && Edge != 1 && DataFreq > 0;
            }
        }

        [JsonProperty("cd")]
        public string TemplateCode { get; set; }

        [JsonProperty("gtw")]
        public Gtw Gtw { get; set; }

        [JsonProperty("edge")]
        public int Edge { get; set; }

        [JsonProperty("pf")]
        public int Pf { get; set; }

        [JsonProperty("hwv")]
        public string Hwv { get; set; }

        [JsonProperty("swv")]
        public string Swv { get; set; }

        [JsonProperty("v")]
        public double V { get; set; }
    }

    public class Gtw
    {
        [JsonProperty("tg")]
        public string Tag { get; set; }

        [JsonProperty("g")]
        public Guid DeviceGuid { get; set; }
    }

    public class Has
    {
        [JsonProperty("d")]
        public bool D { get; set; }

        [JsonProperty("attr")]
        public bool Attr { get; set; }

        [JsonProperty("set")]
        public bool Set { get; set; }

        [JsonProperty("r")]
        public bool R { get; set; }

        [JsonProperty("ota")]
        public bool Ota { get; set; }
    }

    public class Protocol
    {
        [JsonProperty("n")]
        public string N { get; set; }

        [JsonProperty("h")]
        public string H { get; set; }

        [JsonProperty("p")]
        public int P { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("un")]
        public string Un { get; set; }

        [JsonProperty("pwd")]
        public string Pwd { get; set; }

        [JsonProperty("topics")]
        public Topics Topics { get; set; }
    }

    public class Topics
    {
        [JsonProperty("rpt")]
        public string Rpt { get; set; }

        [JsonProperty("erpt")]
        public string Erpt { get; set; }

        [JsonProperty("erm")]
        public string Erm { get; set; }

        [JsonProperty("flt")]
        public string Flt { get; set; }

        [JsonProperty("od")]
        public string OfflineData { get; set; }

        [JsonProperty("hb")]
        public string HeartBeat { get; set; }

        [JsonProperty("ack")]
        public string Ack { get; set; }

        [JsonProperty("dl")]
        public string DeviceLogs { get; set; }

        [JsonProperty("di")]
        public string Di { get; set; }

        [JsonProperty("c2d")]
        public string C2d { get; set; }

        [JsonProperty("set")]
        public Set Set { get; set; }
    }

    public class Set
    {
        [JsonProperty("pub")]
        public string Pub { get; set; }

        [JsonProperty("sub")]
        public string Sub { get; set; }

        [JsonProperty("pubForAll")]
        public string PubForAll { get; set; }

        [JsonProperty("subForAll")]
        public string SubForAll { get; set; }
    }
}