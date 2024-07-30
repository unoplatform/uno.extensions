
namespace TestHarness.Ext.Http.Refit;

[ReactiveBindable(false)]
public partial class HttpRefitOneViewModel : ObservableObject
{
	private readonly INavigator _navigator;
	private readonly IServiceProvider _services;
	private readonly IHttpRefitDummyJsonEndpoint _endpoint;

	[ObservableProperty]
	private string? data;

	public HttpRefitOneViewModel(INavigator navigator, IServiceProvider services, IHttpRefitDummyJsonEndpoint endpoint)
	{
		_navigator = navigator;
		_services = services;
		_endpoint = endpoint;
	}


	public async Task Load()
	{
		var products = await _endpoint.Products(CancellationToken.None);
		Data = $"Found {products.Products.Length} products";
	}
}
