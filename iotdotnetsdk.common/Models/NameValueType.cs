using iotdotnetsdk.common.Models.D2C;
using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iotdotnetsdk.common.Models
{
    internal class NameValueType
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public object Value { get; set; }
        public string Guid { get; set; }
        public string Tag { get; set; }
    }

    internal class ChildAttrInfo
    {
        //[JsonProperty("guid")] //TODO : removed
        //public string Guid { get; set; }

        [JsonProperty("ln")]
        public string LocalName { get; set; }

        [JsonProperty("v")]
        public string Value { get; set; }
        internal string DataType { get; set; }
        internal bool IsValid { get; set; }
    }

    internal class ParentAttrInfo
    {
        [JsonProperty("p")]
        public string Parent { get; set; }

        //[JsonProperty("guid")] //TODO : removed
        //public string Guid { get; set; }

        [JsonProperty("st")]
        public string STime { get; set; }

        [JsonProperty("d")]
        public List<ChildAttrInfo> ChildAttrInfo { get; set; }
    }

    internal class UniqueDeviceInfo
    {
        [JsonProperty("id")]
        public string UniqueId { get; set; }

        [JsonProperty("dt")]
        public string DTime { get; set; }

        [JsonProperty("d")]
        public List<ParentAttrInfo> ParentAttrInfo { get; set; }
        [JsonProperty("tg")]
        public string Tag { get; internal set; }
    }

    internal class TelemetryDataModel : BaseTelemetryDataModel
    {
        [JsonProperty("d")]
        public List<UniqueDeviceInfo> UniqueDeviceInfo { get; set; }

        public TelemetryDataModel()
        {   
            UniqueDeviceInfo = new List<UniqueDeviceInfo>();
        }
    }
}
