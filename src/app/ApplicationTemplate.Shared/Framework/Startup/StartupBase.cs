using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
//using Uno.UI;

namespace ApplicationTemplate
{
    /// <summary>
    /// This class abstracts the startup of the actual app (UWP, iOS, Android, WebAssembly and not test projects).
    /// This abstract class is responsible for building the host of the application as well as startup diagnostics.
    /// The implementator class is responsible for the application-specific code that initializes the application's services.
    /// </summary>
    public abstract class StartupBase
    {
        public StartupBase(CoreStartupBase coreStartup)
        {
            CoreStartup = coreStartup;
        }

        public StartupState State { get; } = new StartupState();

        public IServiceProvider ServiceProvider => CoreStartup.ServiceProvider;

        public Activity PreInitializeActivity { get; } = new Activity(nameof(PreInitialize));

        public Activity InitializeActivity { get; } = new Activity(nameof(Initialize));

        public Activity StartActivity { get; } = new Activity(nameof(Start));

        public CoreStartupBase CoreStartup { get; }

        protected ILogger Logger { get; private set; }

        /// <summary>
        /// Pre-initializes the application.
        /// This must be called as early as possible.
        /// </summary>
        public void PreInitialize()
        {
            if (State.IsPreInitialized)
            {
                throw new InvalidOperationException($"You shouldn't call {nameof(PreInitialize)} more than once.");
            }

            PreInitializeServices();

            PreInitializeActivity.Start();

            CoreStartup.PreInitialize();

            PreInitializeActivity.Stop();

            State.IsPreInitialized = true;
        }

        /// <summary>
        /// Initializes the application.
        /// </summary>
        public void Initialize()
        {
            if (State.IsInitialized)
            {
                throw new InvalidOperationException($"You shouldn't call {nameof(Initialize)} more than once.");
            }

            if (!State.IsPreInitialized)
            {
                throw new InvalidOperationException($"You must call {nameof(PreInitialize)} before calling '{nameof(Initialize)}'.");
            }

            InitializeActivity.Start();

            CoreStartup.Initialize(InitializeViewServices);

            Logger = GetOrCreateLogger(ServiceProvider);

            OnInitialized(ServiceProvider);

            InitializeActivity.Stop();

            State.IsInitialized = true;

            Logger.LogInformation("Initialized startup.");
        }

        /// <summary>
        /// Inialize All App configuration needed before all containers
        /// Ex: Uno Configuration , Languages etc...
        /// </summary>
        protected abstract void PreInitializeServices();

        /// <summary>
        /// Gets a <see cref="ILogger{TCategoryName}"/> typed to the implementator class.
        /// </summary>
        /// <param name="serviceProvider">The service provider from which the implementator class should obtain the logger.</param>
        /// <returns>The <see cref="ILogger{TCategoryName}"/> typed to the implementator class.</returns>
        protected abstract ILogger GetOrCreateLogger(IServiceProvider serviceProvider);

        /// <summary>
        /// Initializes view services into the provided <see cref="IHostBuilder"/>.
        /// </summary>
        /// <param name="hostBuilder">The hostbuilder in which services must be added.</param>
        protected abstract void InitializeViewServices(IHostBuilder hostBuilder);

        /// <summary>
        /// This method will be called once the app is initialized.
        /// This is a chance to apply any configuration required to start the app.
        /// </summary>
        /// <param name="services">Services</param>
        protected abstract void OnInitialized(IServiceProvider services);

        /// <summary>
        /// Starts the application.
        /// This method can be called multiple times.
        /// </summary>
        /// <returns><see cref="Task"/></returns>
        public async Task Start()
        {
            if (!State.IsInitialized)
            {
                throw new InvalidOperationException($"You must call {nameof(Initialize)} before calling '{nameof(Start)}'.");
            }

            Logger.LogDebug("Starting startup.");

            var isFirstLoad = !State.IsStarted;

            if (isFirstLoad)
            {
                StartActivity.Start();
            }

            var coreStart = CoreStartup.Start();
            var viewStart = StartViewServicesWithLogs(ServiceProvider, isFirstLoad);

            await Task.WhenAll(coreStart, viewStart);

            if (isFirstLoad)
            {
                StartActivity.Stop();

                State.IsStarted = true;
            }

            Logger.LogInformation("Started startup.");

            async Task StartViewServicesWithLogs(IServiceProvider services, bool isFirstStart)
            {
                Logger.LogDebug($"Starting view services (isFirstStart: {isFirstStart}).");

                await StartViewServices(services, isFirstStart);

                Logger.LogInformation("Started view services.");
            }
        }

        /// <summary>
        /// Starts the view services.
        /// This method can be called multiple times.
        /// This method will run on a background thread.
        /// </summary>
        /// <param name="services">Services</param>
        /// <param name="isFirstStart">True if it's the first start; false otherwise.</param>
        /// <returns>Task that completes when the services are started.</returns>
        protected abstract Task StartViewServices(IServiceProvider services, bool isFirstStart);
    }
}
