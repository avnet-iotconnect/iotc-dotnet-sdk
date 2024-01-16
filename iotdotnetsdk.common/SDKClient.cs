using iotcdotnetsdk.common.Internals;

using iotdotnetsdk.common.Interfaces;
using iotdotnetsdk.common.Internals;
using iotdotnetsdk.common.Internals.Devices;
using iotdotnetsdk.common.Models;
using iotdotnetsdk.common.Models.D2C;

using MQTTnet.Client;

using Newtonsoft.Json;

using System;

namespace iotdotnetsdk.common
{
    public class SDKClient : ISDKClient, IDisposable
    {
        static readonly object _lockTimer = new object();
        private BaseGateway _device;
        private string _sId;
        private string _uniqueId;

        #region Callbacks
        private Func<string, Task> _connectionStatusCallback;
        #endregion

        public void Init(string uniqueId, string sId, string platform, SDKOptions sdkOptions, Func<string, Task> callback, Func<string, Task> shadowUpdateCallBack)
        {
            DiscoveryCommon discoveryCommon = new DiscoveryCommon();

            if (!SDKCommon.IsInternetConnected)
                throw new NoInternetException();

            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                SDKCommon.Console_WriteLine("ProcessExit");
                //Console.ReadLine();
            };

            _sId = sId;
            _uniqueId = uniqueId;

            SDKCommon.Console_WriteLine($"Init with CPID:{sId} UniqueId:{uniqueId}");

            discoveryCommon.SetOptions(sdkOptions, sId, uniqueId);

            discoveryCommon.Discover(sId, platform);

            _device = Sync.Init(uniqueId, callback, shadowUpdateCallBack, sdkOptions, discoveryCommon, platform);

            if (_device == null)
            {
                SDKCommon.Console_WriteLine("Error while creating Device!");
                return;
            }
        }

        //NOTE: For Device Authority testing
        public void Init(string cpId, string uniqueId, string sId, string environment, string platform, SDKOptions sdkOptions, Func<string, Task> callback, Func<string, Task> shadowUpdateCallBack)
        {
            DiscoveryCommon discoveryCommon = new DiscoveryCommon();

            if (!SDKCommon.IsInternetConnected)
                throw new NoInternetException();

            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                SDKCommon.Console_WriteLine("ProcessExit");
                //Console.ReadLine();
            };

            _sId = sId;
            _uniqueId = uniqueId;

            SDKCommon.Console_WriteLine($"Init with CPID:{sId} UniqueId:{uniqueId}");

            discoveryCommon.SetOptions(sdkOptions, sId, uniqueId, cpId, environment);

            discoveryCommon.Discover(sId, platform, cpId, environment);

            _device = Sync.Init(uniqueId, callback, shadowUpdateCallBack, sdkOptions, discoveryCommon, platform, cpId);

            if (_device == null)
            {
                SDKCommon.Console_WriteLine("Error while creating Device!");
                return;
            }
        }

        public void Connect(Func<string, Task> successCallback, Func<string, Task> failedCallback, Func<string, Task> connectionStatusCallback)
        {
            try
            {
                if (_device == null)
                {
                    SDKCommon.Console_WriteLine("Error while creating Device!");
                    throw new Exception("Error while creating Device!");
                }

                SDKCommon.Console_WriteLine("Connecting to Broker...");
                _connectionStatusCallback = connectionStatusCallback;
                _device.Connect(ConnectionStatusCallbackInternal);

                GetAllSyncData();

                successCallback("Device connection success.");
            }
            catch (Exception ex)
            {
                failedCallback(ex.Message);
            }
        }
        private void OnAttrChangeCallback(string result)
        {
        }
        public void Disconnect()
        {
            SDKCommon.Console_WriteLine(":: Calling Disconnect ::");
            _device.IsDeviceBarred = true;
            _device.Disconnect();
        }

        public void Dispose()
        {
            if (_device == null)
                return;

            SDKCommon.Console_WriteLine(":: Calling Dispose ::");
            _device.IsDeviceBarred = true;
            _device.Dispose();
        }

        public bool SendData(string json)
        {
            if (_device.IsDeviceBarred)
            {
                SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - SendData not Permitted");
                return false;
            }

            if (_device == null)
            {
                throw new SDKInitializationException("Device not initialized");
            }

            Console.WriteLine(":: TELEMETRY JSON ::");
            Console.WriteLine(json);

            _device.SendData(json);
            return true;
        }

        public void SkipDataValidation(bool skipDV)
        {
            if (_device.IsDeviceBarred)
            {
                SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - SkipDataValidation not Permitted");
                return;
            }

            _device.SkipDataValidation(skipDV);
        }

        public async Task<bool> SendImage(string path)
        {
            //if (_device.IsDeviceBarred)
            //{
            //    SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - SendImage not Permitted");
            //    return false;
            //}

            //if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            //{
            //    SDKCommon.Console_WriteLine(":: Called SendImage :: Error : File not found.");
            //    return false;
            //}
            //SDKCommon.Console_WriteLine(":: Called SendImage ::");
            //return await _device.UploadFile(path);
            return true;
        }

        #region Device identity methods like get attributes, shadow, device, create/delete child devices

        public void GetHelloMessage(Func<string, Task> callback)
        {
            if (_device.IsDeviceBarred)
            {
                SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - GetAttributes not Permitted");
                return;
            }

            //if (callback != null)
            //    _device.GetAttributeCallback = callback;

            _device.SyncHelloMessage();
        }

        public void GetAttributes(Func<string, Task> callback)
        {
            if (_device.IsDeviceBarred)
            {
                SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - GetAttributes not Permitted");
                return;
            }

            if (callback != null)
                _device.GetAttributeCallback = callback;

            _device.SyncAttributes();
        }

        public void GetShadows(Func<string, Task> callback)
        {
            if (_device.IsDeviceBarred)
            {
                SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - GetShadows not Permitted");
                return;
            }

            if (callback != null)
                _device.GetShadowCallback = callback;

            _device.SyncSettings(true);
        }

        public void GetRules(Func<string, Task> callback)
        {
            if (_device.IsDeviceBarred)
            {
                SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - GetShadows not Permitted");
                return;
            }

            if (callback != null)
                _device.GetRuleCallback = callback;

            _device.SyncRules(true);
        }

        public void GetChildDevices(Func<string, Task> callback)
        {
            if (_device.IsDeviceBarred)
            {
                SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - GetChildDevices not Permitted");
                return;
            }

            if (callback != null)
                _device.GetChildDeviceCallback = callback;

            _device.SyncChildDevices(true);
        }

        public void GetPendingOTAUpdates(Func<string, Task> callback)
        {
            if (_device.IsDeviceBarred)
            {
                SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - GetPendingOTAUpdates not Permitted");
                return;
            }

            //if (callback != null)
            //    _device.OtaUpdateCommandCallback = callback;

            _device.SyncPendingOTAUpdate(true);
        }

        public void CreateChildDevice(string childId, string deviceTag, string displayName, Func<string, Task> callback)
        {
            //if (_device.SyncData == null || _device.SyncData.Meta == null || _device.SyncData.Meta.Gtw == null || _device.SyncData.Meta.Gtw.DeviceGuid == Guid.Empty)
            //    callback("Child device can be created for gateway devices only.");

            if (_device.IsDeviceBarred)
            {
                SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - CreateChildDevice not Permitted");
                return;
            }

            if (callback != null)
                _device.CreateChildDeviceCallback = callback;

            var childDeviceDetails = new CreateChildDeviceModel()
            {
                Data = new CreateChildDetails()
                {
                    Dn = displayName,
                    Id = childId,
                    Tg = deviceTag,
                    G = _device?.SyncData?.Meta?.Gtw?.DeviceGuid
                },
                Mt = Enums.MessageTypes.CREATE_CHILD_DEVICE
            };

            _device.SendD2C(childDeviceDetails, new KeyValuePair<string, string>("di", "1"));
        }

        public void DeleteChildDevice(string childId, Func<string, Task> callback)
        {
            if (_device.IsDeviceBarred)
            {
                SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - DeleteChildDevice not Permitted");
                return;
            }

            if (callback != null)
                _device.DeleteChildDeviceCallback = callback;

            var childDeviceDetails = new DeleteChildDeviceModel()
            {
                Data = new DeleteChildDetails()
                {
                    Id = childId
                },
                Mt = Enums.MessageTypes.DELETE_CHILD_DEVICE
            };

            _device.SendD2C(childDeviceDetails, new KeyValuePair<string, string>("di", "1"));
        }

        #endregion

        #region update shadow & Send command ACKs
        public void UpdateShadow(string property, dynamic value, string version, Func<string, Task> callback)
        {
            var response = _device.UpdateShadow(property, value, version);
            callback.Invoke(JsonConvert.SerializeObject(new { status = response, data = string.Empty, message = response ? "Shadow updated successfully." : "Error while updating Shadow." }));
        }

        public void SendAckCmd(Guid ackGuid, int status, string msg)
        {
            if (_device.IsDeviceBarred)
            {
                SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - SendAckCmd not Permitted");
                return;
            }

            if (ackGuid == Guid.Empty)
                return;

            var commandAck = new CommandAckModel()
            {
                Data = new CommandAckDetails()
                {
                    Ack = ackGuid,
                    Msg = msg,
                    St = status,
                    Type = Enums.CommandAckType.DeviceCommand
                },
                Dt = DateTime.UtcNow
            };

            _device.SendD2C(commandAck, new KeyValuePair<string, string>("mt", "6"));
        }

        public void SendAckCmd(Guid ackGuid, int status, string msg, string childId)
        {
            if (_device.IsDeviceBarred)
            {
                SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - SendAckCmd not Permitted");
                return;
            }

            if (ackGuid == Guid.Empty)
                return;

            var commandAck = new CommandAckModel()
            {
                Data = new CommandAckDetails()
                {
                    Ack = ackGuid,
                    Msg = msg,
                    St = status,
                    Type = Enums.CommandAckType.DeviceCommand,
                    Cid = childId
                },
                Dt = DateTime.UtcNow
            };

            _device.SendD2C(commandAck, new KeyValuePair<string, string>("mt", "6"));
        }

        public void SendOTAAckCmd(Guid ackGuid, int status, string msg)
        {
            if (_device.IsDeviceBarred)
            {
                SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - SendOTAAckCmd not Permitted");
                return;
            }

            if (ackGuid == Guid.Empty)
                return;

            var otaUpdateAck = new CommandAckModel()
            {
                Data = new CommandAckDetails()
                {
                    Ack = ackGuid,
                    Msg = msg,
                    St = status,
                    Type = Enums.CommandAckType.OtaUpdate
                },
                Dt = DateTime.UtcNow
            };

            _device.SendD2C(otaUpdateAck, new KeyValuePair<string, string>("mt", "6"));
        }

        public void SendOTAAckCmd(Guid ackGuid, int status, string msg, string childId)
        {
            if (_device.IsDeviceBarred)
            {
                SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - SendOTAAckCmd not Permitted");
                return;
            }

            if (ackGuid == Guid.Empty)
                return;

            var otaUpdateAck = new CommandAckModel()
            {
                Data = new CommandAckDetails()
                {
                    Ack = ackGuid,
                    Msg = msg,
                    St = status,
                    Type = Enums.CommandAckType.OtaUpdate,
                    Cid = childId
                },
                Dt = DateTime.UtcNow
            };

            _device.SendD2C(otaUpdateAck, new KeyValuePair<string, string>("mt", "6"));
        }

        public void SendAckModule(Guid ackGuid, int status, string msg)
        {
            if (_device.IsDeviceBarred)
            {
                SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - SendAckModule not Permitted");
                return;
            }

            if (ackGuid == Guid.Empty)
                return;

            var moduleAck = new CommandAckModel()
            {
                Data = new CommandAckDetails()
                {
                    Ack = ackGuid,
                    Msg = msg,
                    St = status,
                    Type = Enums.CommandAckType.ModuleCommand
                },
                Dt = DateTime.UtcNow
            };

            _device.SendD2C(moduleAck, new KeyValuePair<string, string>("mt", "6"));
        }
        #endregion

        #region Direct method
        public void RegisterDirectMethod(string methodName, Func<MqttApplicationMessageReceivedEventArgs, Task> callback)
        {
            if (_device.IsDeviceBarred)
            {
                SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - RegisterDirectMethod not Permitted");
                return;
            }

            _device.RegisterDirectMethod(methodName, callback).Wait();
        }
        #endregion

        //Only for TPM based device
        public string OnEndrocementKeyGet(Func<string, Task> callback)
        {
            throw new NotImplementedException();
        }

        #region Callbacks for C2D Commands
        public void OnAttrChangeCommand(Func<string, Task> callback)
        {
            if (_device.IsDeviceBarred)
            {
                SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - OnAttrChangeCommand not Permitted");
                return;
            }

            if (callback != null)
                _device.AttributeChangedCallback = callback;

            SDKCommon.Console_WriteLine(":: Attribute change command configured ::");
        }

        public void OnShadowChangeCommand(Func<string, Task> callback)
        {
            if (_device.IsDeviceBarred)
            {
                SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - OnShadowChangeCommand not Permitted");
                return;
            }

            if (callback != null)
                _device.ShadowChangedCallback = callback;

            SDKCommon.Console_WriteLine(":: Shadow change command configured ::");
        }

        //public void OnRuleChangeCommand(Func<string, Task> callback)
        //{
        //    SDKCommon.Console_WriteLine(":: Received rule change command ::");
        //    _device.SyncRules();
        //}

        public void OnDeviceChangeCommand(Func<string, Task> callback)
        {
            if (_device.IsDeviceBarred)
            {
                SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - OnDeviceChangeCommand not Permitted");
                return;
            }

            if (callback != null)
                _device.DeviceChangedCallback = callback;

            SDKCommon.Console_WriteLine(":: Child device change command configured ::");
        }

        public void OnDeviceCommand(Func<string, Task> callback)
        {
            if (_device.IsDeviceBarred)
            {
                SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - OnDeviceCommand not Permitted");
                return;
            }

            if (callback != null)
                _device.DeviceCommandCallback = callback;

            SDKCommon.Console_WriteLine(":: Device command configured ::");
        }

        public void OnOTACommand(Func<string, Task> callback)
        {
            if (_device.IsDeviceBarred)
            {
                SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - OnOTACommand not Permitted");
                return;
            }

            if (callback != null)
                _device.OtaUpdateCommandCallback = callback;

            SDKCommon.Console_WriteLine(":: Ota update command configured ::");
        }

        public void OnModuleCommand(Func<string, Task> callback)
        {
            if (_device.IsDeviceBarred)
            {
                SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - OnModuleCommand not Permitted");
                return;
            }

            if (callback != null)
                _device.ModuleCommandCallback = callback;

            SDKCommon.Console_WriteLine(":: Module push command configured ::");
        }
        #endregion

        #region Private Methods
        private async Task ConnectionStatusCallbackInternal(string arg)
        {
            if (_connectionStatusCallback != null)
            {
                await _connectionStatusCallback(arg);
            }
        }

        public bool PrepareAndSendData()
        {
            if (_device.IsDeviceBarred)
            {
                SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - PrepareAndSendData not Permitted");
                return false;
            }

            List<Dictionary<string, dynamic>> json = _device.CreateDataObjectDynamic();
            if (json != null && json.Count > 0)
                SendData(JsonConvert.SerializeObject(json));

            return true;
        }

        private void GetAllSyncData()
        {
            Has dataToBeSync = _device?.SyncData?.Has;
            if (dataToBeSync != null)
            {
                if (dataToBeSync.D)
                    _device.SyncChildDevices();

                if (dataToBeSync.Attr)
                    _device.SyncAttributes();

                if (dataToBeSync.Set)
                    _device.SyncSettings();

                if (dataToBeSync.R)
                    _device.SyncRules();

                if (dataToBeSync.Ota)
                    _device.SyncPendingOTAUpdate();
            }
        }
        #endregion

        #region Edge Device
        public void ProcessEdgeDevice()
        {
            if (_device is EdgeGateway)
            {
                (_device as EdgeGateway).ProcessAttributes();
            }
        }

        #endregion
        public bool SendDirectMethodResponse(string topic, string json)
        {
            if (_device.IsDeviceBarred)
            {
                SDKCommon.Console_WriteLine($"Device:{_uniqueId} is barred - SendData not Permitted");
                return false;
            }

            if (_device == null)
            {
                throw new SDKInitializationException("Device not initialized");
            }

            Console.WriteLine(":: Direct method response JSON ::");
            Console.WriteLine(json);

            _device.SendRegisterDirectMethodResponse(topic, json);
            return true;
        }
    }
}