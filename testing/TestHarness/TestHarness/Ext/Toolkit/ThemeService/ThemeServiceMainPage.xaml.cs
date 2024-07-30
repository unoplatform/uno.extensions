namespace TestHarness.Ext.Navigation.ThemeService;

[TestSectionRoot("ThemeService",TestSections.Toolkit_ThemeService, typeof(ThemeServiceHostInit))]
public sealed partial class ThemeServiceMainPage : BaseTestSectionPage
{
	public ThemeServiceMainPage()
	{
		this.InitializeComponent();
	}

	public async void OnePageClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewModelAsync<ThemeServiceOneViewModel>(this);
	}

}
