
# IOTConnect SDK: iotconnect-dotnet-core-sdk

This is the DOTNET 6.0 library to connect the device with IoTConnect cloud using MQTT protocol. This library only abstract JSON responses from both end D2C and C2D. This SDK supports SAS key based authentication, CA signed(x509) and Self signed certificate(x509) authentication to communicate with cloud.

## Features:

* The SDK supports to send telemetry data and receive commands from IoTConnect portal.
* User can update firmware Over The Air using "OTA update" Feature supported by SDK.
* SDK support x509 certificate authentication.  
* SDK consists of Gateway device with multiple child devices support.
* SDK supports to receive and update the Shadow/Twin property. 
* Provide device connection status receive by command.
* Support hard stop command to stop operations at device client from cloud.
* It allows sending the OTA & Normal command acknowledgment for Normal, Gateway and child device.
* It manages the sensor data sending flow over the cloud by using data frequency("df") configuration for normal devices and uses Tumbling Window("tw") to send data for Edge devices.
* It allows to disconnect the device from firmware.

# Example Usage:

Follow the steps given below to import our SDK as a library
- Open Visual Studio 2019/2022, Create new .net core Console App.
- Extract (get "nupkg" file from “iotconnect-sdk-dotnet-core-v1.14.0.zip”) the zip file and Create new folder called Nuget and place IOTConnect SDK Nuget package.
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
- cpId :: This need to get from the IoTConnect platform "Settings -> Key Vault".
- sId :: This need to get from the IoTConnect platform "Settings -> Key Vault".
    - NOTE: User can pass either cpId or sId. At least one param is required among these two.
- uniqueId :: This is device's unique ID which register on IotConnect platform and also its status must be Active and Acquired.
- env :: This need to get from the IoTConnect platform "Settings -> Key Vault".
- platform :: It should be either "aws" or "az".

**SDKOptions** is for the SDK configuration and need to parse in SDK object initialize call. You need to manage the below configuration as per your device authentications.

```c#
SDKOptions sdkOptions = new SDKOptions();

//For AWS
sdkOptions.Certificate.CACertificatePath = "<<filepath>>/device_cert.crt";
sdkOptions.Certificate.PrivateKeyCertificatePath = "<<filepath>>/device_cert_private_key.pem";
sdkOptions.Certificate.Password = "******";

//For AZ
sdkOptions.Certificate.CACertificatePath = "<<filepath>>/device_cert.pem";
sdkOptions.Certificate.PrivateKeyCertificatePath = "<<filepath>>/device_cert_private_key.key";
sdkOptions.Certificate.Password = "******";

//For Offline Storage only
sdkOptions.OfflineStorage.AvailSpaceInMb = <<Integer Value>>; //size in MB, Default value = unlimited
sdkOptions.OfflineStorage.FileCount = <<Integer Value>>; // Default value = 1
sdkOptions.OfflineStorage.Disabled = <<true/false>>; // //default value = false, false = store data, true = not store data 
```

- Certificate.CACertificatePath :: It is indicated to define the path of the device certificate file. Mandatory for X.509/Device CA signed or self-signed authentication type only.
- Certificate.PrivateKeyCertificatePath :: It is indicated to define the path of the device certificate's private key file. Mandatory for X.509/Device CA signed or self-signed authentication type only.
- Certificate.Password :: Optional, Need to provide this in case if password has been set while creating device certificates.

- OfflineStorage :: Define the configuration related to the offline data storage 
	- Disabled :: false = offline data storing, true = not storing offline data 
	- AvailSpaceInMb :: Define the file size of offline data which should be in (MB)
	- FileCount :: Number of max files can be created for offline data

**Note::** sdkOptions is optional but mandatory for SSL/x509 device authentication type only. Define proper setting or leave it NULL. If you do not provide offline storage, it will set the default settings as per defined above. It may harm your device by storing the large data. Once memory gets full may chance to stop the execution.

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
}
```

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
```

**Note::** Date time will be in ISO 8601 format as "YYYY-MM-DDTHH:MM:SS.sssZ". Example: "2019-12-24T10:06:17.857Z"  
"time" : Date format should be as defined //"2021-01-24T10:06:17.857Z"  
"data" : JSON data type format // {"temperature": 15.55, "gyroscope" : { 'x' : -1.2 }}  

To send an acknowledgment for device command   
```c#
var command = JsonConvert.DeserializeObject<DeviceCommand>(arg);  
client.SendAckCmd(command.Ack.Value, 6, "Ack done.!!!"); //This method allows device to send an acknowledgement for normal device.  
client.SendAckCmd(command.Ack.Value, 6, "Ack done for child device.!!!", "ChildId"); //Overloaded method to send an acknowledgement for child device only.  
```
#"ackGuid"  : Command Acknowledgment GUID which will receive from command payload. (data.ack)  
#"status"   : Acknowledgment status sent to cloud, must be 4 or 6. [4 = Failed, 6 = Executed Ack]  
#"msg"      : It is used to set custom acknowledgement message.  
"childId"   : It should be a child uniqueId for an acknowledgement to be sent for.  
		   	  How to get the "childId" .?  
		   		- You will get child uniqueId for child device from payload "data.id"  
**Note::**  : # indicates the mandatory element of the object.  

To send an acknowledgment for OTA Update command  
```c#
var command = JsonConvert.DeserializeObject<OTACommand>(arg);  
client.SendOTAAckCmd(command.Ack.Value, 7, "OTA Updated.!!!"); //This method allows device to send an acknowledgement for normal/gateway device.  
client.SendOTAAckCmd(command.Ack.Value, 7, "OTA Updated for child device.!!!", "ChildId"); //Overloaded method to send an acknowledgement for child device only.  
```
#"ackId"    : Command Acknowledgment GUID which will receive from command payload (data.ack)  
#"st        : Acknowledgment status sent to cloud, must be 4 or 7. [4 = Failed, 7 = Success]  
#"msg"      : It is used to set custom acknowledgement message  
"childId"   : It is used to send an acknowledgement for Gateway's child device  
		   	  How to get the "childId" .?  
		   		- You need to get child uniqueId based on tag from the response of 204 message and ota update message, Once OTA update completed for the specific child device in firmware.    
**Note::**  : # indicates the mandatory element of the object.  

To disconnect device and dispose the object of device from the cloud  
```c#
sdkClient.Dispose();
```

To get the Desired and Reported shadow/twin property  
```c#
sdkClient.GetShadows();
```

To get all the attributes  
```c#
sdkClient.GetAttributes();
```

# Dependencies:

* This SDK used below packages :
	- MQTTnet, Newtonsoft.Json, Microsoft.NETCore.App

# Integration Notes:

## Prerequisite tools

1. .NET 6.0 
2. Visual studio IDE

## Installation:

* Extract the "iotconnect-sdk-dotnet-core-v1.14.0.zip"

* Follow the steps given below to import our SDK as a library
	- Open Visual Studio 2022, Create new .net core Console App.
	- Extract (get "nupkg" file from “iotconnect-sdk-dotnet-core-v1.14.0.zip”) the zip file and Create new folder called Nuget and place IOTConnect SDK Nuget package.
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