namespace TestHarness.Ext.Navigation.Apps.Commerce;

[TestSectionRoot("Sample App: Commerce",TestSections.Apps_Commerce, typeof(CommerceHostInit))]
[TestSectionRoot("Sample App: Commerce (ShellControl)", TestSections.Apps_Commerce_ShellControl, typeof(CommerceShellControlHostInit))]
public sealed partial class CommerceMainPage : BaseTestSectionPage
{
	public CommerceMainPage()
	{
		this.InitializeComponent();
	}

	public async void ShowAppClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateRouteAsync(this,"");
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
