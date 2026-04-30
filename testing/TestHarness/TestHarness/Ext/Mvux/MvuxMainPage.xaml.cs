namespace TestHarness.Ext.Mvux;

[TestSectionRoot("MVUX Basic", TestSections.Mvux_Basic, typeof(MvuxHostInit))]
public sealed partial class MvuxMainPage : BaseTestSectionPage
{
	public MvuxMainPage()
	{
		this.InitializeComponent();
	}

	public async void FeedPageClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewModelAsync<MvuxFeedModel>(this);
	}

	public async void ListFeedPageClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewModelAsync<MvuxListFeedModel>(this);
	}

	public async void StatePageClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewModelAsync<MvuxStateModel>(this);
	}
}
