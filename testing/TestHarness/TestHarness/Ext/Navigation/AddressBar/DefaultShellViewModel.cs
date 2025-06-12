
namespace TestHarness.Ext.Navigation.AddressBar;

public record DefaultShellViewModel
{
	public INavigator? Navigator { get; init; }

	public DefaultShellViewModel(INavigator navigator)
	{
		Navigator = navigator;
	}
}
