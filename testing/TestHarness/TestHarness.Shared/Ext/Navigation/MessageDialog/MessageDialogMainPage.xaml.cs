

namespace TestHarness.Ext.Navigation.PageNavigation;

[TestSectionRoot("Message Dialog", TestSections.MessageDialog, typeof(MessageDialogHostInit))]
public sealed partial class MessageDialogMainPage : BaseTestSectionPage
{
	public MessageDialogMainPage()
	{
		this.InitializeComponent();

	}

	public async void SimpleDialogsClick(object sender, RoutedEventArgs e)
	{
		await NavigationRoot.Navigator()!.NavigateViewModelAsync<SimpleDialogsViewModel>(this);
	}
	public async void LocalizedDialogsClick(object sender, RoutedEventArgs e)
	{
		await NavigationRoot.Navigator()!.NavigateViewModelAsync<SimpleDialogsViewModel>(this);
	}
}
