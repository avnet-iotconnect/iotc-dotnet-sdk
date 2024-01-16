using iotcdotnetsdk.common.Internals;

using iotdotnetsdk.common.Models;

using Newtonsoft.Json;

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Net;
using System.Reflection;
using System.Text;
using System.Timers;
using System.Web;

namespace iotdotnetsdk.common.Internals
{
    internal static class SDKCommon
    {
        internal static double INTERNET_CHECK_INTERVAL = 65432;
        private static readonly ConcurrentQueue<Action> actionQueue = new ConcurrentQueue<Action>();
        private static System.Timers.Timer _internetCheckTimer;

        static SDKCommon()
        {
            _internetCheckTimer = new System.Timers.Timer(INTERNET_CHECK_INTERVAL);
            _internetCheckTimer.Elapsed += _queueTimer_Elapsed;
            _internetCheckTimer.Enabled = false;
        }

        internal static HttpWebResponse ApiCall(string url, HttpMethod method, Dictionary<string, string> headers = null, string strParams = "")
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = method.ToString();
            request.ContentType = "application/json";
            request.Accept = "application/json";

            if (headers != null && headers.Count > 0)
            {
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            if (!string.IsNullOrEmpty(strParams))
            {
                ASCIIEncoding encoding = new ASCIIEncoding();
                byte[] bytes = encoding.GetBytes(strParams);
                request.ContentLength = bytes.Length;
                var requestStream = request.GetRequestStream();
                requestStream.Write(bytes, 0, bytes.Length);
            }

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            return response;
        }

        internal static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute),
                false);

            if (attributes != null && attributes.Length > 0)
                return attributes[0].Description;
            else
                return value.ToString();
        }

        public static bool IsInternetConnected
        {
            get
            {
                try
                {

                    Console_WriteLine("Checking internet connectivity.");
                    using (var client = new WebClient())
                    using (client.OpenRead(DiscoveryCommon.internetCheckUrl))
                    {
                        Console_WriteLine("Checking internet connectivity - Available");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console_WriteLine($"Error in internet checking : {ex.Message}");
                    return false;
                }
            }
        }

        internal static void Console_WriteError(string message)
        {
            Console.WriteLine($"{DateTime.Now.ToUniversalTime().ToString("o")}: ERROR: {message}");

            if (DiscoveryCommon.DebugModeOn && (!string.IsNullOrWhiteSpace(DiscoveryCommon.DebugDir)))
            {
                string fileName = Path.Combine(DiscoveryCommon.DebugDir, "error.txt");
                try
                {
                    File.AppendAllText(fileName, $"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}: ERROR: {message}{Environment.NewLine}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while addding logs in error.txt : {ex.Message}");
                }
            }
        }

        internal static void Console_WriteLine(string message)
        {
            Console.WriteLine($"{DateTime.Now.ToUniversalTime().ToString("o")}: {message}");
            if (DiscoveryCommon.DebugModeOn && (!string.IsNullOrWhiteSpace(DiscoveryCommon.DebugDir)))
            {
                string fileName = Path.Combine(DiscoveryCommon.DebugDir, "info.txt");
                try
                {
                    File.AppendAllText(fileName, $"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}: Into: {message}{Environment.NewLine}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while addding logs in info.txt : {ex.Message}");
                }
            }
        }

        internal static void _queueTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!IsInternetConnected)
            {
                Console_WriteLine("No Internet Found..");
                return;
            }

            Console_WriteLine("Internet Found. Looking for callbacks");

            while (actionQueue.TryDequeue(out Action callback))
            {
                Console_WriteLine("Callback found... Executing...");
                callback();
                System.Threading.Thread.Sleep(2000);
            }

            if (actionQueue.Count == 0)
            {
                _internetCheckTimer.Stop();
                _internetCheckTimer.Enabled = false;
                Console_WriteLine("Timer Disabled and Stopped");
            }
        }

        internal static void CallOnInternet(Action rcAction)
        {
            actionQueue.Enqueue(rcAction);
            Console_WriteLine("Enqueued.");

            if (!_internetCheckTimer.Enabled)
            {
                Console_WriteLine("Timer Enabled and Started");
                _internetCheckTimer.Enabled = true;
                _internetCheckTimer.Start();
            }
        }

        internal static void WriteConnStatus(string deviceId, string status)
        {
            try
            {
                string fileName = Path.Combine(Directory.GetCurrentDirectory(), "logs", deviceId, "status.txt");
                if (File.Exists(fileName))
                {
                    File.AppendAllText(fileName, $"{status} - {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}{Environment.NewLine}");
                }
            }
            catch (Exception ex)
            {
                Console_WriteError("Error while writing CONNECTION STATUS for device : " + deviceId + ". message : " + ex.Message);
            }
        }

        public static Uri AddParameter(this Uri url, string paramName, string paramValue)
        {
            var uriBuilder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query[paramName] = paramValue;
            uriBuilder.Query = query.ToString();

            return uriBuilder.Uri;
        }
    }
}