
namespace TestHarness.Ext.Navigation.Dialogs;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
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
}
