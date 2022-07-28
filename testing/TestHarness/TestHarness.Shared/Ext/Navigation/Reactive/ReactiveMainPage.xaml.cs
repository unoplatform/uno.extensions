namespace TestHarness.Ext.Navigation.Reactive;

[TestSectionRoot("Reactive ",TestSections.Reactive, typeof(ReactiveHostInit))]
public sealed partial class ReactiveMainPage : BaseTestSectionPage
{
	public ReactiveMainPage()
	{
		this.InitializeComponent();
	}

	public async void OnePageClick(object sender, RoutedEventArgs e)
	{
		await NavigationRoot.Navigator()!.NavigateViewModelAsync<ReactiveOneViewModel>(this);
	}

}
