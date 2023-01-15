

using System.Net;
using Uno.Extensions.Logging;

namespace Uno.Extensions;

internal record CookieManager(ILogger<CookieManager> Logger) : ICookieManager
{
	public CookieContainer? ClearCookies(HttpMessageHandler Handler, Uri requestUri)
	{
		if (Handler is null)
		{
			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage("Handler is null, so not able to clear cookie container");
		}

		if (Handler is DelegatingHandler delegating &&
			delegating.InnerHandler is { } inner)
		{
			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage("Handler is DelegatingHandler, so clearing inner handler");
			return ClearCookies(inner, requestUri);
		}

		CookieContainer? cookies = default;
		if (Handler is HttpClientHandler clientHandler)
		{
			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"{clientHandler.GetType().Name} - settings cookie container if it doesn't exist");
			cookies = clientHandler.CookieContainer;
			if (cookies is null)
			{
				cookies = new CookieContainer();
				try
				{
					clientHandler.CookieContainer = cookies;
				}
				catch
				{
					// If this handler has already been used, we can't reset
					// the container but we can still expire all the cookies
					// to clear them
				}
			}

			try
			{
				clientHandler.UseCookies = true;
			}
			catch
			{
				// If this handler has already been used, we can't reset
				// the container but we can still expire all the cookies
				// to clear them
			}
		}
#if __IOS__ || __Android__ || WINDOWS || WINDOWS_UWP
		else if (
#if __IOS__
			Handler is NSUrlSessionHandler native
#elif __ANDROID__
#if NET6_0_OR_GREATER
			Handler is Xamarin.Android.Net.AndroidMessageHandler native
#else
			Handler is Xamarin.Android.Net.AndroidClientHandler native
#endif
#elif WINDOWS || WINDOWS_UWP
			Handler is WinHttpHandler native
#else
			false
#endif
		)
		{
			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"{native.GetType().Name} - settings cookie container if it doesn't exist");
			cookies = native.CookieContainer;
			if (cookies is null)
			{
				cookies = new CookieContainer();
				try
				{
					native.CookieContainer = cookies;
				}
				catch
				{
					// If this handler has already been used, we can't reset
					// the container but we can still expire all the cookies
					// to clear them
				}
			}

			try
			{

#if __IOS__
				native.UseCookies = true;
#elif __ANDROID__
#if NET6_0_OR_GREATER
				native.UseCookies = true;
#else
				native.UseCookies = true;
#endif
#elif WINDOWS || WINDOWS_UWP
				native.CookieUsePolicy = CookieUsePolicy.UseSpecifiedCookieContainer;
#endif

			}
			catch
			{
				// If this handler has already been used, we can't reset
				// the container but we can still expire all the cookies
				// to clear them
			}
		}
#endif

		if (cookies is not null)
		{
			// Forcibly expire any existing cookie
			var existingCookies = cookies.GetCookies(requestUri);
			foreach (Cookie co in existingCookies)
			{
				co.Expires = DateTime.Now.Subtract(TimeSpan.FromDays(1));
				co.Expired = true;
			}
		}
		return cookies;
	}
}
