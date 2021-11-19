using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Uno.Extensions.Navigation;
using Uno.Extensions.Hosting;
using System;

namespace Commerce.ViewModels
{
	public class ShellViewModel
	{
		private INavigator Navigator { get; }

		public ShellViewModel(INavigator navigator, IConfiguration configuration)
		{
			Navigator = navigator;

			var launchUrl = configuration.GetValue(HostingConstants.WasmLaunchUrlKey, defaultValue: string.Empty);

			if (!string.IsNullOrWhiteSpace(launchUrl))
			{
				var url = new UriBuilder(launchUrl);
				var query = url.Query;
				var path = (url.Path + (!string.IsNullOrWhiteSpace(query) ? "?" : "") + query + "").TrimStart('/');
				if (!string.IsNullOrWhiteSpace(path))
				{
					Navigator.NavigateRouteAsync(this, path);
					return;
				}
			}

			// Go to the login page on app startup
			Login();
		}

		public async Task Login()
		{
			// Navigate to Login page, requesting Credentials
			var response = await Navigator.NavigateViewModelForResultAsync<LoginViewModel.BindableLoginViewModel, Credentials>(this, Schemes.ClearBackStack);
			

			var loginResult = await response.Result;
			if (loginResult.MatchSome(out var creds) && creds?.UserName is { Length: > 0 })
			{
				// Login successful, so navigate to Home
				// Wait for a credentials object to be returned 
				var homeResponse = await Navigator.NavigateViewModelForResultAsync<HomeViewModel, Credentials>(this, Schemes.ClearBackStack);
				_= await homeResponse.Result;
			}

			// At this point we assume that either the login failed, or that the navigation to home
			// was completed by a response being sent back. We don't actually care about the response,
			// since we only care that the navigation has completed and that we should again show the login 
			Login();
		}
	}
}
