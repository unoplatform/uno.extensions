namespace TestHarness.Ext.Navigation.ForResult;

public sealed partial class ForResultFirstPage : Page
{
	public ForResultFirstPage()
	{
		this.InitializeComponent();
	}

	private async void NavigateForResultButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			NavigateForResultButton.IsEnabled = false;
			StatusText.Text = "Status: Navigating...";
			ResultText.Text = "";

			var navigator = this.Navigator();
			if (navigator is null)
			{
				StatusText.Text = "Status: Error - Navigator is null";
				return;
			}

			// Navigate to second page with ForResult
			var response = await navigator.NavigateViewModelForResultAsync<ForResultSecondViewModel, string>(this);
			
			if (response?.Result is { } resultTask)
			{
				StatusText.Text = "Status: Waiting for result...";
				var result = await resultTask;
				
				if (result.Type == OptionType.Some)
				{
					ResultText.Text = $"Result: {result.SomeOrDefault()}";
					StatusText.Text = "Status: Completed successfully";
				}
				else
				{
					ResultText.Text = "Result: None (back navigation detected)";
					StatusText.Text = "Status: Completed with None";
				}
			}
			else
			{
				StatusText.Text = "Status: Navigation failed";
				ResultText.Text = "Result: Navigation response was null";
			}
		}
		catch (Exception ex)
		{
			StatusText.Text = $"Status: Error - {ex.Message}";
			ResultText.Text = $"Exception: {ex}";
		}
		finally
		{
			NavigateForResultButton.IsEnabled = true;
		}
	}
}
