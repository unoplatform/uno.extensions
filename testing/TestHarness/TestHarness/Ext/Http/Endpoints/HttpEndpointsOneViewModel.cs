
namespace TestHarness.Ext.Http.Endpoints;

[ReactiveBindable(false)]
public partial class HttpEndpointsOneViewModel : ObservableObject
{
	private readonly INavigator _navigator;
	private readonly IServiceProvider _services;
	private readonly HttpClient _client;

	[ObservableProperty]
	private string? data;

	public HttpEndpointsOneViewModel(INavigator navigator, IServiceProvider services, HttpClient client)
	{
		_navigator = navigator;
		_services = services;
		_client = client;
	}


	public async Task Load()
	{
		Data = await _client.GetStringAsync("products");
	}
}
