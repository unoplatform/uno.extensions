namespace TestHarness.Ext.Navigation.Apps.Commerce;

[TestSectionRoot("Sample App: Commerce",TestSections.Apps_Commerce, typeof(CommerceHostInit))]
public sealed partial class CommerceMainPage : BaseTestSectionPage
{
	public CommerceMainPage()
	{
		this.InitializeComponent();
	}

	public async void ShowAppClick(object sender, RoutedEventArgs e)
	{
		await NavigationRoot.Navigator()!.NavigateRouteAsync(this,"");
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
