//-:cnd:noEmit
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Uno.Extensions.Navigation;
using Uno.Extensions.Hosting;
using Uno.Extensions.Configuration;
using System;

namespace MyExtensionsApp.ViewModels
{
	public class ShellViewModel
	{
		private INavigator Navigator { get; }


		public ShellViewModel(
			INavigator navigator)
		{

			Navigator = navigator;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			Start();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
		}

		public async Task Start()
		{
			await Navigator.NavigateViewModelAsync<MainViewModel>(this);
		}
	}
}
