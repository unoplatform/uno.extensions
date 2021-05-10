using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
//-:cnd:noEmit
#else
//+:cnd:noEmit
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
//-:cnd:noEmit
#endif
//+:cnd:noEmit

namespace ApplicationTemplate
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeLogging();

            this.InitializeComponent();

//-:cnd:noEmit
#if HAS_UNO || NETFX_CORE
//+:cnd:noEmit
			this.Suspending += OnSuspending;
			//-:cnd:noEmit
#endif
			//+:cnd:noEmit
		}

		/// <summary>
		/// Invoked when the application is launched normally by the end user.  Other entry points
		/// will be used such as when the application is launched to open a specific file.
		/// </summary>
		/// <param name="e">Details about the launch request and process.</param>
//-:cnd:noEmit
#if WINDOWS_UWP
//+:cnd:noEmit
		protected override void OnLaunched(LaunchActivatedEventArgs e)
//-:cnd:noEmit
#else
//+:cnd:noEmit
		protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs e)
//-:cnd:noEmit
#endif
//+:cnd:noEmit
        {
//-:cnd:noEmit
#if DEBUG
//+:cnd:noEmit
			if (System.Diagnostics.Debugger.IsAttached)
            {
                // this.DebugSettings.EnableFrameRateCounter = true;
            }
//-:cnd:noEmit
#endif
//+:cnd:noEmit

//-:cnd:noEmit
#if NET5_0 && WINDOWS
//+:cnd:noEmit
            var window = new Window();
            window.Activate();
//-:cnd:noEmit
#elif WINDOWS_UWP
//+:cnd:noEmit
			var window = Window.Current;
//-:cnd:noEmit
#else
//+:cnd:noEmit
            var window = Microsoft.UI.Xaml.Window.Current;
//-:cnd:noEmit
#endif
//+:cnd:noEmit
			var rootFrame = window.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

//-:cnd:noEmit
#if WINDOWS_UWP
//+:cnd:noEmit

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
//-:cnd:noEmit
#else
//+:cnd:noEmit
                if (e.UWPLaunchActivatedEventArgs.PreviousExecutionState == ApplicationExecutionState.Terminated)
//-:cnd:noEmit
#endif
//+:cnd:noEmit
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                window.Content = rootFrame;
            }

//-:cnd:noEmit
#if WINDOWS_UWP
//+:cnd:noEmit
            if (e.PrelaunchActivated == false)
//-:cnd:noEmit
#elif !(NET5_0 && WINDOWS)
//+:cnd:noEmit
            if (e.UWPLaunchActivatedEventArgs.PrelaunchActivated == false)
//-:cnd:noEmit
#endif
//+:cnd:noEmit
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                window.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
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
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        /// <summary>
        /// Configures global Uno Platform logging
        /// </summary>
        private static void InitializeLogging()
        {
            var factory = LoggerFactory.Create(builder =>
            {
//-:cnd:noEmit
#if __WASM__
//+:cnd:noEmit
                builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
//-:cnd:noEmit
#elif __IOS__
//+:cnd:noEmit
                builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
//-:cnd:noEmit
#elif NETFX_CORE && !WINDOWS_UWP
//+:cnd:noEmit
                builder.AddDebug();
//-:cnd:noEmit
#else
//+:cnd:noEmit
                builder.AddConsole();
//-:cnd:noEmit
#endif
//+:cnd:noEmit

                // Exclude logs below this level
                builder.SetMinimumLevel(LogLevel.Information);

                // Default filters for Uno Platform namespaces
                builder.AddFilter("Uno", LogLevel.Warning);
                builder.AddFilter("Windows", LogLevel.Warning);
                builder.AddFilter("Microsoft", LogLevel.Warning);

                // Generic Xaml events
                // builder.AddFilter("Microsoft.UI.Xaml", LogLevel.Debug );
                // builder.AddFilter("Microsoft.UI.Xaml.VisualStateGroup", LogLevel.Debug );
                // builder.AddFilter("Microsoft.UI.Xaml.StateTriggerBase", LogLevel.Debug );
                // builder.AddFilter("Microsoft.UI.Xaml.UIElement", LogLevel.Debug );
                // builder.AddFilter("Microsoft.UI.Xaml.FrameworkElement", LogLevel.Trace );

                // Layouter specific messages
                // builder.AddFilter("Microsoft.UI.Xaml.Controls", LogLevel.Debug );
                // builder.AddFilter("Microsoft.UI.Xaml.Controls.Layouter", LogLevel.Debug );
                // builder.AddFilter("Microsoft.UI.Xaml.Controls.Panel", LogLevel.Debug );

                // builder.AddFilter("Windows.Storage", LogLevel.Debug );

                // Binding related messages
                // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );
                // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );

                // Binder memory references tracking
                // builder.AddFilter("Uno.UI.DataBinding.BinderReferenceHolder", LogLevel.Debug );

                // RemoteControl and HotReload related
                // builder.AddFilter("Uno.UI.RemoteControl", LogLevel.Information);

                // Debug JS interop
                // builder.AddFilter("Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug );
            });

            global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;
        }
    }
}
