using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Uno.Extensions.Navigation;
using Uno.Extensions.Reactive;
using Windows.ApplicationModel.Core;

namespace Commerce.ViewModels;

public partial class LoginViewModel
{
	private readonly INavigator _navigator;
	private readonly IState<string> _error;

	private LoginViewModel(
		INavigator navigator,
		IOptions<AppInfo> appInfo,
		IFeed<Credentials> credentials,
		IState<string> error,
		ICommandBuilder login)
	{
		Title = appInfo.Value.Title;
		_navigator = navigator;
		_error = error;

		login
			.Given(credentials)
			.When(CanLogin)
			.Then(Login);
	}

	public string Title { get;  }

	private bool CanLogin(Credentials credentials)
		=> credentials is { UserName.Length: > 0 } and { Password.Length: > 0 };

	private async ValueTask Login(Credentials credentials, CancellationToken ct)
	{
		if (credentials is { UserName.Length: >= 3 } and { Password.Length: >= 3 })
		{
			await _error.Set(default, ct);
			await Task.Delay(1000, ct);

			CoreApplication.MainView?.DispatcherQueue.TryEnqueue(() => _navigator.NavigateToRouteAsync(this, "/-/Home", cancellation: ct));
		}
		else
		{
			await _error.Set("Login and password must be at least 3 characters long.", ct);
		}
	}
}
