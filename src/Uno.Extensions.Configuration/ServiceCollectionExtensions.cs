using System.Collections.Immutable;
using System.ComponentModel;

namespace Uno.Extensions.Configuration;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
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
	/// <param name="name">The named options value to register</param>
	/// <returns>The Microsoft.Extensions.DependencyInjection.IServiceCollection so that additional calls can be chained.</returns>
	public static IServiceCollection ConfigureAsWritable<T>(
		this IServiceCollection services,
		IConfigurationSection section,
		string file,
		string? name = "")
			where T : class, new()
	{
		return services
			// Note - we've replaced the Configure method call with the three calls subsequent three calls so that
			// we can use a local copy of ConfigurationBinder that handles ImmutableList
			//.Configure<T>(section)
			.AddOptions()
			.AddSingleton<IOptionsChangeTokenSource<T>>(new ConfigurationChangeTokenSource<T>(name ?? Options.DefaultName, section))
			.AddSingleton<IConfigureOptions<T>>(new Uno.Extensions.Configuration.Internal.NamedConfigureFromConfigurationOptions<T>(name ?? Options.DefaultName, section, _ => { }))

			.AddTransient<IWritableOptions<T>>(provider =>
			{
				var logger = provider.GetRequiredService<ILogger<IWritableOptions<T>>>();
				var root = provider.GetRequiredService<Reloader>();
				var options = provider.GetRequiredService<IOptionsMonitor<T>>();
				return new WritableOptions<T>(logger, root, options, section.Key, file);
			});
	}
}

