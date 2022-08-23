namespace TestHarness.Ext.Navigation.Apps.Commerce;

public record CommerceLoginViewModel(INavigator Navigator)
{
	public async void Login()
	{
		await Navigator.NavigateBackWithResultAsync(this, data: Option.Some(new CommerceCredentials()));
	}

}
