using Uno.Extensions;

namespace TestHarness.Ext.Navigation.Apps.Commerce;

public record CommerceLoginViewModel(INavigator Navigator)
{
	public async Task Login()
	{
		await Navigator.NavigateBackWithResultAsync(this, data: Option.Some(new CommerceCredentials()));
	}

}
