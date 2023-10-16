using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Playground.ViewModels;

public  class AuthTokenViewModel
    {
	private INavigator Navigator { get; }

	public ICommand SaveCommand { get; }

	public string? AuthToken { get; set; }

	public AuthTokenViewModel(
		INavigator navigator)
	{

		Navigator = navigator;

		SaveCommand = new AsyncRelayCommand(Save);
	}

	public async Task Save()
	{
		await Navigator.NavigateBackWithResultAsync(this, data: AuthToken);
	}
}
