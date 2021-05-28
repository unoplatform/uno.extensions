using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Uno.Extensions.Hosting
{
#if __ANDROID__ || __IOS__
    /// <summary>
    /// Listens for Ctrl+C or SIGTERM and initiates shutdown - replaces
    /// the default ConsoleLifetime class that's part of the Hosting extensions
    /// library in order to prevent wire up of the CancelKeyPress event
    /// handler that isn't supported on iOS and Android (throws exception).
    /// </summary>
#pragma warning disable IDE0079 // implementation copied from Hosting extensions library
#pragma warning disable IDE0090
#pragma warning disable CA1062
#pragma warning disable CA1063
#pragma warning disable CA1816
#pragma warning disable CA2213
#pragma warning disable SA1512
    public sealed class XamarinConsoleLifetime : IHostLifetime, IDisposable
    {
        private readonly ManualResetEvent _shutdownBlock = new ManualResetEvent(false);
        private CancellationTokenRegistration _applicationStartedRegistration;
        private CancellationTokenRegistration _applicationStoppingRegistration;

        public XamarinConsoleLifetime(IOptions<ConsoleLifetimeOptions> options, IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, IOptions<HostOptions> hostOptions)
            : this(options, environment, applicationLifetime, hostOptions, NullLoggerFactory.Instance)
        {
        }

        public XamarinConsoleLifetime(IOptions<ConsoleLifetimeOptions> options, IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, IOptions<HostOptions> hostOptions, ILoggerFactory loggerFactory)
        {
            Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            Environment = environment ?? throw new ArgumentNullException(nameof(environment));
            ApplicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
            HostOptions = hostOptions?.Value ?? throw new ArgumentNullException(nameof(hostOptions));
            Logger = loggerFactory.CreateLogger("Microsoft.Hosting.Lifetime");
        }

        private ConsoleLifetimeOptions Options { get; }

        private IHostEnvironment Environment { get; }

        private IHostApplicationLifetime ApplicationLifetime { get; }

        private HostOptions HostOptions { get; }

        private ILogger Logger { get; }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // There's nothing to do here
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _shutdownBlock.Set();

            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            // Console.CancelKeyPress -= OnCancelKeyPress; // Not supported on iOS/Android

            _applicationStartedRegistration.Dispose();
            _applicationStoppingRegistration.Dispose();
        }

        public Task WaitForStartAsync(CancellationToken cancellationToken)
        {
            if (!Options.SuppressStatusMessages)
            {
                _applicationStartedRegistration = ApplicationLifetime.ApplicationStarted.Register(state =>
                {
                    ((XamarinConsoleLifetime)state).OnApplicationStarted();
                },
                this);
                _applicationStoppingRegistration = ApplicationLifetime.ApplicationStopping.Register(state =>
                {
                    ((XamarinConsoleLifetime)state).OnApplicationStopping();
                },
                this);
            }

            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            // Console.CancelKeyPress += OnCancelKeyPress; // Not supported on iOS/Android

            // Console applications start immediately.
            return Task.CompletedTask;
        }

        private void OnApplicationStarted()
        {
            Logger.LogInformation("Application started. Press Ctrl+C to shut down.");
            Logger.LogInformation("Hosting environment: {envName}", Environment.EnvironmentName);
            Logger.LogInformation("Content root path: {contentRoot}", Environment.ContentRootPath);
        }

        private void OnApplicationStopping()
        {
            Logger.LogInformation("Application is shutting down...");
        }

        private void OnProcessExit(object sender, EventArgs e)
        {
            ApplicationLifetime.StopApplication();
            if (!_shutdownBlock.WaitOne(HostOptions.ShutdownTimeout))
            {
                Logger.LogInformation("Waiting for the host to be disposed. Ensure all 'IHost' instances are wrapped in 'using' blocks.");
            }
            _shutdownBlock.WaitOne();
            // On Linux if the shutdown is triggered by SIGTERM then that's signaled with the 143 exit code.
            // Suppress that since we shut down gracefully. https://github.com/dotnet/aspnetcore/issues/6526
            System.Environment.ExitCode = 0;
        }

        private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            ApplicationLifetime.StopApplication();
        }
    }
#pragma warning restore SA1512
#pragma warning restore CA2213
#pragma warning restore CA1816
#pragma warning restore CA1063
#pragma warning restore CA1062
#pragma warning restore IDE0090
#pragma warning restore IDE0079
#endif
}
