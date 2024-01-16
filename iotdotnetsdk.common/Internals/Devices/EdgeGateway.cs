using iotcdotnetsdk.common.Internals;

using iotdotnetsdk.common.Enums;
using iotdotnetsdk.common.Internals.Dynamic;
using iotdotnetsdk.common.Models;
using iotdotnetsdk.common.Models.D2C;
using iotdotnetsdk.common.Models.Identity;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace iotdotnetsdk.common.Internals.Devices
{
    internal class EdgeGateway : BaseGateway
    {
        public EdgeGateway(SyncData syncInfo, Func<string, Task> deviceCallBack, Func<string, Task> twinUpdateCallBack, SDKOptions options, DiscoveryCommon discoveryCommon, string platform)
            : base(syncInfo, deviceCallBack, twinUpdateCallBack, options, discoveryCommon, platform)
        {
        }

        public void ProcessAttributes()
        {
            if (AttributeList != null)
            {
                foreach (var item in AttributeList)
                {
                    //Process Non Parent Attributes
                    if (string.IsNullOrWhiteSpace(item.P))
                    {
                        foreach (var attr in item.D)
                        {
                            if ((attr.Dt == (int)DataTypes.Integer || attr.Dt == (int)DataTypes.Decimal || attr.Dt == (int)DataTypes.Long) && (!string.IsNullOrWhiteSpace(attr.Tw)))
                            {
                                CreateSendEdgeDataJob(item, attr);
                            }
                        }
                    }
                    else if (((DataTypes)item.Dt).ToString().Equals("Object", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (item.D != null && item.D.Count > 0)
                        {
                            CreateSendEdgeDataJob(item, null);
                        }

                    }
                }
            }

        }

        private void CreateSendEdgeDataJob(BaseAttrDetails item, AttrDetails attr)
        {
            try
            {
                double interval = -1;
                char hms;
                int duration;

                if (attr == null)
                {
                    hms = item.D.FirstOrDefault().Tw[item.D.FirstOrDefault().Tw.Length - 1];
                    duration = int.Parse(item.D.FirstOrDefault().Tw.Substring(0, item.D.FirstOrDefault().Tw.Length - 1));
                }
                else
                {
                    hms = attr.Tw[attr.Tw.Length - 1];
                    duration = int.Parse(attr.Tw.Substring(0, attr.Tw.Length - 1));
                }

                switch (hms)
                {
                    case 'h':
                        interval = duration * 3600000;
                        break;
                    case 'm':
                        interval = duration * 60000;
                        break;
                    case 's':
                        interval = duration * 1000;
                        break;
                }

                if (interval > 0)
                {
                    string localName = string.Empty, attrGuid = string.Empty;
                    AggregateTypeFlags aggregateType;

                    if (attr == null)
                    {
                        localName = item.P;
                        aggregateType = item.AggregateType;
                    }
                    else
                    {
                        localName = attr.Ln;
                        aggregateType = attr.AggregateType;
                    }

                    SDKCommon.Console_WriteLine($"Creating job for UniqueId:{UniqueId} LocalName:{item.P}_{localName} Interval:{interval}");

                    Job.Create($"{UniqueId}_{item.P}_{localName}", interval, new EdgeJobActionArgs()
                    {
                        UniqueId = UniqueId,
                        Parent = item.P,
                        //parentguid = item.guid,
                        LocalName = localName,
                        Guid = attrGuid,
                        AggregateType = aggregateType
                    }, SendEdgeData);
                }
            }
            catch (Exception ex)
            {
                SDKCommon.Console_WriteLine($"Creating job for UniqueId:{UniqueId} Error:{ex.Message}");
                new InternalException("CreateSendEdgeDataJob Constructor", ex);
            }
        }

        List<EdgeDataModel> offlineData = new List<EdgeDataModel>();

        internal void SendEdgeData(DateTime signalTime, JobActionArgs args)
        {
            try
            {
                EdgeJobActionArgs edgeArgs = args as EdgeJobActionArgs;
                if (offlineData.Count == 0) return;

                List<EdgeDataModel> telemDataOffline;
                bool isObject = false;


                if (string.IsNullOrWhiteSpace(edgeArgs.Parent))
                    telemDataOffline = GetTelemetryData(edgeArgs.LocalName);
                else
                {
                    isObject = true;
                    telemDataOffline = GetTelemetryData(edgeArgs.Parent, true);
                }

                if (telemDataOffline != null && telemDataOffline.Count > 0)
                {
                    var groupByUniqueId = telemDataOffline.GroupBy(x => x.UniqueId).ToList();

                    foreach (var itemUniqueId in groupByUniqueId)
                    {
                        Dictionary<string, object> attrObject2 = new Dictionary<string, object>();
                        var telemetryData = itemUniqueId.ToList();
                        if (isObject)
                        {
                            var query = (from d in telemetryData
                                         group d by new { d.LocalName, d.ChildGuid } into gData
                                         select new
                                         {
                                             Attribute = gData.Key,
                                             Avg = gData.Average(g => decimal.Parse(g.Value)),
                                             Sum = gData.Sum(g => decimal.Parse(g.Value)),
                                             Min = gData.Min(g => decimal.Parse(g.Value)),
                                             Max = gData.Max(g => decimal.Parse(g.Value)),
                                             LatestValue = decimal.Parse(gData.OrderByDescending(g => g.DTime).FirstOrDefault().Value),
                                             Count = gData.Count()
                                         }).ToList();
                            var attrDataArray2 = new Dictionary<string, object>();
                            foreach (var item in query)
                            {
                                List<decimal> edgeValue = new List<decimal>
                                {
                                    item.Min,
                                    item.Max,
                                    item.Sum,
                                    decimal.Round(item.Avg, 2),
                                    item.Count,
                                    item.LatestValue
                                };
                                attrDataArray2.Add(item.Attribute.LocalName, edgeValue);
                            }
                            attrObject2.TryAdd(edgeArgs.Parent, attrDataArray2);
                        }
                        else
                        {
                            List<decimal> edgeValue = new List<decimal>
                            {
                                telemetryData.Min((a) => decimal.Parse(a.Value)),
                                telemetryData.Max((a) => decimal.Parse(a.Value)),
                                telemetryData.Sum((a) => decimal.Parse(a.Value)),
                                decimal.Round(telemetryData.Average((a) => decimal.Parse(a.Value)), 2),
                                telemetryData.Count(),
                                decimal.Parse(telemetryData.OrderByDescending(a => a.DTime).FirstOrDefault().Value)
                            };
                            attrObject2.TryAdd(edgeArgs.LocalName, edgeValue);
                        }

                        var jArr = new JArray
                        {
                            JObject.FromObject(attrObject2)
                        };

                        var deviceObject2 = new Device2()
                        {
                            DeviceUniqueId = itemUniqueId.Key,
                            DeviceTime = DateTime.Now.ToUniversalTime(),
                            Data = JObject.FromObject(attrObject2),
                            Tag = itemUniqueId.FirstOrDefault().Tag
                        };

                        var deviceArray2 = new List<Device2>
                        {
                            deviceObject2
                        };

                        var header2 = new DeviceDataModel()
                        {
                            Time = DateTime.Now.ToUniversalTime(),
                            UniqueDeviceInfo = deviceArray2
                        };

                        SendD2C(header2, new KeyValuePair<string, string>("mt", ((int)MessageTypes.RPT_EDGE).ToString()));
                        SDKCommon.Console_WriteLine($"Edge Message Sent for UniqueId:{itemUniqueId.Key}, Parent:{edgeArgs.Parent}, LocalName:{edgeArgs.LocalName}");
                    }
                }
            }
            catch (Exception ex)
            {
                SDKCommon.Console_WriteError($"SendEdgeData Error:{ex.Message}");
            }
        }

        internal void RestartJobs()
        {
            SDKCommon.Console_WriteLine("RestartJobs for Edge");
            Job.Clear(UniqueId);
            ProcessAttributes();
        }

        private List<EdgeDataModel> GetTelemetryData(string localName, bool isParent = false)
        {
            List<EdgeDataModel> telemetyData = null;
            try
            {
                if (isParent)
                    telemetyData = offlineData.Where(a => !string.IsNullOrWhiteSpace(a.Parent) && a.Parent.Equals(localName, StringComparison.CurrentCultureIgnoreCase)).ToList();
                else
                    telemetyData = offlineData.Where(a => a.LocalName.Equals(localName, StringComparison.CurrentCultureIgnoreCase)).ToList();

                if (telemetyData != null)
                {
                    foreach (var item in telemetyData)
                    {
                        offlineData.Remove(item);
                    }
                }
                //var removedCount = offlineData.RemoveAll((a) => a.LocalName.Equals(localName, StringComparison.CurrentCultureIgnoreCase));
                return telemetyData;
            }
            catch
            {
                return null;
            }
        }

        internal override void SendData(string data)
        {
            List<DeviceTelemetryModel> devices;
            SDKCommon.Console_WriteLine($"Deserializing data for UniqueId:{UniqueId}");

            try
            {
                devices = JsonConvert.DeserializeObject<List<DeviceTelemetryModel>>(data);
                SDKCommon.Console_WriteLine($"Deserializing data for UniqueId:{UniqueId} - Success");
            }
            catch (Exception ex)
            {
                SDKCommon.Console_WriteLine($"Deserializing data for UniqueId:{UniqueId} - Failed");
                SDKCommon.Console_WriteError($"Serialization exception for UniqueId:{UniqueId} Data:{data} Exception:{ex.Message}");
                return;
            }

            BaseTelemetryDataModel hubMsg = null;

            try
            {
                SDKCommon.Console_WriteLine($"Preparing IoTConnect edge message for UniqueId:{UniqueId}");
                hubMsg = PrepareMessage(devices);
                SDKCommon.Console_WriteLine($"Preparing IoTConnect edge message for UniqueId:{UniqueId} - Success");
            }
            catch (Exception ex)
            {
                SDKCommon.Console_WriteError($"Error while preparing edge message for UniqueId:{UniqueId} Data:{data} Exception:{ex.Message}");
                return;
            }
            CreateFaultDataJob(hubMsg);
            TelemetryDataModel tdm = null;
            Type type = null;
            try
            {
                var properties = (from elm in AttributeList
                                  from attr in elm.D
                                  where attr.Dt != (int)DataTypes.Object
                                  select new NameValueType
                                  {
                                      Tag = string.IsNullOrWhiteSpace(elm.P) ? attr.Tg : elm.Tg,
                                      Name = (string.IsNullOrWhiteSpace(elm.P) ?
                (!string.IsNullOrWhiteSpace(attr.Tg) ? $"{attr.Tg}____{attr.Ln}" : attr.Ln) : (!string.IsNullOrWhiteSpace(elm.Tg) ? $"{elm.Tg}____{elm.P}____{attr.Ln}" : $"{elm.P}____{attr.Ln}")),
                                      Type = (attr.Dt == (int)DataTypes.Integer || attr.Dt == (int)DataTypes.Decimal || attr.Dt == (int)DataTypes.Long) ? "System.Double" : "System.String",
                                      Value = (attr.Dt == (int)DataTypes.Integer || attr.Dt == (int)DataTypes.Decimal || attr.Dt == (int)DataTypes.Long) ? "0" : ""
                                  }).ToList();

                var className = string.Format("type_{0}", SyncData.Protocol.Id.ToString().Replace("-", "_"));
                type = ClassBuilder.CreateObject(className, properties).GetType();
                tdm = ConvertToTelemetyDM(hubMsg.Reporting);
            }
            catch (Exception ex)
            {
                SDKCommon.Console_WriteError($"Error while converting telemetry model for UniqueId:{UniqueId} Data:{data} Exception:{ex.Message}");
                return;
            }

            try
            {
                EvaluteRule(tdm, type, hubMsg.Reporting);
            }
            catch (Exception ex)
            {
                SDKCommon.Console_WriteError($"Error while Evaluting rule UniqueId:{UniqueId} Data:{data} Exception:{ex.Message}");
                return;
            }
        }

        public static TelemetryDataModel ConvertToTelemetyDM(ReportingModel telemetry)
        {
            TelemetryDataModel dm = new TelemetryDataModel
            {
                UniqueDeviceInfo = new List<UniqueDeviceInfo>()
            };

            foreach (var jdevice in telemetry.Data)
            {
                var device = new UniqueDeviceInfo() { UniqueId = jdevice.Id, DTime = jdevice.Dt.ToUniversalTime().ToString("o"), Tag = jdevice.Tg };
                dm.UniqueDeviceInfo.Add(device);

                device.ParentAttrInfo = new List<ParentAttrInfo>();
                ParentAttrInfo noParent = new ParentAttrInfo() { Parent = "", ChildAttrInfo = new List<ChildAttrInfo>(), STime = jdevice.Dt.ToUniversalTime().ToString("o") };
                device.ParentAttrInfo.Add(noParent);

                foreach (JProperty item1 in jdevice.Data.Properties())
                {
                    if (item1.Value is JObject)
                    {
                        ParentAttrInfo parent = new ParentAttrInfo()
                        {
                            Parent = item1.Name,
                            ChildAttrInfo = new List<ChildAttrInfo>(),
                            STime = jdevice.Dt.ToUniversalTime().ToString("o")
                        };
                        device.ParentAttrInfo.Add(parent);

                        var objData = item1.Value as JObject;
                        foreach (JProperty c in objData.Children<JProperty>())
                        {
                            ChildAttrInfo child = new ChildAttrInfo();
                            child.LocalName = c.Name;
                            child.Value = c.Value.ToString();
                            parent.ChildAttrInfo.Add(child);
                        }
                    }
                    else
                    {
                        ChildAttrInfo child = new ChildAttrInfo();
                        noParent.ChildAttrInfo.Add(child);
                        child.LocalName = item1.Name;
                        child.Value = item1.Value.ToString();
                    }
                }
            }
            return dm;
        }

        static JsonSerializerSettings settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private void EvaluteRule(TelemetryDataModel dataModel, Type type, ReportingModel telemetry)
        {
            List<UniqueDeviceInfo> uniqueDeviceInfo = dataModel.UniqueDeviceInfo;

            if (uniqueDeviceInfo == null || uniqueDeviceInfo.Count == 0)
                return;

            var attrData = (from device in dataModel.UniqueDeviceInfo
                            from attr in device.ParentAttrInfo
                            from child in attr.ChildAttrInfo.ConvertAll(a => a as ChildAttrInfo)
                            select new
                            {
                                Name = (string.IsNullOrWhiteSpace(device.Tag) ? "" : device.Tag + "____") + (string.IsNullOrWhiteSpace(attr.Parent) ? child.LocalName : (attr.Parent + "____" + child.LocalName)),
                                child.Value,
                                device.UniqueId
                            });

            var distDevice = attrData.SkipWhile(a => a.UniqueId == dataModel.UniqueDeviceInfo[0].UniqueId).Select(s => s.UniqueId).Distinct();
            var enumerable = (new[] { dataModel.UniqueDeviceInfo[0].UniqueId }).AsEnumerable();

            var rank = attrData.SkipWhile(a => a.UniqueId == dataModel.UniqueDeviceInfo[0].UniqueId).GroupBy(d => d.UniqueId)
                .SelectMany(g => g.Select((x, i) => new { UniqueId = g.Key, item = x, rank = i + 1 })).ToList();

            foreach (var dd in distDevice)
            {
                var t = rank.Where(a => a.UniqueId == dd).Select(a => $"{a.UniqueId}###{a.rank}").ToArray();
                enumerable = enumerable.SelectMany(p => t, (a, b) => $"{a},{b}").ToList();
            }

            JObject jo = new JObject();

            foreach (var item in attrData)
            {
                jo[item.Name] = item.Value;
            }

            var templateClass = JsonConvert.DeserializeObject(jo.ToString(), type, settings);

            IList list = null;
            Type listType = typeof(List<>).MakeGenericType(new[] { templateClass.GetType() });
            list = (IList)Activator.CreateInstance(listType);

            foreach (var item in enumerable.ToList())
            {
                jo = new JObject();
                var uids = item.Split(',').ToList();
                var gateway = attrData.Where(at => at.UniqueId == dataModel.UniqueDeviceInfo[0].UniqueId);

                foreach (var gattr in gateway)
                {
                    jo[gattr.Name] = gattr.Value;
                }

                foreach (var aa in uids.Skip(1))
                {
                    var sd = aa.Split(new string[] { "###" }, StringSplitOptions.RemoveEmptyEntries);
                    var attrs = rank.Where(r => r.UniqueId == sd[0] && r.rank.ToString() == sd[1]);

                    foreach (var attr in attrs)
                    {
                        jo[attr.item.Name] = attr.item.Value;
                    }
                }

                var tmpClass = JsonConvert.DeserializeObject(jo.ToString(), type, settings);
                list.Add(tmpClass);
            }

            var gatewayDevice = uniqueDeviceInfo[0];
            bool isGateway = !string.IsNullOrWhiteSpace(gatewayDevice.Tag);
            //var childDevices = uniqueDeviceInfo.Select(d => d.UniqueId).ToList();

            if (SyncData.Has.R != null)
            {
                foreach (var item in SyncData.Rules)
                {
                    List<string> attrs = new List<string>();
                    var matchedAttributes = list.AsQueryable().Where(item.ConditionText.Replace("#", "____"), attrs);
                    if (matchedAttributes.Count() > 0)
                    {
                        RuleDataModel2 dm = new RuleDataModel2()
                        {
                            Devices = new List<DeviceRuleInfo>()
                        };
                        Dictionary<string, object> conditionValue = new Dictionary<string, object>();

                        if (!string.IsNullOrWhiteSpace(item.CommandText))
                        {
                            JObject objDevice = new JObject
                            {
                                ["uniqueId"] = gatewayDevice.UniqueId,
                                ["command"] = item.CommandText,
                                ["ack"] = false,
                                ["cmdType"] = "0"
                            };

                            JObject cmdMessage = new JObject
                            {
                                ["cmdType"] = "0",
                                ["data"] = objDevice
                            };

                            CloudMessages(JsonConvert.SerializeObject(cmdMessage), _deviceCallBack);
                        }

                        foreach (var itemList in list)
                        {
                            var properties = itemList.GetType().GetProperties();
                            foreach (var itemAttrs in attrs)
                            {
                                var prop = properties.FirstOrDefault(p => p.Name.Equals(itemAttrs, StringComparison.CurrentCultureIgnoreCase));
                                if (prop != null)
                                {
                                    var val = itemList.GetType().GetProperty(prop.Name).GetValue(itemList);
                                    if (!string.IsNullOrWhiteSpace(prop.Name) && prop.Name.Contains("____"))
                                    {
                                        var parentAttr = prop.Name.Split("____");

                                        if (isGateway)
                                        {
                                            string parentAttrWithTag = $"{parentAttr[0]}.{parentAttr[1]}";

                                            if (!conditionValue.ContainsKey(parentAttrWithTag) && parentAttr.Length > 2)
                                                conditionValue.Add(parentAttrWithTag, new Dictionary<string, object>());

                                            if (val != null)
                                            {
                                                if (parentAttr.Length == 3)
                                                {
                                                    var childAttr = conditionValue[parentAttrWithTag];
                                                    (childAttr as Dictionary<string, object>).TryAdd(parentAttr[2], val);
                                                }
                                                else
                                                {
                                                    conditionValue.TryAdd(parentAttrWithTag, val);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (!conditionValue.ContainsKey(parentAttr[0]))
                                                conditionValue.Add(parentAttr[0], new Dictionary<string, object>());

                                            if (val != null)
                                            {
                                                var childAttr = conditionValue[parentAttr[0]];
                                                (childAttr as Dictionary<string, object>).TryAdd(parentAttr[1], val);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        conditionValue.TryAdd(prop.Name, val);
                                    }
                                }
                            }
                        }

                        var telemData = telemetry as ReportingModel;

                        foreach (var itemCondition in conditionValue)
                        {
                            var tagAttr = itemCondition.Key.Split('.');
                            if (tagAttr.Length == 1)
                            {
                                dm.Devices.AddRange(telemData.Data.Where(z => string.IsNullOrWhiteSpace(z.Tg)).Select(x => new DeviceRuleInfo()
                                {
                                    ConditionText = item.ConditionText,
                                    DTime = gatewayDevice.DTime,
                                    RuleGuid = item.Guid,
                                    Tag = x.Tg,
                                    UniqueId = x.Id,
                                    ConditionValue = conditionValue,
                                    EventSubscriptionGuid = item.EventSubscriptionGuid,
                                    Data = x.Data// new JArray(x.Data.Select(y => y[tagAttr[0]]))
                                }));
                            }
                            else
                            {
                                dm.Devices.AddRange(telemData.Data.Where(z => z.Tg.Equals(tagAttr[0])).Select(x => new DeviceRuleInfo()
                                {
                                    ConditionText = item.ConditionText,
                                    DTime = gatewayDevice.DTime,
                                    RuleGuid = item.Guid,
                                    Tag = x.Tg,
                                    UniqueId = x.Id,
                                    ConditionValue = conditionValue,
                                    EventSubscriptionGuid = item.EventSubscriptionGuid,
                                    Data = x.Data// new JArray(x.Data.Select(y => y))
                                }));
                            }
                        }

                        var jsonRule = JsonConvert.SerializeObject(dm);

                        SendD2C(dm, new KeyValuePair<string, string>("mt", ((int)MessageTypes.RULE_MATCHED_EDGE).ToString()));

                        SDKCommon.Console_WriteLine($"Rule matched for UniqueId:{gatewayDevice.UniqueId} ConditionText:{item.ConditionText} EventSubscriptionGuid{item.EventSubscriptionGuid}");
                    }
                }
            }
        }

        internal virtual BaseTelemetryDataModel PrepareMessage(List<DeviceTelemetryModel> devices)
        {
            long lVal; decimal dVal;
            try
            {
                BaseTelemetryDataModel hubMsg = new BaseTelemetryDataModel
                {
                    Reporting = new ReportingModel()
                    {
                        Data = new List<DeviceTelemetryModel>(),
                        Dt = DateTime.UtcNow
                    },
                    Fault = new ReportingModel()
                    {
                        Data = new List<DeviceTelemetryModel>(),
                        Dt = DateTime.UtcNow
                    }
                };

                foreach (var d2 in devices)
                {
                    if (d2.Dt == DateTime.MinValue)
                        d2.Dt = DateTime.UtcNow;

                    var vd = DeviceList.FirstOrDefault(d => d.Id == d2.Id);
                    if (vd != null)
                    {
                        d2.Tg = vd.Tg;
                    }

                    DeviceTelemetryModel rd2 = new DeviceTelemetryModel() { Id = d2.Id, Tg = d2.Tg, Dt = d2.Dt, Data = new JObject() };
                    DeviceTelemetryModel fd2 = new DeviceTelemetryModel() { Id = d2.Id, Tg = d2.Tg, Dt = d2.Dt, Data = new JObject() };
                    bool hasFault = false;
                    bool hasReporting = false;

                    foreach (var root in d2.Data.Children<JProperty>())
                    {
                        if (root.Value is JObject)
                        {
                            JObject rObj = new JObject();
                            JObject fObj = new JObject();

                            var objData = root.Value as JObject;
                            JObject rChild = new JObject();
                            JObject fChild = new JObject();
                            foreach (JProperty c in objData.Children<JProperty>())
                            {
                                var attrs = (from attr in AttributeList
                                             from cattr in attr.D
                                             where (!string.IsNullOrWhiteSpace(attr.P)) && cattr.Ln == c.Name
                                             select new { cattr = cattr, parent = attr.P });

                                if (attrs == null || attrs.Count() == 0)
                                {
                                    hasFault = true;
                                    //Add to Fault
                                    fChild.Add(new JProperty(c.Name, c.Value));
                                }
                                var SopportedDataTypes = new int[] { 1, 2, 3 };
                                var dataTypeFilter = (from typeFilter in attrs
                                                      where SopportedDataTypes.Contains(typeFilter.cattr.Dt)
                                                      select new { cattr = typeFilter.cattr, parent = typeFilter.parent });

                                if (dataTypeFilter == null || dataTypeFilter.Count() == 0)
                                    continue;

                                if (attrs != null && attrs.Count() > 0)
                                {
                                    var v = (from attr in dataTypeFilter
                                             where attr.cattr.Tg == d2.Tg && attr.parent == root.Name
                                             select attr.cattr).FirstOrDefault();

                                    if (c.HasValues && !string.IsNullOrWhiteSpace(c.Value.ToString()))
                                    {
                                        if (v != null && ValidateValueWithDataType(c, v.Dt, v.Dv, out _))
                                        {
                                            hasReporting = true;

                                            //Add to Reporting
                                            var edm = new EdgeDataModel() { Parent = root.Name, LocalName = c.Name, DataType = v.Dt.ToString(), DTime = d2.Dt, UniqueId = d2.Id, Tag = v.Tg };
                                            if (long.TryParse(c.Value.ToString(), out lVal))
                                                edm.Value = lVal.ToString();
                                            else if (decimal.TryParse(c.Value.ToString(), out dVal))
                                                edm.Value = dVal.ToString();
                                            if (edm != null)
                                            {
                                                offlineData.Add(edm);
                                            }

                                            //Add to Reporting
                                            rChild.Add(new JProperty(c.Name, c.Value));
                                        }
                                        else
                                        {
                                            hasFault = true;
                                            //Add to Fault
                                            fChild.Add(new JProperty(c.Name, c.Value));
                                        }
                                    }
                                }
                            }

                            rObj[root.Name] = rChild;
                            fObj[root.Name] = fChild;
                            if (rChild.Count > 0)
                            {
                                foreach (var item in rObj.Children<JProperty>())
                                {
                                    rd2.Data.Add(item);
                                }
                            }

                            if (fChild.Count > 0)
                            {
                                foreach (var item in fObj.Children<JProperty>())
                                {
                                    fd2.Data.Add(item);
                                }
                            }
                        }
                        else
                        {
                            var attrs = (from attr in AttributeList
                                         from cattr in attr.D
                                         where string.IsNullOrWhiteSpace(attr.P) && cattr.Ln == root.Name
                                         select cattr);

                            if (attrs == null || attrs.Count() == 0)
                            {
                                hasFault = true;
                                //Add to Fault
                                fd2.Data.Add(new JProperty(root.Name, root.Value));
                                continue;
                            }

                            var v = (from attr in attrs
                                     where attr.Tg == d2.Tg
                                     select attr).FirstOrDefault();

                            if (root.HasValues && !string.IsNullOrWhiteSpace(root.Value.ToString()))
                            {
                                if (v != null && ValidateValueWithDataType(root, v.Dt, v.Dv, out _))
                                {
                                    hasReporting = true;

                                    //Add to Reporting
                                    var edm = new EdgeDataModel() { LocalName = root.Name, DataType = v.Dt.ToString(), DTime = d2.Dt, UniqueId = d2.Id, Tag = v.Tg };
                                    if (long.TryParse(root.Value.ToString(), out lVal))
                                        edm.Value = lVal.ToString();
                                    else if (decimal.TryParse(root.Value.ToString(), out dVal))
                                        edm.Value = dVal.ToString();

                                    if (edm != null)
                                    {
                                        offlineData.Add(edm);
                                    }

                                    //Add to Reporting
                                    rd2.Data.Add(new JProperty(root.Name, root.Value));
                                }
                                else
                                {
                                    hasFault = true;
                                    //Add to Fault
                                    fd2.Data.Add(new JProperty(root.Name, root.Value));
                                }
                            }
                        }
                    }

                    if (hasFault)
                        hubMsg.Fault.Data.Add(fd2);
                    if (hasReporting)
                        hubMsg.Reporting.Data.Add(rd2);
                }


                return hubMsg;
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        private void CreateFaultDataJob(BaseTelemetryDataModel hubMsg)
        {
            if (!hubMsg.Fault.HasData)
                return;
            try
            {
                DateTime currentTime = DateTime.UtcNow;
                foreach (var item in hubMsg.Fault.Data)
                {
                    foreach (var root in item.Data.Children<JProperty>())
                    {
                        string localName = root.Name;
                        string jobKey = $"{item.Id}_{root.Name}_faulty";
                        if (!faultAttrDetails.ContainsKey(jobKey) || currentTime > faultAttrDetails[jobKey])
                        {
                            JToken value = root.Value;
                            var objData = new JObject();
                            if (value.Type == JTokenType.Object)
                            {
                                objData = ((JObject)value);
                            }
                            else
                            {
                                objData = (new JObject(new JProperty(root.Name, value)));
                            }
                            SendFaultData(currentTime, new DeviceTelemetryModel()
                            {
                                Id = item.Id,
                                Data = objData,
                                Tg = item.Tg
                            });
                            if (!faultAttrDetails.ContainsKey(jobKey))
                                faultAttrDetails.Add(jobKey, currentTime.AddSeconds(60));
                            else
                                faultAttrDetails[jobKey] = currentTime.AddSeconds(60);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                SDKCommon.Console_WriteLine($"Creating fault data job for UniqueId:{UniqueId} Error:{ex.Message}");
                new InternalException("CreateFaultDataJob Constructor", ex);
            }
        }

        private void SendFaultData(DateTime signalTime, DeviceTelemetryModel args)
        {
            try
            {
                var deviceObject2 = new Device2()
                {
                    DeviceUniqueId = args.Id,
                    DeviceTime = DateTime.Now.ToUniversalTime(),
                    Data = new JObject(args.Data),
                    Tag = args.Tg
                };

                var deviceArray2 = new List<Device2>
                        {
                            deviceObject2
                        };

                var header2 = new DeviceDataModel()
                {
                    Time = DateTime.Now.ToUniversalTime(),
                    UniqueDeviceInfo = deviceArray2
                };

                SendD2C(header2, new KeyValuePair<string, string>("mt", ((int)MessageTypes.FLT).ToString()));
                SDKCommon.Console_WriteLine($"Edge fault Message Sent for UniqueId:{args.Id}");
            }
            catch
            {
                SDKCommon.Console_WriteLine($"Error while sending fault data for UniqueId:{args.Id}");
            }
        }

        public class EdgeJobActionArgs : JobActionArgs
        {
            public string Parent { get; internal set; }
            public string LocalName { get; internal set; }
            public AggregateTypeFlags AggregateType { get; internal set; }
            public string UniqueId { get; internal set; }
            public string ParentGuid { get; internal set; }
            public string Guid { get; internal set; }
        }

        internal class JobActionArgs : EventArgs
        {

        }

        internal class Job
        {
            private static Dictionary<string, Job> runningJobs = new Dictionary<string, Job>();
            private static readonly object _lock = new object();

            private Job()
            {
                cronTimer = new System.Timers.Timer();
            }

            private System.Timers.Timer cronTimer;

            internal static bool Clear(string jobName)
            {
                var keys = runningJobs.Keys.Where(k => k.StartsWith(jobName)).ToList();
                for (int i = 0; i < keys.Count; i++)
                {
                    var timer = runningJobs[keys[i]];
                    runningJobs.Remove(keys[i]);
                    timer.cronTimer.Enabled = false;
                    timer.cronTimer.Stop();
                    timer.cronTimer.Close();
                    timer.cronTimer.Dispose();
                }

                return true;
            }

            internal static bool ClearAll()
            {
                foreach (var key in runningJobs.Select(k => k.Key))
                {
                    Clear(key);
                }

                return false;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="jobName">Unique name of Job</param>
            /// <param name="interval">Gets or sets the interval, expressed in milliseconds, at which to raise the job runs.</param>
            /// <param name="args"></param>
            /// <param name="eventAction">Action Delegate when the interval elapses</param>
            /// <param name="enabled"></param>
            /// <param name="autoReset"></param>
            internal static bool Create(string jobName, double interval, JobActionArgs args, Action<DateTime, JobActionArgs> eventAction, bool enabled = true, bool autoReset = true)
            {
                if (!runningJobs.ContainsKey(jobName))
                {
                    lock (_lock)
                    {
                        if (!runningJobs.ContainsKey(jobName))
                        {
                            Job c = new Job();
                            c.cronTimer.Interval = interval;
                            c.cronTimer.Enabled = enabled;
                            c.cronTimer.AutoReset = autoReset;
                            c.cronTimer.Elapsed += (sender, e) =>
                            {
                                c.cronTimer.Stop();
                                eventAction(e.SignalTime, args);
                                c.cronTimer.Start();
                            };
                            runningJobs.Add(jobName, c);
                            c.cronTimer.Start();
                            return true;
                        }
                    }
                }
                return false;
            }

            internal static bool Exist(string jobName)
            {
                return runningJobs.ContainsKey(jobName);
            }
        }

        internal class EdgeDataModel : ICloneable
        {
            public string UniqueId { get; internal set; }
            public string LocalName { get; internal set; }
            public string Value { get; internal set; }
            public string Parent { get; internal set; }
            public string ParentGuid { get; internal set; }
            public string ChildGuid { get; internal set; }
            public string DataType { get; internal set; }
            public DateTime DTime { get; internal set; }

            public string Tag { get; set; }
            public decimal Avg { get; internal set; }
            public decimal Sum { get; internal set; }
            public decimal Min { get; internal set; }
            public decimal Max { get; internal set; }
            public decimal LatestValue { get; internal set; }
            public int Count { get; internal set; }

            public object Clone()
            {
                return MemberwiseClone();
            }

            internal void ResetData()
            {
                Avg = Sum = Min = Max = LatestValue = Count = 0;
            }

            internal void SetData(decimal value)
            {
                LatestValue = value;
                Sum += value;
                Min = (Count == 0) ? value : Min < value ? Min : value;
                Max = (Count == 0) ? value : Max > value ? Max : value;
                Count++;
                Avg = Sum / Count;
            }
        }
    }
}