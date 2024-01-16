using iotdotnetsdk.common;
using iotdotnetsdk.common.Models;
using iotdotnetsdk.common.Models.C2D;
using iotdotnetsdk.common.Models.Identity;

using MQTTnet.Client;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Text;

namespace TestClient
{
    class Program
    {
        static SDKClient client;
        static string uniqueId;
        static string platform;
        static CancellationTokenSource tokenSource2 = new CancellationTokenSource();
        static CancellationToken ct = tokenSource2.Token;
        static bool isCallbackCompleted = false;
        static bool isCallbackRunning = false;
        static string azPlatform = "az";
        static string awsPlatform = "aws";
        static bool isAzPlatform = false;
        static List<DeviceDetails> DeviceList { get; set; }
        public static List<Task> _whileLoopTask { get; set; }

        static void Main(string[] args)
        {
            try
            {
                string input = string.Empty;
                string cpId = string.Empty;
                string sId = string.Empty;
                string environment = string.Empty;

                Console.Write($"select Platform ({azPlatform} or {awsPlatform}): ");
                platform = Console.ReadLine();
                platform = platform.ToLower();
                isAzPlatform = (platform == azPlatform);
                if (string.IsNullOrEmpty(platform))
                {
                    Console.WriteLine("Select Platform");
                    return;
                }
                else if (platform != azPlatform && platform != awsPlatform)
                {
                    Console.WriteLine("Invalid Platform");
                    return;
                }

                Console.Write("Enter UniqueId: ");
                uniqueId = Console.ReadLine();

                Console.Write($"{Environment.NewLine}Wants to add cpId? (Y|N) [Default N]: ");
                input = Console.ReadLine();
                if (input.Equals("Y", StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.Write("Enter CPID: ");
                    cpId = Console.ReadLine();

                    Console.Write("Enter Environment: ");
                    environment = Console.ReadLine();
                }
                else
                {
                    Console.Write("Enter sId: ");
                    sId = Console.ReadLine();
                }


                //NOTE: For Device Authority testing
                //Console.Write("Enter cpId: ");
                //string cpId = Console.ReadLine();

                //uniqueId = "JKtype12point1";// Console.ReadLine();
                //string sId = "ZjNmNGIxMmQ4MmUxNDllNzliYjM4NGYxNWQ0OGExZTU=UDI6MDE6OTIuMzA=";// Console.ReadLine();

                //uniqueId = "latlongtest";
                //sId = "MDNkMzhkMjUwNDI1NGEwNzhjYjA0YTBkMGY1MjllYWQ=UTE6MDE6NzAuOTM=";

                //uniqueId = "AAug2022";// "testLatLong";
                //sId = "ZWJlYmYwZmVjMjUwNGM2NmJiYmEzZGMwMGRkMzQzOTg=UDI6MDE6OTIuMzA=";

                //string environment = "preqa";


                //uniqueId = "DevTest11";
                //sId = "YTIwZDVjODhjZTA5NDhhZDgxYzMxNWUwMjAzZDFlZDA=UTI6MDE6MDIuMDA=";
                //environment = "qa";

                //if (args.Length > 0)
                //    environment = args[0];

                if ((string.IsNullOrWhiteSpace(sId) && string.IsNullOrWhiteSpace(cpId)) || string.IsNullOrEmpty(sId) &&
                    (string.IsNullOrEmpty(cpId) || string.IsNullOrEmpty(environment)) || string.IsNullOrWhiteSpace(uniqueId))
                {
                    Console.WriteLine("Invalid Inputs!");
                    return;
                }



                string discoveryUrl = "";
                Console.Write($"{Environment.NewLine}Wants to change discovery url? (Y|N) [Default N]: ");
                input = Console.ReadLine();
                if (input.Equals("Y", StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.Write($"{Environment.NewLine}Enter discovery url: ");
                    discoveryUrl = Console.ReadLine();
                    if (!Uri.IsWellFormedUriString(discoveryUrl, UriKind.Absolute) || string.IsNullOrWhiteSpace(discoveryUrl))
                    {
                        Console.WriteLine("Invalid discovery URL. Press any key to continue..."); Console.ReadKey(); return;
                    }
                }

                SDKOptions options;
                if (string.IsNullOrWhiteSpace(discoveryUrl))
                    options = new SDKDebugOption() { IsDebug = false };
                else
                    options = new SDKAltDebugOption() { DiscoveryURL = discoveryUrl, IsDebug = false };

                Console.Write($"{Environment.NewLine}Wants to setup SDK Options? (Y|N) [Default N]: ");
                input = Console.ReadLine();

                if (input.Equals("Y", StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.Write($"{Environment.NewLine}Enable debugging? (Y|N) [Default N]:");
                    input = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(discoveryUrl))
                        (options as SDKDebugOption).IsDebug = input.Equals("Y", StringComparison.CurrentCultureIgnoreCase);
                    else
                        (options as SDKAltDebugOption).IsDebug = input.Equals("Y", StringComparison.CurrentCultureIgnoreCase);

                    Console.Write($"{Environment.NewLine}Disabled Offline Storage? : (Y|N) [Default N]:");
                    input = Console.ReadLine();
                    if (input.Equals("Y", StringComparison.CurrentCultureIgnoreCase))
                    {
                        options.OfflineStorage = new OfflineStorageInfo() { Disabled = true };
                    }

                    Console.Write($"{Environment.NewLine}AvailSpace Space for log file: (in MB):");
                    input = Console.ReadLine();
                    if (!string.IsNullOrEmpty(input) && int.TryParse(input, out int availSpaceInMb))
                    {
                        options.OfflineStorage.AvailSpaceInMb = availSpaceInMb;
                    }
                    else
                    {
                        Console.WriteLine(string.IsNullOrEmpty(input) ? "Setting default to unlimited" : "Invalid input for AvailSpace! Setting default to unlimited");
                    }

                    Console.Write($"{Environment.NewLine}Number of log files to be created: [Default 1]:");
                    input = Console.ReadLine();
                    if (!string.IsNullOrEmpty(input) && int.TryParse(input, out int fileCount))
                    {
                        options.OfflineStorage.FileCount = fileCount;
                    }
                    else
                        Console.WriteLine(string.IsNullOrEmpty(input) ? "Setting default to 1" : "Invalid input for FileCount! Setting default to 1");

                    Console.Write($"{Environment.NewLine}Is data frequency applicable ? (Y|N) [Default Y]: ");
                    input = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(input) && !input.Equals("Y", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (options is SDKDebugOption)
                            (options as SDKDebugOption).IsDataFreqEnable = false;
                        else
                            (options as SDKAltDebugOption).IsDataFreqEnable = false;
                    }

                    Console.Write($"{Environment.NewLine}Enter device primary key: ");
                    options.DevicePK = Console.ReadLine();
                }

                Console.Write("Setting up Certificate? (Y|N) [Default N]: ");
                input = Console.ReadLine();


                if (input.Equals("Y", StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.Write("Enter CA Certificate Path : ");
                    var certiPath = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(certiPath))
                        options.Certificate.CACertificatePath = certiPath;

                    Console.Write("Enter Private Key Certificate Path : ");
                    var privateKeyCertiPath = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(privateKeyCertiPath))
                        options.Certificate.PrivateKeyCertificatePath = privateKeyCertiPath;

                    if (string.IsNullOrEmpty(certiPath) || string.IsNullOrEmpty(privateKeyCertiPath))
                    {
                        Console.WriteLine("Invalid Inputs!");
                        return;
                    }

                    Console.Write("Enter Certificate Password : ");
                    var certiPass = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(certiPass))
                        options.Certificate.Password = certiPass;
                }

                client = new SDKClient();

                //NOTE: For Device Authority testing
                if (!string.IsNullOrWhiteSpace(cpId))
                    client.Init(cpId, uniqueId, sId, environment, platform, options, FirmwareDelegate, ShadowUpdateCallBack);
                else
                    client.Init(uniqueId, sId, platform, options, FirmwareDelegate, ShadowUpdateCallBack);

                SetAllCommandCallback();
                client.Connect(InitSuccessCallback, InitFailedCallback, ConnectionStatusCallback);

            Jignesh:
                isCallbackCompleted = false;
                tokenSource2 = new CancellationTokenSource();
                ct = tokenSource2.Token;
                var task = Task.Run(async () =>
                {
                    // Were we already canceled?
                    ct.ThrowIfCancellationRequested();
                    await WhileLoopMethod();
                }, tokenSource2.Token); // Pass same token to Task.Run.

                _whileLoopTask = new List<Task>();
                _whileLoopTask.Add(task);

                try
                {
                    Task.WaitAll(_whileLoopTask.ToArray());
                }
                catch (Exception ex)
                {
                    if (ex.InnerException.Message.Equals("A task was canceled."))
                    {
                        while (!isCallbackCompleted && isCallbackRunning)
                        {
                            Task.Delay(5000).Wait();
                        }

                        if (isCallbackCompleted)
                        {
                            isCallbackRunning = false;
                            goto Jignesh;
                        }
                    }
                }
                Console.WriteLine("press enter to exit.");
                Console.ReadLine();
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine("FIRMWARE ::: GETTING ERROR ::: " + ex.Message);
                if (!ex.InnerException.Message.Equals("A task was canceled."))
                {
                    throw;
                }
            }
        }

        private static async Task WhileLoopMethod()
        {
            while (true)
            {
                // Poll on this property if you have to do
                // other cleanup before throwing.
                if (ct.IsCancellationRequested)
                {
                    // Clean up here, then...
                    ct.ThrowIfCancellationRequested();
                }

                Console.WriteLine(Environment.NewLine);
                Console.WriteLine("1. Hello 200");
                Console.WriteLine("2. Get Attr 201");
                Console.WriteLine("3. Get Setting 202");
                Console.WriteLine("4. Get Rules 203");
                Console.WriteLine("5. Get Devices 204");
                Console.WriteLine("6. Get OTAs 205");
                Console.WriteLine("7. Get All 210");
                Console.WriteLine("8. Send Telemetry");
                Console.WriteLine("9. Create Child Device");
                Console.WriteLine("10. Delete Child Device");
                if (isAzPlatform)
                {
                    //Console.WriteLine("11. Upload Image");
                }

                Console.WriteLine("12. Set callbacks for all C2D Commands");
                Console.WriteLine("13. Skip data validation");
                if (isAzPlatform)
                {
                    Console.WriteLine("14. Register direct method");
                }
                Console.WriteLine("15. Set callbacks for all C2D Commands WITHOUT Confirmation");
                Console.WriteLine("99. Exit");
                Console.Write("Select Option : ");

                var input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        client.GetHelloMessage(null);
                        break;
                    case "2":
                        client.GetAttributes(AttributesCallback);
                        break;
                    case "3":
                        client.GetShadows(ShadowListCallback);
                        break;
                    case "4":
                        client.GetRules(null);
                        break;
                    case "5":
                        client.GetChildDevices(ChildDeviceCallback);
                        break;
                    case "6":
                        client.GetPendingOTAUpdates(null);
                        break;
                    case "7":
                        //TODO : pending
                        Console.WriteLine("This feature is not enabled yet.");
                        break;
                    case "8":
                        Console.WriteLine($"Do you want to send data as JSON? (Y|N) [Default N]: ");
                        input = Console.ReadLine();

                        if (input.Equals("Y", StringComparison.CurrentCultureIgnoreCase))
                        {
                            Console.WriteLine("Enter data to send : ");
                            string data = Console.ReadLine();
                            client.SendData(data);
                        }
                        else
                        {
                            if (client.PrepareAndSendData())
                                Console.WriteLine($"{Environment.NewLine}{DateTime.Now.ToUniversalTime():o}: Sent!");
                        }
                        break;
                    case "9":

                        Console.WriteLine("Enter child device uniqueId: ");
                        var childDeviceId = Console.ReadLine();

                        Console.WriteLine("Enter child device tag: ");
                        var tag = Console.ReadLine();

                        Console.WriteLine("Enter child device Displayname: ");
                        var displayName = Console.ReadLine();

                        client.CreateChildDevice(childDeviceId, tag, displayName, ChildDeviceCallback);

                        break;
                    case "10":

                        Console.WriteLine("Enter child device uniqueId: ");
                        var deleteDeviceId = Console.ReadLine();

                        client.DeleteChildDevice(deleteDeviceId, ChildDeviceCallback);

                        break;

                    //case "11":
                    //    if (isAzPlatform)
                    //    {
                    //        Console.WriteLine("Enter absolute file path : ");
                    //        var filePath = Console.ReadLine();
                    //        //filePath = @"C:\Users\Jignesh.Khokhariya\Desktop\jktest.png";
                    //        client.SendImage(filePath).Wait();
                    //    }
                    //    break;

                    case "12":
                        string confirmation;

                        Console.Write($"Want to set AttrChangeCallback (Y/N) : ");
                        confirmation = Console.ReadLine();
                        if (!confirmation.Equals("n", StringComparison.CurrentCultureIgnoreCase))
                            client.OnAttrChangeCommand(OnAttrChangeCallback);
                        Task.Delay(1000).Wait();

                        Console.Write($"Want to set ShadowChangeCallback (Y/N) : ");
                        confirmation = Console.ReadLine();
                        if (!confirmation.Equals("n", StringComparison.CurrentCultureIgnoreCase))
                            client.OnShadowChangeCommand(OnShadowChangeCallback);
                        Task.Delay(1000).Wait();

                        Console.Write($"Want to set ChildDeviceChangeCallback (Y/N) : ");
                        confirmation = Console.ReadLine();
                        if (!confirmation.Equals("n", StringComparison.CurrentCultureIgnoreCase))
                            client.OnDeviceChangeCommand(OnDeviceChangeCallback);
                        Task.Delay(1000).Wait();

                        Console.Write($"Want to set DeviceCommandCallback (Y/N) : ");
                        confirmation = Console.ReadLine();
                        if (!confirmation.Equals("n", StringComparison.CurrentCultureIgnoreCase))
                            client.OnDeviceCommand(OnDeviceCommandCallback);
                        Task.Delay(1000).Wait();

                        Console.Write($"Want to set PushModuleCommandCallback (Y/N) : ");
                        confirmation = Console.ReadLine();
                        if (!confirmation.Equals("n", StringComparison.CurrentCultureIgnoreCase))
                            client.OnModuleCommand(OnModuleCommandCallback);
                        Task.Delay(1000).Wait();

                        Console.Write($"Want to set OTACommandCallback (Y/N) : ");
                        confirmation = Console.ReadLine();
                        if (!confirmation.Equals("n", StringComparison.CurrentCultureIgnoreCase))
                            client.OnOTACommand(OnOTACommandCallback);
                        Task.Delay(1000).Wait();

                        Console.WriteLine("FIRMWARE ::: All callbacks for C2D has been set.");
                        break;

                    case "13":
                        Console.WriteLine("Enter 0/1 to [0 = enable, 1 = skip] Data Validation : ");
                        var skipDVStr = Console.ReadLine();
                        bool skipDV = skipDVStr == "1" ? true : false;
                        client.SkipDataValidation(skipDV);
                        break;

                    case "14":
                        if (isAzPlatform)
                        {
                            RegisterDirectMethodWithCallback().Wait();
                        }
                        break;

                    case "15":
                        SetAllCommandCallback();

                        Console.WriteLine("FIRMWARE ::: All callbacks for C2D has been set.");
                        break;

                    case "99":
                        Console.WriteLine("FIRMWARE ::: Disposing....");
                        client.Dispose();
                        return;
                }
            }
        }

        private static void SetAllCommandCallback()
        {
            client.OnDeviceCommand(OnDeviceCommandCallbackWithoutFirmwareInput);
            //Task.Delay(100).Wait();

            client.OnOTACommand(OnOTACommandCallbackWithoutFirmwareInput);
            //Task.Delay(100).Wait();

            client.OnModuleCommand(OnModuleCommandCallback);
            //Task.Delay(100).Wait();

            client.OnAttrChangeCommand(OnAttrChangeCallback);
            //Task.Delay(100).Wait();

            client.OnShadowChangeCommand(OnShadowChangeCallback);
            //Task.Delay(100).Wait();

            client.OnDeviceChangeCommand(OnDeviceChangeCallback);
            //Task.Delay(100).Wait();
        }

        private async static Task CancelWhileLoopTask()
        {
            isCallbackRunning = true; //NOTE: Remove this line and put it back into specific callback if "CancelWhileLoopTask()" needs to call from other than callback
            if (_whileLoopTask != null)
            {
                foreach (var item in _whileLoopTask)
                {
                    tokenSource2.Cancel();
                    try
                    {
                        Console.WriteLine("FIRMWARE ::: Press enter to continue with callback method.");
                        await item;
                    }
                    catch (OperationCanceledException e)
                    {
                        Console.WriteLine($"{nameof(OperationCanceledException)} thrown with message: {e.Message}");
                    }
                    finally
                    {
                        tokenSource2?.Dispose();
                    }
                }
            }

        }

        private async static Task OnOTACommandCallback(string arg)
        {
            await CancelWhileLoopTask();
            Console.WriteLine("FIRMWARE ::: OTA update received : " + arg);
            Console.WriteLine("0 = Success,\n1 = Failed,\n2 = Executed or DownloadingInProgress,\n3 = Executed or DownloadDone,\n4 = Failed or DownloadFailed");

            var command = JsonConvert.DeserializeObject<OTACommand>(arg);
            if (command != null && command.Urls != null && command.Urls.Count > 0)
            {
                Console.WriteLine("FIRMWARE ::: DOWNLOADING OTA FILE...!!!");
                var fileDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Files");
                if (!Directory.Exists(fileDirectory))
                {
                    Directory.CreateDirectory(fileDirectory);
                }

                if (Directory.Exists(fileDirectory))
                {
                    foreach (var itemUrl in command.Urls)
                    {
                        try
                        {
                            using (var httpClient = new HttpClient())
                            {
                                using (var fileData = await httpClient.GetStreamAsync(itemUrl.Url))
                                {
                                    var fileName = Path.Combine(fileDirectory, itemUrl.FileName);
                                    if (File.Exists(fileName))
                                    {
                                        FileInfo fi = new FileInfo(itemUrl.FileName);
                                        fileName = Path.Combine(fileDirectory, $"{Guid.NewGuid().ToString().ToUpper()}{fi.Extension}");
                                    }
                                    using (var streamData = new FileStream(fileName, FileMode.CreateNew))
                                    {
                                        await fileData.CopyToAsync(streamData);
                                    }
                                    Console.WriteLine("File stored at :::" + fileName);
                                }
                            }
                        }
                        catch (Exception exOta)
                        {
                            Console.WriteLine("Error while downloading OTA file" + exOta.Message);
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("FIRMWARE ::: NO OTA FILE FOUND...!!!");
            }

            if (command.Ack.HasValue && command.Ack.Value != Guid.Empty)
            {
                Console.WriteLine("Enter ota update ack status : ");
                var status = Console.ReadLine();

                Console.WriteLine("Enter ota update ack message: ");
                var msg = Console.ReadLine();

                if (DeviceList != null && DeviceList.Count > 1)
                {
                    foreach (var item in DeviceList)
                    {
                        Console.WriteLine($"Want to send ack for this parent/child device (Y/N) : {item.Id}");
                        var confirmation = Console.ReadLine();
                        if (!confirmation.Equals("n", StringComparison.CurrentCultureIgnoreCase))
                            client.SendOTAAckCmd(command.Ack.Value, Convert.ToInt32(status), msg, item.Id);
                    }
                }
                else
                    client.SendOTAAckCmd(command.Ack.Value, Convert.ToInt32(status), msg);

                Console.WriteLine("FIRMWARE ::: Acknowledgement sent.");
            }
            else
                Console.WriteLine("FIRMWARE ::: No acknowledgement required.");

            isCallbackCompleted = true;
        }

        private async static Task OnModuleCommandCallback(string arg)
        {
            await CancelWhileLoopTask();
            Console.WriteLine("FIRMWARE ::: Module command received : " + arg);

            //var command = JsonConvert.DeserializeObject<ModuleCommand>(arg);
            //if (command.Ack.HasValue && command.Ack.Value != Guid.Empty)
            //{
            //    Console.WriteLine("Enter module ack status: ");
            //    var status = Console.ReadLine();

            //    Console.WriteLine("Enter module ack message: ");
            //    var msg = Console.ReadLine();

            //    if (DeviceList.Count > 1)
            //    {
            //        foreach (var item in DeviceList)
            //        {
            //            Console.WriteLine($"Want to send ack for this child device (Y/N) : {item.Id}");
            //            var confirmation = Console.ReadLine();
            //            if (!confirmation.Equals("n", StringComparison.CurrentCultureIgnoreCase))
            //              client.SendAckModule(command.Ack.Value, Convert.ToInt32(status), msg, item.Id);
            //        }
            //    }
            //    else
            //        client.SendAckModule(command.Ack.Value, Convert.ToInt32(status), msg);
            //}
            //else
            Console.WriteLine("FIRMWARE ::: No acknowledgement required.");

            isCallbackCompleted = true;
        }

        private static async Task OnDeviceCommandCallback(string arg)
        {
            await CancelWhileLoopTask();
            Console.WriteLine("FIRMWARE ::: Device command received : " + arg);
            Console.WriteLine("\n4 = Failed, \n5 = Executed, \n6 = Executed ACK, \n7 = Success");

            var command = JsonConvert.DeserializeObject<DeviceCommand>(arg);
            if (command.Ack.HasValue && command.Ack.Value != Guid.Empty)
            {
                Console.WriteLine("Enter command ack status: ");
                var status = Console.ReadLine();

                Console.WriteLine("Enter command ack message: ");
                var msg = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(command.ChildId))
                    client.SendAckCmd(command.Ack.Value, Convert.ToInt32(status), msg, command.ChildId);
                else
                    client.SendAckCmd(command.Ack.Value, Convert.ToInt32(status), msg);
            }
            else
                Console.WriteLine("FIRMWARE ::: No acknowledgement required.");

            isCallbackCompleted = true;
        }

        private static Task OnDeviceChangeCallback(string arg)
        {
            Console.WriteLine("FIRMWARE ::: Device change command received : " + arg);
            client.GetChildDevices(ChildDeviceCallback);
            return Task.CompletedTask;
        }

        private static Task OnShadowChangeCallback(string arg)
        {
            Console.WriteLine("FIRMWARE ::: Shadow change command received : " + arg);
            return Task.CompletedTask;
        }

        private static Task OnAttrChangeCallback(string arg)
        {
            Console.WriteLine("FIRMWARE ::: Attribute change command received : " + arg);
            return Task.CompletedTask;
        }

        private static Task ChildDeviceCallback(string arg)
        {
            Console.WriteLine($"FIRMWARE ::: ChildDeviceCallback :::: {arg}");
            if (DeviceList != null)
                DeviceList.Clear();

            if (DeviceList == null)
                DeviceList = new List<DeviceDetails>();

            DeviceList.Add(new DeviceDetails()
            {
                Id = uniqueId
            });

            var childDevices = JsonConvert.DeserializeObject<DevicesModel>(arg)?.Data?.DeviceList;
            if (childDevices != null && childDevices.Count > 0)
                DeviceList.AddRange(childDevices);

            return Task.CompletedTask;
        }

        private static Task ConnectionStatusCallback(string arg)
        {
            Console.WriteLine($"FIRMWARE ::: ConnectionStatusCallback :::: {arg}");
            return Task.CompletedTask;
        }

        private static Task InitFailedCallback(string arg)
        {
            CancelWhileLoopTask().Wait();
            Console.WriteLine($"FIRMWARE ::: InitFailedCallback :::: {arg}");
            return Task.CompletedTask;
        }

        private static Task InitSuccessCallback(string arg)
        {
            Console.WriteLine($"FIRMWARE ::: InitSuccessCallbak :::: {arg}");
            client.GetAttributes((string arg) => AttrChangeCallback(arg, "Init"));
            return Task.CompletedTask;
        }

        private static Task ShadowListCallback(string arg)
        {
            Console.WriteLine($"FIRMWARE ::: ShadowListCallback :::: {arg}");
            return Task.CompletedTask;
        }

        private static Task AttrChangeCallback(string arg, string method)
        {
            if (method == "Init")
            {
                client.ProcessEdgeDevice();
            }
            return Task.CompletedTask;
        }

        private static Task AttributesCallback(string arg)
        {
            Console.WriteLine($"FIRMWARE ::: AttributesCallback :::: {arg}");
            return Task.CompletedTask;
        }

        private static Task ShadowUpdateCallBack(string arg)
        {
            Console.WriteLine($"FIRMWARE ::: Shadow update received :::: {arg}");
            try
            {
                Console.WriteLine("Shadow : " + arg);
                dynamic results = JsonConvert.DeserializeObject<dynamic>(arg);
                JObject shadow = (JObject)JToken.FromObject(results);

                if (shadow.Count > 0)
                {
                    Dictionary<string, object> shadowDict = new Dictionary<string, object>();
                    var version = shadow.Children<JProperty>().Where(a => a.Name.Equals("$version")).FirstOrDefault()?.Value;
                    foreach (JProperty item in shadow.Children<JProperty>())
                    {
                        if (item.Name.Equals("$version", StringComparison.CurrentCultureIgnoreCase))
                            continue;
                        client.UpdateShadow(item.Name, item.Value, Convert.ToString(version), PostShadowUpdateCallback);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return Task.CompletedTask;
        }

        private static Task PostShadowUpdateCallback(string arg)
        {
            Console.WriteLine($"FIRMWARE ::: Shadow update callback after Shadow update :::: {arg}");
            return Task.CompletedTask;
        }

        private static Task FirmwareDelegate(string arg)
        {
            Console.WriteLine($"FIRMWARE ::: FirmwareDelegate :::: {arg}");
            return Task.CompletedTask;
        }

        private static Task RegisterDirectMethodWithCallback()
        {
            try
            {
                Console.Write("Enter direct method name : ");
                var methodName = Console.ReadLine();
                client.RegisterDirectMethod(methodName, DirectMethodCallbackHandler);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return Task.CompletedTask;
        }

        private static async Task DirectMethodCallbackHandler(MqttApplicationMessageReceivedEventArgs methodRequest)
        {
            string topic = "$iothub/methods/res/{status}/?$rid={request-id}";
            string message = string.Empty;
            string filterTopic = methodRequest.ApplicationMessage.Topic.Replace("$iothub/methods/POST/", "");
            var methodName = filterTopic.Split("/")[0];
            var requestId = filterTopic.Split("/")[1].Split("=")[1];
            try
            {
                message = Encoding.UTF8.GetString(methodRequest.ApplicationMessage.Payload);
                await CancelWhileLoopTask();
                var output = JsonConvert.DeserializeObject<dynamic>(message);
                Console.WriteLine($"FIRMWARE ::: direct method executed : {methodName} with data : " + message.ToString());
                Console.Write("Enter response status (integer value only). If you want to make timeout, enter N in string : ");
                var responseStatusStr = Console.ReadLine();
                if (responseStatusStr.Equals("n", StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.Write("Enter timeout of your direct method: ");
                    if (int.TryParse(Console.ReadLine(), out int timeoutValue))
                    {
                        Task.Delay(timeoutValue).Wait();
                    }
                    else
                    {
                        Task.Delay(60000).Wait();
                    }
                }
                else
                {
                    if (int.TryParse(responseStatusStr, out int responseStatus))
                    {
                        topic = $"$iothub/methods/res/{responseStatus}/?$rid={requestId}";
                        client.SendDirectMethodResponse(topic, message);
                    }
                    else
                    {
                        topic = $"$iothub/methods/res/200/?$rid={requestId}";
                        client.SendDirectMethodResponse(topic, message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while directmethod call back handler :: " + ex.Message);
                //return new MethodResponse(methodRequest.Data, 200);
                topic = $"$iothub/methods/res/200/?$rid={requestId}";
                client.SendDirectMethodResponse(topic, message);
            }
            finally
            {
                isCallbackCompleted = true;
            }
        }


        #region command ack without taking response from firmware
        private async static Task OnOTACommandCallbackWithoutFirmwareInput(string arg)
        {
            await CancelWhileLoopTask();
            Console.WriteLine("FIRMWARE ::: OTA update received : " + arg);
            Console.WriteLine("0 = Success,\n1 = Failed,\n2 = Executed or DownloadingInProgress,\n3 = Executed or DownloadDone,\n4 = Failed or DownloadFailed");

            var command = JsonConvert.DeserializeObject<OTACommand>(arg);
            if (command != null && command.Ack.HasValue && command.Ack.Value != Guid.Empty)
            {
                if (DeviceList != null && DeviceList.Count > 1)
                {
                    foreach (var item in DeviceList)
                    {
                        client.SendOTAAckCmd(command.Ack.Value, 0, "Ack done.!!!", item.Id);
                    }
                }
                else
                    client.SendOTAAckCmd(command.Ack.Value, 0, "Ack done.!!!");

                Console.WriteLine("FIRMWARE ::: Acknowledgement sent.");
            }
            else
                Console.WriteLine("FIRMWARE ::: No acknowledgement required.");

            isCallbackCompleted = true;
        }

        private static async Task OnDeviceCommandCallbackWithoutFirmwareInput(string arg)
        {
            await CancelWhileLoopTask();
            Console.WriteLine("FIRMWARE ::: Device command received : " + arg);
            Console.WriteLine("\n4 = Failed, \n5 = Executed, \n6 = Executed ACK, \n7 = Success");

            var command = JsonConvert.DeserializeObject<DeviceCommand>(arg);
            if (command.Ack.HasValue && command.Ack.Value != Guid.Empty)
            {
                if (!string.IsNullOrWhiteSpace(command.ChildId))
                    client.SendAckCmd(command.Ack.Value, 6, "Ack done.!!!", command.ChildId);
                else
                    client.SendAckCmd(command.Ack.Value, 6, "Ack done.!!!");
            }
            else
                Console.WriteLine("FIRMWARE ::: No acknowledgement required.");

            isCallbackCompleted = true;
        }
        #endregion
    }

}