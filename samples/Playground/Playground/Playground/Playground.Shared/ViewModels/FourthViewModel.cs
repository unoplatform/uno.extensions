using System.Threading.Tasks;
using Uno.Extensions.Navigation;

namespace Playground.ViewModels
{
	public class FourthViewModel
    {
		public string Title => "Fourth page with View Model";

		private INavigator _navigator;
		public FourthViewModel(INavigator navigator)
		{
			_navigator = navigator;
		}

		public async Task NavigateToFifth()
		{
			await _navigator.NavigateViewModelAsync<FifthViewModel>(this);
		}
	}
}
