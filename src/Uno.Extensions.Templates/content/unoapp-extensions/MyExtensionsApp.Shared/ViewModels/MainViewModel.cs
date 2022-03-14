//-:cnd:noEmit
using System.Threading.Tasks;
using Uno.Extensions.Navigation;

namespace MyExtensionsApp.ViewModels
{
	public class MainViewModel
	{
		private INavigator Navigator { get; }


		public MainViewModel(
			INavigator navigator)
		{ 
		
			Navigator = navigator;
		}

		public async Task GoToSecondPage()
		{
			await Navigator.NavigateViewModelAsync<SecondViewModel>(this);
		}
	}
}
