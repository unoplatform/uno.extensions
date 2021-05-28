﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.Extensions.Hosting;
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
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
//-:cnd:noEmit
#endif
//+:cnd:noEmit
using Uno.Extensions.Hosting;
using Uno.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace ApplicationTemplate
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        private Window _window;
        private Frame _rootFrame;
        private IHost host { get; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            host = UnoHost.CreateDefaultBuilder()
                .UseEnvironment("Staging")
                .UseAppSettings<App>()
                .UseConfigurationSectionInApp<CustomIntroduction>("CustomSettings")
                .UseRouting<RouterConfiguration, LaunchMessage>(() => _rootFrame)
                .Build();

            var settings = host.Services.GetService<IOptions<CustomIntroduction>>();


            this.InitializeComponent();

#if HAS_UNO || NETFX_CORE
            this.Suspending += OnSuspending;
#endif
        }

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

            var rootFrame = _window.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                _rootFrame = rootFrame;

                rootFrame.NavigationFailed += OnNavigationFailed;

#if WINDOWS_UWP
                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
#else
                if (e.UWPLaunchActivatedEventArgs.PreviousExecutionState == ApplicationExecutionState.Terminated)
#endif
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                _window.Content = rootFrame;
            }
#if WINDOWS_UWP
            if (e.PrelaunchActivated == false)
#elif !(NET5_0 && WINDOWS)
            if (e.UWPLaunchActivatedEventArgs.PrelaunchActivated == false)
#endif
            {
                if (rootFrame.Content == null)
                {
                    //// When the navigation stack isn't restored navigate to the first page,
                    //// configuring the new page by passing required information as a navigation
                    //// parameter
                    //rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                _window.Activate();
            }

            _ = Task.Run(() =>
            {
                host.Run();
            });
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
    }

    public class CustomIntroduction
    {
        public string Introduction { get; set; }
    }

    public class RouterConfiguration : IRouteDefinitions
    {
        public const string ActionsKey = "action";

        public enum Actions
        {
            Login
        }

        public IReadOnlyDictionary<string, (Type, Type)> Routes { get; } = new Dictionary<string, (Type, Type)>()
            .RegisterPage<MainPageViewModel, MainPage>("")
            .RegisterPage<SecondPageViewModel, SecondPage>();

    }


    public static class RouteTypeExtensions
    {
        public static string AsRoute(this Type routeViewModel)
        {
            return routeViewModel.Name.ToLower().Replace("pageviewmodel", "");
        }

        public static Dictionary<string, (Type, Type)> RegisterPage<TViewModel, TPage>(this Dictionary<string, (Type, Type)> routeDictionary, string path=null)
        {
            if(path !=null)
            {
                routeDictionary[path] = (typeof(TPage), typeof(TViewModel));
            }
            routeDictionary[typeof(TViewModel).AsRoute()] = (typeof(TPage), typeof(TViewModel));
            return routeDictionary;
        }
    }

    public class MainPageViewModel : ObservableValidator
    {

        public string Introduction { get; }

        private IRouteMessenger Messenger { get; }

        public ICommand GoSecondCommand { get; }

        public MainPageViewModel(
            IOptions<CustomIntroduction> settings,
            IRouteMessenger messenger)
        {
            Introduction = settings.Value.Introduction;
            Messenger = messenger;
            GoSecondCommand = new RelayCommand(GoSecond);
        }

        public void GoSecond()
        {
            Messenger.Send(new RoutingMessage(this, typeof(SecondPageViewModel).AsRoute()));
        }
    }

    public class SecondPageViewModel : ObservableObject
    {
        public string Title { get; } = "Page 2";

        private IRouteMessenger Messenger { get; }

        public ICommand GoBackCommand { get; }

        public SecondPageViewModel(
            IRouteMessenger messenger)
        {
            Messenger = messenger;
            GoBackCommand = new RelayCommand(GoBack);
        }

        public void GoBack()
        {
            Messenger.Send(new CloseMessage(this));
        }
    }
}
