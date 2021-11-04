using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Uno.Extensions.Configuration;

namespace Uno.Extensions.Localization
{
    public class LocalizationService : IHostedService
    {
        private ThreadCultureOverrideService? _cultureOverrideService;

        public LocalizationService(IWritableOptions<LocalizationSettings> settings)
        {
            Settings = settings;
        }

        private IWritableOptions<LocalizationSettings> Settings { get; }

        private CultureInfo[] SupportedCultures => !(Settings?.Value?.Cultures?.Any() ?? false) ?
            new[] { new CultureInfo("en-US") } :
            Settings.Value.Cultures.Select(c => new CultureInfo(c)).ToArray();

        private CultureInfo CurrentCulture => (Settings?.Value?.CurrentCulture is not null) ?
            new CultureInfo(Settings.Value.CurrentCulture) :
            null;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cultureOverrideService = new ThreadCultureOverrideService(
                Thread.CurrentThread,
                SupportedCultures.Select(c => c.TwoLetterISOLanguageName).ToArray(),
                CurrentCulture,
                SupportedCultures.First()
            );

            _cultureOverrideService.TryApply();

#if NET461
            // This is required for test projects otherwise the ResourceLoader will throw an exception.
            Windows.ApplicationModel.Resources.ResourceLoader.DefaultLanguage = SupportedCultures.First().Name;
#endif
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
