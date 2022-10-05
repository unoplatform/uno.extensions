

namespace TestHarness.Ext.Navigation.Dialogs;

[TestSectionRoot("Dialogs", TestSections.Navigation_Dialogs, typeof(DialogsHostInit))]
public sealed partial class DialogsMainPage : BaseTestSectionPage
{
	public DialogsMainPage()
	{
		this.InitializeComponent();

	}

	public async void MessageDialogsClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewAsync<MessageDialogsPage>(this);
	}

	public async void LocalizedDialogsClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewAsync<LocalizedDialogsPage>(this);
	}

	public async void ContentDialogsClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewAsync<ContentDialogsPage>(this);
	}
	public async void FlyoutsClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewAsync<DialogsFlyoutsPage>(this);
	}
}
