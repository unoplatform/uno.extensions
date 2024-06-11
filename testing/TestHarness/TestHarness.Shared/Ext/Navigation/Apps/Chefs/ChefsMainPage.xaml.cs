namespace TestHarness.Ext.Navigation.Apps.Chefs;

[TestSectionRoot("Sample App: Chefs", TestSections.Apps_Chefs, typeof(ChefsHostInit))]
public sealed partial class ChefsMainPage : BaseTestSectionPage
{
	public ChefsMainPage()
	{
		this.InitializeComponent();
	}

	public async void ShowAppClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateRouteAsync(this, "");
	}
	public async void NarrowClick(object sender, RoutedEventArgs e)
	{
		VisualStateManager.GoToState(this, nameof(NarrowWindow), true);
	}

	public async void WideClick(object sender, RoutedEventArgs e)
	{
		VisualStateManager.GoToState(this, nameof(WideWindow), true);
	}
}
