using Duende.IdentityModel.OidcClient.Browser;
using System.Diagnostics;
using Windows.Foundation;

namespace Uno.Extensions.Authentication.Oidc;

public class WebAuthenticatorBrowser : IBrowser
{
	public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
	{
		using var cts = new CancellationTokenSource(options.Timeout);
		using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);

		try
		{
			cancellationToken.ThrowIfCancellationRequested();
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

			return userResult.ResponseStatus switch
			{
				WebAuthenticationStatus.Success => new BrowserResult { Response = userResult.ResponseData },
				// The desktop broker reports its own AuthenticationTimeout as UserCancel (WinRT has
				// no timeout status), marked by the error detail.
				WebAuthenticationStatus.UserCancel when userResult.ResponseErrorDetail == DesktopWebAuthenticationBrokerProvider.TimeoutErrorDetail =>
					new BrowserResult
					{
						ResultType = BrowserResultType.Timeout,
						Error = "The browser interaction did not complete within the broker's AuthenticationTimeout.",
					},
				// Error deliberately left null: OidcClient then reports the result type by name,
				// which is how OidcAuthenticationProvider tells a cancelled sign-in from a failed
				// one and keeps the previous session (spec 013 F5).
				WebAuthenticationStatus.UserCancel => new BrowserResult { ResultType = BrowserResultType.UserCancel },
				_ => new BrowserResult
				{
					ResultType = BrowserResultType.HttpError,
					Error = $"{userResult.ResponseStatus} (error detail {userResult.ResponseErrorDetail})",
				},
			};
#endif
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			// The caller cancelled: propagate rather than reporting a failed login (spec 013 F3).
			throw;
		}
		catch (OperationCanceledException)
		{
			// Only the per-invocation timeout can be left: surface it as such.
			return new BrowserResult()
			{
				ResultType = BrowserResultType.Timeout,
				Error = $"The browser interaction did not complete within {options.Timeout}."
			};
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
}



