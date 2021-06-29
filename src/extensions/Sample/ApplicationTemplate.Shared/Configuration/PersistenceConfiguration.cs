using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using ApplicationTemplate.Business;
using GeneratedSerializers;
using Microsoft.Extensions.DependencyInjection;
using Nventive.Persistence;

namespace ApplicationTemplate
{
    /// <summary>
    /// This class is used for persistence configuration.
    /// - Configures the application settings.
    /// </summary>
    public static class PersistenceConfiguration
    {
        /// <summary>
        /// Adds the persistence services to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <returns><see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddPersistence(this IServiceCollection services)
        {
            return services
                .AddSingleton(s => CreateSecureDataPersister(s, defaultValue: ApplicationSettings.Default));
        }

        private static IObservableDataPersister<T> CreateSecureDataPersister<T>(IServiceProvider services, T defaultValue = default(T))
        {
//-:cnd:noEmit
#if __ANDROID__
//+:cnd:noEmit
            return new KeyStoreSettingsStorage(
                services.GetRequiredService<ISettingsSerializer>(),
                Uno.UI.ContextHelper.Current.GetFileStreamPath(typeof(T).Name).AbsolutePath
            ).ToDataPersister<T>(typeof(T).Name);
            //-:cnd:noEmit
#elif __IOS__
//+:cnd:noEmit
            return new KeychainSettingsStorage(
                services.GetRequiredService<ISettingsSerializer>()
            ).ToDataPersister<T>(typeof(T).Name);
//-:cnd:noEmit
#else
//+:cnd:noEmit
            return CreateDataPersister(services, defaultValue);
//-:cnd:noEmit
#endif
            //+:cnd:noEmit
        }

        private static IObservableDataPersister<T> CreateDataPersister<T>(IServiceProvider services, T defaultValue = default(T))
        {
            return UnoDataPersister.CreateFromFile<T>(
                FolderType.WorkingData,
                typeof(T).Name + ".json",
                async (ct, s) => (T)services.GetRequiredService<IObjectSerializer>().FromStream(s, typeof(T)),
                async (ct, s, b) => services.GetRequiredService<IObjectSerializer>().WriteToStream(s, typeof(T), b, canDisposeStream: true)
            )
            .ToObservablePersister(services.GetRequiredService<IBackgroundScheduler>());
        }
    }
}
