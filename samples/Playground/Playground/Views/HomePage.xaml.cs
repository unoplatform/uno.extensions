namespace Playground.Views;

public sealed partial class HomePage : Page
{
	public HomePage()
	{
		this.InitializeComponent();
	}

	public async void GoToSecondPageClick()
	{
		var nav = this.Navigator();
		await nav!.NavigateRouteAsync(this, "Second");
	}
}
