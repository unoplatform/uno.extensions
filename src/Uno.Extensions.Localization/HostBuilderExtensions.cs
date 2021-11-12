using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Uno.Extensions.Configuration;

namespace Uno.Extensions.Localization
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseLocalization(this IHostBuilder builder)
        {
            return builder
                .UseSettings<LocalizationSettings>(ctx => ctx.Configuration.GetSection(nameof(LocalizationSettings)))

                .ConfigureServices((ctx, services) =>
            {
                _ = services
                .AddHostedService<LocalizationService>()
                .AddSingleton<IStringLocalizer, ResourceLoaderStringLocalizer>();
            });
        }
    }
}
