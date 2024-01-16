using iotdotnetsdk.common.Enums;
using iotdotnetsdk.common.Interfaces;
using iotdotnetsdk.common.Models;
using iotdotnetsdk.common.Models.C2D;
using iotdotnetsdk.common.Models.D2C;
using iotdotnetsdk.common.Models.Identity;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using MQTTnet.Client;
using iotdotnetsdk.common.Brokers;
using MQTTnet.Packets;
using System.Security.AccessControl;
using iotcdotnetsdk.common.Internals;
using System.Diagnostics;
using System.Xml;

namespace iotdotnetsdk.common.Internals.Devices
{
    internal abstract class BaseGateway : IDisposable
    {
        #region Device Identity properties
        internal SyncData SyncData { get; set; }
        internal Has SyncHasInfo { get; set; }
        internal List<BaseAttrDetails> AttributeList { get; set; }
        internal List<SettingDetails> SettingList { get; set; }
        internal List<RuleDetails> RuleList { get; set; }
        internal List<DeviceDetails> DeviceList { get; set; }
        internal OtaDetails OtaUpdate { get; set; }
        #endregion

        private readonly string _templateCode;
        private readonly string _uniqueId;
        private SDKOptions _options;
        private IMessageBroker _brokerInstance;
        private bool _isConnected;
        private bool _isInternetAvailableLastTime;
        private bool _skipValidation = false;
        private static string topic = "";
        private static string platform = "";
        private static bool isAZPlatform = false;
        private static bool hasChildDevice = false;

        protected DateTime? LastDataSentTime { get; set; }
        protected Dictionary<string, DateTime> faultAttrDetails = new Dictionary<string, DateTime>();
        internal DiscoveryCommon _discoveryCommon { get; set; }

        internal string UniqueId
        {
            get { return _uniqueId; }
        }
        internal string TemplateCode
        {
            get { return _templateCode; }
        }

        internal bool IsDeviceBarred = false;

        #region Callbacks
        internal protected Func<string, Task> _deviceCallBack;
        internal protected Func<string, Task> _shadowUpdateCallBack;
        internal protected Func<string, Task> _connectionStatusCallback;

        internal Func<string, Task> GetAttributeCallback;
        internal Func<string, Task> GetShadowCallback;
        internal Func<string, Task> GetRuleCallback;
        internal Func<string, Task> GetChildDeviceCallback;
        internal Func<string, Task> CreateChildDeviceCallback;
        internal Func<string, Task> DeleteChildDeviceCallback;
        internal Func<string, Task> AttributeChangedCallback;
        internal Func<string, Task> ShadowChangedCallback;
        internal Func<string, Task> DeviceChangedCallback;
        internal Func<string, Task> DeviceCommandCallback;
        internal Func<string, Task> OtaUpdateCommandCallback;
        internal Func<string, Task> ModuleCommandCallback;
        #endregion

        public BaseGateway(string uniqueId, Func<string, Task> deviceCallBack, Func<string, Task> shadowUpdateCallBack)
        {
            _uniqueId = uniqueId;
            _deviceCallBack = deviceCallBack;
            _shadowUpdateCallBack = shadowUpdateCallBack;
        }

        internal protected BaseGateway(SyncData syncResponse, Func<string, Task> deviceCallBack, Func<string, Task> shadowUpdateCallBack, SDKOptions options, DiscoveryCommon discoveryCommon, string pf)
        {
            SyncData = syncResponse;
            SyncHasInfo = syncResponse.Has;
            _templateCode = SyncData.Meta.TemplateCode;
            _uniqueId = SyncData.Meta.Pf == 1 ? string.Join("-", SyncData.Protocol.Id.Split('-').Skip(1)) : SyncData.Protocol.Id;
            _deviceCallBack = deviceCallBack;
            _shadowUpdateCallBack = shadowUpdateCallBack;
            _options = options;
            if (DeviceList == null)
                DeviceList = new List<DeviceDetails>();

            DeviceList.Add(new DeviceDetails()
            {
                Id = _uniqueId,
                Tg = SyncData?.Meta?.Gtw?.Tag
            });
            _discoveryCommon = discoveryCommon;
            platform = pf;
            isAZPlatform = (platform == "az");
        }

        internal async void Connect(Func<string, Task> connectionStatusCallback)
        {
            if (IsDeviceBarred)
            {
                SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - Connect not Permitted");
                throw new Exception($"Device:{_uniqueId} is barred - Connect not Permitted");
            }

            AuthType authType = (AuthType)SyncData.Meta.At;

            switch (authType)
            {
                case AuthType.Token:
                case AuthType.SymmetricKey:
                    if (authType == AuthType.SymmetricKey)
                    {
                        if (string.IsNullOrWhiteSpace(_options.DevicePK))
                        {
                            throw new Exception("Please provide primarykey information in SDKOptions.");
                        }
                        var resourceUri = $"{SyncData.Protocol.H}/devices/{SyncData.Protocol.Id}";
                        //symmetric key
                        SyncData.Protocol.Pwd = GenerateSasToken(resourceUri, _options.DevicePK, null);
                    }
                    _brokerInstance = new MqttBroker(SyncData.Protocol.H, SyncData.Protocol.P, SyncData.Protocol.Id, _options.Certificate, SyncData.Protocol.Un, SyncData.Protocol.Pwd);
                    break;

                case AuthType.CASignedIndividual:
                case AuthType.SelfSigned:
                case AuthType.CASigned:
                    if (_options == null || _options.Certificate == null ||
                        ((string.IsNullOrWhiteSpace(_options.Certificate.CACertificatePath) || string.IsNullOrWhiteSpace(_options.Certificate.PrivateKeyCertificatePath)) &&
                        (string.IsNullOrWhiteSpace(_options.Certificate.CACertificateContent) || string.IsNullOrWhiteSpace(_options.Certificate.PrivateKeyCertificateContent))))
                    {
                        throw new Exception("Please provide certificate information in SDKOptions.");
                    }
                    if (isAZPlatform)
                    {
                        _brokerInstance = new MqttBroker(SyncData.Protocol.H, SyncData.Protocol.P, SyncData.Protocol.Id, _options.Certificate, SyncData.Protocol.Un);
                    }
                    else
                    {
                        _brokerInstance = new MqttBroker(SyncData.Protocol.H, SyncData.Protocol.P, SyncData.Protocol.Id, _options.Certificate);
                    }

                    break;
                case AuthType.BootstrapCertificate:
                    if (_options == null || _options.Certificate == null)
                    {
                        throw new Exception("Please provide certificate information in SDKOptions.");
                    }
                    break;

                default:
                    break;
            }

            _connectionStatusCallback = connectionStatusCallback;
            _brokerInstance.Connect(ConnectedStatusHandler, DisonnectedStatusHandler);
            if (isAZPlatform)
            {
                _brokerInstance.Receive(SyncData.Protocol.Topics.C2d, "$iothub/twin/res/#", "$iothub/twin/PATCH/properties/desired/#", C2dMessages);
                _brokerInstance.SendTwinData("$iothub/twin/GET/?$rid=0", SendExecptionCallback);
            }
            else
            {
                _brokerInstance.Receive(SyncData.Protocol.Topics.C2d, SyncData.Protocol.Topics.Set.Sub, C2dMessages);
                _brokerInstance.SendShadowData(SyncData.Protocol.Topics.Set.Sub, SendExecptionCallback);
            }
        }

        private async Task ConnectedStatusHandler(MqttClientConnectedEventArgs arg)
        {
            _isConnected = true;
            _connectionStatusCallback.Invoke(true.ToString());
            Console.WriteLine("### Device Connected ###");
            SendCollectedDeviceData();
        }

        private async Task DisonnectedStatusHandler(MqttClientDisconnectedEventArgs arg)
        {
            _isConnected = false;
            _connectionStatusCallback.Invoke(false.ToString());
            Console.WriteLine("### Device DisConnected ###");
        }

        internal void ReConnect()
        {
            SDKCommon.Console_WriteLine("ReConnect Called");
            if (!_isConnected && SDKCommon.IsInternetConnected)
            {
                SDKCommon.Console_WriteLine("Device not connected. Internet available. Connecting.");
                lock (this)
                {
                    if (_isConnected)
                    {
                        SDKCommon.Console_WriteLine("Device not connected. Internet available. Connecting. Duplicate Call. Its connected!");
                        return;
                    }

                    SDKCommon.Console_WriteLine("ReConnect Called - Disconnecting");
                    _brokerInstance.Disconnect();
                    SDKCommon.Console_WriteLine("ReConnect Called - Calling Sync.Reload");
                    Sync.ReLoad(this, _discoveryCommon, platform);
                    SDKCommon.Console_WriteLine("ReConnect Called - Connecting");
                    Connect(_connectionStatusCallback);
                    SDKCommon.Console_WriteLine("ReConnect Called - ReConnect Done!");
                }
            }
            else
                SDKCommon.Console_WriteLine("ReConnect Called - Device Connected! Skipping ReConnect.");
        }

        internal void Disconnect(bool noRetry = false)
        {
            if (_brokerInstance != null)
                _brokerInstance.Disconnect(noRetry);
            LastDataSentTime = null;
        }

        public void Dispose()
        {
            IsDeviceBarred = true;
            LastDataSentTime = null;
            SendDeviceConnectionCommand(false, CommandType.DEVICE_CONNECTION_STATUS);
            if (_brokerInstance != null)
            {
                _brokerInstance.Disconnect(true);
                _brokerInstance.Dispose();
            }
        }

        public void SkipDataValidation(bool skipDV)
        {
            _skipValidation = skipDV;
        }

        internal virtual BaseTelemetryDataModel PrepareMessage(List<DeviceTelemetryModel> devices)
        {
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
                                if (attrs != null && attrs.Count() > 0)
                                {
                                    var v = (from attr in attrs
                                             where attr.cattr.Tg == d2.Tg && attr.parent == root.Name
                                             select attr.cattr).FirstOrDefault();

                                    if (c.HasValues && !string.IsNullOrWhiteSpace(c.Value.ToString()))
                                    {
                                        if (v != null && ValidateValueWithDataType(c, v.Dt, v.Dv, out _))
                                        {
                                            hasReporting = true;
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

        #region Data validation methods
        public bool ValidateValueWithDataType(JProperty attributeDetails, int dataType, string dataValidation, out dynamic attrValue)
        {
            bool response = false;
            attrValue = null;
            //string format = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
            try
            {
                string[] dvRange = null;
                if (!string.IsNullOrWhiteSpace(dataValidation) && !_skipValidation)
                    dvRange = dataValidation.Split(',').Select(a => a.Trim()).ToArray();

                if (attributeDetails.Value == null)
                    return false;

                switch ((DataTypes)dataType)
                {
                    case DataTypes.Integer:
                        var decimalValInt = ConvertDecimalToInt(attributeDetails.Value);
                        if (decimalValInt != null)
                        {
                            response = Int32.TryParse(Convert.ToString(decimalValInt), out Int32 convertedInt);
                            if (response)
                            {
                                attrValue = convertedInt;
                                response = ValidateNumericData(dvRange, DataTypes.Integer, convertedInt);
                            }
                        }
                        break;

                    case DataTypes.Long:
                        var decimalValLng = ConvertDecimalToInt(attributeDetails.Value);
                        if (decimalValLng != null)
                        {
                            response = long.TryParse(Convert.ToString(decimalValLng), out long convertedLng);
                            if (response)
                            {
                                attrValue = convertedLng;
                                response = ValidateNumericData(dvRange, DataTypes.Long, convertedLng);
                            }
                        }
                        break;

                    case DataTypes.Decimal:
                        response = decimal.TryParse(attributeDetails.Value.ToString(), out decimal convertedDcml);
                        if (response)
                        {
                            attrValue = convertedDcml;
                            response = ValidateNumericData(dvRange, DataTypes.Long, convertedDcml);
                        }
                        break;

                    case DataTypes.Time:
                        attributeDetails.Value = attributeDetails.Value.ToString(Newtonsoft.Json.Formatting.None).Replace("\"", "");
                        response = DateTime.TryParseExact(attributeDetails.Value.ToString(), "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime convertedTime);
                        if (response)
                        {
                            attrValue = attributeDetails.Value;
                            response = ValidateDateTimeData(dvRange, DataTypes.Time, convertedTime);
                        }
                        break;

                    case DataTypes.Date:
                        attributeDetails.Value = attributeDetails.Value.ToString(Newtonsoft.Json.Formatting.None).Replace("\"", "");
                        response = DateTime.TryParseExact(attributeDetails.Value.ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime convertedDate);
                        if (response)
                        {
                            attrValue = attributeDetails.Value;
                            response = ValidateDateTimeData(dvRange, DataTypes.Date, convertedDate);
                        }
                        break;

                    case DataTypes.DateTime:
                        attributeDetails.Value = attributeDetails.Value.ToString(Newtonsoft.Json.Formatting.None).Replace("\"", "");
                        response = DateTime.TryParse(attributeDetails.Value.ToString(), out DateTime convertedDT);//ORIGINAL CODE
                        if (response)
                        {
                            attrValue = attributeDetails.Value;
                            response = ValidateDateTimeData(dvRange, DataTypes.DateTime, convertedDT);
                        }
                        break;

                    case DataTypes.BIT:
                        if (attributeDetails.Value.ToString().Equals("0") || attributeDetails.Value.ToString().Equals("1"))
                        {
                            var bitVal = Convert.ToBoolean(Convert.ToInt16(attributeDetails.Value.ToString()));
                            response = ValidateBitOrBoolData(dvRange, DataTypes.BIT, bitVal);// bool.TryParse(bitVal.ToString(), out _);
                        }
                        break;

                    case DataTypes.Boolean:
                        response = ValidateBitOrBoolData(dvRange, DataTypes.BIT, attributeDetails.Value); //bool.TryParse(attributeDetails.Value.ToString(), out bool convertedBool);
                        attrValue = response && !string.IsNullOrWhiteSpace(Convert.ToString(attributeDetails.Value)) ? Boolean.Parse(Convert.ToString(attributeDetails.Value)) : attributeDetails.Value;
                        break;

                    case DataTypes.LatLong:
                        dynamic tempLatLong = attributeDetails.Value;
                        if (tempLatLong != null && (!tempLatLong.GetType().IsArray || tempLatLong.GetType().FullName != typeof(JArray).FullName))
                            tempLatLong = JsonConvert.DeserializeObject(Convert.ToString(attributeDetails.Value));

                        if (tempLatLong is JArray)
                        {
                            List<decimal> latLngVal = new List<decimal>();
                            foreach (var item in (tempLatLong as JArray))
                            {
                                if (!decimal.TryParse(item.ToString(), out decimal convertedVal))
                                {
                                    response = false;
                                }
                                else
                                {
                                    latLngVal.Add(convertedVal);
                                    response = true;
                                }
                            }

                            //NOTE: not setting actual value because of faulty data need to return back
                            if (response)
                                attrValue = latLngVal.ToArray();
                        }
                        break;

                    case DataTypes.String:
                        attrValue = Convert.ToString(attributeDetails.Value);
                        response = ValidateStringData(dvRange, DataTypes.String, attrValue);
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in IsValidData : {ex.Message} for Attr : {attributeDetails}");
            }

            if (!response || attrValue == null)
                attrValue = attributeDetails.Value;

            return response;
        }

        private bool ValidateNumericData(string[] dvRange, DataTypes dataType, dynamic attrValue)
        {
            if (dvRange == null || dvRange.Length == 0)
                return true;

            if (!double.TryParse(Convert.ToString(attrValue), out double dValue))
                return false;

            List<double> singleItem = new List<double>();
            List<string> rangeItem = new List<string>();

            foreach (var item in dvRange)
            {
                if (item.LastIndexOf("to") > 0)
                    rangeItem.Add(item.Trim());
                else
                    singleItem.Add(Convert.ToDouble(item.Trim()));
            }

            string[] dashArray;
            foreach (var item in rangeItem)
            {
                dashArray = item.Split("to", StringSplitOptions.RemoveEmptyEntries);
                if (dValue >= Convert.ToDouble(dashArray[0].Trim()) && dValue <= Convert.ToDouble(dashArray[1].Trim()))
                    return true;
            }

            return singleItem.Count(s => s == dValue) > 0;
        }

        private bool ValidateStringData(string[] dvRange, DataTypes dataType, dynamic attrValue)
        {
            if (dvRange == null || dvRange.Length == 0)
                return true;

            return !string.IsNullOrWhiteSpace(attrValue) ? Array.FindAll(dvRange, s => s.Trim().Equals(attrValue)).Count() > 0 : false;
        }

        private bool ValidateDateTimeData(string[] dvRange, DataTypes dataType, DateTime attrValue)
        {
            bool response = false;
            if (dvRange == null || dvRange.Length == 0)
                return true;

            List<DateTime> singleItem = new List<DateTime>();
            List<string> rangeItem = new List<string>();

            foreach (var item in dvRange)
            {
                if (item.LastIndexOf("to") > 0)
                    rangeItem.Add(item.Trim());
                else
                    singleItem.Add(Convert.ToDateTime(item.Trim(), CultureInfo.CurrentCulture));
            }

            string[] dashArray;
            foreach (var item in rangeItem)
            {
                dashArray = item.Split("to", StringSplitOptions.RemoveEmptyEntries);

                if (dataType == DataTypes.Date)
                {
                    response = attrValue.Date >= Convert.ToDateTime(dashArray[0].Trim()).Date && attrValue.Date <= Convert.ToDateTime(dashArray[1].Trim()).Date;
                }
                else if (dataType == DataTypes.Time)
                {
                    response = attrValue.TimeOfDay >= Convert.ToDateTime(dashArray[0].Trim()).TimeOfDay && attrValue.TimeOfDay <= Convert.ToDateTime(dashArray[1].Trim()).TimeOfDay;
                }
                else if (dataType == DataTypes.DateTime)
                {
                    response = attrValue >= Convert.ToDateTime(dashArray[0].Trim(), CultureInfo.CurrentCulture) && attrValue <= Convert.ToDateTime(dashArray[1].Trim(), CultureInfo.CurrentCulture);
                }
            }

            if (!response)
            {
                if (dataType == DataTypes.Date)
                {
                    return singleItem.Count(s => s.Date == attrValue.Date) > 0;
                }
                else if (dataType == DataTypes.Time)
                {
                    return singleItem.Count(s => s.TimeOfDay == attrValue.TimeOfDay) > 0;
                }
                else if (dataType == DataTypes.DateTime)
                {
                    return singleItem.Count(s => s == attrValue) > 0;
                }
            }

            return response;
        }

        private bool ValidateBitOrBoolData(string[] dvRange, DataTypes dataType, dynamic attrValue)
        {
            if (string.IsNullOrWhiteSpace(Convert.ToString(attrValue)))
                return true;

            if (dvRange == null || dvRange.Length == 0)
                return true;

            if (!bool.TryParse(Convert.ToString(attrValue), out bool bitValue))
                return false;

            List<bool> singleItem = new List<bool>();
            List<string> rangeItem = new List<string>();

            foreach (var item in dvRange)
            {
                if (item.LastIndexOf("to") > 0)
                    rangeItem.Add(item.Trim());
                else
                    singleItem.Add(Convert.ToBoolean(Convert.ToInt16(item.Trim())));
            }

            string[] dashArray;
            foreach (var item in rangeItem)
            {
                dashArray = item.Split("to", StringSplitOptions.RemoveEmptyEntries);
                if (bitValue == Convert.ToBoolean(Convert.ToInt16(dashArray[0].Trim())) || bitValue == Convert.ToBoolean(Convert.ToInt16(dashArray[1].Trim())))
                    return true;
            }

            return singleItem.Count(s => s == bitValue) > 0;
        }

        private dynamic ConvertDecimalToInt(dynamic attrValue)
        {
            if (decimal.TryParse(attrValue.ToString(), out decimal convertedVal))
            {
                return Math.Round(decimal.Parse(convertedVal.ToString()), 0, MidpointRounding.AwayFromZero);
            }
            return null;
        }
        #endregion

        #region Internal methods : D2C
        internal virtual void SendData(string data)
        {

        }

        internal protected void SendD2C(object model, KeyValuePair<string, string> mtWithValue)
        {
            string mjson = string.Empty;
            try
            {
                GetTopicByMTValue(mtWithValue, ref topic);
                if (model is string)
                    mjson = model.ToString();
                else
                    mjson = JsonConvert.SerializeObject(model);

                bool isInternetConnected = SDKCommon.IsInternetConnected;
                if (_isConnected && isInternetConnected)
                {
                    _brokerInstance.Send(topic, mjson, SendExecptionCallback).Wait();
                    //if (!_isInternetAvailableLastTime) ConnectionStatusHandler(ConnectionStatus.Connected, ConnectionStatusChangeReason.Connection_Ok);
                    _isInternetAvailableLastTime = true;

                }
                else
                {

                    var offlineData = JObject.Parse(mjson);
                    if (offlineData["od"] == null)
                        offlineData.Add("od", 1);
                    else
                        offlineData["od"] = 1;

                    if (!isInternetConnected)
                    {
                        SDKCommon.Console_WriteLine("Connection Status:Disabled... Internet not found... sleepping...");
                        _isConnected = false;
                        if (_isInternetAvailableLastTime)
                        {
                            SDKCommon.CallOnInternet(ReConnect);
                        }
                    }

                    LocalStorageManager.Instance.AddDeviceData(_uniqueId, JsonConvert.SerializeObject(offlineData), _options.OfflineStorage);
                    if (_isConnected && !isInternetConnected)
                        _isInternetAvailableLastTime = false;
                }
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null && ex.InnerException is UnauthorizedAccessException)
                {
                    ReConnect();
                    if (_isConnected)
                        SendD2C(model, mtWithValue);
                    else
                        LocalStorageManager.Instance.AddDeviceData(_uniqueId, mjson, _options.OfflineStorage);
                }
                else
                {
                    //keep data and send when device is available
                    LocalStorageManager.Instance.AddDeviceData(_uniqueId, mjson, _options.OfflineStorage);
                }
            }
        }

        //internal async Task<bool> UploadFile(string filePath)
        //{
        //    return await (_brokerInstance as IoTHubBroker).UploadFile(filePath);
        //}

        internal bool UpdateShadow(string key, object value, string version)
        {
            var shadowTopic = SyncData.Protocol.Topics.Set.Pub;
            if (isAZPlatform)
            {
                shadowTopic = $"$iothub/twin/PATCH/properties/reported/?$rid={version}";
            }
            if (_brokerInstance != null)
            {
                return (_brokerInstance as MqttBroker).UpdateShadow(key, value, shadowTopic, SendExecptionCallback);
            }
            return false;
        }

        internal bool GetAllShadow()
        {
            var shadowTopic = SyncData.Protocol.Topics.Set.PubForAll;
            if (_brokerInstance != null)
            {
                return (_brokerInstance as MqttBroker).GetAllShadows(shadowTopic, SendExecptionCallback);
            }
            return false;
        }

        internal async Task RegisterDirectMethod(string methodName, Func<MqttApplicationMessageReceivedEventArgs, Task> methodCallBack)
        {
            (_brokerInstance as MqttBroker).RegisterDirectMethod(methodName, methodCallBack);
        }

        #endregion

        #region Internal methods : C2D receive
        internal void SendExecptionCallback(string mjson)
        {
            if (_isConnected)
            {
                SDKCommon.Console_WriteLine("Send Exception callback: Device Connected... Resending... ");
                //SendD2C(mjson, mtWithValue);
            }
            else
            {
                LocalStorageManager.Instance.AddDeviceData(_uniqueId, mjson, _options.OfflineStorage);
            }
        }

        internal void SendDeviceConnectionCommand(bool isConnected, CommandType commandType)
        {
            var deviceStatusModel = new
            {
                guid = "",
                uniqueId = UniqueId,
                command = isConnected,
                ack = false,
                ackId = "",
                cmdType = commandType
            };
            _deviceCallBack(JsonConvert.SerializeObject(deviceStatusModel));
        }

        protected async Task C2dMessages(MqttApplicationMessageReceivedEventArgs receivedEventArgs)
        {
            string message = Encoding.UTF8.GetString(receivedEventArgs.ApplicationMessage.Payload);
            if (receivedEventArgs.ApplicationMessage.Topic == SyncData.Protocol.Topics.Set.Sub || receivedEventArgs.ApplicationMessage.Topic.Contains("twin/PATCH/properties/desired"))
            {
                var output = JsonConvert.DeserializeObject<dynamic>(message);
                if (((JObject)output).Count > 0)
                {
                    await ShadowUpdateCallback(message);
                }
                return;
            }
            try
            {
                var command = JsonConvert.DeserializeObject<BaseCommand>(message);
                if (command != null && command.V == 2.1)
                {
                    SDKCommon.Console_WriteLine($"Received Command ::: {message}");
                    ProcessCommand((CommandType)command.Ct, message, _deviceCallBack);
                    return;
                }
            }
            catch
            {
                SDKCommon.Console_WriteLine($"{message} - Invalid command received!");
                return;
            }

            try
            {
                var diMessages = JsonConvert.DeserializeObject<BaseIdentityCommonModel>(message);
                if (diMessages != null && diMessages.Data != null)
                {
                    if (diMessages.Data.Ec == 0)
                    {
                        SDKCommon.Console_WriteLine($"Received Command ::: {message}");
                        ProcessIdentityMessages(message, diMessages.Data.Ct);
                    }
                    else
                    {
                        throw new Exception($"Error while receiving identity data : {SDKCommon.GetEnumDescription((ErrorCodes)diMessages.Data.Ec)}");
                    }
                }
            }
            catch (Exception ex)
            {
                SDKCommon.Console_WriteLine($"{message} - Invalid command received!");
                return;
            }
        }

        protected async Task CloudMessages(string message, Func<string, Task> deviceCallBack)
        {
            CommandFirmwareModel command;

            try
            {
                command = JsonConvert.DeserializeObject<CommandFirmwareModel>(message);
            }
            catch
            {
                SDKCommon.Console_WriteLine($"{message} - Invalid command received!");
                return;
            }

            if ((command.Ct == (int)CommandType.DEVICE_COMMAND)
                || (command.Ct == (int)CommandType.OTA_COMMAND)
                || (command.Ct == (int)CommandType.MODULE_COMMAND)
                || (command.Ct == (int)CommandType.DEVICE_CONNECTION_STATUS))
            {
                await this._deviceCallBack?.Invoke(JsonConvert.SerializeObject(command.Data));
            }
            else
                ProcessCommand((CommandType)command.Ct, message, _deviceCallBack);
        }

        protected virtual void ProcessCommand(CommandType commandType, string commandMsg, Func<string, Task> deviceCallBack)
        {
            SDKCommon.Console_WriteLine($"Command Received: {commandType}");

            switch (commandType)
            {
                case CommandType.DEVICE_COMMAND:
                    DeviceCommandCallback?.Invoke(commandMsg);
                    break;
                case CommandType.OTA_COMMAND:
                    OtaUpdateCommandCallback?.Invoke(commandMsg);
                    break;
                case CommandType.MODULE_COMMAND:
                    ModuleCommandCallback?.Invoke(commandMsg);
                    break;
                case CommandType.REFRESH_ATTRIBUTE:
                    AttributeChangedCallback?.Invoke(commandMsg);
                    this.SyncAttributes();
                    break;
                case CommandType.REFRESH_TWIN:
                    ShadowChangedCallback?.Invoke(commandMsg);
                    this.SyncSettings();
                    break;
                case CommandType.REFRESH_EDGE_RULE:
                    this.SyncRules();
                    break;
                case CommandType.REFRESH_CHILD_DEVICE:
                    DeviceChangedCallback?.Invoke(commandMsg);
                    this.SyncChildDevices();
                    break;
                case CommandType.DATA_FREQUENCY_CHANGE:
                    SyncData.Meta.DataFreq = JsonConvert.DeserializeObject<DeviceFrequencyCommand>(commandMsg).Df;
                    break;
                case CommandType.DEVICE_DELETED:
                    IsDeviceBarred = true;
                    Dispose();
                    SendDeviceConnectionCommand(false, CommandType.DEVICE_DELETED);
                    break;
                case CommandType.DEVICE_DISABLED:
                    IsDeviceBarred = true;
                    Dispose();
                    SendDeviceConnectionCommand(false, CommandType.DEVICE_DISABLED);
                    break;
                case CommandType.DEVICE_RELEASED:
                    IsDeviceBarred = true;
                    Dispose();
                    SendDeviceConnectionCommand(false, CommandType.DEVICE_RELEASED);
                    break;
                case CommandType.STOP_OPERATION:
                    IsDeviceBarred = true;
                    //options.ClearOfflineStorage();
                    Disconnect(true);
                    SendDeviceConnectionCommand(false, CommandType.STOP_OPERATION);
                    break;
                case CommandType.START_HEARTBEAT:
                    HeartbeatCommand(commandMsg, true);
                    break;
                case CommandType.STOP_HEARTBEAT:
                    HeartbeatCommand(commandMsg, false);
                    break;
                case CommandType.DEVICE_LOG_COMMAND:
                    //TODO:
                    break;
                case CommandType.SKIP_DATA_VALIDATION_COMMAND:
                    //_skipValidation = JsonConvert.DeserializeObject<SkipValidationCommand>(commandMsg).SkipValidation;
                    _skipValidation = true;
                    break;
                case CommandType.SEND_SDK_LOG_COMMAND:
                    //TODO:
                    break;
                default:
                    break;
            }

            //else if (command.CommandType == SDKCommon.GetEnumDescription(CommandType.PasswordChanged))
            //{
            //    Sync.Protocol(this);
            //    ReConnect();
            //}
        }

        protected virtual void ProcessIdentityMessages(string message, int mt)
        {
            switch ((MessageTypes)mt)
            {
                case MessageTypes.INFO:
                    var baseSyncInfo = JsonConvert.DeserializeObject<SyncResponse>(message);
                    //SyncData = baseSyncInfo.Data;
                    break;

                case MessageTypes.ATTRS:
                    var attributeInfo = JsonConvert.DeserializeObject<AttributesModel>(message);
                    AttributeList = attributeInfo.Data.AttrList;

                    if (SyncHasInfo.D)
                    {
                        if (hasChildDevice)
                        {
                            hasChildDevice = false;
                            GetAttributeCallback?.Invoke(JsonConvert.SerializeObject(AttributeList));
                        }
                    }
                    else
                    {
                        GetAttributeCallback?.Invoke(JsonConvert.SerializeObject(AttributeList));

                        // Following code will impact in simulator case
                        if (GetAttributeCallback?.Target != null)
                        {
                            GetAttributeCallback = null;
                        }
                    }
                    break;

                case MessageTypes.SHADOW:
                    var settingInfo = JsonConvert.DeserializeObject<SettingsModel>(message);
                    SettingList = settingInfo.Data.SettingList;

                    GetShadowCallback?.Invoke(JsonConvert.SerializeObject(SettingList));
                    break;

                case MessageTypes.EDGE_RULE:
                    var ruleInfo = JsonConvert.DeserializeObject<RulesModel>(message);
                    RuleList = ruleInfo.Data.RuleList;

                    GetRuleCallback?.Invoke(JsonConvert.SerializeObject(RuleList));
                    break;

                case MessageTypes.DEVICES:
                    if (DeviceList == null)
                        DeviceList = new List<DeviceDetails>();

                    DeviceList.Clear();
                    DeviceList.Add(new DeviceDetails()
                    {
                        Id = _uniqueId,
                        Tg = SyncData?.Meta?.Gtw?.Tag
                    });

                    var deviceInfo = JsonConvert.DeserializeObject<DevicesModel>(message);
                    DeviceList.AddRange(deviceInfo?.Data?.DeviceList);
                    hasChildDevice = true;
                    if (AttributeList != null && AttributeList.Count > 0)
                    {
                        hasChildDevice = false;
                        GetAttributeCallback?.Invoke(JsonConvert.SerializeObject(AttributeList));
                    }
                    GetChildDeviceCallback?.Invoke(message);
                    break;

                case MessageTypes.PENDING_OTA:
                    var otaUpdateInfo = JsonConvert.DeserializeObject<OTAUpdateModel>(message);
                    OtaUpdate = otaUpdateInfo.Data.Ota;
                    break;

                case MessageTypes.ALL_DATA:
                    var allSyncInfo = JsonConvert.DeserializeObject<SyncResponse>(message);
                    SyncData = allSyncInfo.Data;
                    //TODO: pending MessageTypes.ALL_DATA
                    break;

                case MessageTypes.CREATE_CHILD_DEVICE:
                    var createChildResponse = JsonConvert.DeserializeObject<BaseIdentityCommonModel>(message);
                    CreateChildDeviceCallback?.Invoke(message);
                    break;

                case MessageTypes.DELETE_CHILD_DEVICE:
                    var deleteChildResponse = JsonConvert.DeserializeObject<BaseIdentityCommonModel>(message);
                    DeleteChildDeviceCallback?.Invoke(message);
                    break;

                default:
                    break;
            }

        }
        #endregion

        #region Private methods : connection status & Shadow callbacks internals
        private string GenerateSasToken(string resourceUri, string key, string policyName, int expiryInSeconds = 31536000)
        {
            TimeSpan fromEpochStart = DateTime.UtcNow - new DateTime(1970, 1, 1);
            string expiry = Convert.ToString((int)fromEpochStart.TotalSeconds + expiryInSeconds);

            string stringToSign = WebUtility.UrlEncode(resourceUri) + "\n" + expiry;

            HMACSHA256 hmac = new HMACSHA256(Convert.FromBase64String(key));
            string signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

            string token = string.Format(CultureInfo.InvariantCulture, "SharedAccessSignature sr={0}&sig={1}&se={2}", WebUtility.UrlEncode(resourceUri), WebUtility.UrlEncode(signature), expiry);

            if (!string.IsNullOrEmpty(policyName))
            {
                token += "&skn=" + policyName;
            }

            return token;
        }

        //private void ConnectionStatusHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
        //{
        //    SDKCommon.WriteConnStatus(_uniqueId, status.ToString());
        //    SDKCommon.Console_WriteLine($"Device:{_uniqueId} Connection status Changed : {status.ToString()} with reason : {reason.ToString()}");
        //    if (IsDeviceBarred)
        //    {
        //        SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred");
        //        if (_isConnected)
        //        {
        //            SDKCommon.Console_WriteLine($"Device:{_uniqueId} is IsDeviceBarred - Disconnecting again...");
        //            this.Dispose();
        //            SDKCommon.Console_WriteLine($"Device:{_uniqueId} is IsDeviceBarred - Disconnecting done.");
        //        }
        //        return;
        //    }

        //    _isConnected = false;
        //    _isInternetAvailableLastTime = false;

        //    if (status == ConnectionStatus.Connected)
        //    {
        //        _isConnected = true;
        //        _isInternetAvailableLastTime = true;
        //        //SendCollectedDeviceData();
        //        SendDeviceConnectionCommand(true, CommandType.DEVICE_CONNECTION_STATUS);
        //    }
        //    else if (status == ConnectionStatus.Disabled || status == ConnectionStatus.Disconnected
        //        || reason == ConnectionStatusChangeReason.Retry_Expired || reason == ConnectionStatusChangeReason.Communication_Error)
        //    {
        //        if (SDKCommon.IsInternetConnected)
        //        {
        //            SDKCommon.Console_WriteLine("Connection Status:Disabled... Internet connection found... retrying...");
        //        }
        //        else
        //        {
        //            SDKCommon.Console_WriteLine("Connection Status:Disabled... Internet not found... sleepping...");
        //            SDKCommon.CallOnInternet(ReConnect);
        //        }
        //        SendDeviceConnectionCommand(false, CommandType.DEVICE_CONNECTION_STATUS);
        //    }

        //    if (_connectionStatusCallback != null)
        //        _connectionStatusCallback(status.ToString());
        //}

        internal protected Task ShadowUpdateCallback(string shadowProperties)
        {
            try
            {
                JObject shadowJson = new JObject();
                var shadowProperty = JsonConvert.DeserializeObject<Dictionary<string, string>>(shadowProperties);
                if (shadowProperty != null && shadowProperty.Count > 0)
                {
                    foreach (var item in shadowProperty)
                    {
                        shadowJson.Add(item.Key, item.Value);
                    }

                }
                if (_shadowUpdateCallBack != null)
                    _shadowUpdateCallBack(shadowJson.ToString());
            }
            catch (Exception ex)
            {
                SDKCommon.Console_WriteLine($"Invalid Shadow Update received. Ex:{ex.Message}");
            }
            return Task.CompletedTask;
        }

        #endregion

        #region Helper Methods
        internal List<Dictionary<string, dynamic>> CreateDataObjectDynamic(bool showConsole = true)
        {
            List<Dictionary<string, dynamic>> list = new List<Dictionary<string, dynamic>>();
            if (DeviceList == null || DeviceList.Count == 0 || AttributeList == null || AttributeList.Count == 0)
            {
                Console.WriteLine("Syncing device/attribute details. Please try again in some time.");
                return null;
            }

            foreach (var device in DeviceList)
            {
                if (showConsole) Console.WriteLine("Gateway Name:" + device.Id);
                Dictionary<string, dynamic> root = new Dictionary<string, dynamic>();
                root.Add("id", device.Id);
                root.Add("tg", device.Tg);
                root.Add("dt", DateTime.UtcNow);

                string input = string.Empty;
                Dictionary<string, dynamic> dataObject = new Dictionary<string, dynamic>();
                int i = 0;
                foreach (var item in AttributeList)
                {
                    if (string.IsNullOrEmpty(item.P))
                    {
                        foreach (var child in item.D)
                        {
                            if (child.Tg == device.Tg)
                            {
                                if (showConsole) Console.Write("Enter " + child.Ln + " : ");
                                if (showConsole)
                                    input = Console.ReadLine();
                                else
                                    input = (i++).ToString();

                                ValidateValueWithDataType(new JProperty(child.Ln, input), child.Dt, child.Dv, out dynamic attrValue);
                                dataObject.Add(child.Ln, attrValue);
                            }
                        }
                    }
                    else
                    {
                        Dictionary<string, dynamic> childObject = null;
                        if (item.D.Where(a => a.Tg == device.Tg).Count() > 0)
                        {
                            if (showConsole) SDKCommon.Console_WriteLine("Enter " + item.P + " :");

                            foreach (var child in item.D)
                            {
                                if (child.Tg == device.Tg)
                                {
                                    if (childObject == null)
                                        childObject = new Dictionary<string, dynamic>();
                                    if (showConsole) Console.Write("\tEnter " + item.P + "." + child.Ln + " : ");
                                    if (showConsole)
                                        input = Console.ReadLine();
                                    else
                                        input = (i++).ToString();

                                    ValidateValueWithDataType(new JProperty(child.Ln, input), child.Dt, child.Dv, out dynamic attrValue);
                                    childObject.Add(child.Ln, attrValue);
                                }
                            }
                            if (childObject != null)
                                dataObject.Add(item.P, childObject);
                        }
                    }
                }
                root.Add("d", dataObject);
                list.Add(root);
            }

            return list;
        }
        //SDK Helper Methods:
        internal void HeartbeatCommand(string commandMsg, bool isStart)
        {
            var heartBeatCmd = JsonConvert.DeserializeObject<HeartBeatCommand>(commandMsg);
            if (isStart)
            {
                //TODO: start/reset timer of heartbeat message send.
                heartBeatCmd.Freq = Convert.ToInt32(heartBeatCmd.Freq);
            }
            else
            {
                //TODO: stop timer of heartbeat message send.
            }
        }

        internal void DeviceDeleteCommand()
        {
        }

        internal void OnSendSDKLogCommand()
        {
        }

        internal void GetTopicByMTValue(KeyValuePair<string, string> mtWithValue, ref string topic)
        {
            if (mtWithValue.Key == "mt")
            {
                switch (mtWithValue.Value)
                {
                    case "0":
                        topic = SyncData.Protocol.Topics.Rpt;
                        break;

                    case "1":
                        topic = SyncData.Protocol.Topics.Erpt;
                        break;

                    case "2":
                        topic = SyncData.Protocol.Topics.Erm;
                        break;

                    case "3":
                        topic = SyncData.Protocol.Topics.Flt;
                        break;

                    case "4":
                        topic = SyncData.Protocol.Topics.OfflineData;
                        break;

                    case "5":
                        topic = SyncData.Protocol.Topics.HeartBeat;
                        break;

                    case "6":
                        topic = SyncData.Protocol.Topics.Ack;
                        break;

                    case "7":
                        topic = SyncData.Protocol.Topics.DeviceLogs;
                        break;
                }
            }
            else if (mtWithValue.Key == "di")
            {
                topic = (!string.IsNullOrEmpty(SyncData?.Protocol?.Topics?.Di) ? SyncData.Protocol.Topics.Di : topic);
            }
        }
        #endregion

        private void SendCollectedDeviceData()
        {
            List<string> deviceData;
            var stopwatch = new Stopwatch();
            DateTime? stopwatchTime = null;
            do
            {
                if (!(_isConnected && SDKCommon.IsInternetConnected)) break;
                deviceData = LocalStorageManager.Instance.GetDeviceData(_uniqueId, this._options.OfflineStorage);
                if (deviceData != null && deviceData.Count > 0)
                {
                    stopwatch.Start();
                    int counter = 0;
                    foreach (var item in deviceData)
                    {
                        var stopwatchSeconds = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds).TotalSeconds;
                        if (stopwatchSeconds > 1 && Math.Ceiling(Convert.ToDecimal(stopwatchSeconds / 10)) % 2 == 0)
                        {
                            if (stopwatchTime != null)
                            {
                                Console.WriteLine($"Offline data sent during {stopwatchTime.Value.ToUniversalTime().ToString("o")} - {DateTime.UtcNow.ToUniversalTime().ToString("o")} : {counter}/{deviceData.Count}");
                            }
                            Thread.Sleep(10000);
                            stopwatchTime = DateTime.UtcNow;
                        }
                        SendD2C(item, new KeyValuePair<string, string>("mt", ((int)MessageTypes.OFFLINE_DATA).ToString()));
                        //Console.WriteLine("offline datasent : " + item);
                        //Thread.Sleep(2000);
                        counter++;
                    }
                }
                stopwatch.Reset();
            }
            while (deviceData != null);
        }

        internal void SendRegisterDirectMethodResponse(string topic, string json)
        {
            _brokerInstance.SendRegisterMethodResponse(topic, json, SendExecptionCallback);
        }

        public class DeviceTwinCallBackModel
        {
            public object Desired { get; set; }
            public object Reported { get; set; }
            public string UniqueId { get; set; }
        }
    }
}