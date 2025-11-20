namespace TestHarness.Ext.Navigation.ForResult;

public partial record ForResultSecondViewModel
{
	private readonly INavigator _navigator;

	public ForResultSecondViewModel(INavigator navigator)
	{
		_navigator = navigator;
		
		// Simulate heavy initialization work
		// This creates the race condition window where the user can press back
		_ = InitializeAsync();
	}

	public string InitStatus { get; init; } = "Initializing...";

	private async Task InitializeAsync()
	{
		// Simulate heavy async initialization (e.g., loading data, complex calculations)
		// During this time, if the user presses the back button in NavigationBar,
		// the SystemNavigationManager.BackRequested event fires
		await Task.Delay(2000);
		
		this.InitStatus = "Initialization complete!";
	}
}
