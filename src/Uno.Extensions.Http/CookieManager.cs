

using System.Net;
using Uno.Extensions.Logging;

namespace Uno.Extensions;

public record CookieManager(ILogger<CookieManager> Logger) : ICookieManager
{
	public void ClearCookies(HttpMessageHandler Handler, HttpRequestMessage request)
	{
		if(Handler is null)
		{
			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage("Handler is null, so not able to clear cookie container");
		}

		if (Handler is DelegatingHandler delegating &&
			delegating.InnerHandler is { } inner)
		{
			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage("Handler is DelegatingHandler, so clearing inner handler");
			ClearCookies(inner, request);
			return;
		}


		if (
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
			Handler is HttpClientHandler native
#endif
		)
		{
			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"{native.GetType().Name} - settings cookie container if it doesn't exist");
			var cookies = native.CookieContainer;
			try
			{
				native.CookieContainer = new CookieContainer();

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
#else
				native.UseCookies = true;
#endif

			}
			catch
			{
				// If this handler has already been used, we can't reset
				// the container but we can still expire all the cookies
				// to clear them
			}

			if(cookies is not null &&
				request.RequestUri is not null)
			{
				// Forcibly expire any existing cookie
				var existingCookies = cookies.GetCookies(request.RequestUri);
				foreach (Cookie co in existingCookies)
				{
					co.Expires = DateTime.Now.Subtract(TimeSpan.FromDays(1));
					co.Expired = true;
				}
			}
		}
	}
}
