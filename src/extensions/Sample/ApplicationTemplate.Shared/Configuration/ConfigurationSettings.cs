using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ApplicationTemplate
{
    public static class ConfigurationSettings
    {
        /// <summary>
        /// Adding a cache to the settings as an application restart is required
        /// to see the changes applied by these settings.
        /// </summary>
        private static readonly Dictionary<string, bool> _cache = new Dictionary<string, bool>();

        /// <summary>
        /// Gets whether or not a setting is enabled.
        /// This method checks the presence of a file on disk which
        /// is faster than resolving any service and deserialize the setting content.
        /// </summary>
        /// <param name="settingFileName">File name of the setting. This must be unique.</param>
        /// <param name="defaultValue">Default value if the setting has not been set.</param>
        /// <returns>True if the setting is enabled, false otherwise.</returns>
        public static bool GetIsSettingEnabled(string settingFileName, bool defaultValue)
        {
            var result = defaultValue;

            if (_cache.TryGetValue(settingFileName, out var cachedResult))
            {
                result = cachedResult;
            }
            else
            {
                var filePath = GetSettingsFilePath(settingFileName);

                if (File.Exists(filePath))
                {
                    var bytes = File.ReadAllBytes(filePath);

                    if (bytes.Length == 1)
                    {
                        result = Convert.ToBoolean(bytes.Single());
                    }
                }

                _cache[settingFileName] = result;
            }

            return result;
        }

        /// <summary>
        /// Sets whether or not a setting is enabled.
        /// </summary>
        /// <param name="settingFileName">File name of the setting. This must be unique.</param>
        /// <param name="isEnabled">Is the setting enabled.</param>
        public static void SetIsSettingEnabled(string settingFileName, bool isEnabled)
        {
            var filePath = GetSettingsFilePath(settingFileName);

            var byteValue = Convert.ToByte(isEnabled);

            File.WriteAllBytes(filePath, new byte[] { byteValue });
        }

        private static string GetSettingsFilePath(string fileName)
        {
//-:cnd:noEmit
#if !WINUI
//+:cnd:noEmit
            var folderPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
//-:cnd:noEmit
#elif __ANDROID__ || __IOS__
//+:cnd:noEmit
            var folderPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
//-:cnd:noEmit
#else
//+:cnd:noEmit
            var folderPath = string.Empty;
//-:cnd:noEmit
#endif
//+:cnd:noEmit

            return Path.Combine(folderPath, fileName);
        }
    }
}
