using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TestHarness.Ext.Http.KiotaSourceGen.SourceGenClient;

namespace TestHarness.Ext.Http.KiotaSourceGen;

/// <summary>
/// ViewModel for the Kiota Roslyn Source Generator test page.
/// <para>
/// Validates that the <see cref="SourceGenPetClient"/> type — generated
/// at compile time by the Roslyn incremental source generator from an
/// <c>&lt;AdditionalFiles&gt;</c> OpenAPI spec — can be resolved from
/// the DI container and used to invoke API endpoints.
/// </para>
/// </summary>
[ObservableObject]
[ReactiveBindable(false)]
public partial class KiotaSourceGenHomeViewModel
{
	private readonly SourceGenPetClient? _client;

	[ObservableProperty]
	private string _initializationStatus = "Checking...";

	[ObservableProperty]
	private string _clientTypeName = "N/A";

	[ObservableProperty]
	private string _petsEndpointInfo = "N/A";

	[ObservableProperty]
	private string _fetchResult = string.Empty;

	public KiotaSourceGenHomeViewModel(SourceGenPetClient? client = null)
	{
		_client = client;

		if (_client is not null)
		{
			InitializationStatus = "Success";
			ClientTypeName = _client.GetType().FullName ?? _client.GetType().Name;
			PetsEndpointInfo = "Available";
		}
		else
		{
			InitializationStatus = "Failed – client not resolved";
			ClientTypeName = "N/A";
			PetsEndpointInfo = "Unavailable";
		}
	}

	[RelayCommand]
	private async Task FetchPetsAsync()
	{
		if (_client is null)
		{
			FetchResult = "Error: client not available";
			return;
		}

		try
		{
			var pets = await _client.Pets.GetAsync();
			FetchResult = pets is not null
				? $"OK – received {pets.Count} pet(s)"
				: "OK – null response";
		}
		catch (Exception ex)
		{
			FetchResult = $"Error: {ex.Message}";
		}
	}
}
