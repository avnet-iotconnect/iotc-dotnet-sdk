using iotdotnetsdk.common.Interfaces;
using iotdotnetsdk.common.Internals;
using iotdotnetsdk.common.Models;

using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;

using Newtonsoft.Json;

using System.Net;
using System;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Runtime.ConstrainedExecution;
using System.Reflection;

namespace iotdotnetsdk.common.Brokers
{
    public class MqttBroker : IMqttBroker
    {
        IMqttClient _deviceClient;
        MqttClientOptions _options;
        MqttFactory factory;
        public MqttBroker(string host, int port, string deviceId, CertificateInfo? certInfo, string username = "", string password = "")
        {
            if (certInfo != null)
            {
                factory = new MqttFactory();
                _deviceClient = factory.CreateMqttClient();
                var builder = new MqttClientOptionsBuilder()
                .WithClientId(deviceId)
                .WithTcpServer(host, port)
                .WithKeepAlivePeriod(new TimeSpan(0, 0, 0, 300))
                .WithTimeout(new TimeSpan(0, 0, 0, 300));

                if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                {
                    builder.WithCredentials(username, password);
                    builder.WithTls(
                      o =>
                      {
                          o.UseTls = true;
                          // The default value is determined by the OS. Set manually to force version.
                          o.SslProtocol = SslProtocols.Tls12;
                      });
                }
                else
                {
                    if (!string.IsNullOrEmpty(certInfo.CACertificatePath) && !File.Exists(certInfo.CACertificatePath) || !string.IsNullOrEmpty(certInfo.PrivateKeyCertificatePath) && !File.Exists(certInfo.PrivateKeyCertificatePath))
                    {
                        throw new Exception("Certificate not found.");
                    }
                    if (!string.IsNullOrWhiteSpace(username))
                    {
                        builder.WithCredentials(username);
                    }
                    builder.WithTls(
                        o =>
                        {
                            o.UseTls = true;
                            // The default value is determined by the OS. Set manually to force version.
                            o.SslProtocol = SslProtocols.Tls12;
                            o.Certificates = GetCertificates(certInfo);
                        });
                }
                _options = builder.WithCleanSession()
                .Build();
            }
        }

        public MqttBroker(string host, int port, string deviceId, CertificateInfo? certInfo)
        {
            if (certInfo != null)
            {
                factory = new MqttFactory();
                _deviceClient = factory.CreateMqttClient();
                var builder = new MqttClientOptionsBuilder()
                .WithClientId(deviceId)
                .WithTcpServer(host, port)
                .WithKeepAlivePeriod(new TimeSpan(0, 0, 0, 300))
                .WithTimeout(new TimeSpan(0, 0, 0, 300));


                if (!string.IsNullOrEmpty(certInfo.CACertificatePath) && !File.Exists(certInfo.CACertificatePath) || !string.IsNullOrEmpty(certInfo.PrivateKeyCertificatePath) && !File.Exists(certInfo.PrivateKeyCertificatePath))
                {
                    throw new Exception("Certificate not found.");
                }
                builder.WithTls(
                    o =>
                    {
                        o.UseTls = true;
                        // The default value is determined by the OS. Set manually to force version.
                        o.SslProtocol = SslProtocols.Tls12;
                        o.Certificates = GetCertificates(certInfo);
                    });

                _options = builder.WithCleanSession()
                .Build();
            }
        }

        private IEnumerable<X509Certificate>? GetCertificates(CertificateInfo? certInfo)
        {
            X509Certificate2 cert = new X509Certificate2();
            if (certInfo != null && !string.IsNullOrEmpty(certInfo.CACertificatePath) && !string.IsNullOrEmpty(certInfo.PrivateKeyCertificatePath))
            {
                cert = X509Certificate2.CreateFromPemFile(
                certInfo.CACertificatePath,
                certInfo.PrivateKeyCertificatePath);
            }
            else if (certInfo != null && !string.IsNullOrEmpty(certInfo.CACertificateContent) && !string.IsNullOrEmpty(certInfo.PrivateKeyCertificateContent))
            {
                cert = X509Certificate2.CreateFromPem(
                certInfo.CACertificateContent,
                certInfo.PrivateKeyCertificateContent);
            }
            var pfxCertificate = new X509Certificate2(cert.Export(X509ContentType.Pfx));
            return new List<X509Certificate>() { new X509Certificate2(pfxCertificate) };
        }

        public void Connect(Func<MqttClientConnectedEventArgs, Task> ConnectedAsync, Func<MqttClientDisconnectedEventArgs, Task> DisconnectedAsync)
        {
            try
            {
                _deviceClient.ConnectedAsync += ConnectedAsync;
                _deviceClient.DisconnectedAsync += DisconnectedAsync;
                _deviceClient.ConnectAsync(_options).Wait();
                SDKCommon.Console_WriteLine("Connected.");
            }
            catch (Exception ex)
            {
                SDKCommon.Console_WriteLine($"{ex.Message} - Connection Failed!");
                throw ex;
            }
        }

        public void Disconnect(bool noRetry = false)
        {
            SDKCommon.Console_WriteLine(":: Calling Disconnect ::");
            _deviceClient.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().WithReason(MqttClientDisconnectReason.NormalDisconnection).Build());
        }

        public void SendShadowData(string ShadowTopic, Action<string> SendExecptionCallback)
        {
            string message = "{ \"_connectionStatus\": \"true\" }";
            Send(ShadowTopic, message, SendExecptionCallback).Wait();
        }

        public void SendTwinData(string twinTopic, Action<string> SendExecptionCallback)
        {
            string message = "{}";
            Send(twinTopic, message, SendExecptionCallback).Wait();
        }

        public void Dispose()
        {
            SDKCommon.Console_WriteLine(":: Calling Dispose ::");
            _deviceClient.Dispose();
        }

        public bool GetAllShadows(string ShadowTopic, Action<string> SendExecptionCallback)
        {
            try
            {
                Send(ShadowTopic, string.Empty, SendExecptionCallback).Wait();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Receive(string subTopic, string shadowTopic, Func<MqttApplicationMessageReceivedEventArgs, Task> ApplicationMessageReceivedAsync)
        {
            var mqttSubscribeOptions = factory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(
                    f =>
                    {
                        f.WithTopic(subTopic);
                    })
                .WithTopicFilter(
                    f =>
                    {
                        f.WithTopic(shadowTopic);
                    })
                .Build();

            _deviceClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None).Wait();
            _deviceClient.ApplicationMessageReceivedAsync += ApplicationMessageReceivedAsync;

            //while (true)
            //{
            //    //Message receivedMessage = await _deviceClient.ReceiveAsync();
            //    //if (receivedMessage == null)
            //    //    continue;

            //    //await _deviceClient.CompleteAsync(receivedMessage);
            //    //await sdkCallBack(Encoding.ASCII.GetString(receivedMessage.GetBytes()), deviceCallBack);
            //}
        }

        public void Receive(string subTopic, string twinTopic, string twinTopic2, Func<MqttApplicationMessageReceivedEventArgs, Task> ApplicationMessageReceivedAsync)
        {
            var mqttSubscribeOptions = factory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(
                    f =>
                    {
                        f.WithTopic(subTopic);
                    })
                .WithTopicFilter(
                    f =>
                    {
                        f.WithTopic(twinTopic);
                    })
                 .WithTopicFilter(
                    f =>
                    {
                        f.WithTopic(twinTopic2);
                    })
                .Build();

            _deviceClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None).Wait();
            _deviceClient.ApplicationMessageReceivedAsync += ApplicationMessageReceivedAsync;


            //while (true)
            //{
            //    //Message receivedMessage = await _deviceClient.ReceiveAsync();
            //    //if (receivedMessage == null)
            //    //    continue;

            //    //await _deviceClient.CompleteAsync(receivedMessage);
            //    //await sdkCallBack(Encoding.ASCII.GetString(receivedMessage.GetBytes()), deviceCallBack);
            //}
        }

        public async Task Send(string topic, string message, Action<string> SendExecptionCallback)
        {
            try
            {
                var applicationMessage = new MqttApplicationMessageBuilder()
               .WithTopic(topic)
               .WithPayload(message)
               .Build();

                _deviceClient.PublishAsync(applicationMessage, CancellationToken.None).Wait();


            }
            catch (Exception ex)
            {
                SendExecptionCallback(message);
                SDKCommon.Console_WriteLine(ex.Message);
            }

        }

        public bool UpdateShadow(string key, object value, string shadowTopic, Action<string> SendExecptionCallback)
        {
            try
            {
                Dictionary<string, dynamic> dict = new Dictionary<string, dynamic>() { { key, value } };
                var message = JsonConvert.SerializeObject(dict);
                Send(shadowTopic, message, SendExecptionCallback).Wait();
                SDKCommon.Console_WriteLine("Shadow updated successfully");
                return true;
            }
            catch (Exception ex)
            {
                SDKCommon.Console_WriteLine($"Error while updating Shadow : {ex.Message}");
            }
            return false;
        }

        public void RegisterDirectMethod(string directMethodTopic, Func<MqttApplicationMessageReceivedEventArgs, Task> sdkCallBack)
        {
            var mqttSubscribeOptions = factory.CreateSubscribeOptionsBuilder()
               .WithTopicFilter(
                   f =>
                   {
                       f.WithTopic("$iothub/methods/POST/#");
                   })
               .Build();

            _deviceClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None).Wait();
            _deviceClient.ApplicationMessageReceivedAsync += sdkCallBack;
            SDKCommon.Console_WriteLine("Direct method registered successfully." + directMethodTopic);
        }

        public void SendRegisterMethodResponse(string directMethodTopic, string message, Action<string> SendExecptionCallback)
        {
            Send(directMethodTopic, message, SendExecptionCallback).Wait();
        }
    }
}
