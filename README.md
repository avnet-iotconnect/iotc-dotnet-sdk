
# IOTConnect SDK: iotconnect-dotnet-core-sdk

This is an DOTNET CORE SDK library to connect the device with IoTConnect cloud by MQTT protocol. This library only abstract JSON responses from both end D2C and C2D. This SDK supports SAS key based authentication, CA signed and Self signed certificate authentication to communicate with cloud.

## Features:

* The SDK supports to send telemetry data and receive commands from IoTConnect portal.
* User can update firmware Over The Air using "OTA update" Feature supported by SDK.
* SDK support x509 certificate authentication.  
* SDK consists of Gateway device with multiple child devices support.
* SDK supports to receive and update the Shadow property. 
* SDK supports device and OTA command Acknowledgement.
* Provide device connection status receive by command.
* Support hard stop command to stop device client from cloud.
* It allows sending the OTA command acknowledgment for Gateway and child device.
* It manages the sensor data sending flow over the cloud by using data frequency("df") configuration.
* It allows to disconnect the device from firmware.

# Example Usage:

Follow the steps given below to import our SDK as a library
- Open Visual Studio 2017/2019, Create new .net core Console App.
- Extract (get "nupkg" file from �iotconnect-sdk-dotnet-core-v1.14.0.zip�) the zip file and Create new folder called Nuget and place IOTConnect SDK Nuget package.
- Open Nuget Package Manager of Console App and click on Settings for Package source.
- Add new Package source, name it as LocalPackage and in source give path of Nuget folder created in step #2
- Install package from server/local nuget manager.
- Copy content from firmware file into program.cs file in newly created project.

Prerequisite input data
```c#
			string cpId = "<cpid>";
            string uniqueId = "<deviceUniqueId>";
            string env = "<env>";
            string sId = "<sId>";
            string platform = "<platform>";       
```
- cpId :: It need to get from the IoTConnect platform "Settings->Key Vault".
- uniqueId :: Its device ID which register on IotConnect platform and also its status has Active and Acquired.
- env :: It need to get from the IoTConnect platform "Settings->Key Vault".
- sId :: It need to get from the IoTConnect platform "Settings->Key Vault".
- platform :: It is either aws or az".

**SDKOptions** is for the SDK configuration and need to parse in SDK object initialize call. You need to manage the below configuration as per your device authentications.

```c#
SDKOptions sdkOptions = new SDKOptions();
sdkOptions.Certificate.CACertificatePath = "<<filepath>>/cert.pfx";
sdkOptions.Certificate.PrivateKeyCertificatePath = "<<filepath>>/cert.pfx";
sdkOptions.Certificate.Password = "******";
//For Offline Storage only
sdkOptions.OfflineStorage.AvailSpaceInMb = <<Integer Value>>; //size in MB, Default value = unlimited
sdkOptions.OfflineStorage.FileCount = <<Integer Value>>; // Default value = 1
sdkOptions.OfflineStorage.Disabled = <<true/false>>; // //default value = false, false = store data, true = not store data 
```
"Certificate.CACertificatePath": It is indicated to define the path of the device certificate file. Mandatory for X.509/Device CA signed or self-signed authentication type only.
"Certificate.PrivateKeyCertificatePath": It is indicated to define the path of the private key certificate file. Mandatory for X.509/Device CA signed or self-signed authentication type only.
"offlineStorage" : Define the configuration related to the offline data storage 
	- Disabled : false = offline data storing, true = not storing offline data 
	- AvailSpaceInMb : Define the file size of offline data which should be in (MB)
	- FileCount : Number of files need to create for offline data
**Note:**: sdkOptions is optional but mandatory for SSL/x509 device authentication type only. Define proper setting or leave it NULL. If you do not provide offline storage, it will set the default settings as per defined above. It may harm your device by storing the large data. Once memory gets full may chance to stop the execution.

To Initialize the SDK object and connect to the cloud
```c#
sdkClient = new SDKClient();
sdkClient.Init(cpId, uniqueId, sId, env, platform, sdkOptions, FirmwareDelegate, ShadowUpdateCallBack);
```

To receive the command from Cloud to Device(C2D)
```c#
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
```

To receive the twin from Cloud to Device(C2D)
```c#
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
        }```

This is the standard data input format for Gateway and non Gateway device to send the data on IoTConnect cloud(D2C).
```c#
1. For Non Gateway Device 
String deviceData = "[{
	'uniqueId': '<< Device UniqueId >>',
	'time' : '<< date >>',
	'data': {}
}]";

2. For Gateway and multiple child device 
String deviceData = "[{
	'uniqueId': '<< Gateway Device UniqueId >>', // It should be must first object of the array
	'time': '<< date >>',
	'data': {}
},
{
	'uniqueId':'<< Child DeviceId >>', //Child device
	'time': '<< date >>',
	'data': {}
}]";

sdkClient.SendData(deviceData);

Note: Date time will be in ISO 8601 format as "YYYY-MM-DDTHH:MM:SS.sssZ". Example: "2019-12-24T10:06:17.857Z"
```
"time" : Date format should be as defined //"2021-01-24T10:06:17.857Z" 
"data" : JSON data type format // {"temperature": 15.55, "gyroscope" : { 'x' : -1.2 }}

To send the command acknowledgment
```c#
var command = JsonConvert.DeserializeObject<DeviceCommand>(arg);
client.SendAckCmd(command.Ack.Value, 6, "Ack done.!!!");
```
"ackId(*)" 	: Command Acknowledgment GUID which will receive from command payload (data.ackId)
"st(*)"		: Acknowledgment status sent to cloud (4 = Fail, 6 = Device command[0x01], 7 = Firmware OTA command[0x02])
"msg" 		: It is used to send your custom message
"childId" 	: It is used for Gateway's child device OTA update only
				0x01 : null or "" for Device command
			  	0x02 : null or "" for Gateway device and mandatory for Gateway child device's OTA update.
		   		How to get the "childId" .?
		   		- You will get child uniqueId for child device OTA command from payload "data.urls[~].uniqueId"
"msgType" 	: Message type (5 = "0x01" device command, 11 = "0x02" Firmware OTA command)
Note : (*) indicates the mandatory element of the object.

To disconnect the device from the cloud
```c#
sdkClient.Dispose();
```

To get the shadow property Desired and Reported
```c#
sdkClient.GetShadows();
```

To get the all attributes 
```c#
sdkClient.GetAttributes();
```

# Dependencies:

* This SDK used below packages :
	- Microsoft.Azure.Devices, Microsoft.Azure.Devices.Client, Newtonsoft.Json, Microsoft.NETCore.App

# Integration Notes:

## Prerequisite tools

1. .net core 2.0 
2. Visual studio IDE

## Installation:

* Extract the "iotconnect-sdk-dotnet-core-v1.14.0.zip"

* Follow the steps given below to import our SDK as a library
	- Open Visual Studio 2022, Create new .net core Console App.
	- Extract (get "nupkg" file from �iotconnect-sdk-dotnet-core-v1.14.0.zip�) the zip file and Create new folder called Nuget and place IOTConnect SDK Nuget package.
	- Open Nuget Package Manager of Console App and click on Settings for Package source.
	- Add new Package source, name it as LocalPackage and in source give path of Nuget folder created in step #2
	- Install package from server/local nuget manager.
	- Copy content from firmware file into program.cs file in newly created project.
	
* You can take the firmware file from the above location and update the following details
    - Prerequisite input data as explained in the usage section as above
    - Update sensor attributes according to added in IoTConnect cloud platform.
    - If your device is secure then need to configure the x.509 certificate path such as given above in SDK Options otherwise leave it as it is.

* Ready to go:
    - Press the "F5" button in Visual Studio IDE.
    
## Release Note :

1. Offline data storage functionality with specific settings.
2. Device and OTA command acknowledgment.
3. It allows to disconnecting the device client. 
4. Support hard stop command from cloud.
5. Support OTA command with Gateway and child device.
6. It allows sending the OTA command acknowledgment for Gateway and child device.
7. Introduce new command(0x16) in device callback for Device connection status true(connected) or false(disconnected).
8. Introduce the "df" Data Frequency feature to control the flow of data which publish on cloud (For Non-Edge device only).