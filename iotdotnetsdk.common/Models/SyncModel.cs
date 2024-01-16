using iotcdotnetsdk.common.Internals;

using iotdotnetsdk.common.Internals;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iotdotnetsdk.common.Models
{
    internal class SyncModel
    {
        [JsonProperty("d")]
        public RootSyncData Data { get; set; }
        [JsonProperty("data")]
        private RootSyncData _Data { set { Data = value; } }

        public bool IsValid
        {
            get { return true; }
        }
    }

    [Flags]
    public enum AggregateTypeFlags
    {
        Min = 1,
        Max = 2,
        Sum = 4,
        Average = 8,
        Count = 16,
        LatestValue = 32
    }


    internal class Log
    {
        [JsonProperty("h")]
        public string Host { get; set; }

        [JsonProperty("un")]
        public string User { get; set; }

        [JsonProperty("pwd")]
        public string Password { get; set; }

        [JsonProperty("pub")]
        public string Topic { get; set; }
    }

    internal class HeartBeat
    {
        [JsonProperty("fq")]
        public int Frequency { get; set; }

        [JsonProperty("h")]
        public string Host { get; set; }

        [JsonProperty("un")]
        public string User { get; set; }

        [JsonProperty("pwd")]
        public string Password { get; set; }

        [JsonProperty("pub")]
        public string Topic { get; set; }
    }

    internal class SDKConfig
    {
        [JsonProperty("hb")]
        public HeartBeat HeartBeat { get; set; }

        [JsonProperty("log")]
        public HeartBeat Log { get; set; }

        [JsonProperty("sf")]
        public int SyncFreq { get; set; }

        [JsonProperty("df")]
        public int? DataFreq { get; set; }
    }

    internal class Device
    {
        
        [JsonProperty("tg")]
        public string Tag { get; set; }
        [JsonProperty("tag")]
        private string _Tag { set { Tag = value; } }

        [JsonProperty("id")]
        public string UniqueId { get; set; }
        [JsonProperty("uniqueId")]
        private string _UniqueId { set { UniqueId = value; } }

        [JsonProperty("s")]
        public string Status { get; set; }
        [JsonProperty("status")]
        private string _Status
        {
            set { Status = value; }
        }
    }
    public class AttributeSyncData
    {
        [JsonProperty("ln")]
        public string LocalName { get; set; }
        [JsonProperty("localName")]
        private string _LocalName { set { LocalName = value; } }

        [JsonProperty("dt")]//Datatype [0-Number, 1-String]
        public int DataType { get; set; }
        [JsonProperty("dataType")]//Datatype [0-Number, 1-String]
        private string _DataType { set { DataType = value.Equals("INTEGER") ? 0 : 1; } }

        [JsonProperty("dv")]
        public string DataValidation { get; set; }
        [JsonProperty("dataValidation")]
        private string _DataValidation { set { DataValidation = value; } }

        [JsonProperty("tg")]
        public string Tag { get; set; }
        [JsonProperty("tag")]
        private string _Tag { set { Tag = value; } }

        [JsonProperty("sq")]
        public int Sequence { get; set; }
        [JsonProperty("cSeq")]
        private int _Sequence { set { Sequence = value; } }

        [JsonProperty("agt")]
        public AggregateTypeFlags AggregateType { get; set; }
        [JsonProperty("aggregateType")]
        public AggregateTypeFlags _AggregateType { get; set; }

        [JsonProperty("tw")]
        public string TumblingWindow { get; set; }
        [JsonProperty("tumblingWindow")]
        private string _TumblingWindow { set { TumblingWindow = value; } }

    }

    public class Attribute
    {
        [JsonProperty("p")]
        public string Parent { get; set; }
        [JsonProperty("parent")]
        private string _Parent { set { Parent = value; } }

        [JsonProperty("dt")]
        public int? DataType { get; set; }
        [JsonProperty("dataType")]
        private string _DataType { set { DataType = value.Equals("INTEGER") ? 0 : 1; } }

        [JsonProperty("agt")]
        public AggregateTypeFlags AggregateType { get; set; }
        [JsonProperty("aggregateType")]
        private AggregateTypeFlags _AggregateType { set { AggregateType = value; } }

        [JsonProperty("tw")]
        public string TumblingWindow { get; set; }
        [JsonProperty("tumblingWindow")]
        private string _TumblingWindow { set { TumblingWindow = value; } }

        [JsonProperty("tg")]
        public string Tag { get; set; }
        [JsonProperty("tag")]
        private string _Tag { set { Tag = value; } }

        [JsonProperty("d")]
        public List<AttributeSyncData> AttributeSyncData { get; set; }
        [JsonProperty("data")]
        private List<AttributeSyncData> _AttributeSyncData { set { AttributeSyncData = value; } }
    }

    internal class Setting
    {
        [JsonProperty("ln")]
        public string LocalName { get; set; }


        [JsonProperty("dt")]//Datatype [0-Number, 1-String]
        public int DataType { get; set; }

        [JsonProperty("dv")]
        public string DataValidation { get; set; }
    }

    internal class Config
    {
        [JsonProperty("syncConfig")]
        public string SyncConfig { get; set; }
    }


    internal class Rule
    {
        [JsonProperty("g")]
        public string Guid { get; set; }
        [JsonProperty("es")]
        public string EventSubscriptionGuid { get; set; }
        [JsonProperty("con")]
        public string ConditionText { get; set; }
        [JsonProperty("cmd")]
        public string CommandText { get; set; }
    }

    internal class RootSyncData
    {
        [JsonProperty("cpId")]
        public string CpId { get; set; }

        [JsonProperty("rc")]
        public string ResponseCode { get; set; }

        [JsonProperty("ee")]
        public bool IsEdgeEnabled { get; set; }

        [JsonProperty("sc")]
        public SDKConfig SDKConfig { get; set; }

        [JsonProperty("p")]
        public Protocol Protocol { get; set; }

        [JsonProperty("d")]
        public List<Device> Devices { get; set; }

        [JsonProperty("att")]
        public List<Attribute> Attributes { get; set; }

        [JsonProperty("set")]
        public List<Setting> Settings { get; set; }

        [JsonProperty("r")]
        public List<Rule> Rules { get; set; }

        [JsonProperty("at")]
        public int AuthType { get; set; }

        public string GatewayDevice { get { return Devices.FirstOrDefault().UniqueId; } }

        [JsonProperty("dtg")]
        public Guid DeviceTemplateGuid { get; set; }

        public bool IsDataFreqApplicable
        {
            get
            {
                return DiscoveryCommon.IsDataFreqEnable && !IsEdgeEnabled && SDKConfig.DataFreq.HasValue && SDKConfig.DataFreq.Value > 0;
            }
        }

    }
}
