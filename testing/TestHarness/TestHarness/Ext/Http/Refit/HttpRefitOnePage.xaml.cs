
namespace TestHarness.Ext.Http.Refit;

public sealed partial class HttpRefitOnePage : Page
{
	public HttpRefitOneViewModel? ViewModel => DataContext as HttpRefitOneViewModel;
	public HttpRefitOnePage()
	{
		this.InitializeComponent();
	}

}
