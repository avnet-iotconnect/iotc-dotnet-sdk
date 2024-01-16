using iotcdotnetsdk.common.Internals;

using iotdotnetsdk.common.Enums;
using iotdotnetsdk.common.Internals.Devices;
using iotdotnetsdk.common.Models;
using iotdotnetsdk.common.Models.D2C;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace iotdotnetsdk.common.Internals
{
    internal static class Sync
    {
        internal static BaseGateway Init(string uniqueId, Func<string, Task> deviceCallBack, Func<string, Task> twinUpdateCallBack, SDKOptions options, DiscoveryCommon discoveryCommon, string platform, string cpId = "")
        {
            SDKCommon.Console_WriteLine($"Initializing Gateway for UniqueId:{uniqueId}");

            SyncResponse syncResponse = GetSyncResponse(uniqueId, discoveryCommon, cpId);
            syncResponse.Platform = platform;

            if (syncResponse != null && syncResponse.Data != null && syncResponse.Data.Ec == ErrorCodes.Ok)
            {
                SDKCommon.Console_WriteLine($"Auth Type:{syncResponse.Data.Meta.At}, CD:{syncResponse.Data.Meta.TemplateCode}, EdgeEnabled:{syncResponse.Data.Meta.Edge}, Protocol:{syncResponse.Data.Protocol}, " +
                    $"Attributes:{syncResponse.Data.Has.Attr}, {syncResponse.Data.Has.Set}" +
                    $"Rules:{syncResponse.Data.Has.R}");

                //TODO : Implement Edge
                if (syncResponse.Data.Meta.Edge == 1)
                {
                    SDKCommon.Console_WriteLine("Initializing Edge Device...");
                    return new EdgeGateway(syncResponse.Data, deviceCallBack, twinUpdateCallBack, options, discoveryCommon, platform);
                }
                else
                {
                    SDKCommon.Console_WriteLine("Initializing Device...");
                    return new Gateway(syncResponse.Data, deviceCallBack, twinUpdateCallBack, options, discoveryCommon, platform);
                }
            }
            else
            {
                SDKCommon.Console_WriteLine("Invalid Sync response");
                throw new SyncException("Invalid Sync response");
            }

            throw new SDKInitializationException("Error Initializing SDK");
        }

        private static SyncResponse GetSyncResponse(string uniqueId, DiscoveryCommon discoveryCommon, string cpId = "")
        {
            HttpWebResponse apiResponse;

            try
            {
                SDKCommon.Console_WriteLine($"Calling Sync service for UniqueId:{uniqueId}");
                //NOTE: For Device Authority testing
                //if (!string.IsNullOrWhiteSpace(cpId))
                //    apiResponse = SDKCommon.ApiCall($"{SDKCommon.SyncUrl}/{cpId}-{uniqueId}", HttpMethod.Get);
                //else
                apiResponse = SDKCommon.ApiCall($"{discoveryCommon.SyncUrl}/{uniqueId}", HttpMethod.Get);
                SDKCommon.Console_WriteLine($"Calling Sync service for UniqueId:{uniqueId} - Success with {apiResponse.StatusCode}");
            }
            catch (Exception ex)
            {
                SDKCommon.Console_WriteLine($"Calling Sync service for UniqueId:{uniqueId} - Failed");
                SDKCommon.Console_WriteError($"Error while calling Sync service for UniqueId:{uniqueId} Exception:{ex.Message}");
                throw new SDKInitializationException("Error while getting Sync response");
            }

            if (apiResponse.StatusCode == HttpStatusCode.OK)
            {
                StreamReader reader = new StreamReader(apiResponse.GetResponseStream());
                string str = reader.ReadToEnd();

                SyncResponse syncResponse;

                try
                {
                    SDKCommon.Console_WriteLine($"Deserializing sync response for UniqueId:{uniqueId}");
                    syncResponse = JsonConvert.DeserializeObject<SyncResponse>(str);
                    SDKCommon.Console_WriteLine($"Deserializing sync response for UniqueId:{uniqueId} - Success with {syncResponse.Data.Ec}");
                }
                catch (Exception ex)
                {
                    SDKCommon.Console_WriteLine($"Deserializing sync response for UniqueId:{uniqueId} - Failed");
                    SDKCommon.Console_WriteError($"Serialization exception of sync response for UniqueId:{uniqueId} Exception:{ex.Message}");
                    throw new SDKSerializationException(ex.Message, str);
                }

                if (syncResponse == null || syncResponse.Data == null)
                {
                    throw new SDKInitializationException("Error while getting Sync response");
                }
                else if (syncResponse.Data.Ec == ErrorCodes.Ok)
                {
                    return syncResponse;
                }
                else
                {
                    string errorMsg = SDKCommon.GetEnumDescription(syncResponse.Data.Ec);
                    SDKCommon.Console_WriteLine($"Sync response : {uniqueId} {errorMsg}");
                    switch (syncResponse.Data.Ec)
                    {
                        case ErrorCodes.DeviceNotFound:
                        case ErrorCodes.DeviceNotActive:
                            throw new DeviceNotFoundException(errorMsg);
                            break;

                        case ErrorCodes.DeviceUnAssociated:
                        case ErrorCodes.DeviceNotAcquired:
                            throw new DeviceNotAcquiredException(errorMsg);
                            break;

                        case ErrorCodes.DeviceDisabled:
                        case ErrorCodes.ConnectionNotAllowed:
                            throw new DeviceUnauthorizedException(errorMsg, new Exception(errorMsg));
                            break;

                        case ErrorCodes.CompanyNotFound:
                            throw new CompanyNotFoundException(errorMsg);
                            break;

                        case ErrorCodes.SubscriptionExpired:
                            throw new SubscriptionExpiredException(errorMsg);
                            break;

                        default:
                            break;
                    }
                }
            }

            SDKCommon.Console_WriteLine($"Something went wrong while sync call for UniqueId:{uniqueId}");
            throw new SDKInitializationException("Error while parsing Sync response");
        }

        internal static void ReLoad(BaseGateway gateway, DiscoveryCommon discoveryCommon, string platform)
        {
            SDKCommon.Console_WriteLine($"Re-sync for UniqueId:{gateway.UniqueId}");
            SyncResponse syncResponse = GetSyncResponse(gateway.UniqueId, discoveryCommon);
            syncResponse.Platform = platform;
            if (syncResponse != null && syncResponse.Data != null && syncResponse.Data.Ec == ErrorCodes.Ok)
            {
                SDKCommon.Console_WriteLine($"Re-sync for UniqueId:{gateway.UniqueId} - Success");
                gateway.SyncData = syncResponse.Data;
            }
            else
            {
                SDKCommon.Console_WriteError($"Something went wrong while re-sync for UniqueId:{gateway.UniqueId}");
                throw new SDKInitializationException("Error Initializing SDK");
            }

            //TODO: Implement Edge
            if (gateway is EdgeGateway)
                (gateway as EdgeGateway).RestartJobs();
        }

        public static void SyncHelloMessage(this BaseGateway gateway, string sId = null)
        {
            try
            {
                SDKCommon.Console_WriteLine($":: Syncing hello message :: mt: {(int)MessageTypes.INFO}");
                var msg = new BaseMtModel()
                {
                    Mt = MessageTypes.INFO,
                    sId = sId
                };

                gateway.SendD2C(msg, new KeyValuePair<string, string>("di", "1"));

                //TODO : Implement Edge
                //if (gateway is EdgeGateway2) (gateway as EdgeGateway2).RestartJobs();
            }
            catch (Exception ex)
            {
                SDKCommon.Console_WriteLine($"Calling attribute sync call for UniqueId:{gateway.UniqueId} - Failed");
                SDKCommon.Console_WriteError($"Calling attribute sync call for UniqueId:{gateway.UniqueId} Exception:{ex.Message}");
            }
        }

        public static void SyncAttributes(this BaseGateway gateway)
        {
            try
            {
                if (!gateway.SyncHasInfo.Attr)
                {
                    SDKCommon.Console_WriteLine($":: No attributes to sync :: mt: {(int)MessageTypes.ATTRS}");
                    return;
                }

                SDKCommon.Console_WriteLine($":: Syncing attributes :: mt: {(int)MessageTypes.ATTRS}");
                var msg = new BaseMtModel()
                {
                    Mt = MessageTypes.ATTRS
                };

                gateway.SendD2C(JsonConvert.SerializeObject(msg), new KeyValuePair<string, string>("di", "1"));

                //TODO : Implement Edge
                //if (gateway is EdgeGateway) (gateway as EdgeGateway).RestartJobs();
            }
            catch (Exception ex)
            {
                SDKCommon.Console_WriteLine($"Calling attribute sync call for UniqueId:{gateway.UniqueId} - Failed");
                SDKCommon.Console_WriteError($"Calling attribute sync call for UniqueId:{gateway.UniqueId} Exception:{ex.Message}");
            }
        }

        public static void SyncSettings(this BaseGateway gateway, bool getForcefully = false)
        {
            try
            {
                if (!gateway.SyncHasInfo.Set && !getForcefully)
                {
                    SDKCommon.Console_WriteLine($":: No shadow properties to sync :: mt: {(int)MessageTypes.SHADOW}");
                    return;
                }

                SDKCommon.Console_WriteLine($":: Syncing shadow properties :: mt: {(int)MessageTypes.SHADOW}");
                var msg = new BaseMtModel()
                {
                    Mt = MessageTypes.SHADOW
                };

                gateway.SendD2C(msg, new KeyValuePair<string, string>("di", "1"));
            }
            catch (Exception ex)
            {
                SDKCommon.Console_WriteLine($"Calling setting sync call for UniqueId:{gateway.UniqueId} - Failed");
                SDKCommon.Console_WriteError($"Calling setting sync call for UniqueId:{gateway.UniqueId} Exception:{ex.Message}");
            }
        }

        public static void SyncChildDevices(this BaseGateway gateway, bool getForcefully = false)
        {
            try
            {
                if (!gateway.SyncHasInfo.D && !getForcefully)
                {
                    SDKCommon.Console_WriteLine($":: No devices to sync :: mt: {(int)MessageTypes.DEVICES}");
                    return;
                }

                if (getForcefully)
                    SDKCommon.Console_WriteLine($"FIRMWARE ::: Syncing devices :: mt: {(int)MessageTypes.DEVICES}");
                else
                    SDKCommon.Console_WriteLine($":: Syncing devices :: mt: {(int)MessageTypes.DEVICES}");

                var msg = new BaseMtModel()
                {
                    Mt = MessageTypes.DEVICES
                };

                gateway.SendD2C(msg, new KeyValuePair<string, string>("di", "1"));
            }
            catch (Exception ex)
            {
                SDKCommon.Console_WriteLine($"Calling setting sync call for UniqueId:{gateway.UniqueId} - Failed");
                SDKCommon.Console_WriteError($"Calling setting sync call for UniqueId:{gateway.UniqueId} Exception:{ex.Message}");
            }
        }

        public static void SyncRules(this BaseGateway gateway, bool getForcefully = false)
        {
            try
            {
                if (!gateway.SyncHasInfo.R && !getForcefully)
                {
                    SDKCommon.Console_WriteLine($":: No rules to sync :: mt: {(int)MessageTypes.EDGE_RULE}");
                    return;
                }

                SDKCommon.Console_WriteLine($":: Syncing rules :: mt: {(int)MessageTypes.EDGE_RULE}");
                var msg = new BaseMtModel()
                {
                    Mt = MessageTypes.EDGE_RULE
                };

                gateway.SendD2C(msg, new KeyValuePair<string, string>("di", "1"));
            }
            catch (Exception ex)
            {
                SDKCommon.Console_WriteLine($"Calling rule sync call for UniqueId:{gateway.UniqueId} - Failed");
                SDKCommon.Console_WriteError($"Calling rule sync call for UniqueId:{gateway.UniqueId} Exception:{ex.Message}");
            }
        }

        public static void SyncPendingOTAUpdate(this BaseGateway gateway, bool getForcefully = false)
        {
            try
            {
                if (!gateway.SyncHasInfo.Ota && !getForcefully)
                {
                    SDKCommon.Console_WriteLine($":: No pending OTA updates :: mt: {(int)MessageTypes.PENDING_OTA}");
                    return;
                }

                SDKCommon.Console_WriteLine($":: Syncing pending OTA updates :: mt: {(int)MessageTypes.PENDING_OTA}");
                var msg = new BaseMtModel()
                {
                    Mt = MessageTypes.PENDING_OTA
                };

                gateway.SendD2C(msg, new KeyValuePair<string, string>("di", "1"));
            }
            catch (Exception ex)
            {
                SDKCommon.Console_WriteLine($"Calling pending OTA updates sync call for UniqueId:{gateway.UniqueId} - Failed");
                SDKCommon.Console_WriteError($"Calling pending OTA updates sync call for UniqueId:{gateway.UniqueId} Exception:{ex.Message}");
            }
        }

        //public static void Protocol(BaseGateway gateway)
        //{
        //    try
        //    {
        //        var request = new SyncRequestModel
        //        {
        //            CpId = gateway.Cpid,
        //            UniqueId = gateway.UniqueId,
        //            Options =
        //              new SyncRequestOptions
        //              {

        //                  Protocol = true
        //              }
        //        };

        //        SDKCommon.Console_WriteLine($"Calling protocol sync call for UniqueId:{gateway.UniqueId}");
        //        var apiResponse = SDKCommon.ApiCall(SDKCommon.SyncUrl, HttpMethod.Post, null, JsonConvert.SerializeObject(request));
        //        StreamReader reader = new StreamReader(apiResponse.GetResponseStream());
        //        var response = JsonConvert.DeserializeObject<SyncModel>(reader.ReadToEnd());

        //        if (response != null && response.Data != null && response.Data.Protocol != null)
        //        {
        //            SDKCommon.Console_WriteLine($"Calling protocol sync call for UniqueId:{gateway.UniqueId} - Success");
        //            gateway.SyncData.Data.Protocol = response.Data.Protocol;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        SDKCommon.Console_WriteLine($"Calling protocol sync call for UniqueId:{gateway.UniqueId} - Failed");
        //        SDKCommon.Console_WriteError($"Calling protocol sync call for UniqueId:{gateway.UniqueId} Exception:{ex.Message}");
        //    }
        //}
    }
}