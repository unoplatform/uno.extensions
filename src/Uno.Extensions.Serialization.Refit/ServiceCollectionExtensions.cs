using Microsoft.Extensions.Hosting;

namespace Uno.Extensions
{
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
		/// <param name="context">The <see cref="HostBuilderContext"/> to use when adding services</param>
		/// <returns><see cref="IServiceCollection"/>.</returns>
		public static IServiceCollection AddContentSerializer(this IServiceCollection services, HostBuilderContext context)
		{
			if (context.IsRegistered(nameof(AddContentSerializer)))
			{
				return services;
			}
			return services
                .AddSingleton<IHttpContentSerializer, SerializerToContentSerializerAdapter>();
        }
    }
}
