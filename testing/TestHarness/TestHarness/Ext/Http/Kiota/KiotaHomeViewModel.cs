using System.Text.Json;
using TestHarness.Ext.Http.Kiota.Client;
using TestHarness.Ext.Http.Kiota.Client.Models;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;

namespace TestHarness.Ext.Http.Kiota;

[ReactiveBindable(false)]
public partial class KiotaHomeViewModel : ObservableObject
{
	private readonly KiotaTestClient _kiotaClient;
	private readonly INavigator _navigator;

	[ObservableProperty]
	private string _fetchPostsResult = string.Empty;

	[ObservableProperty]
	private string _initializationStatus = string.Empty;

	public KiotaHomeViewModel(KiotaTestClient kiotaClient, INavigator navigator)
	{
		_kiotaClient = kiotaClient;
		_navigator = navigator;

		InitializationStatus = "Kiota Client initialized successfully.";
	}

	public async void FetchPosts()
	{
		try
		{
			FetchPostsResult = "Logging in...";

			var loginRequest = new LoginRequest
			{
				Username = "testuser",
				Password = "password"
			};

			var loginStream = await _kiotaClient.Kiota.Login.PostAsync(loginRequest);
			if (loginStream == null)
			{
				FetchPostsResult = "Failed to authenticate.";
				return;
			}

			var loginJson = await new StreamReader(loginStream).ReadToEndAsync();
			var loginResponse = await KiotaJsonSerializer.DeserializeAsync<AuthResponse>(loginJson);

			if (loginResponse == null || string.IsNullOrEmpty(loginResponse.AccessToken))
			{
				FetchPostsResult = "Failed to authenticate.";
				return;
			}

			FetchPostsResult = "Fetching items...";
			var requestConfig = new Action<RequestConfiguration<DefaultQueryParameters>>(config =>
			{
				config.Headers.Add("Authorization", $"Bearer {loginResponse.AccessToken}");
			});

			var dataResponse = await _kiotaClient.Kiota.Data.GetAsync(requestConfig);
			if (dataResponse == null)
			{
				FetchPostsResult = "No data received.";
				return;
			}

			Console.WriteLine($"Received Data: {string.Join(", ", dataResponse.Data)}");

			FetchPostsResult = dataResponse.Data is { Count: > 0 }
				? $"Retrieved data: {string.Join(", ", dataResponse.Data)}"
				: "No data received.";

		}
		catch (Exception ex)
		{
			FetchPostsResult = $"Failed to fetch posts. Error: {ex.Message}";
		}
	}
}
