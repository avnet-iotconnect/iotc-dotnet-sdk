/**
  ******************************************************************************
  * @file   : Firmware.cs 
  * @author : Softweb Solutions An Avnet Company
  * @modify : 09-OCT-2023
  * @brief  : Firmware part for DOTNET CORE SDK v1.14.0
  ******************************************************************************
*/

/**
 * Hope you have installed the DOTNET CORE SDK v1.14.0 as guided in README.md file or from documentation portal. 
 * Import the IoTConnect SDK package and other required packages
 */

using iotdotnetsdk.common;
using iotdotnetsdk.common.Models;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Security.Cryptography;
using System;

namespace IoTConnectSDK_Sample
{
    class Program
    {
        static SDKClient sdkClient;
        static void Main(string[] args)
        {
            /*
            ## Prerequisite parameter to run this sample code
            - cpId              :: It need to get from the IoTConnect platform "Settings->Key Vault".
            - uniqueId          :: Its device ID which register on IotConnect platform and also its status has Active and Acquired.
            - env               :: It need to get from the IoTConnect platform "Settings->Key Vault".
            - sdkOptions        :: It helps to define the path of self signed and CA signed certificate as well as define the offline storage params
            */
            string cpId = "<cpid>";
            string uniqueId = "<deviceUniqueId>";
            string env = "<env>";
            string sId = "<sId>";
            string platform = "<platform>";
            /** 
			* sdkOptions is optional. Mandatory for "certificate" X.509 device authentication type
			"certificate": It is indicated to define the path of the certificate file. Mandatory for X.509/SSL device CA signed or self-signed authentication type only.
				- SSL.Certificate: your device certificate
				- SSL.Password : certificate password
			"offlineStorage" : Define the configuration related to the offline data storage 
				- Disabled : false = offline data storing, true = not storing offline data 
				- AvailSpaceInMb : Define the file size of offline data which should be in (MB)
				- FileCount : Number of files need to create for offline data
			  Note: sdkOptions is optional but mandatory for SSL/x509 device authentication type only. Define proper setting or leave it NULL. If you do not provide offline storage, it will set the default settings as per defined above. It may harm your device by storing the large data. Once memory gets full may chance to stop the execution.
			*/

            SDKOptions sdkOptions = new SDKOptions();
            //For SSL CA signed and SelfSigned authorized device only
            sdkOptions.Certificate.CACertificatePath = "<<filepath>>/cert.pfx";
            sdkOptions.Certificate.PrivateKeyCertificatePath = "<<filepath>>/cert.pfx";
            sdkOptions.Certificate.Password = "******";
            //For Offline Storage only
            sdkOptions.OfflineStorage.AvailSpaceInMb = <<Integer Value>>; //size in MB, Default value = unlimited
            sdkOptions.OfflineStorage.FileCount = <<Integer Value>>; // Default value = 1
            sdkOptions.OfflineStorage.Disabled = <<true / false>>; // //default value = false, false = store data, true

            //Initialize device sdk client to connect device. 
            sdkClient = new SDKClient();
            sdkClient.Init(cpId, uniqueId, sId, env, platform, sdkOptions, FirmwareDelegate, ShadowUpdateCallBack);

            // Non Gateway
            string deviceData = "[{'uniqueId': '<< Device UniqueId >>','time' : '<< date >>','data': { }}]";

            // For Gateway and multiple child device 
            // string deviceData = "[{
            // 		'uniqueId': '<< Gateway Device UniqueId >>', // It should be must first object of the array
            // 		'time': '<< date >>',
            // 		'data': {}
            // 	},
            // 	{
            // 		'uniqueId':'<< Child DeviceId >>', //Child device
            // 		'time': '<< date >>',
            // 		'data': {}
            // 	}]";

            /**
			* Add your device attributes and respective value here as per standard format defined in sdk documentation
			* "time" : Date format should be as defined //"2021-01-24T10:06:17.857Z" 
			* "data" : JSON data type format // {"temperature": 15.55, "gyroscope" : { 'x' : -1.2 }}
			*/

            //Send data to device using SendData method.
            sdkClient.SendData(deviceData);

            /*
			Type    : Public Method "UpdateTwin()"
			Usage   : Update the twin reported property
			Input   : "key" and "value" as below
			Output  : 
			// string key = "<< Desired property key >>"; // Desired property key received from Twin callback message
			// string value = "<< Desired Property value >>"; // Value of respective desired property
			// sdkClient.UpdateTwin(key,value)

            /*
			Type    : Public Method "GetAllTwins()"
			Usage   : To get all the twin properties Desired and Reported
			Output  : All twin property will receive in callback function "TwinUpdateCallBack()"
			*/
            //	sdkClient.GetAllTwins();


            /*
			Type    : Public Method "GetAttributes()"
			Usage   : Send request to get all the attributes
			Input   :
			Output  :
			*/
            //  sdkClient.GetAttributes();

            /*
			Type    : Public Method "Dispose()"
			Usage   : Disconnect the device from cloud
			Note : It will disconnect the device after defined time
			//  sdkClient.Dispose();
			*/

            Console.ReadLine();
        }


        private static Task FirmwareDelegate(string arg)
        {
            Console.WriteLine($"FIRMWARE ::: FirmwareDelegate :::: {arg}");
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
                        sdkClient.UpdateShadow(item.Name, item.Value, Convert.ToString(version), PostShadowUpdateCallback);
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
    }
}