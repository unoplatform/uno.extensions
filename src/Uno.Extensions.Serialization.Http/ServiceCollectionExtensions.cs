namespace Uno.Extensions;

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
    public static IServiceCollection AddResponseContentDeserializer(this IServiceCollection services)
    {
        return services
            .AddSingleton<IResponseContentDeserializer, SerializerToResponseContentDeserializer>();
    }
}
