using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Serialization;

/// <summary>
/// This class is used for serialization configuration.
/// - Configures the serializers.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Adds the serialization services to the <see cref="IServiceCollection"/>.
	/// </summary>
	/// <param name="services">Service collection.</param>
	/// <returns><see cref="IServiceCollection"/>.</returns>
	public static IServiceCollection AddSystemTextJsonSerialization(
		this IServiceCollection services)
	{
		return services
			.AddSingleton(sp => new JsonSerializerOptions
			{
				NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
				AllowTrailingCommas = true
			})
			.AddSingleton<SystemTextJsonStreamSerializer>()
			.AddSingleton<ISerializer>(services => services.GetRequiredService<SystemTextJsonStreamSerializer>())
			.AddSingleton(typeof(ISerializer<>), typeof(SystemTextJsonStreamSerializer<>))
			.AddSingleton<IStreamSerializer>(services => services.GetRequiredService<SystemTextJsonStreamSerializer>())
			.AddSingleton(typeof(IStreamSerializer<>), typeof(SystemTextJsonStreamSerializer<>));
	}
}
