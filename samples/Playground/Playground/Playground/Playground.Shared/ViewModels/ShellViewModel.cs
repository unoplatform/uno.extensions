namespace Playground.ViewModels;

public class ShellViewModel
{
	public ShellViewModel(INavigator navigator)
	{
		navigator.NavigateViewAsync<HomePage>(this);
	}
}
