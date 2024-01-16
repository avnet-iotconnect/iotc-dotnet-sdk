using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Timers;
using iotdotnetsdk.common.Internals;
using iotdotnetsdk.common.Models;
using Timer = System.Timers.Timer;
using System.Data;

namespace iotcdotnetsdk.common.Internals
{
    internal class DiscoveryCommon
    {
        internal static double INTERNET_CHECK_INTERVAL = 65432;
        internal string discoveryUrl = "https://discovery.iotconnect.io/";
        internal static bool IsDataFreqEnable;
        internal string DevicePrimaryKey;
        internal static string internetCheckUrl = "http://google.com/generate_204";
        internal static bool DebugModeOn { get; private set; }
        internal static string DebugDir { get; set; }
        private static Timer _internetCheckTimer;
        private DiscoveryModel discoveryModel = null;

        internal void SetOptions(SDKOptions options, string sId, string uniqueId, string cpId = "", string environment = "")
        {
            IsDataFreqEnable = true;
            DevicePrimaryKey = options.DevicePK;

            if (!string.IsNullOrWhiteSpace(options.DiscoveryURL))
            {
                if (!Uri.IsWellFormedUriString(options.DiscoveryURL, UriKind.Absolute))
                    SDKCommon.Console_WriteError("Invalid discovery url - setting default.");
                else
                    discoveryUrl = options.DiscoveryURL;
            }

            if (!string.IsNullOrEmpty(cpId))
            {
                discoveryUrl = string.Concat(discoveryUrl, "/api/v2.1/dsdk/cpId/{0}/env/{1}");
            }
            else
            {
                discoveryUrl = string.Concat(discoveryUrl, "/api/v2.1/dsdk/sid/{0}");
            }
            //NOTE: For Device Authority testing
            //discoveryUrl = string.Concat(discoveryUrl, "/{0}");

            if (options is SDKAltOptions)
            {
                var altOpt = options as SDKAltOptions;
                if (!string.IsNullOrWhiteSpace(altOpt.InternetCheckUrl))
                {
                    if (!Uri.IsWellFormedUriString(altOpt.InternetCheckUrl, UriKind.Absolute))
                        SDKCommon.Console_WriteError("Invalid internet check url - setting default.");
                    else
                        internetCheckUrl = altOpt.InternetCheckUrl;
                }
            }

            if (options is IDebugOption)
            {
                DebugModeOn = (options as IDebugOption).IsDebug;
                DebugDir = Path.Combine(Environment.CurrentDirectory, $@"logs\debug");
                if (DebugModeOn)
                {
                    try
                    {
                        Directory.CreateDirectory(DebugDir);
                    }
                    catch (Exception ex)
                    {
                        SDKCommon.Console_WriteLine($"Debug file generation will be disabled. {ex.Message}");
                        DebugDir = string.Empty;
                    }
                }
                IsDataFreqEnable = (options as IDebugOption).IsDataFreqEnable.HasValue ? (options as IDebugOption).IsDataFreqEnable.Value : true;
            }

            //_internetCheckTimer = new System.Timers.Timer(INTERNET_CHECK_INTERVAL);
            //_internetCheckTimer.Elapsed += SDKCommon._queueTimer_Elapsed;
            //_internetCheckTimer.Enabled = false;

            if (!options.OfflineStorage.Disabled)
            {
                var folderName = (!string.IsNullOrEmpty(sId) ? $"{sId}_{uniqueId}" : $"{cpId}_{uniqueId}");
                options.OfflineStorage.LogDir = Path.Combine(Environment.CurrentDirectory, $@"logs\offline\{folderName}");

                try
                {
                    Directory.CreateDirectory(options.OfflineStorage.LogDir);
                }
                catch (Exception ex)
                {
                    SDKCommon.Console_WriteLine($"Log file generation will be disabled. {ex.Message}");
                    options.OfflineStorage.Disabled = true;
                }
            }
        }

        internal string SyncUrl
        {
            get
            {
                return Path.Combine(discoveryModel.D.Bu, "uid");

                //NOTE: For Device Authority testing
                //return discoveryModel.D.Bu;
            }
        }

        internal void Discover(string sId, string platform, string cpId = "", string environment = "")
        {
            if (discoveryModel == null)
            {
                string url = "";
                if (!string.IsNullOrEmpty(cpId))
                {
                    url = string.Format(discoveryUrl, new object[] { cpId, environment });
                }
                else
                {
                    url = string.Format(discoveryUrl, sId);
                }
                SDKCommon.Console_WriteLine($"Discovering with {url}");

                Uri myUri = new Uri(url, UriKind.Absolute);
                myUri = myUri.AddParameter("version", "v2.1");
                myUri = myUri.AddParameter("pf", platform);

                try
                {
                    var apiResponse = SDKCommon.ApiCall(myUri.ToString(), HttpMethod.Get, new Dictionary<string, string>() { }, null);

                    if (apiResponse.StatusCode == HttpStatusCode.OK)
                    {
                        StreamReader reader = new StreamReader(apiResponse.GetResponseStream());
                        discoveryModel = JsonConvert.DeserializeObject<DiscoveryModel>(Convert.ToString(reader.ReadToEnd()));
                    }
                }
                catch (Exception ex)
                {
                    SDKCommon.Console_WriteLine($"Error in Discovery:{ex.Message}");
                    throw new DiscoveryException(ex.Message);
                }
            }

            SDKCommon.Console_WriteLine($"Discovery Done: Base URL : {discoveryModel.D.Bu}");
        }
    }
}