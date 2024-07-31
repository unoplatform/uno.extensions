namespace TestHarness.Ext.Navigation.Apps.ToDo;

public record ToDoShellViewModel
{
	public INavigator? Navigator { get; init; }

	public static ToDoCredentials? Credentials { get; set; }

	public ToDoShellViewModel(INavigator navigator)
	{
		Navigator = navigator;

		_ = Start();
	}

	public async Task Start()
	{
		if (Credentials is null)
		{
			await Navigator!.NavigateViewModelAsync<ToDoWelcomeViewModel>(this);
		}
		else
		{
			await Navigator!.NavigateViewModelAsync<ToDoHomeViewModel>(this);
		}
	}

}
