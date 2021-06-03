using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Uno.Extensions.Localization
{
    public class ThreadCultureOverrideService
    {
        private readonly Thread _uiThread;
        private readonly string[] _supportedLanguages;
        private readonly CultureInfo _fallbackCulture;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadCultureOverrideService"/> class.
        /// </summary>
        /// <param name="uiThread">UI thread</param>
        /// <param name="supportedLanguages">Supported languages</param>
        /// <param name="fallbackCulture">Fallback culture</param>
        /// <param name="settingFilePath">Path to the file where the preference will be stored</param>
        /// <param name="logger">Logger</param>
        public ThreadCultureOverrideService(
            Thread uiThread,
            string[] supportedLanguages,
            CultureInfo fallbackCulture,
            ILogger<ThreadCultureOverrideService> logger = null
        )
        {
            _uiThread = uiThread ?? throw new ArgumentNullException(nameof(uiThread));
            _supportedLanguages = supportedLanguages ?? throw new ArgumentNullException(nameof(supportedLanguages));
            _fallbackCulture = fallbackCulture ?? throw new ArgumentNullException(nameof(supportedLanguages));
            _logger = logger ?? NullLogger<ThreadCultureOverrideService>.Instance;
        }

        /// <summary>
        /// If there was a culture override set using the <see cref="SetCulture"/> method,
        /// then this method will apply the culture override on top of the system culture.
        /// </summary>
        /// <returns>True if the culture was overwritten; false otherwise.</returns>
        public bool TryApply()
        {
            try
            {
                var culture = CultureInfo.CurrentCulture;

                // Use the fallback culture if the language is not supported.
                if (!_supportedLanguages.Any(l => l.StartsWith(culture.TwoLetterISOLanguageName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    culture = _fallbackCulture;
                }

                // Override the current thread culture
                _uiThread.CurrentCulture = culture;
                _uiThread.CurrentUICulture = culture;

                // Override any new thread culture
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to apply the culture override.");

                return false;
            }
        }
    }
}
