using System;
using ApplicationTemplate;
using ApplicationTemplate.Business;
using ApplicationTemplate.Client;
using GeneratedSerializers;
using Microsoft.Extensions.DependencyInjection;
using Nventive.Persistence;

// In case that you need to have ImmutableList<T> at the root for endpoint (to not have arrays in endpoint)
// You need to add <IsImmutablesAtRootDisabled>0</IsImmutablesAtRootDisabled> in SerializationConfig.xml
// This will enable the Immutables serializer and you will be able to register them here without compilation errors

[assembly: JsonSerializationConfiguration(GenerateOnlyRegisteredTypes = true)]

[assembly: JsonSerializableType(typeof(bool))]
[assembly: JsonSerializableType(typeof(string))]
[assembly: JsonSerializableType(typeof(string[]))]
[assembly: JsonSerializableType(typeof(AuthenticationToken))]
[assembly: JsonSerializableType(typeof(AuthenticationData))]
[assembly: JsonSerializableType(typeof(ApplicationSettings))]
[assembly: JsonSerializableType(typeof(PostData))]
[assembly: JsonSerializableType(typeof(PostData[]))]
[assembly: JsonSerializableType(typeof(ChuckNorrisResponse))]
[assembly: JsonSerializableType(typeof(ChuckNorrisErrorResponse))]
[assembly: JsonSerializableType(typeof(UserProfileData))]
[assembly: JsonSerializableType(typeof(ChuckNorrisQuote))]

namespace ApplicationTemplate
{
    ///// <summary>
    ///// This class is used for serialization configuration.
    ///// - Configures the serializers.
    ///// </summary>
    //public static class SerializationConfiguration
    //{
    //    /// <summary>
    //    /// Adds the serialization services to the <see cref="IServiceCollection"/>.
    //    /// </summary>
    //    /// <param name="services">Service collection.</param>
    //    /// <returns><see cref="IServiceCollection"/>.</returns>
    //    public static IServiceCollection AddSerialization(this IServiceCollection services)
    //    {
    //        SerializationGeneratorConfiguration.Initialize(serializerRegistry =>
    //        {
    //            // If you want to use a custom implementation of ISerializer,
    //            // replace this with your implementation.
    //            var customSerializer = default(ISerializer);

    //            var serializer = serializerRegistry(customSerializer);

    //            services
    //                .AddSingleton<ISerializer>(c => serializer)
    //                .AddSingleton<IObjectSerializer>(c => serializer)
    //                .AddSingleton<ISettingsSerializer>(c => new ObjectSerializerToSettingsSerializerAdapter(serializer));
    //        });

    //        return services;
    //    }
    //}

    /// <summary>
    /// This class is used by the generated code from the static serializers.
    /// Do not move or update without reasons.
    /// </summary>
    public partial class SerializationGeneratorConfiguration
    {
        public static void Initialize(Action<Func<ISerializer, ISerializer>> serializerRegistry)
        {
            InitSerializer(serializerRegistry);
        }

        static partial void InitSerializer(Action<Func<ISerializer, ISerializer>> serializerRegister);
    }
}
