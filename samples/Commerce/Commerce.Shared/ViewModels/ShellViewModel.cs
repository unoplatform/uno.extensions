using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Navigation;

namespace Commerce.ViewModels
{
	public class ShellViewModel
	{
		public static ShellViewModel Instance { get; private set; }

		private INavigator Navigator { get; }

		private IServiceProvider Services { get; }
		public ShellViewModel(INavigator navigator, IServiceProvider services)
		{
			Navigator = navigator;
			Instance = this;
			Services = services;
			Login();
		}

		public async Task Login()
		{
			var response = await Navigator.NavigateViewModelForResultAsync<LoginViewModel.BindableLoginViewModel, Credentials>(this,"-/");
			var resultTask = response.Result;
			var result = await resultTask;
			if (result.MatchSome(out var creds) &&
				creds is not null &&
				creds.UserName is { Length: > 0 })
			{
				var homeResponse = await Navigator.NavigateRouteForResultAsync<Credentials>(this, "-/Home");
				var homeResult = await homeResponse.Result;
				Login();
			}
			else
			{
				Login();
			}


		}
	}
}
