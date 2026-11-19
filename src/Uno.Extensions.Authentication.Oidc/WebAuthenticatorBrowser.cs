using Duende.IdentityModel.OidcClient.Browser;
using Microsoft.Extensions.Logging.Abstractions;
using Windows.Foundation;

namespace Uno.Extensions.Authentication.Oidc;

/// <summary>
/// The OIDC client's <see cref="IBrowser"/> over the platform's <c>WebAuthenticationBroker</c>
/// (WinUIEx's authenticator on WinAppSDK): opens the authorization or end-session request and
/// hands the redirect back to the client as a <see cref="BrowserResult"/>.
/// </summary>
public class WebAuthenticatorBrowser : IBrowser
{
	private const string TimeoutError = "The sign-in page did not redirect back before the authentication timeout";

	private readonly ILogger<WebAuthenticatorBrowser> _logger;

	/// <summary>
	/// Creates a browser that logs nothing. The host's DI uses
	/// <see cref="WebAuthenticatorBrowser(ILogger{WebAuthenticatorBrowser})"/>.
	/// </summary>
	public WebAuthenticatorBrowser()
		: this(NullLogger<WebAuthenticatorBrowser>.Instance)
	{
	}

	/// <summary>
	/// Creates a browser that logs failed flows through <paramref name="logger"/>.
	/// </summary>
	/// <param name="logger">The logger for failed flows.</param>
	public WebAuthenticatorBrowser(ILogger<WebAuthenticatorBrowser> logger)
	{
		_logger = logger;
	}

	/// <inheritdoc />
	/// <remarks>
	/// The caller's <paramref name="cancellationToken"/> propagates as an
	/// <see cref="OperationCanceledException"/> rather than a result: a result would read as a
	/// failed sign-in, and a cancelled sign-out would count as complete. Both the client's
	/// <see cref="BrowserOptions.Timeout"/> and the broker's own timeout come back as
	/// <see cref="BrowserResultType.Timeout"/>.
	/// </remarks>
	public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
	{
		using var timeout = new CancellationTokenSource(options.Timeout);
		using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);

		try
		{
#if WINDOWS
			var userResult = await WinUIEx.WebAuthenticator.AuthenticateAsync(new Uri(options.StartUrl), new Uri(options.EndUrl), linked.Token);
			var callbackurl = $"{options.EndUrl}/?{string.Join("&", userResult.Properties.Select(x => $"{x.Key}={x.Value}"))}";
			return new BrowserResult
			{
				Response = callbackurl
			};
#else

			var userResult = await WebAuthenticationBroker
				.AuthenticateAsync(WebAuthenticationOptions.None, new Uri(options.StartUrl), new Uri(options.EndUrl))
				.AsTask(linked.Token);

			return ToBrowserResult(userResult);
#endif
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (OperationCanceledException)
		{
			// Only the client's own timeout is left: the broker reports its timeout as a result
			// (mapped above), not as a cancellation.
			return new BrowserResult { ResultType = BrowserResultType.Timeout, Error = TimeoutError };
		}
#if !WINDOWS
		catch (NotImplementedException ex)
		{
			// Uno's built-in broker on Skia Desktop: the static WebAuthenticationBroker was used
			// before AddOidc registered the loopback one, and keeps the stub for the process
			// lifetime. Named here because nothing in the stack trace says so.
			const string error = $"WebAuthenticationBroker has no implementation on this platform: it was used before AddOidc() ran {nameof(DesktopWebAuthenticationBrokerProvider)}.{nameof(DesktopWebAuthenticationBrokerProvider.TryRegister)}(), and it keeps the provider it first resolved. Move the earlier WebAuthenticationBroker call after UseAuthentication.";
			if (_logger.IsEnabled(LogLevel.Error))
			{
				_logger.LogError(ex, error);
			}

			return new BrowserResult { ResultType = BrowserResultType.UnknownError, Error = error };
		}
#endif
		catch (Exception ex)
		{
			if (_logger.IsEnabled(LogLevel.Warning))
			{
				_logger.LogWarning(ex, "The sign-in browser flow failed");
			}

			return new BrowserResult { ResultType = BrowserResultType.UnknownError, Error = ex.Message };
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
				new BrowserResult { ResultType = BrowserResultType.Timeout, Error = TimeoutError },
			WebAuthenticationStatus.UserCancel => new BrowserResult { ResultType = BrowserResultType.UserCancel, Error = "The user cancelled the sign-in" },
			WebAuthenticationStatus.ErrorHttp => new BrowserResult { ResultType = BrowserResultType.HttpError, Error = $"The sign-in page failed with HTTP {result.ResponseErrorDetail}" },
			_ => new BrowserResult { ResultType = BrowserResultType.UnknownError, Error = $"Unexpected web authentication status {result.ResponseStatus}" },
		};
#endif
}
