namespace TestHarness.Ext.Navigation.Localization;

[TestSectionRoot("Localization",TestSections.Localization, typeof(LocalizationHostInit))]
public sealed partial class LocalizationMainPage : BaseTestSectionPage
{
	public LocalizationMainPage()
	{
		this.InitializeComponent();
	}

	public async void OnePageClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewModelAsync<LocalizationOneViewModel>(this);
	}

}
