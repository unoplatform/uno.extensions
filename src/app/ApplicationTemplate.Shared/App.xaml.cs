using System.Diagnostics;
using System.Threading.Tasks;
using ApplicationTemplate.Views;
//using Chinook.SectionsNavigation;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Graphics.Display;
using Uno.Extensions.Hosting;
using Uno.Extensions.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CommunityToolkit.Mvvm.Messaging;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Uno.Extensions.Logging.Serilog;
using Uno.Extensions.Logging;
using ApplicationTemplate.Client;
using ApplicationTemplate.Presentation;
using CommunityToolkit.Mvvm.DependencyInjection;
using Uno.Extensions.Configuration;

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

namespace ApplicationTemplate
{
    sealed partial class App : Application
    {
        private IHost host { get; }
        public App()
        {
            Instance = this;
            host = UnoHost.CreateDefaultBuilder()
                .UseEnvironment("Staging")
                .UseAppSettingsForHostConfiguration<App>()
                .UseHostConfigurationForApp()
                .UseEnvironmentAppSettings<App>()
                .UseCustomAppSettings()
                .UseWritableSettings< EndpointOptions>(ctx => ctx.Configuration.GetSection("ChuckNorrisEndpoint"))
                //.ConfigureServices((ctx,services) =>
                //{
                //    //services.Configure<EndpointOptions>(ctx.Configuration.GetSection("ChuckNorrisEndpoint"));
                //    services.ConfigureWritable<EndpointOptions>(ctx.Configuration.GetSection("ChuckNorrisEndpoint"));
                //})
                .UseRouting<RouterConfiguration, LaunchMessage>(() => App.Instance.NavigationFrame)
                .ConfigureServices(services =>
                {
                    services.AddTransient<HomePageViewModel>();
                })
                .UseUnoLogging(logBuilder =>
                {
                    logBuilder
                    .MinimumLogLevel(Microsoft.Extensions.Logging.LogLevel.Information)
                    .CoreLogLevel(Microsoft.Extensions.Logging.LogLevel.Information)
                    .XamlBindingLogLevel(Microsoft.Extensions.Logging.LogLevel.Information)
                    .XamlLayoutLogLevel(Microsoft.Extensions.Logging.LogLevel.Information)
                    .XamlLogLevel(Microsoft.Extensions.Logging.LogLevel.Information);
                })
                .UseSerilog(true,true, true)
                .Build();
            Ioc.Default.ConfigureServices(host.Services);
            //host = UnoHost.CreateDefaultHostWithStartup<AppServiceConfigurer>();
            //var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

            //var services = host.Services;
            //lifetime.ApplicationStarted.Register(() =>
            //{
            //    //var router = services.GetRequiredService<IRouter>();
            //    //var messenger = services.GetRequiredService<IMessenger>();
            //    //messenger.Send<BaseRoutingMessage>(new ShowMessage(this));

            //});
            //lifetime.ApplicationStopping.Register(() =>
            //{
            //});
            //lifetime.ApplicationStopped.Register(() =>
            //{
            //});

            //Startup = new Startup();
            //Startup.PreInitialize();

            InitializeComponent();

            ConfigureOrientation();
        }

        public Activity ShellActivity { get; } = new Activity(nameof(Shell));

        public static App Instance { get; private set; }

        public static Startup Startup { get; private set; }

        public Shell Shell { get; private set; }

        public Frame NavigationFrame => Shell?.NavigationFrame;

        public Window CurrentWindow { get; private set; }

        //-:cnd:noEmit
#if WINDOWS_UWP
//+:cnd:noEmit
        protected override void OnLaunched(Windows.ApplicationModel.Activation.LaunchActivatedEventArgs args)
        {
            InitializeAndStart(args);
        }
//-:cnd:noEmit
#else
        //+:cnd:noEmit
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            InitializeAndStart(args.UWPLaunchActivatedEventArgs);
        }
        //-:cnd:noEmit
#endif
        //+:cnd:noEmit

        //-:cnd:noEmit
#if !(NET5_0 && WINDOWS)
//+:cnd:noEmit
        //protected override void OnActivated(IActivatedEventArgs args)
        //{
        //    // This is where your app launches if you use custom schemes, Universal Links, or Android App Links.
        //    InitializeAndStart(args);
        //}
//-:cnd:noEmit
#endif
        //+:cnd:noEmit

        private void InitializeAndStart(IActivatedEventArgs args)
        {
#if NET5_0 && WINDOWS
            CurrentWindow = new Window();
#elif WINDOWS_UWP
			CurrentWindow = Window.Current;
#else
            CurrentWindow = Microsoft.UI.Xaml.Window.Current;
#endif

            Shell = CurrentWindow.Content as Shell;

            var isFirstLaunch = Shell == null;

            if (isFirstLaunch)
            {
                ConfigureViewSize();
                ConfigureStatusBar();

                //Startup.Initialize();

                //#if (IncludeFirebaseAnalytics)
                //                ConfigureFirebase();
                //#endif

                ShellActivity.Start();

                CurrentWindow.Content = Shell = new Shell(args);

                ShellActivity.Stop();

            }

            CurrentWindow.Activate();

            _ = Task.Run(() =>
            {
                //Startup.Start();
                host.Run();
            });
        }

        private void ConfigureOrientation()
        {
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
        }

        private void ConfigureViewSize()
        {
            //-:cnd:noEmit
#if WINDOWS_UWP
//+:cnd:noEmit
            ApplicationView.PreferredLaunchViewSize = new Size(480, 800);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(320, 480));
//-:cnd:noEmit
#endif
            //+:cnd:noEmit
        }

        private void ConfigureStatusBar()
        {
            var resources = Application.Current.Resources;

            //-:cnd:noEmit
#if WINDOWS_UWP || (NET5_0 && WINDOWS) || HAS_UNO_SKIA
            //+:cnd:noEmit
            var statusBarHeight = 0;
            //-:cnd:noEmit
#else
            //+:cnd:noEmit
            var statusBarHeight = Windows.UI.ViewManagement.StatusBar.GetForCurrentView().OccludedRect.Height;
                        Windows.UI.ViewManagement.StatusBar.GetForCurrentView().ForegroundColor = Windows.UI.Colors.White;
            //-:cnd:noEmit
#endif
            //+:cnd:noEmit

            resources.Add("StatusBarDouble", (double)statusBarHeight);
            resources.Add("StatusBarThickness", new Thickness(0, statusBarHeight, 0, 0));
            resources.Add("StatusBarGridLength", new GridLength(statusBarHeight, GridUnitType.Pixel));
        }

        //#if (IncludeFirebaseAnalytics)
        //        private void ConfigureFirebase()
        //        {
        ////-:cnd:noEmit
        //#if __IOS__
        ////+:cnd:noEmit
        //            // This is used to initalize firebase and crashlytics.
        //            Firebase.Core.App.Configure();
        ////-:cnd:noEmit
        //#endif
        ////+:cnd:noEmit
        //        }
        //#endif
    }



}
