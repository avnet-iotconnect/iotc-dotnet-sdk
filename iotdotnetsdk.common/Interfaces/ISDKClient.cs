using iotdotnetsdk.common.Models;

using System;
using System.Threading.Tasks;

namespace iotdotnetsdk.common.Interfaces
{
    public interface ISDKClient
    {
        void Init(string uniqueId, string sId, string platform, SDKOptions sdkOptions, Func<string, Task> callback, Func<string, Task> twinUpdateCallBack);
        void Connect(Func<string, Task> successCallback, Func<string, Task> failedCallback, Func<string, Task> connectionStatusCallback);
        void Disconnect();
        void Dispose();

        bool SendData(string json);
        void SkipDataValidation(bool skipDV);
        //bool SendImage(string path);
        Task<bool> SendImage(string path);

        void GetHelloMessage(Func<string, Task> callback);
        void GetAttributes(Func<string, Task> callback);
        void GetShadows(Func<string, Task> callback);
        void GetRules(Func<string, Task> callback);
        void GetChildDevices(Func<string, Task> callback);
        void CreateChildDevice(string childId, string deviceTag, string displayName, Func<string, Task> callback);
        void DeleteChildDevice(string childId, Func<string, Task> callback);

        void UpdateShadow(string property, dynamic value, string version, Func<string, Task> callback);
        void SendAckCmd(Guid ackGuid, int status, string msg);
        void SendAckCmd(Guid ackGuid, int status, string msg, string childId);
        void SendOTAAckCmd(Guid ackGuid, int status, string msg);
        void SendOTAAckCmd(Guid ackGuid, int status, string msg, string childId);
        void SendAckModule(Guid ackGuid, int status, string msg);


        string OnEndrocementKeyGet(Func<string, Task> callback);//Only for TPM based device

        //Callback for C2D Commands
        void OnDeviceCommand(Func<string, Task> callback);
        void OnOTACommand(Func<string, Task> callback);
        void OnModuleCommand(Func<string, Task> callback);
        void OnShadowChangeCommand(Func<string, Task> callback);
        void OnAttrChangeCommand(Func<string, Task> callback);
        void OnDeviceChangeCommand(Func<string, Task> callback);
        //void OnRuleChangeCommand(Func<string, Task> callback);
    }
}
