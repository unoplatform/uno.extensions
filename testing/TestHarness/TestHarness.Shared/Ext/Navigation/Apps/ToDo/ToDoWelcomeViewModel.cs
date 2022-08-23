namespace TestHarness.Ext.Navigation.Apps.ToDo;

public record ToDoWelcomeViewModel(INavigator Navigator)
{
	public async void Login()
	{
		ToDoShellViewModel.Credentials = new ToDoCredentials();
		await Navigator.NavigateRouteAsync(this, string.Empty);
	}
}
