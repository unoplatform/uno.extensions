namespace TestHarness.Ext.Navigation.Apps.Commerce;

public record CommerceShellViewModel
{
	public INavigator? Navigator { get; init; }

	private CommerceCredentials? _credentials;

	public CommerceShellViewModel(INavigator navigator)
	{
		Navigator = navigator;

		_ = Start();
	}

	public async Task Start()
	{
		if (_credentials is not null)
		{
			await Navigator!.NavigateDataAsync(this, _credentials, Qualifiers.ClearBackStack);
			_credentials = null; // Reset credentials to logout works (in actual app the process of logging out would clear the cached credentials)
		}
		else
		{
			_credentials = await Navigator!.GetDataAsync<CommerceCredentials>(this, qualifier: Qualifiers.ClearBackStack);
			_ = Start();
		}
	}

}
