
namespace TestHarness.Ext.Navigation.AddressBar;

public record ShellViewModel
{
	public INavigator? Navigator { get; init; }

	public ShellViewModel(INavigator navigator)
	{
		Navigator = navigator;

		_ = Start();
	}

	private async Task Start() => await Navigator.NavigateViewModelAsync<AddressBarRootModel>(this);
}
