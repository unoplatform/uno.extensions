//-:cnd:noEmit

namespace MyExtensionsApp.ViewModels;

public class ShellViewModel
{
	private INavigator Navigator { get; }


	public ShellViewModel(
		INavigator navigator)
	{

		Navigator = navigator;

		Start();
	}

	public async Task Start()
	{
		await Navigator.NavigateViewModelAsync<MainViewModel>(this);
	}
}