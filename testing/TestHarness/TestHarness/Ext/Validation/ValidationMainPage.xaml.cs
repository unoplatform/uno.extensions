
namespace TestHarness.Ext.Navigation.Validation;

[TestSectionRoot("Validation", TestSections.Validation, typeof(ValidationHostInit))]
public sealed partial class ValidationMainPage : BaseTestSectionPage
{
	public ValidationMainPage()
	{
		this.InitializeComponent();
	}

	public async void OnePageClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewModelAsync<ValidationOneViewModel>(this);
	}
}
