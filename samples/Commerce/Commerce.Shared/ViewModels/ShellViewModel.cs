using Uno.Extensions.Navigation;

namespace Commerce.ViewModels
{
	public class ShellViewModel
    {
		public ShellViewModel(INavigator navigator)
		{
			navigator.NavigateViewModelAsync<LoginViewModel.BindableLoginViewModel>(this);
		}
    }
}
