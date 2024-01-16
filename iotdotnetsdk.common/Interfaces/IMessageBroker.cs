using MQTTnet.Client;

namespace iotdotnetsdk.common.Interfaces
{
    internal interface IMessageBroker
    {
        void Connect(Func<MqttClientConnectedEventArgs, Task> _deviceClient_ConnectedAsync, Func<MqttClientDisconnectedEventArgs, Task> _deviceClient_DisconnectedAsync);
        Task Send(string topic, string message, Action<string> SendExecptionCallback);

        void Receive(string subTopic, string twinTopic, Func<MqttApplicationMessageReceivedEventArgs, Task> sdkCallBack);

        void Receive(string subTopic, string twinTopic, string twinTopic2, Func<MqttApplicationMessageReceivedEventArgs, Task> sdkCallBack);
        void Disconnect(bool noRetry = false);
        void Dispose();

        void SendShadowData(string twinTopic, Action<string> SendExecptionCallback);

        void SendTwinData(string twinTopic, Action<string> SendExecptionCallback);

        void RegisterDirectMethod(string directMethodTopic, Func<MqttApplicationMessageReceivedEventArgs, Task> sdkCallBack);

        void SendRegisterMethodResponse(string directMethodTopic, string message, Action<string> SendExecptionCallback);
    }

    internal interface IMqttBroker : IMessageBroker
    {
        bool UpdateShadow(string key, object value, string shadowTopic, Action<string> SendExecptionCallback);

        bool GetAllShadows(string shadowTopic, Action<string> SendExecptionCallback);

    }
}