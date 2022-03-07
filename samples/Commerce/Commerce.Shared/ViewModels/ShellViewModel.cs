﻿using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Uno.Extensions.Navigation;
using Uno.Extensions.Hosting;
using Uno.Extensions.Configuration;
using System;

namespace Commerce.ViewModels
{
	public class ShellViewModel
	{
		private INavigator Navigator { get; }

		private IWritableOptions<Credentials> CredentialsSettings { get; }

		public ShellViewModel(
			ILogger<ShellViewModel> logger,
			INavigator navigator,
			IOptions<HostConfiguration> configuration,
			IWritableOptions<Credentials> credentials)
		{
			Navigator = navigator;
			CredentialsSettings = credentials;

			if (logger.IsEnabled(LogLevel.Information)) logger.LogInformation($"Launch url '{configuration.Value?.LaunchUrl}'");
			var initialRoute = configuration.Value?.LaunchRoute();

			// Go to the login page on app startup
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			Start(initialRoute);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
		}

		public async Task Start(Route? initialRoute = null)
		{
			var currentCredentials = CredentialsSettings.Value;

			if (currentCredentials?.UserName is { Length: > 0 })
			{
				if (initialRoute is not null &&
					!initialRoute.IsEmpty())
				{
					var initialResponse = await Navigator.NavigateRouteForResultAsync<object>(this, initialRoute);
					if (initialResponse is not null)
					{
						_ = await initialResponse.Result;
					}
				}
				else
				{
					var homeResponse = await Navigator.NavigateDataAsync(this, currentCredentials, Qualifiers.ClearBackStack);
				}
			}
			else
			{
				// Navigate to Login page, requesting Credentials
				var response = await Navigator.NavigateForResultAsync<Credentials>(this, Qualifiers.ClearBackStack);


				var loginResult = await response.Result;
				if (loginResult.IsSome(out var creds) && creds?.UserName is { Length: > 0 })
				{
					await CredentialsSettings.Update(c => creds);

					Start();
				}
			}
		}
	}
}
