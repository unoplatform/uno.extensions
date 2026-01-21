
namespace TestHarness.Ext.Navigation.PageNavigation;

public sealed partial class PageNavigationOnePage : Page
{
	public PageNavigationOneViewModel? ViewModel => DataContext as PageNavigationOneViewModel;

	private Type expectedDataContext = typeof(PageNavigationOneViewModel);

	public PageNavigationOnePage()
	{
		this.InitializeComponent();

		this.DataContextChanged += PageNavigationOnePage_DataContextChanged;
	}

	private void PageNavigationOnePage_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		// For the test `When_PageNavigationDataContextDidntChange`
		// If DataContext is ever changed to anything other than the expected the text will be "DataContext is not correct"
		// So that we can validade on the test
		if (sender.DataContext is { } dataContext && dataContext.GetType() != expectedDataContext)
		{
			TxtDataContext.Text = "DataContext is not correct";
		}
	}

	public async void OnePageToTwoPageCodebehindClick(object sender, RoutedEventArgs e)
	{
		await this.Navigator()!.NavigateViewAsync<PageNavigationTwoPage>(this);
	}

	public async void GetUrlFromBrowser(object sender, RoutedEventArgs e)
	{
#if __WASM__
		var url = AddressBarJSImports.GetUrl();

		TxtUrl.Text = url;
#else
		TxtUrl.Text = "Not supported";
#endif
	}
}
