using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Floor;
using Map = Esri.ArcGISRuntime.Mapping.Map;

namespace MauiApp1;

public partial class MainPage : ContentPage
{
	private FloorManager _floorManager;
	int count = 0;
	// Collection of floors.
	private readonly Dictionary<string, FloorLevel> _floorOptions = new Dictionary<string, FloorLevel>();
	public MainPage()
	{
		InitializeComponent();

		_ = Initialize();
	}


	private async Task Initialize()
	{
		try
		{
			// Gets the floor data from ArcGIS Online and creates a map with it.
			Map map = new Map(new Uri("https://ess.maps.arcgis.com/home/item.html?id=f133a698536f44c8884ad81f80b6cfc7"));

			mapView.Map = map;

			// Map needs to be loaded in order for floormanager to be used.
			await mapView.Map.LoadAsync();
			List<string> floorName = new List<string>();

			// Checks to see if the layer is floor aware.
			if (mapView.Map.FloorDefinition == null)
			{
				await Application.Current.MainPage.DisplayAlert("Alert", "The layer is not floor aware.", "OK");
				return;
			}

			await mapView.Map.FloorManager.LoadAsync();
			_floorManager = mapView.Map.FloorManager;

			// Use the dictionary to add the level's name as the key and the FloorLevel object with the associated level's name.
			foreach (FloorLevel level in _floorManager.Facilities[0].Levels)
			{
				_floorOptions.Add(level.ShortName, level);
				floorName.Add(level.ShortName);
			}

			//FloorChooser.ItemsSource = floorName;
		}
		catch (Exception ex)
		{
			await Application.Current.MainPage.DisplayAlert("Alert", ex.Message, "OK");
		}
	}

	private void OnCounterClicked(object sender, EventArgs e)
	{
		count++;

		if (count == 1)
			CounterBtn.Text = $"Clicked {count} time";
		else
			CounterBtn.Text = $"Clicked {count} times";

		SemanticScreenReader.Announce(CounterBtn.Text);
	}
}

