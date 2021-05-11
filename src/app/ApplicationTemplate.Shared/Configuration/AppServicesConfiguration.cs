using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using ApplicationTemplate;
using ApplicationTemplate.Business;
using ApplicationTemplate.Presentation;
using MessageDialogService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Nventive.ExtendedSplashScreen;
using Windows.ApplicationModel;
using Xamarin.Essentials.Implementation;
using Xamarin.Essentials.Interfaces;

namespace ApplicationTemplate
{
	/// <summary>
	/// This class is used for application services configuration.
	/// - Configures business services.
	/// - Configures platform services.
	/// </summary>
	public static class AppServicesConfiguration
	{
		/// <summary>
		/// Adds the application services to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services">Service collection.</param>
		/// <returns><see cref="IServiceCollection"/>.</returns>
		public static IServiceCollection AddAppServices(this IServiceCollection services)
		{
			return services
				.AddXamarinEssentials()
				.AddMessageDialog()
				.AddSingleton<IBackgroundScheduler>(s => TaskPoolScheduler.Default.ToBackgroundScheduler())
				.AddSingleton<IApplicationSettingsService, ApplicationSettingsService>()
				.AddSingleton<IPostService, PostService>()
				.AddSingleton<IChuckNorrisService, ChuckNorrisService>()
				.AddSingleton<IAuthenticationService, AuthenticationService>()
				.AddSingleton<IUserProfileService, UserProfileService>()
				.AddSingleton<DiagnosticsCountersService>();
		}

		private static IServiceCollection AddXamarinEssentials(this IServiceCollection services)
		{
			return services
				.AddSingleton<IDeviceInfo, DeviceInfoImplementation>()
				.AddSingleton<IAppInfo, AppInfoImplementation>()
//-:cnd:noEmit
#if WINDOWS_UWP || __IOS__ || __ANDROID__
//+:cnd:noEmit
				//.AddSingleton<IBrowser>(s => new DispatcherBrowserDecorator(new BrowserImplementation(), App.Instance.Shell.Dispatcher))
//-:cnd:noEmit
#endif
//+:cnd:noEmit
				.AddSingleton<IEmail, EmailImplementation>();
		}

		private static IServiceCollection AddMessageDialog(this IServiceCollection services)
		{
			return services.AddSingleton<IMessageDialogService>(s =>
//-:cnd:noEmit
#if WINDOWS_UWP || __IOS__ || __ANDROID__
//+:cnd:noEmit
				new MessageDialogService.MessageDialogService(
					() => s.GetRequiredService<Windows.UI.Core.CoreDispatcher>(),
					new MessageDialogBuilderDelegate(
						key => s.GetRequiredService<IStringLocalizer>()[key]
					)
				)
//-:cnd:noEmit
#else
//+:cnd:noEmit
				new AcceptOrDefaultMessageDialogService()
//-:cnd:noEmit
#endif
//+:cnd:noEmit
			);
		}
	}
}
