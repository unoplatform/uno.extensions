using System;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Uno.Extensions.Configuration
{
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddAppSettings<TApplicationRoot>(this IConfigurationBuilder configurationBuilder)
            where TApplicationRoot : class
        {
            var generalAppSettingsFileName = $"{AppSettings.AppSettingsFileName}.json";
            var generalAppSettings =
                AppSettings.AllAppSettings<TApplicationRoot>()
                .FirstOrDefault(s => s.FileName.EndsWith(generalAppSettingsFileName, StringComparison.OrdinalIgnoreCase));

            if (generalAppSettings != null)
            {
                configurationBuilder.AddJsonStream(generalAppSettings.GetContent());
            }

            return configurationBuilder;
        }

        public static IConfigurationBuilder AddEnvironmentAppSettings<TApplicationRoot>(
            this IConfigurationBuilder configurationBuilder,
            string environmentName)
                where TApplicationRoot : class
        {
            var environmentAppSettingsFileName = $"{AppSettings.AppSettingsFileName}.{environmentName}.json";
            var environmentAppSettings =
                AppSettings.AllAppSettings<TApplicationRoot>()
                .FirstOrDefault(s => s.FileName.EndsWith(environmentAppSettingsFileName, StringComparison.OrdinalIgnoreCase));

            if (environmentAppSettings != null)
            {
                configurationBuilder.AddJsonStream(environmentAppSettings.GetContent());
            }

            return configurationBuilder;
        }
    }
}
