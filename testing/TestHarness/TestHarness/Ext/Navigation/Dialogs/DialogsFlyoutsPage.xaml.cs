namespace TestHarness.Ext.Navigation.Dialogs;

public sealed partial class DialogsFlyoutsPage : Page
{
	public DialogsFlyoutsPage()
	{
		this.InitializeComponent();
	}

	private async void FlyoutFromBackgroundClick(object sender, RoutedEventArgs e)
	{
		var nav = this.Navigator()!;
		var result = await Task.Run(async () =>
		{
			// Note: Passing object in as sender to make sure navigation doesn't use the sender when showing flyout
			return await nav.NavigateRouteAsync(new object(), "!DialogsBasic");
		});

	}

	private async void FlyoutFromBackgroundRequestingDataClick(object sender, RoutedEventArgs e)
	{
		var nav = this.Navigator()!;
		var result = await Task.Run(async () =>
		{
			// Note: Passing object in as sender to make sure navigation doesn't use the sender when showing flyout
			return await nav.NavigateRouteForResultAsync<Widget>(new object(), "!DialogsBasic").AsResult();
		});

	}

	private async void FlyoutRequestingDataWithCancelClick(object sender, RoutedEventArgs args)
	{
#if USE_UITESTS && __ANDROID__
		var waitTime = 4;
#else
		var waitTime = 2;
#endif
		var cancelSource = new CancellationTokenSource(TimeSpan.FromSeconds(waitTime));
		var nav = this.Navigator()!;
		var result = await nav.NavigateRouteForResultAsync<Widget>(new object(), "!DialogsBasic", cancellation: cancelSource.Token).AsResult();
	}
}
