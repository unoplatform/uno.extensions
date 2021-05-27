using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Uno.Extensions.Configuration;
using Windows.ApplicationModel.Resources;

namespace Uno.Extensions.Localization
{
    public class LocalizationSettings
    {
        public string[] Cultures { get; set; }
        public string CurrentCulture { get; set; }
    }


    public static class ServiceCollectionExtensions
    {
        public static IHostBuilder UseLocalization(this IHostBuilder builder)
        {
            return builder
                .UseWritableSettings<LocalizationSettings>(ctx => ctx.Configuration.GetSection(nameof(LocalizationSettings)))

                .ConfigureServices((ctx, services) =>
            {
                services
                .AddHostedService<LocalizationService>()
                .AddSingleton<IStringLocalizer, ResourceLoaderStringLocalizer>();
            });
        }
    }

    public class LocalizationService : IHostedService
    {
        private ThreadCultureOverrideService _cultureOverrideService;

        private IWritableOptions<LocalizationSettings> Settings { get; }

        private CultureInfo[] SupportedCultures => !(Settings?.Value?.Cultures?.Any() ?? false) ?
            new[] { new CultureInfo("en-US") } :
            Settings.Value.Cultures.Select(c => new CultureInfo(c)).ToArray();

        public LocalizationService(IWritableOptions<LocalizationSettings> settings)
        {
            Settings = settings;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _cultureOverrideService = new ThreadCultureOverrideService(
                Thread.CurrentThread,
                SupportedCultures.Select(c => c.TwoLetterISOLanguageName).ToArray(),
                SupportedCultures.First()
            );

            _cultureOverrideService.TryApply();

            //-:cnd:noEmit
#if NET461
//+:cnd:noEmit
            // This is required for test projects otherwise the ResourceLoader will throw an exception.
            Windows.ApplicationModel.Resources.ResourceLoader.DefaultLanguage = SupportedCultures.First().Name;
//-:cnd:noEmit
#endif
            //+:cnd:noEmit
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
        }
    }

    /// <summary>
    /// This implementation of <see cref="IStringLocalizer"/> uses <see cref="ResourceLoader"/>
    /// to get the string resources.
    /// </summary>
    public class ResourceLoaderStringLocalizer : IStringLocalizer
    {
        private const string SearchLocation = "Resources";
        private readonly ResourceLoader _resourceLoader;
        private readonly bool _treatEmptyAsNotFound;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceLoaderStringLocalizer"/> class.
        /// </summary>
        /// <param name="treatEmptyAsNotFound">If empty strings should be treated as not found.</param>
        public ResourceLoaderStringLocalizer(bool treatEmptyAsNotFound = true)
        {
            _treatEmptyAsNotFound = treatEmptyAsNotFound;
            _resourceLoader = ResourceLoader.GetForViewIndependentUse();
        }

        /// <inheritdoc/>
        public LocalizedString this[string name] => GetLocalizedString(name);

        /// <inheritdoc/>
        public LocalizedString this[string name, params object[] arguments] => GetLocalizedString(name, arguments);

        private LocalizedString GetLocalizedString(string name, params object[] arguments)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var resource = _resourceLoader.GetString(name);

            if (_treatEmptyAsNotFound && string.IsNullOrEmpty(resource))
            {
                resource = null;
            }

            resource = resource ?? name;

            var value = arguments.Any()
                ? string.Format(CultureInfo.CurrentCulture, resource, arguments)
                : resource;

            return new LocalizedString(name, value, resourceNotFound: resource == null, searchedLocation: SearchLocation);
        }

        /// <inheritdoc/>
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => throw new NotSupportedException("ResourceLoader doesn't support listing all strings.");

        /// <inheritdoc/>
        public IStringLocalizer WithCulture(CultureInfo culture) =>
            throw new NotSupportedException("This method is obsolete.");
    }

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
