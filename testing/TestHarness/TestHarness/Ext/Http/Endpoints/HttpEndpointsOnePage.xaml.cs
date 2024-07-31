
namespace TestHarness.Ext.Http.Endpoints;

public sealed partial class HttpEndpointsOnePage : Page
{
	public HttpEndpointsOneViewModel? ViewModel => DataContext as HttpEndpointsOneViewModel;
	public HttpEndpointsOnePage()
	{
		this.InitializeComponent();
	}

}
