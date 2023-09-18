
using IdentityModel.OidcClient.Browser;
using System.Diagnostics;

namespace Uno.Extensions.Authentication.Oidc;

public class WebAuthenticatorBrowser : IBrowser
{
	public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
	{
		try
		{
#if WINDOWS
			var userResult = await WinUIEx.WebAuthenticator.AuthenticateAsync(new Uri(options.StartUrl), new Uri(options.EndUrl));
			var callbackurl = $"{options.EndUrl}/?{string.Join("&", userResult.Properties.Select(x => $"{x.Key}={x.Value}"))}";
			return new BrowserResult
			{
				Response = callbackurl
			};
#else
			var userResult = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, new Uri(options.StartUrl), new Uri(options.EndUrl));

			return new BrowserResult
			{
				Response = userResult.ResponseData
			};
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
}



