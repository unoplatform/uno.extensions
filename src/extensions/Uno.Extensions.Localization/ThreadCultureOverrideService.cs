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
        private readonly CultureInfo _settingsCulture;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadCultureOverrideService"/> class.
        /// </summary>
        /// <param name="uiThread">UI thread</param>
        /// <param name="supportedLanguages">Supported languages</param>
        /// <param name="fallbackCulture">Fallback culture</param>
        /// <param name="settingsCulture">Culture read from settings</param>
        /// <param name="logger">Logger</param>
        public ThreadCultureOverrideService(
            Thread uiThread,
            string[] supportedLanguages,
            CultureInfo settingsCulture,
            CultureInfo fallbackCulture,
            ILogger<ThreadCultureOverrideService> logger = null
        )
        {
            _uiThread = uiThread ?? throw new ArgumentNullException(nameof(uiThread));
            _supportedLanguages = supportedLanguages ?? throw new ArgumentNullException(nameof(supportedLanguages));
            _fallbackCulture = fallbackCulture ?? throw new ArgumentNullException(nameof(supportedLanguages));
            _settingsCulture = settingsCulture;
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
                // Pick language from supported languages
                // Check first for culture saved to settings
                // If no culture set, then use CurrentCulture
                var language = (_settingsCulture is not null) ?
                    PickSupportedLanguage(_settingsCulture) :
                    PickSupportedLanguage(CultureInfo.CurrentCulture);

                // Use the fallback culture if the language is not supported.
                var culture = (!string.IsNullOrWhiteSpace(language)) ?
                                    new CultureInfo(language) :
                                    _fallbackCulture;

                // Override the current thread culture
                _uiThread.CurrentCulture = culture;
                _uiThread.CurrentUICulture = culture;

                // Override any new thread culture
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
                Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = culture.Name;

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to apply the culture override.");

                return false;
            }
        }

        private string PickSupportedLanguage(CultureInfo culture)
            => _supportedLanguages.FirstOrDefault(l => l.StartsWith(culture.TwoLetterISOLanguageName, StringComparison.InvariantCultureIgnoreCase));
    }
}
