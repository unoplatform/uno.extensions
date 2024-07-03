namespace TestHarness.Ext.Navigation.Apps.Regions;

[TestSectionRoot("Sample App: Regions", TestSections.Apps_Regions, typeof(RegionsHostInit))]
public sealed partial class RegionsMainPage : BaseTestSectionPage
{
	public RegionsMainPage()
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
