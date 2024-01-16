using iotcdotnetsdk.common;

using iotdotnetsdk.common.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace iotdotnetsdk.common.Internals
{
    internal sealed class LocalStorageManager
    {
        private static LocalStorageManager _instance;
        static readonly object _lock = new object();
        static readonly object _fileLock = new object();

        private LocalStorageManager()
        {

        }

        public static LocalStorageManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new LocalStorageManager();
                        }
                    }
                }
                return _instance;
            }
        }

        

        public List<string> GetDeviceData(string uniqueId, OfflineStorageInfo oso)
        {
            if (oso.Disabled) return null;
            var fileNames = Directory.GetFiles(oso.LogDir);
            if (fileNames.Count() == 0) return null;

            var fn = fileNames.OrderByDescending(f => f).FirstOrDefault();
            {
                if (!string.IsNullOrWhiteSpace(oso.CurrentFileName) && oso.CurrentFileName.Equals(fn)) return null;

                List<string> data = new List<string>();
                foreach (var lb in File.ReadAllLines(Path.Combine(oso.LogDir, fn)))
                {
                    data.Add(lb);
                }
                try
                {
                    File.Delete(Path.Combine(oso.LogDir, fn));
                }
                catch
                {

                }
                return data;
            }
        }

        public void AddDeviceData(string uniqueId, string data, Models.OfflineStorageInfo oso)
        {
            lock (_fileLock)
            {
                if (oso.Disabled) return;
                oso.ManageSpace();
                File.AppendAllText(Path.Combine(oso.LogDir, oso.CurrentFileName), $"{data}{Environment.NewLine}");
            }
        }
    }
}