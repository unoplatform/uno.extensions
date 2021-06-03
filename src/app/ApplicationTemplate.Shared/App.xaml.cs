#pragma warning disable SA1005 // Single line comments should begin with single space
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
//-:cnd:noEmit
#if WINDOWS_UWP
//+:cnd:noEmit
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.ViewManagement;
//-:cnd:noEmit
#else
//+:cnd:noEmit
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
//-:cnd:noEmit
#endif
//+:cnd:noEmit
using System;
using System.Threading.Tasks;
using Uno.Extensions.Configuration;
using Uno.Extensions.Hosting;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Messages;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using ApplicationTemplate.Views;
using Uno.Extensions.Logging.Serilog;
#pragma warning restore SA1005 // Single line comments should begin with single space

namespace ApplicationTemplate
{
    public sealed partial class App : Application
    {
        private Window _window;
        private Frame _frame;

        public App()
        {
            Host = UnoHost
#if __WASM__
                .CreateDefaultBuilderForWASM()
#else
                .CreateDefaultBuilder()
#endif
                .UseEnvironment("Staging")
                .UseAppSettings<App>()
                .UseConfigurationSectionInApp<CustomIntroduction>(nameof(CustomIntroduction))
                .UseUnoLogging(logBuilder =>
                    {
                        logBuilder
                            .SetMinimumLevel(LogLevel.Debug)
                            .XamlLogLevel(LogLevel.Information)
                            .XamlLayoutLogLevel(LogLevel.Information);
                    }
#if __WASM__
                    , new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider()
#endif
                )
                .UseSerilog(true)
                .UseRouting<RouterConfiguration, LaunchMessage>(() => _frame)
                .Build()
                .EnableUnoLogging();

            InitializeComponent();

#if HAS_UNO || NETFX_CORE
            Suspending += OnSuspending;
#endif
        }

        private IHost Host { get; }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
#if WINDOWS_UWP
        protected override void OnLaunched(LaunchActivatedEventArgs e)
#else
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs e)
#endif
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

#if NET5_0 && WINDOWS
            _window = new Window();
            _window.Activate();
#elif WINDOWS_UWP
            _window = Window.Current;
#else
            _window = Microsoft.UI.Xaml.Window.Current;
#endif

            _frame = _window.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (_frame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                _frame = new Frame();

                _frame.NavigationFailed += OnNavigationFailed;

#if WINDOWS_UWP
                if (e?.PreviousExecutionState == ApplicationExecutionState.Terminated)
#else
                if (e?.UWPLaunchActivatedEventArgs?.PreviousExecutionState == ApplicationExecutionState.Terminated)
#endif
                {
                    // TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                _window.Content = _frame;
            }
#if WINDOWS_UWP
            if (e?.PrelaunchActivated == false)
#elif !(NET5_0 && WINDOWS)
            if (e?.UWPLaunchActivatedEventArgs.PrelaunchActivated == false)
#endif
            {
                if (_frame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    //_frame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                _window.Activate();
            }

            _ = Task.Run(() =>
            {
                Host.Run();
            });
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails.
        /// </summary>
        /// <param name="sender">The Frame which failed navigation.</param>
        /// <param name="e">Details about the navigation failure.</param>
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            // TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
