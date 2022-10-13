namespace TestHarness.Ext.Navigation.Responsive;

[TestSectionRoot("Responsive Navigation",TestSections.Navigation_Responsive, typeof(ResponsiveHostInit))]
public sealed partial class ResponsiveMainPage : BaseTestSectionPage
{
	public ResponsiveMainPage()
	{
		this.InitializeComponent();
	}

	public async void ResponsiveHomeClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewModelAsync<ResponsiveHomeViewModel>(this);
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
