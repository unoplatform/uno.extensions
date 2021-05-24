using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Uno.Extensions.Configuration
{
    public class AppSettings
    {
        public const string AppSettingsFileName = "appsettings";

        //-:cnd:noEmit
#if PRODUCTION
//+:cnd:noEmit
		public const string DefaultEnvironment = "PRODUCTION";
//-:cnd:noEmit
#elif DEBUG
        //+:cnd:noEmit
        public const string DefaultEnvironment = "DEVELOPMENT";
        //-:cnd:noEmit
#else
//+:cnd:noEmit
		public const string DefaultEnvironment = "STAGING";
//-:cnd:noEmit
#endif
        //+:cnd:noEmit

        private static AppSettings[] _appSettings;

        private readonly Assembly _assembly;
        private readonly Lazy<string> _environment;

        public AppSettings(string fileName, Assembly assembly)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            _environment = new Lazy<string>(GetEnvironment);
        }

        public string FileName { get; }

        public string Environment => _environment.Value;

        private string GetEnvironment()
        {
            var environmentMatch = Regex.Match(FileName, "appsettings.(\\w+).json");

            return environmentMatch.Groups.Count > 1
                ? environmentMatch.Groups[1].Value
                : null;
        }

        public Stream GetContent()
        {
            using (var resourceFileStream = _assembly.GetManifestResourceStream(FileName))
            {
                if (resourceFileStream != null)
                {
                    var memoryStream = new MemoryStream();

                    resourceFileStream.CopyTo(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    return memoryStream;
                }

                return null;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1307:Specify StringComparison", Justification = "Not available for desktop.")]
        public static AppSettings[] AllAppSettings<TApplicationRoot>()
             where TApplicationRoot : class
        {
            if (_appSettings == null)
            {
                var executingAssembly = typeof(TApplicationRoot).Assembly;

                _appSettings = executingAssembly
                    .GetManifestResourceNames()
                    .Where(fileName => fileName.ToUpperInvariant().Contains(AppSettingsFileName.ToUpperInvariant()))
                    .Select(fileName => new AppSettings(fileName, executingAssembly))
                    .ToArray();
            }

            return _appSettings;
        }
    }

    public static class ServiceCollectionExtensions
    {

        public static IHostBuilder UseHostConfigurationForApp(this IHostBuilder hostBuilder)
        {
            return hostBuilder.ConfigureServices((ctx, s) =>
                s.AddSingleton(a => ctx.Configuration)
            );
        }

        public static IHostBuilder UseAppSettingsForHostConfiguration<TApplicationRoot>(this IHostBuilder hostBuilder)
            where TApplicationRoot : class
        {
            return hostBuilder.ConfigureHostConfiguration(b =>
                b.AddAppSettings< TApplicationRoot>()
            );
        }

        public static IHostBuilder UseAppSettings<TApplicationRoot>(this IHostBuilder hostBuilder)
            where TApplicationRoot : class
        {
            return hostBuilder.ConfigureAppConfiguration(b =>
                b.AddAppSettings<TApplicationRoot>()
            );
        }

        public static IHostBuilder UseEnvironmentAppSettings<TApplicationRoot>(this IHostBuilder hostBuilder)
            where TApplicationRoot : class
        {
            return hostBuilder.ConfigureAppConfiguration((ctx,b) =>
            {
                IHostEnvironment env = ctx.HostingEnvironment;
                b.AddEnvironmentAppSettings<TApplicationRoot>(env.EnvironmentName ?? Environments.Production); // This is consistent with HostBuilder defaults
            }
            );
        }

        private static IConfigurationBuilder AddAppSettings<TApplicationRoot>(this IConfigurationBuilder configurationBuilder)
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

        private static IConfigurationBuilder AddEnvironmentAppSettings<TApplicationRoot>(this IConfigurationBuilder configurationBuilder, string environmentName)
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
