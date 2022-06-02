

namespace TestHarness.Ext.Navigation.PageNavigation;

[TestSectionRoot("Message Dialog", TestSections.MessageDialog, typeof(MessageDialogHostInit))]
public sealed partial class MessageDialogMainPage : BaseTestSectionPage
{
	public MessageDialogMainPage()
	{
		this.InitializeComponent();
	}

	public async void TestClick(object sender, RoutedEventArgs e)
	{
		await NavigationRoot.Navigator()!.ShowMessageDialogAsync(this, "Confirm");
	}

}
