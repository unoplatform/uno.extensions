using TestHarness.Ext.Http.Kiota.Client;

namespace TestHarness.Ext.Http.Kiota;

[ReactiveBindable(false)]
public partial class KiotaHomeViewModel : ObservableObject
{
	private readonly KiotaTestClient _kiotaClient;
	private readonly IAuthenticationService _authService;

	[ObservableProperty]
	private string _fetchItemsResult = string.Empty;

	[ObservableProperty]
	private string _initializationStatus = string.Empty;


	public KiotaHomeViewModel(KiotaTestClient kiotaClient, IAuthenticationService authService)
	{
		_kiotaClient = kiotaClient;
		_authService = authService;
		InitializationStatus = "Kiota Client initialized successfully.";
	}

	public async void FetchItems()
	{
		try
		{
			FetchItemsResult = "Logging in...";

			var isLoggedIn = await _authService.LoginAsync(null, new Dictionary<string, string>
			{
				{ "Username", "testuser" },
				{ "Password", "password" }
			});

			if (!isLoggedIn)
			{
				FetchItemsResult = "Authentication failed.";
				return;
			}

			FetchItemsResult = "Fetching data...";

			var dataResponse = await _kiotaClient.Kiota.Data.GetAsync();

			if (dataResponse == null)
			{
				FetchItemsResult = "No data received.";
				return;
			}

			FetchItemsResult = $"Retrieved data: {string.Join(", ", dataResponse.Data)}\nAuthenticated Request with Token: {dataResponse.Token}";
		}
		catch (Exception ex)
		{
			FetchItemsResult = $"Failed to fetch data. Error: {ex.Message}";
		}
	}
}
