namespace Uno.Extensions;

/// <summary>
/// Extensions for <see cref="IServiceCollection"/>
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Registers the native HttpMessageHandler as well as the CookieManager implementation
	/// </summary>
	/// <param name="services">The service collection to register</param>
	/// <param name="context">The host builder context</param>
	/// <returns>Updated service collection</returns>
	public static IServiceCollection AddCookieManager(this IServiceCollection services, HostBuilderContext context)
	{
		if (context.IsRegistered(nameof(AddCookieManager)))
		{
			return services;
		}

		return services
			.AddSingleton<ICookieManager, CookieManager>();
	}

	/// <summary>
	/// Registers the native HttpMessageHandler as well as the CookieManager implementation
	/// </summary>
	/// <param name="services">The service collection to register</param>
	/// <param name="context">The host builder context</param>
	/// <returns>Updated service collection</returns>
	public static IServiceCollection AddNativeHandler(this IServiceCollection services, HostBuilderContext context)
	{
		if (context.IsRegistered(nameof(AddNativeHandler)))
		{
			return services;
		}

		return services
			.AddTransient<HttpMessageHandler>(s =>
#if __IOS__
				new NSUrlSessionHandler()
#elif __ANDROID__
#if NET6_0_OR_GREATER
				new Xamarin.Android.Net.AndroidMessageHandler()
#else
			new Xamarin.Android.Net.AndroidClientHandler()
#endif
#elif WINDOWS || WINDOWS_UWP
				new WinHttpHandler()
#else
			new HttpClientHandler()
#endif
	);
	}
}
