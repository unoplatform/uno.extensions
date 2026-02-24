using TestHarness.Ext.Http.KiotaBuildGen.BuildGenClient;

namespace TestHarness.Ext.Http.KiotaBuildGen;

/// <summary>
/// View model for the Kiota MSBuild task build-time generation test page.
/// Verifies that the <c>BuildGenPetClient</c> was generated and injected via DI.
/// </summary>
[ReactiveBindable(false)]
public partial class KiotaBuildGenHomeViewModel : ObservableObject
{
	private readonly BuildGenPetClient _petClient;

	[ObservableProperty]
	private string _initializationStatus = string.Empty;

	[ObservableProperty]
	private string _clientTypeName = string.Empty;

	[ObservableProperty]
	private string _petsEndpointInfo = string.Empty;

	[ObservableProperty]
	private string _fetchResult = string.Empty;

	public KiotaBuildGenHomeViewModel(BuildGenPetClient petClient)
	{
		_petClient = petClient;
		InitializationStatus = "BuildGen client initialized successfully.";
		ClientTypeName = _petClient.GetType().Name;

		// Verify the Pets request builder is available (generated from /pets path)
		try
		{
			var petsBuilder = _petClient.Pets;
			PetsEndpointInfo = petsBuilder is not null
				? "Pets endpoint: available"
				: "Pets endpoint: null";
		}
		catch (Exception ex)
		{
			PetsEndpointInfo = $"Pets endpoint error: {ex.Message}";
		}
	}

	public async void FetchPets()
	{
		try
		{
			FetchResult = "Fetching pets...";
			var pets = await _petClient.Pets.GetAsync();

			if (pets is null || pets.Count == 0)
			{
				FetchResult = "No pets returned (endpoint may not be running).";
				return;
			}

			FetchResult = $"Retrieved {pets.Count} pet(s): {string.Join(", ", pets.Select(p => p.Name))}";
		}
		catch (Exception ex)
		{
			// Expected when TestBackend doesn't serve /pets — the test
			// primarily validates that the generated client compiles and
			// resolves from DI.
			FetchResult = $"Fetch error (expected if backend is offline): {ex.Message}";
		}
	}
}
