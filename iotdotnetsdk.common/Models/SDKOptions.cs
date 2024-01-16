using System;
using System.IO;
using System.Linq;

namespace iotdotnetsdk.common.Models
{
    public class SDKOptions
    {
        public SDKOptions()
        {
            Certificate = new CertificateInfo();
            DpsInfo = new DpsInfo();
            OfflineStorage = new OfflineStorageInfo()
            {
                Disabled = false
            };
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Debug { get; set; } = false;

        /// <summary>
        /// 
        /// </summary>
        public string DiscoveryURL { get; set; }

        /// <summary>
        /// Device symmetric key for authType = 5 (SymmetricKey)
        /// </summary>
        public string DevicePK { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool SkipValidation { get; set; } = false;

        /// <summary>
        /// 
        /// </summary>
        public bool KeepAlive { get; set; } = true;

        /// <summary>
        /// Class to represents Certificate infromation
        /// </summary>
        public CertificateInfo Certificate { get; set; }

        /// <summary>
        /// Class to represents offline storage control option
        /// </summary>
        public OfflineStorageInfo OfflineStorage { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DpsInfo DpsInfo { get; set; }


    }

    public class CertificateInfo
    {
        public string? Password { get; set; }
        public string CACertificatePath { get; set; }
        public string PrivateKeyCertificatePath { get; set; }
        public string CACertificateContent { get; set; }
        public string PrivateKeyCertificateContent { get; set; }
    }

    public class DpsInfo
    {
        public string ScopeId { get; set; }
        public string GlobalEndpoint { get; set; }
    }

    public class OfflineStorageInfo
    {
        private int availSpaceInMb = -1;
        private int fileCount = 1;
        private float singleFileSize = 0;
        /// <summary>
        /// Available space in MB to store log file. Default umlimited
        /// </summary>
        public int AvailSpaceInMb
        {
            get { return availSpaceInMb; }
            set { availSpaceInMb = value; }
        }

        /// <summary>
        /// Number of logger files to be created. Default 1
        /// </summary>
        public int FileCount
        {
            get { return fileCount; }
            set { fileCount = value; }
        }

        /// <summary>
        /// A boolean value to control turn on /off offline storage
        /// </summary>
        public bool Disabled { get; set; }

        internal string CurrentFileName { get; set; }
        internal string LogDir { get; set; }

        internal void ManageSpace()
        {
            var files = (new DirectoryInfo(LogDir)).EnumerateFiles().ToList();
            if (files.Count == 0) { CurrentFileName = $"Active_{DateTime.UtcNow.Ticks}.txt"; }

            //unlimited space
            if (availSpaceInMb == -1 || files.Count == 0) return;

            if (string.IsNullOrWhiteSpace(CurrentFileName)) CurrentFileName = files.Select(fi => fi.Name).FirstOrDefault(n => n.Contains("Active_"));

            if (singleFileSize == 0) { singleFileSize = (availSpaceInMb * 1024) / FileCount; }

            //files.Count
            var space = (files.Sum(fi => fi.Length) / 1024f);

            //If dir space is higher then available then no space!
            if (availSpaceInMb * 1024 < space)
            {
                File.Delete(Directory.GetFiles(LogDir).ToList().OrderBy(fn => fn).FirstOrDefault());
            }

            var avg = files.Average(fi => fi.Length) / 1024f;
            if (avg > singleFileSize)
            {
                File.Move(Path.Combine(LogDir, CurrentFileName), Path.Combine(LogDir, CurrentFileName.Replace("Active_", string.Empty)));
                CurrentFileName = $"Active_{DateTime.UtcNow.Ticks}.txt";
            }
        }
    }

    public class SDKAltOptions : SDKOptions
    {
        /// <summary>
        /// A string represents the internet check url
        /// </summary>
        public string InternetCheckUrl { get; set; }
        /// <summary>
        /// Double represents the interval in milliseconds
        /// </summary>
        public double InternetCheckInterval { get; set; }
    }


    public interface IDebugOption
    {
        bool IsDebug { get; set; }
        bool? IsDataFreqEnable { get; set; }
    }
    public class SDKDebugOption : SDKOptions, IDebugOption
    {
        /// <summary>
        /// To generate operation log
        /// </summary>
        public bool IsDebug { get; set; }

        /// <summary>
        /// Represent data frequency set for device template applicable or not.Default applicable
        /// </summary>
        public bool? IsDataFreqEnable { get; set; }
    }

    public class SDKAltDebugOption : SDKAltOptions, IDebugOption
    {
        /// <summary>
        /// To generate operation log
        /// </summary>
        public bool IsDebug { get; set; }

        /// <summary>
        /// Represent data frequency set for device template applicable or not.Default applicable
        /// </summary>
        public bool? IsDataFreqEnable { get; set; }
    }
}