//-:cnd:noEmit

namespace MyExtensionsApp.ViewModels;

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
