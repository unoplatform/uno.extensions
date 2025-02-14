namespace TestHarness.Ext.Navigation.Apps.Regions;

public record RegionsSecondTbiDataViewModel
{
	public RegionEntityData? Entity { get; set; }

	public RegionsSecondTbiDataViewModel(RegionEntityData? entity)
	{
		Entity = entity;
	}
}
