
namespace Playground.Views;

public sealed partial class FlyoutsPopupsDrawerPage : Page
{
	public FlyoutsPopupsDrawerPage()
	{
		this.InitializeComponent();
	}

	private void OpenDrawerClick(object sender, RoutedEventArgs e)
	{
		SampleDrawerControl.IsOpen = !SampleDrawerControl.IsOpen;
	}

	private async void OpenDrawerResponseClick(object sender, RoutedEventArgs e)
	{
		var result = await this.Navigator()!.NavigateRouteForResultAsync<Widget>(this, "MyDrawer/Show").AsResult();
		await this.Navigator()!.ShowMessageDialogAsync(this, content: $"Widget: {result.SomeOrDefault()?.Name}");
	}

	private async void CloseDrawerWithResultClick(object sender, RoutedEventArgs e)
	{
		await (sender as FrameworkElement)!.Navigator()!.NavigateBackWithResultAsync(this, data: new Widget { Name = "Drawer Widget" });
	}
}
