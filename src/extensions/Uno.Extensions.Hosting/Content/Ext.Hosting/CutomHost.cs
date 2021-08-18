using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Uno.Extensions.Hosting
{
    //
    // Summary:
    //     Provides convenience methods for creating instances of Microsoft.Extensions.Hosting.IHostBuilder
    //     with pre-configured defaults.
    public static class CustomHost
    {
        //
        // Summary:
        //     Initializes a new instance of the Microsoft.Extensions.Hosting.HostBuilder class
        //     with pre-configured defaults.
        //
        // Returns:
        //     The initialized Microsoft.Extensions.Hosting.IHostBuilder.
        public static IHostBuilder CreateDefaultBuilder()
        {
            return CreateDefaultBuilder(null);
        }

        //
        // Summary:
        //     Initializes a new instance of the Microsoft.Extensions.Hosting.HostBuilder class
        //     with pre-configured defaults.
        //
        // Parameters:
        //   args:
        //     The command line args.
        //
        // Returns:
        //     The initialized Microsoft.Extensions.Hosting.IHostBuilder.
        public static IHostBuilder CreateDefaultBuilder(string[] args)
        {
            IHostBuilder builder = new CustomHostBuilder();
            return builder.ConfigureCustomDefaults(args);
        }
    }
}



// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// A program initialization utility.
    /// </summary>
    public class CustomHostBuilder : IHostBuilder
    {
        private List<Action<IConfigurationBuilder>> _configureHostConfigActions = new List<Action<IConfigurationBuilder>>();
        private List<Action<HostBuilderContext, IConfigurationBuilder>> _configureAppConfigActions = new List<Action<HostBuilderContext, IConfigurationBuilder>>();
        private List<Action<HostBuilderContext, IServiceCollection>> _configureServicesActions = new List<Action<HostBuilderContext, IServiceCollection>>();
        private List<IConfigureContainerAdapter> _configureContainerActions = new List<IConfigureContainerAdapter>();
        private IServiceFactoryAdapter _serviceProviderFactory = new ServiceFactoryAdapter<IServiceCollection>(new DefaultServiceProviderFactory());
        private bool _hostBuilt;
        private IConfiguration _hostConfiguration;
        private IConfiguration _appConfiguration;
        private HostBuilderContext _hostBuilderContext;
        private HostingEnvironment _hostingEnvironment;
        private IServiceProvider _appServices;
        private PhysicalFileProvider _defaultProvider;

        /// <summary>
        /// A central location for sharing state between components during the host building process.
        /// </summary>
        public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

        /// <summary>
        /// Set up the configuration for the builder itself. This will be used to initialize the <see cref="IHostEnvironment"/>
        /// for use later in the build process. This can be called multiple times and the results will be additive.
        /// </summary>
        /// <param name="configureDelegate">The delegate for configuring the <see cref="IConfigurationBuilder"/> that will be used
        /// to construct the <see cref="IConfiguration"/> for the host.</param>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            _configureHostConfigActions.Add(configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate)));
            return this;
        }

        /// <summary>
        /// Sets up the configuration for the remainder of the build process and application. This can be called multiple times and
        /// the results will be additive. The results will be available at <see cref="HostBuilderContext.Configuration"/> for
        /// subsequent operations, as well as in <see cref="IHost.Services"/>.
        /// </summary>
        /// <param name="configureDelegate">The delegate for configuring the <see cref="IConfigurationBuilder"/> that will be used
        /// to construct the <see cref="IConfiguration"/> for the host.</param>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            _configureAppConfigActions.Add(configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate)));
            return this;
        }

        /// <summary>
        /// Adds services to the container. This can be called multiple times and the results will be additive.
        /// </summary>
        /// <param name="configureDelegate">The delegate for configuring the <see cref="IConfigurationBuilder"/> that will be used
        /// to construct the <see cref="IConfiguration"/> for the host.</param>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            _configureServicesActions.Add(configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate)));
            return this;
        }

        /// <summary>
        /// Overrides the factory used to create the service provider.
        /// </summary>
        /// <typeparam name="TContainerBuilder">The type of the builder to create.</typeparam>
        /// <param name="factory">A factory used for creating service providers.</param>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory)
        {
            _serviceProviderFactory = new ServiceFactoryAdapter<TContainerBuilder>(factory ?? throw new ArgumentNullException(nameof(factory)));
            return this;
        }

        /// <summary>
        /// Overrides the factory used to create the service provider.
        /// </summary>
        /// <param name="factory">A factory used for creating service providers.</param>
        /// <typeparam name="TContainerBuilder">The type of the builder to create.</typeparam>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory)
        {
            _serviceProviderFactory = new ServiceFactoryAdapter<TContainerBuilder>(() => _hostBuilderContext, factory ?? throw new ArgumentNullException(nameof(factory)));
            return this;
        }

        /// <summary>
        /// Enables configuring the instantiated dependency container. This can be called multiple times and
        /// the results will be additive.
        /// </summary>
        /// <typeparam name="TContainerBuilder">The type of the builder to create.</typeparam>
        /// <param name="configureDelegate">The delegate for configuring the <see cref="IConfigurationBuilder"/> that will be used
        /// to construct the <see cref="IConfiguration"/> for the host.</param>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
        {
            _configureContainerActions.Add(new ConfigureContainerAdapter<TContainerBuilder>(configureDelegate
                ?? throw new ArgumentNullException(nameof(configureDelegate))));
            return this;
        }

        /// <summary>
        /// Run the given actions to initialize the host. This can only be called once.
        /// </summary>
        /// <returns>An initialized <see cref="IHost"/></returns>
        public IHost Build()
        {
            if (_hostBuilt)
            {
                throw new InvalidOperationException("BuildCalled"); // SR.BuildCalled);
            }
            _hostBuilt = true;

            // REVIEW: If we want to raise more events outside of these calls then we will need to
            // stash this in a field.
            using var diagnosticListener = new DiagnosticListener("Microsoft.Extensions.Hosting");
            const string hostBuildingEventName = "HostBuilding";
            const string hostBuiltEventName = "HostBuilt";

            if (diagnosticListener.IsEnabled() && diagnosticListener.IsEnabled(hostBuildingEventName))
            {
                Write(diagnosticListener, hostBuildingEventName, this);
            }

            BuildHostConfiguration();
            CreateHostingEnvironment();
            CreateHostBuilderContext();
            BuildAppConfiguration();
            CreateServiceProvider();

            var host = _appServices.GetRequiredService<IHost>();
            if (diagnosticListener.IsEnabled() && diagnosticListener.IsEnabled(hostBuiltEventName))
            {
                Write(diagnosticListener, hostBuiltEventName, host);
            }

            return host;
        }

        //[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
        //    Justification = "The values being passed into Write are being consumed by the application already.")]
        private static void Write<T>(
            DiagnosticSource diagnosticSource,
            string name,
            T value)
        {
            diagnosticSource.Write(name, value);
        }

        private void BuildHostConfiguration()
        {
            IConfigurationBuilder configBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(); // Make sure there's some default storage since there are no default providers

            foreach (Action<IConfigurationBuilder> buildAction in _configureHostConfigActions)
            {
                buildAction(configBuilder);
            }
            _hostConfiguration = configBuilder.Build();
        }

        private void CreateHostingEnvironment()
        {
            _hostingEnvironment = new HostingEnvironment()
            {
                ApplicationName = _hostConfiguration[HostDefaults.ApplicationKey],
                EnvironmentName = _hostConfiguration[HostDefaults.EnvironmentKey] ?? Environments.Production,
                ContentRootPath = ResolveContentRootPath(_hostConfiguration[HostDefaults.ContentRootKey], AppContext.BaseDirectory),
            };

            if (string.IsNullOrEmpty(_hostingEnvironment.ApplicationName))
            {
                // Note GetEntryAssembly returns null for the net4x console test runner.
                _hostingEnvironment.ApplicationName = Assembly.GetEntryAssembly()?.GetName().Name;
            }

            _hostingEnvironment.ContentRootFileProvider = _defaultProvider = new PhysicalFileProvider(_hostingEnvironment.ContentRootPath);
        }

        private string ResolveContentRootPath(string contentRootPath, string basePath)
        {
            if (string.IsNullOrEmpty(contentRootPath))
            {
                return basePath;
            }
            if (Path.IsPathRooted(contentRootPath))
            {
                return contentRootPath;
            }
            return Path.Combine(Path.GetFullPath(basePath), contentRootPath);
        }

        private void CreateHostBuilderContext()
        {
            _hostBuilderContext = new HostBuilderContext(Properties)
            {
                HostingEnvironment = _hostingEnvironment,
                Configuration = _hostConfiguration
            };
        }

        private void BuildAppConfiguration()
        {
            IConfigurationBuilder configBuilder = new ConfigurationBuilder()
                .SetBasePath(_hostingEnvironment.ContentRootPath)
                .AddConfiguration(_hostConfiguration, shouldDisposeConfiguration: true);

            foreach (Action<HostBuilderContext, IConfigurationBuilder> buildAction in _configureAppConfigActions)
            {
                buildAction(_hostBuilderContext, configBuilder);
            }
            _appConfiguration = configBuilder.Build();
            _hostBuilderContext.Configuration = _appConfiguration;
        }

        private void CreateServiceProvider()
        {
            var services = new ServiceCollection();
            //#pragma warning disable CS0618 // Type or member is obsolete
            //            services.AddSingleton<IHostingEnvironment>(_hostingEnvironment);
            //#pragma warning restore CS0618 // Type or member is obsolete
            services.AddSingleton<IHostEnvironment>(_hostingEnvironment);
            services.AddSingleton(_hostBuilderContext);
            // register configuration as factory to make it dispose with the service provider
            services.AddSingleton(_ => _appConfiguration);
            //#pragma warning disable CS0618 // Type or member is obsolete
            //            services.AddSingleton<IApplicationLifetime>(s => (IApplicationLifetime)s.GetService<IHostApplicationLifetime>());
            //#pragma warning restore CS0618 // Type or member is obsolete
            services.AddSingleton<IHostApplicationLifetime, ApplicationLifetime>();
            //services.AddSingleton<IHostLifetime, ConsoleLifetime>();
            services.AddSingleton<IHost>(_ =>
            {
                return new Internal.Host(_appServices,
                    _hostingEnvironment,
                    _defaultProvider,
                    _appServices.GetRequiredService<IHostApplicationLifetime>(),
                    _appServices.GetRequiredService<ILogger<Internal.Host>>(),
                    _appServices.GetService<IHostLifetime>(),
                    _appServices.GetService<IOptions<HostOptions>>());
            });
            //services.AddOptions().Configure<HostOptions>(options => { options.Initialize(_hostConfiguration); });
            services.AddLogging();

            foreach (Action<HostBuilderContext, IServiceCollection> configureServicesAction in _configureServicesActions)
            {
                configureServicesAction(_hostBuilderContext, services);
            }

            object containerBuilder = _serviceProviderFactory.CreateBuilder(services);

            foreach (IConfigureContainerAdapter containerAction in _configureContainerActions)
            {
                containerAction.ConfigureContainer(_hostBuilderContext, containerBuilder);
            }

            _appServices = _serviceProviderFactory.CreateServiceProvider(containerBuilder);

            if (_appServices == null)
            {
                throw new InvalidOperationException("NullIServiceProvider");// SR.NullIServiceProvider);
            }

            //// resolve configuration explicitly once to mark it as resolved within the
            //// service provider, ensuring it will be properly disposed with the provider
            //_ = _appServices.GetService<IConfiguration>();
        }
    }
}


namespace Microsoft.Extensions.Hosting.Internal
{
    internal sealed class ConfigureContainerAdapter<TContainerBuilder> : IConfigureContainerAdapter
    {
        private Action<HostBuilderContext, TContainerBuilder> _action;

        public ConfigureContainerAdapter(Action<HostBuilderContext, TContainerBuilder> action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public void ConfigureContainer(HostBuilderContext hostContext, object containerBuilder)
        {
            _action(hostContext, (TContainerBuilder)containerBuilder);
        }
    }
}

namespace Microsoft.Extensions.Hosting.Internal
{
    internal interface IConfigureContainerAdapter
    {
        void ConfigureContainer(HostBuilderContext hostContext, object containerBuilder);
    }
}

namespace Microsoft.Extensions.Hosting.Internal
{
    internal interface IServiceFactoryAdapter
    {
        object CreateBuilder(IServiceCollection services);

        IServiceProvider CreateServiceProvider(object containerBuilder);
    }
}

namespace Microsoft.Extensions.Hosting.Internal
{
    internal sealed class ServiceFactoryAdapter<TContainerBuilder> : IServiceFactoryAdapter
    {
        private IServiceProviderFactory<TContainerBuilder> _serviceProviderFactory;
        private readonly Func<HostBuilderContext> _contextResolver;
        private Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> _factoryResolver;

        public ServiceFactoryAdapter(IServiceProviderFactory<TContainerBuilder> serviceProviderFactory)
        {
            _serviceProviderFactory = serviceProviderFactory ?? throw new ArgumentNullException(nameof(serviceProviderFactory));
        }

        public ServiceFactoryAdapter(Func<HostBuilderContext> contextResolver, Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factoryResolver)
        {
            _contextResolver = contextResolver ?? throw new ArgumentNullException(nameof(contextResolver));
            _factoryResolver = factoryResolver ?? throw new ArgumentNullException(nameof(factoryResolver));
        }

        public object CreateBuilder(IServiceCollection services)
        {
            if (_serviceProviderFactory == null)
            {
                _serviceProviderFactory = _factoryResolver(_contextResolver());

                if (_serviceProviderFactory == null)
                {
                    throw new InvalidOperationException("ResolverReturnedNull");// SR.ResolverReturnedNull);
                }
            }
            return _serviceProviderFactory.CreateBuilder(services);
        }

        public IServiceProvider CreateServiceProvider(object containerBuilder)
        {
            if (_serviceProviderFactory == null)
            {
                throw new InvalidOperationException("CreateBuilderCallBeforeCreateServiceProvider");// SR.CreateBuilderCallBeforeCreateServiceProvider);
            }

            return _serviceProviderFactory.CreateServiceProvider((TContainerBuilder)containerBuilder);
        }
    }
}



namespace Microsoft.Extensions.Hosting.Internal
{
    internal sealed class Host : IHost, IAsyncDisposable
    {
        private readonly ILogger<Host> _logger;
        private readonly IHostLifetime _hostLifetime;
        private readonly ApplicationLifetime _applicationLifetime;
        private readonly HostOptions _options;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly PhysicalFileProvider _defaultProvider;
        private IEnumerable<IHostedService> _hostedServices;

        public Host(IServiceProvider services,
                    IHostEnvironment hostEnvironment,
                    PhysicalFileProvider defaultProvider,
                    IHostApplicationLifetime applicationLifetime,
                    ILogger<Host> logger,
                    IHostLifetime hostLifetime,
                    IOptions<HostOptions> options)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            _applicationLifetime = (applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime))) as ApplicationLifetime;
            _hostEnvironment = hostEnvironment;
            _defaultProvider = defaultProvider;

            if (_applicationLifetime is null)
            {
                throw new ArgumentException("Replacing IHostApplicationLifetime is not supported.", nameof(applicationLifetime));
            }
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _hostLifetime = hostLifetime;// ?? throw new ArgumentNullException(nameof(hostLifetime));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public IServiceProvider Services { get; }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _logger.Starting();

            using var combinedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _applicationLifetime.ApplicationStopping);
            CancellationToken combinedCancellationToken = combinedCancellationTokenSource.Token;

            if (_hostLifetime != null)
            {
                await _hostLifetime.WaitForStartAsync(combinedCancellationToken).ConfigureAwait(false);
            }

            combinedCancellationToken.ThrowIfCancellationRequested();
            _hostedServices = Services.GetService<IEnumerable<IHostedService>>();

            foreach (IHostedService hostedService in _hostedServices)
            {
                // Fire IHostedService.Start
                await hostedService.StartAsync(combinedCancellationToken).ConfigureAwait(false);

                if (hostedService is BackgroundService backgroundService)
                {
                    _ = TryExecuteBackgroundServiceAsync(backgroundService);
                }
            }

            // Fire IHostApplicationLifetime.Started
            _applicationLifetime.NotifyStarted();

            _logger.Started();
        }

        private async Task TryExecuteBackgroundServiceAsync(BackgroundService backgroundService)
        {
            try
            {
                await backgroundService.ExecuteTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.BackgroundServiceFaulted(ex);
                if (_options.BackgroundServiceExceptionBehavior == BackgroundServiceExceptionBehavior.StopHost)
                {
                    _logger.BackgroundServiceStoppingHost(ex);
                    _applicationLifetime.StopApplication();
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _logger.Stopping();

            using (var cts = new CancellationTokenSource(_options.ShutdownTimeout))
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken))
            {
                CancellationToken token = linkedCts.Token;
                // Trigger IHostApplicationLifetime.ApplicationStopping
                _applicationLifetime.StopApplication();

                IList<Exception> exceptions = new List<Exception>();
                if (_hostedServices != null) // Started?
                {
                    foreach (IHostedService hostedService in _hostedServices.Reverse())
                    {
                        try
                        {
                            await hostedService.StopAsync(token).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }

                // Fire IHostApplicationLifetime.Stopped
                _applicationLifetime.NotifyStopped();

                try
                {
                    if (_hostLifetime != null)
                    {
                        await _hostLifetime.StopAsync(token).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }

                if (exceptions.Count > 0)
                {
                    var ex = new AggregateException("One or more hosted services failed to stop.", exceptions);
                    _logger.StoppedWithException(ex);
                    throw ex;
                }
            }

            _logger.Stopped();
        }

        public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

        public async ValueTask DisposeAsync()
        {
            // The user didn't change the ContentRootFileProvider instance, we can dispose it
            if (ReferenceEquals(_hostEnvironment.ContentRootFileProvider, _defaultProvider))
            {
                // Dispose the content provider
                await DisposeAsync(_hostEnvironment.ContentRootFileProvider).ConfigureAwait(false);
            }
            else
            {
                // In the rare case that the user replaced the ContentRootFileProvider, dispose it and the one
                // we originally created
                await DisposeAsync(_hostEnvironment.ContentRootFileProvider).ConfigureAwait(false);
                await DisposeAsync(_defaultProvider).ConfigureAwait(false);
            }

            // Dispose the service provider
            await DisposeAsync(Services).ConfigureAwait(false);

            static async ValueTask DisposeAsync(object o)
            {
                switch (o)
                {
                    case IAsyncDisposable asyncDisposable:
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                        break;
                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                }
            }
        }
    }
}


namespace Microsoft.Extensions.Hosting.Internal
{
    internal static class HostingLoggerExtensions
    {
        public static void ApplicationError(this ILogger logger, EventId eventId, string message, Exception exception)
        {
            if (exception is ReflectionTypeLoadException reflectionTypeLoadException)
            {
                foreach (Exception ex in reflectionTypeLoadException.LoaderExceptions)
                {
                    message = message + Environment.NewLine + ex.Message;
                }
            }

            logger.LogCritical(
                eventId: eventId,
                message: message,
                exception: exception);
        }

        public static void Starting(this ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                   //eventId: LoggerEventIds.Starting,
                   message: "Hosting starting");
            }
        }

        public static void Started(this ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    //eventId: LoggerEventIds.Started,
                    message: "Hosting started");
            }
        }

        public static void Stopping(this ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    //eventId: LoggerEventIds.Stopping,
                    message: "Hosting stopping");
            }
        }

        public static void Stopped(this ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    //eventId: LoggerEventIds.Stopped,
                    message: "Hosting stopped");
            }
        }

        public static void StoppedWithException(this ILogger logger, Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    //eventId: LoggerEventIds.StoppedWithException,
                    exception: ex,
                    message: "Hosting shutdown exception");
            }
        }

        public static void BackgroundServiceFaulted(this ILogger logger, Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(
                    //eventId: LoggerEventIds.BackgroundServiceFaulted,
                    exception: ex,
                    message: "BackgroundService failed");
            }
        }

        public static void BackgroundServiceStoppingHost(this ILogger logger, Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Critical))
            {
                logger.LogCritical(
                    //eventId: LoggerEventIds.BackgroundServiceStoppingHost,
                    exception: ex,
                    message: "BackgroundServiceExceptionStoppedHost");// SR.BackgroundServiceExceptionStoppedHost);
            }
        }
    }
}





namespace Microsoft.Extensions.Hosting
{
    public static class HostingHostBuilderExtensions
    {
        /////// <summary>
        /////// Specify the environment to be used by the host. To avoid the environment being overwritten by a default
        /////// value, ensure this is called after defaults are configured.
        /////// </summary>
        /////// <param name="hostBuilder">The <see cref="IHostBuilder"/> to configure.</param>
        /////// <param name="environment">The environment to host the application in.</param>
        /////// <returns>The <see cref="IHostBuilder"/>.</returns>
        ////public static IHostBuilder UseEnvironment(this IHostBuilder hostBuilder, string environment)
        ////{
        ////    return hostBuilder.ConfigureHostConfiguration(configBuilder =>
        ////    {
        ////        configBuilder.AddInMemoryCollection(new[]
        ////        {
        ////            new KeyValuePair<string, string>(HostDefaults.EnvironmentKey,
        ////                environment ?? throw new ArgumentNullException(nameof(environment)))
        ////        });
        ////    });
        ////}

        /////// <summary>
        /////// Specify the content root directory to be used by the host. To avoid the content root directory being
        /////// overwritten by a default value, ensure this is called after defaults are configured.
        /////// </summary>
        /////// <param name="hostBuilder">The <see cref="IHostBuilder"/> to configure.</param>
        /////// <param name="contentRoot">Path to root directory of the application.</param>
        /////// <returns>The <see cref="IHostBuilder"/>.</returns>
        ////public static IHostBuilder UseContentRoot(this IHostBuilder hostBuilder, string contentRoot)
        ////{
        ////    return hostBuilder.ConfigureHostConfiguration(configBuilder =>
        ////    {
        ////        configBuilder.AddInMemoryCollection(new[]
        ////        {
        ////            new KeyValuePair<string, string>(HostDefaults.ContentRootKey,
        ////                contentRoot ?? throw new ArgumentNullException(nameof(contentRoot)))
        ////        });
        ////    });
        ////}

        ///// <summary>
        ///// Specify the <see cref="IServiceProvider"/> to be the default one.
        ///// </summary>
        ///// <param name="hostBuilder">The <see cref="IHostBuilder"/> to configure.</param>
        ///// <param name="configure"></param>
        ///// <returns>The <see cref="IHostBuilder"/>.</returns>
        //public static IHostBuilder UseDefaultServiceProvider(this IHostBuilder hostBuilder, Action<ServiceProviderOptions> configure)
        //    => UseDefaultServiceProvider(hostBuilder, (context, options) => configure(options));

        ///// <summary>
        ///// Specify the <see cref="IServiceProvider"/> to be the default one.
        ///// </summary>
        ///// <param name="hostBuilder">The <see cref="IHostBuilder"/> to configure.</param>
        ///// <param name="configure">The delegate that configures the <see cref="IServiceProvider"/>.</param>
        ///// <returns>The <see cref="IHostBuilder"/>.</returns>
        //public static IHostBuilder UseDefaultServiceProvider(this IHostBuilder hostBuilder, Action<HostBuilderContext, ServiceProviderOptions> configure)
        //{
        //    return hostBuilder.UseServiceProviderFactory(context =>
        //    {
        //        var options = new ServiceProviderOptions();
        //        configure(context, options);
        //        return new DefaultServiceProviderFactory(options);
        //    });
        //}

        /////// <summary>
        /////// Adds a delegate for configuring the provided <see cref="ILoggingBuilder"/>. This may be called multiple times.
        /////// </summary>
        /////// <param name="hostBuilder">The <see cref="IHostBuilder" /> to configure.</param>
        /////// <param name="configureLogging">The delegate that configures the <see cref="ILoggingBuilder"/>.</param>
        /////// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        ////public static IHostBuilder ConfigureLogging(this IHostBuilder hostBuilder, Action<HostBuilderContext, ILoggingBuilder> configureLogging)
        ////{
        ////    return hostBuilder.ConfigureServices((context, collection) => collection.AddLogging(builder => configureLogging(context, builder)));
        ////}

        /////// <summary>
        /////// Adds a delegate for configuring the provided <see cref="ILoggingBuilder"/>. This may be called multiple times.
        /////// </summary>
        /////// <param name="hostBuilder">The <see cref="IHostBuilder" /> to configure.</param>
        /////// <param name="configureLogging">The delegate that configures the <see cref="ILoggingBuilder"/>.</param>
        /////// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        ////public static IHostBuilder ConfigureLogging(this IHostBuilder hostBuilder, Action<ILoggingBuilder> configureLogging)
        ////{
        ////    return hostBuilder.ConfigureServices((context, collection) => collection.AddLogging(builder => configureLogging(builder)));
        ////}

        ///// <summary>
        ///// Adds a delegate for configuring the <see cref="HostOptions"/> of the <see cref="IHost"/>.
        ///// </summary>
        ///// <param name="hostBuilder">The <see cref="IHostBuilder" /> to configure.</param>
        ///// <param name="configureOptions">The delegate for configuring the <see cref="HostOptions"/>.</param>
        ///// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        //public static IHostBuilder ConfigureHostOptions(this IHostBuilder hostBuilder, Action<HostBuilderContext, HostOptions> configureOptions)
        //{
        //    return hostBuilder.ConfigureServices(
        //        (context, collection) => collection.Configure<HostOptions>(options => configureOptions(context, options)));
        //}

        /////// <summary>
        /////// Adds a delegate for configuring the <see cref="HostOptions"/> of the <see cref="IHost"/> instance
        /////// related to th.
        /////// </summary>
        /////// <param name="hostBuilder">The <see cref="IHostBuilder" /> to configure.</param>
        /////// <param name="configureOptions">The delegate for configuring the <see cref="HostOptions"/>.</param>
        /////// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        ////public static IHostBuilder ConfigureHostOptions(this IHostBuilder hostBuilder, Action<HostOptions> configureOptions)
        ////{
        ////    return ConfigureServices(hostBuilder, collection => collection.Configure(configureOptions));
        ////}

        ///// <summary>
        ///// Sets up the configuration for the remainder of the build process and application. This can be called multiple times and
        ///// the results will be additive. The results will be available at <see cref="HostBuilderContext.Configuration"/> for
        ///// subsequent operations, as well as in <see cref="IHost.Services"/>.
        ///// </summary>
        ///// <param name="hostBuilder">The <see cref="IHostBuilder" /> to configure.</param>
        ///// <param name="configureDelegate">The delegate for configuring the <see cref="IConfigurationBuilder"/> that will be used
        ///// to construct the <see cref="IConfiguration"/> for the host.</param>
        ///// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        //public static IHostBuilder ConfigureAppConfiguration(this IHostBuilder hostBuilder, Action<IConfigurationBuilder> configureDelegate)
        //{
        //    return hostBuilder.ConfigureAppConfiguration((context, builder) => configureDelegate(builder));
        //}

        /////// <summary>
        /////// Adds services to the container. This can be called multiple times and the results will be additive.
        /////// </summary>
        /////// <param name="hostBuilder">The <see cref="IHostBuilder" /> to configure.</param>
        /////// <param name="configureDelegate">The delegate for configuring the <see cref="IServiceCollection"/>.</param>
        /////// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        ////public static IHostBuilder ConfigureServices(this IHostBuilder hostBuilder, Action<IServiceCollection> configureDelegate)
        ////{
        ////    return hostBuilder.ConfigureServices((context, collection) => configureDelegate(collection));
        ////}

        ///// <summary>
        ///// Enables configuring the instantiated dependency container. This can be called multiple times and
        ///// the results will be additive.
        ///// </summary>
        ///// <typeparam name="TContainerBuilder"></typeparam>
        ///// <param name="hostBuilder">The <see cref="IHostBuilder" /> to configure.</param>
        ///// <param name="configureDelegate">The delegate for configuring the <typeparamref name="TContainerBuilder"/>.</param>
        ///// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        //public static IHostBuilder ConfigureContainer<TContainerBuilder>(this IHostBuilder hostBuilder, Action<TContainerBuilder> configureDelegate)
        //{
        //    return hostBuilder.ConfigureContainer<TContainerBuilder>((context, builder) => configureDelegate(builder));
        //}

        /// <summary>
        /// Configures an existing <see cref="IHostBuilder"/> instance with pre-configured defaults. This will overwrite
        /// previously configured values and is intended to be called before additional configuration calls.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the <see cref="IHostBuilder"/>:
        ///   <list type="bullet">
        ///     <item><description>set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/></description></item>
        ///     <item><description>load host <see cref="IConfiguration"/> from "DOTNET_" prefixed environment variables</description></item>
        ///     <item><description>load host <see cref="IConfiguration"/> from supplied command line args</description></item>
        ///     <item><description>load app <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json'</description></item>
        ///     <item><description>load app <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly</description></item>
        ///     <item><description>load app <see cref="IConfiguration"/> from environment variables</description></item>
        ///     <item><description>load app <see cref="IConfiguration"/> from supplied command line args</description></item>
        ///     <item><description>configure the <see cref="ILoggerFactory"/> to log to the console, debug, and event source output</description></item>
        ///     <item><description>enables scope validation on the dependency injection container when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development'</description></item>
        ///   </list>
        /// </remarks>
        /// <param name="builder">The existing builder to configure.</param>
        /// <param name="args">The command line args.</param>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public static IHostBuilder ConfigureCustomDefaults(this IHostBuilder builder, string[] args)
        {
            //UseContentRoot(builder, Directory.GetCurrentDirectory());
            //builder.ConfigureHostConfiguration(config =>
            //{
            //    config.AddEnvironmentVariables(prefix: "DOTNET_");
            //    if (args is { Length: > 0 })
            //    {
            //        config.AddCommandLine(args);
            //    }
            //});

            //builder.ConfigureAppConfiguration((hostingContext, config) =>
            //{
            //    IHostEnvironment env = hostingContext.HostingEnvironment;
            //    bool reloadOnChange = GetReloadConfigOnChangeValue(hostingContext);

            //    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: reloadOnChange)
            //            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: reloadOnChange);

            //    if (env.IsDevelopment() && env.ApplicationName is { Length: > 0 })
            //    {
            //        var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
            //        if (appAssembly is not null)
            //        {
            //            config.AddUserSecrets(appAssembly, optional: true, reloadOnChange: reloadOnChange);
            //        }
            //    }

            //    config.AddEnvironmentVariables();

            //    if (args is { Length: > 0 })
            //    {
            //        config.AddCommandLine(args);
            //    }
            //});

            builder.ConfigureLogging((hostingContext, logging) =>
            {
                //bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

                //// IMPORTANT: This needs to be added *before* configuration is loaded, this lets
                //// the defaults be overridden by the configuration.
                //if (isWindows)
                //{
                //    // Default the EventLogLoggerProvider to warning or above
                //    logging.AddFilter<EventLogLoggerProvider>(level => level >= LogLevel.Warning);
                //}

                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                //logging.AddConsole();
                logging.AddDebug();
                //logging.AddEventSourceLogger();

                //if (isWindows)
                //{
                //    // Add the EventLogLoggerProvider on windows machines
                //    logging.AddEventLog();
                //}

                logging.Configure(options =>
                {
                    options.ActivityTrackingOptions =
                        ActivityTrackingOptions.SpanId |
                        ActivityTrackingOptions.TraceId |
                        ActivityTrackingOptions.ParentId;
                });

            });

            builder.UseDefaultServiceProvider((context, options) =>
            {
                bool isDevelopment = context.HostingEnvironment.IsDevelopment();
                options.ValidateScopes = isDevelopment;
                options.ValidateOnBuild = isDevelopment;
            });

            return builder;

            //[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Calling IConfiguration.GetValue is safe when the T is bool.")]
            static bool GetReloadConfigOnChangeValue(HostBuilderContext hostingContext) => hostingContext.Configuration.GetValue("hostBuilder:reloadConfigOnChange", defaultValue: true);
        }

        ///// <summary>
        ///// Listens for Ctrl+C or SIGTERM and calls <see cref="IHostApplicationLifetime.StopApplication"/> to start the shutdown process.
        ///// This will unblock extensions like RunAsync and WaitForShutdownAsync.
        ///// </summary>
        ///// <param name="hostBuilder">The <see cref="IHostBuilder" /> to configure.</param>
        ///// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        //public static IHostBuilder UseConsoleLifetime(this IHostBuilder hostBuilder)
        //{
        //    return hostBuilder.ConfigureServices(collection => collection.AddSingleton<IHostLifetime, ConsoleLifetime>());
        //}

        ///// <summary>
        ///// Listens for Ctrl+C or SIGTERM and calls <see cref="IHostApplicationLifetime.StopApplication"/> to start the shutdown process.
        ///// This will unblock extensions like RunAsync and WaitForShutdownAsync.
        ///// </summary>
        ///// <param name="hostBuilder">The <see cref="IHostBuilder" /> to configure.</param>
        ///// <param name="configureOptions">The delegate for configuring the <see cref="ConsoleLifetime"/>.</param>
        ///// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        //public static IHostBuilder UseConsoleLifetime(this IHostBuilder hostBuilder, Action<ConsoleLifetimeOptions> configureOptions)
        //{
        //    return hostBuilder.ConfigureServices(collection =>
        //    {
        //        collection.AddSingleton<IHostLifetime, ConsoleLifetime>();
        //        collection.Configure(configureOptions);
        //    });
        //}

        ///// <summary>
        ///// Enables console support, builds and starts the host, and waits for Ctrl+C or SIGTERM to shut down.
        ///// </summary>
        ///// <param name="hostBuilder">The <see cref="IHostBuilder" /> to configure.</param>
        ///// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the console.</param>
        ///// <returns>A <see cref="Task"/> that only completes when the token is triggered or shutdown is triggered.</returns>
        //public static Task RunConsoleAsync(this IHostBuilder hostBuilder, CancellationToken cancellationToken = default)
        //{
        //    return hostBuilder.UseConsoleLifetime().Build().RunAsync(cancellationToken);
        //}

        ///// <summary>
        ///// Enables console support, builds and starts the host, and waits for Ctrl+C or SIGTERM to shut down.
        ///// </summary>
        ///// <param name="hostBuilder">The <see cref="IHostBuilder" /> to configure.</param>
        ///// <param name="configureOptions">The delegate for configuring the <see cref="ConsoleLifetime"/>.</param>
        ///// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the console.</param>
        ///// <returns>A <see cref="Task"/> that only completes when the token is triggered or shutdown is triggered.</returns>
        //public static Task RunConsoleAsync(this IHostBuilder hostBuilder, Action<ConsoleLifetimeOptions> configureOptions, CancellationToken cancellationToken = default)
        //{
        //    return hostBuilder.UseConsoleLifetime(configureOptions).Build().RunAsync(cancellationToken);
        //}
    }
}
