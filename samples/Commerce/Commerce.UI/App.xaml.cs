using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Commerce.Data;
using Commerce.Data.Models;
using Commerce.Business;
using Commerce.Business.Models;
using Commerce.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Configuration;
using Uno.Extensions.Hosting;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.Toolkit;
using Uno.Extensions.Serialization;
using Commerce.Views;
using Commerce.Reactive;

#if WINUI
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
using Window = Microsoft.UI.Xaml.Window;
using CoreApplication = Windows.ApplicationModel.Core.CoreApplication;
#else
using Windows.ApplicationModel;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using LaunchActivatedEventArgs = Windows.ApplicationModel.Activation.LaunchActivatedEventArgs;
using Window = Windows.UI.Xaml.Window;
using CoreApplication = Windows.ApplicationModel.Core.CoreApplication;
#endif


namespace Commerce
{
	public sealed partial class App : Application
	{
		private Window? _window;
		public Window? Window => _window;

		public App()
		{
			this.InitializeComponent();
		}

		/// <summary>
		/// Invoked when the application is launched normally by the end user.  Other entry points
		/// will be used such as when the application is launched to open a specific file.
		/// </summary>
		/// <param name="args">Details about the launch request and process.</param>
		protected async override void OnLaunched(LaunchActivatedEventArgs args)
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
#else
			_window = Window.Current;
#endif

			var notif = _host.Services.GetRequiredService<IRouteNotifier>();
			notif.RouteChanged += RouteUpdated;


			_window.AttachNavigation(_host.Services);
			_window.Activate();

			await Task.Run(async () =>
			{
				await _host.StartAsync();
			});

		}

		public void RouteUpdated(object? sender, RouteChangedEventArgs e)
		{
			try
			{
				var rootRegion = e.Region.Root();
				var route = rootRegion.GetRoute();


#if !__WASM__ && !WINUI
				CoreApplication.MainView?.DispatcherQueue.TryEnqueue(() =>
				{
					var appTitle = ApplicationView.GetForCurrentView();
					appTitle.Title = "Commerce: " + (route + "").Replace("+", "/");
				});
#endif

			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
			}
		}
	}
}
