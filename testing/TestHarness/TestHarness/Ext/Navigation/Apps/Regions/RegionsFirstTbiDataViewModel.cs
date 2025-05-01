namespace TestHarness.Ext.Navigation.Apps.Regions;

public record RegionsFirstTbiDataViewModel
{
	public RegionEntityData? Entity { get; set; }

	public RegionsFirstTbiDataViewModel(RegionEntityData? entity)
	{
		Entity = entity;
	}
}
