using Duende.IdentityModel.OidcClient.Browser;
using System.Diagnostics;
using Windows.Foundation;

namespace Uno.Extensions.Authentication.Oidc;

public class WebAuthenticatorBrowser : IBrowser
{
	public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
	{
		var cts = new CancellationTokenSource(options.Timeout);
		var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);

		try
		{
#if WINDOWS
			var userResult = await WinUIEx.WebAuthenticator.AuthenticateAsync(new Uri(options.StartUrl), new Uri(options.EndUrl), linkedCts.Token);
			var callbackurl = $"{options.EndUrl}/?{string.Join("&", userResult.Properties.Select(x => $"{x.Key}={x.Value}"))}";
			return new BrowserResult
			{
				Response = callbackurl
			};
#else

			var userResult = await WebAuthenticationBroker
				.AuthenticateAsync(WebAuthenticationOptions.None, new Uri(options.StartUrl), new Uri(options.EndUrl))
				.AsTask(linkedCts.Token);

			return ToBrowserResult(userResult);
#endif
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex);
			return new BrowserResult()
			{
				ResultType = BrowserResultType.UnknownError,
				Error = ex.ToString()
			};
		}
	}

#if !WINDOWS
	/// <summary>
	/// The broker's result in the OIDC client's terms. WinRT has no timeout status: a broker that
	/// gave up waiting reports <see cref="WebAuthenticationStatus.UserCancel"/> with
	/// <see cref="DesktopWebAuthenticationBrokerProvider.TimeoutErrorDetail"/>, which is the one
	/// cancel the client should treat as a timeout.
	/// </summary>
	internal static BrowserResult ToBrowserResult(WebAuthenticationResult result) =>
		result.ResponseStatus switch
		{
			WebAuthenticationStatus.Success => new BrowserResult { ResultType = BrowserResultType.Success, Response = result.ResponseData },
			WebAuthenticationStatus.UserCancel when result.ResponseErrorDetail == DesktopWebAuthenticationBrokerProvider.TimeoutErrorDetail =>
				new BrowserResult { ResultType = BrowserResultType.Timeout, Error = "The sign-in page did not redirect back before the authentication timeout" },
			WebAuthenticationStatus.UserCancel => new BrowserResult { ResultType = BrowserResultType.UserCancel, Error = "The user cancelled the sign-in" },
			WebAuthenticationStatus.ErrorHttp => new BrowserResult { ResultType = BrowserResultType.HttpError, Error = $"The sign-in page failed with HTTP {result.ResponseErrorDetail}" },
			_ => new BrowserResult { ResultType = BrowserResultType.UnknownError, Error = $"Unexpected web authentication status {result.ResponseStatus}" },
		};
#endif
}
