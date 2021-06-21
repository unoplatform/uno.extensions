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
}
