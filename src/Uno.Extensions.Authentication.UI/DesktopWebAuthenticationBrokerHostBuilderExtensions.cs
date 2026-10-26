#if !WINDOWS
using Microsoft.Extensions.Hosting;

namespace Uno.Extensions.Authentication;

/// <summary>How <see cref="DesktopWebAuthenticationBrokerProvider.TryRegister"/> ended.</summary>
internal enum DesktopWebAuthenticationBrokerRegistration
{
	/// <summary>Not a desktop OS: the platform's native broker keeps handling flows.</summary>
	NotDesktop,

	/// <summary>Another provider was registered first and keeps winning.</summary>
	OtherProviderRegistered,

	/// <summary>The desktop loopback broker is the registered provider.</summary>
	Registered,
}

/// <summary>
/// Registers the desktop loopback broker from <c>AddWeb</c>/<c>AddOidc</c>. The registration has
/// to run at builder time, before anything can touch Uno's static <c>WebAuthenticationBroker</c>
/// (which keeps whatever provider is registered when it is first used) - when no logger exists
/// yet. So the outcome is logged once the host is built, through the host's own logger.
/// </summary>
internal static class DesktopWebAuthenticationBrokerHostBuilderExtensions
{
	internal static IHostBuilder RegisterDesktopWebAuthenticationBroker(this IHostBuilder hostBuilder)
	{
		var registration = DesktopWebAuthenticationBrokerProvider.Register();
		return hostBuilder.ConfigureServices((ctx, services) =>
		{
			// One line per host, however many providers registered.
			if (ctx.IsRegistered(nameof(RegisterDesktopWebAuthenticationBroker)))
			{
				return;
			}

			services.AddSingleton<IServiceInitialize>(sp =>
				new RegistrationAnnouncer(registration, sp.GetRequiredService<ILogger<DesktopWebAuthenticationBrokerProvider>>()));
		});
	}

	private sealed record RegistrationAnnouncer(DesktopWebAuthenticationBrokerRegistration Registration, ILogger Logger) : IServiceInitialize
	{
		public void Initialize()
		{
			switch (Registration)
			{
				case DesktopWebAuthenticationBrokerRegistration.Registered when Logger.IsEnabled(LogLevel.Debug):
					Logger.LogDebug("The desktop loopback WebAuthenticationBroker is registered for the sign-in flows of this process");
					break;
				case DesktopWebAuthenticationBrokerRegistration.OtherProviderRegistered when Logger.IsEnabled(LogLevel.Information):
					// Said once so a missing loopback broker is explainable from the log.
					Logger.LogInformation("An IWebAuthenticationBrokerProvider was registered before AddWeb/AddOidc ran; the desktop loopback broker is not used");
					break;
			}
		}
	}
}
#endif
