using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Uno.Extensions.Configuration
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configures a Configuration section to be exposed to the
        /// application as either IOptions<typeparamref name="T"/> (static)
        /// or as IWriteableOptions<typeparamref name="T"/> which can be
        /// updated and persisted (aka application settings).
        /// </summary>
        /// <typeparam name="T">The DTO that the Configuration section will be deserialized to.</typeparam>
        /// <param name="services">The Microsoft.Extensions.DependencyInjection.IServiceCollection to add the services to.</param>
        /// <param name="section">The Microsoft.Extensions.Configuration.IConfigurationSection to retrieve.</param>
        /// <param name="file">The full path to the file where updated section data will be written.</param>
        /// <returns>The Microsoft.Extensions.DependencyInjection.IServiceCollection so that additional calls can be chained.</returns>
        public static IServiceCollection ConfigureAsWritable<T>(
            this IServiceCollection services,
            IConfigurationSection section,
            string file = null)
                where T : class, new()
        {
            return services
                .Configure<T>(section)
                .AddTransient<IWritableOptions<T>>(provider =>
                {
                    var logger = provider.GetService<ILogger<IWritableOptions<T>>>();
                    var root = provider.GetService<Reloader>();
                    var options = provider.GetService<IOptionsMonitor<T>>();
                    return new WritableOptions<T>(logger, root, options, section.Key, file);
                });
        }
    }
}
