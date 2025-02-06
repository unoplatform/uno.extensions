using TestHarness.Ext.Http.Kiota.Client;

namespace TestHarness.Ext.Http.Kiota;

[ReactiveBindable(false)]
public partial class KiotaHomeViewModel : ObservableObject
{
	private readonly KiotaTestClient _kiotaClient;
	private readonly IAuthenticationService _authService;

	[ObservableProperty]
	private string _fetchPostsResult = string.Empty;

	public KiotaHomeViewModel(KiotaTestClient kiotaClient, IAuthenticationService authService)
	{
		_kiotaClient = kiotaClient;
		_authService = authService;
	}

	public async void FetchPosts()
	{
		try
		{
			FetchPostsResult = "Logging in...";

			var isLoggedIn = await _authService.LoginAsync(null, new Dictionary<string, string>
			{
				{ "Username", "testuser" },
				{ "Password", "password" }
			});

			if (!isLoggedIn)
			{
				FetchPostsResult = "Authentication failed.";
				return;
			}

			FetchPostsResult = "Fetching data...";

			var dataResponse = await _kiotaClient.Kiota.Data.GetAsync();

			if (dataResponse == null)
			{
				FetchPostsResult = "No data received.";
				return;
			}

			FetchPostsResult = $"Retrieved data: {string.Join(", ", dataResponse.Data)}\nToken: {dataResponse.Token}";
		}
		catch (Exception ex)
		{
			FetchPostsResult = $"Failed to fetch data. Error: {ex.Message}";
		}
	}
}
