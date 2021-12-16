using System;
using System.Collections.Generic;
using System.Text;
using Playground.Views;
using Uno.Extensions.Navigation;

namespace Playground.ViewModels
{
    public class ShellViewModel
    {
		public ShellViewModel(INavigator navigator)
		{
			navigator.NavigateViewAsync<HomePage>(this);
		}
	}
}
