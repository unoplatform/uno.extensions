using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Uno.Extensions.Hosting;

namespace Uno.Extensions.Configuration
{
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddAppSettings(this IConfigurationBuilder configurationBuilder, HostBuilderContext hostingContext)
        {
            var prefix = hostingContext.Configuration.GetValue(HostingConstants.AppSettingsPrefixKey, defaultValue: string.Empty);
            prefix = !string.IsNullOrWhiteSpace(prefix) ? $"{prefix}/" : prefix;
            return configurationBuilder.AddJsonFile($"{prefix}appsettings.json", optional: true, reloadOnChange: false);
        }
        public static IConfigurationBuilder AddEmbeddedAppSettings<TApplicationRoot>(this IConfigurationBuilder configurationBuilder)
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

        public static IConfigurationBuilder AddEnvironmentAppSettings(this IConfigurationBuilder configurationBuilder, HostBuilderContext hostingContext)
        {
            var env = hostingContext.HostingEnvironment;

            var prefix = hostingContext.Configuration.GetValue(HostingConstants.AppSettingsPrefixKey, defaultValue: string.Empty);
            prefix = !string.IsNullOrWhiteSpace(prefix) ? $"{prefix}/" : prefix;
            return configurationBuilder.AddJsonFile($"{prefix}appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false);
        }

        public static IConfigurationBuilder AddEmbeddedEnvironmentAppSettings<TApplicationRoot>(
            this IConfigurationBuilder configurationBuilder, HostBuilderContext hostingContext)
                where TApplicationRoot : class
        {
            var env = hostingContext.HostingEnvironment;

            var environmentAppSettingsFileName = $"{AppSettings.AppSettingsFileName}.{env.EnvironmentName}.json";
            var environmentAppSettings =
                AppSettings.AllAppSettings<TApplicationRoot>()
                .FirstOrDefault(s => s.FileName.EndsWith(environmentAppSettingsFileName, StringComparison.OrdinalIgnoreCase));

            if (environmentAppSettings != null)
            {
                configurationBuilder.AddJsonStream(environmentAppSettings.GetContent());
            }

            return configurationBuilder;
        }

        public static IConfigurationBuilder AddSectionFromEntity<TEntity>(
            this IConfigurationBuilder configurationBuilder,
            TEntity entity,
            string? sectionName = null)
        {
            return configurationBuilder
                .AddJsonStream(
                    new MemoryStream(
                        Encoding.ASCII.GetBytes(
                            JsonSerializer.Serialize(
                                new Dictionary<string, TEntity>
                                {
                                    { sectionName ?? typeof(TEntity).Name, entity }
                                }))));
        }
    }
}
