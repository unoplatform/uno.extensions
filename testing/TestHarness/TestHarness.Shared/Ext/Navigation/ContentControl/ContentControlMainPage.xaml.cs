namespace TestHarness.Ext.Navigation.ContentControl;

[TestSectionRoot("ContentControl",TestSections.Navigation_ContentControl, typeof(ContentControlHostInit))]
public sealed partial class ContentControlMainPage : BaseTestSectionPage
{
	public ContentControlMainPage()
	{
		this.InitializeComponent();
	}

	public async void ContentControlHomeClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewModelAsync<ContentControlHomeViewModel>(this);
	}

}
