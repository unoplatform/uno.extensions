using System;
using GeneratedSerializers;
using Microsoft.Extensions.DependencyInjection;
using Nventive.Persistence;

[assembly: JsonSerializationConfiguration(GenerateOnlyRegisteredTypes = true)]

namespace Uno.Extensions.Serialization
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
        /// <returns><see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddSerialization(this IServiceCollection services, Action<Action<Func<ISerializer, ISerializer>>> initialize)
        {
            initialize(serializerRegistry =>
            {
                // If you want to use a custom implementation of ISerializer,
                // replace this with your implementation.
                var customSerializer = default(ISerializer);

                var serializer = serializerRegistry(customSerializer);

                services
                    .AddSingleton<ISerializer>(c => serializer)
                    .AddSingleton<IObjectSerializer>(c => serializer)
                    .AddSingleton<ISettingsSerializer>(c => new ObjectSerializerToSettingsSerializerAdapter(serializer));
            });

            return services;
        }
    }
}
