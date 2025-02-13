namespace TestHarness.Ext.Navigation.Apps.Regions;

public record RegionsSecondViewModel
{
	public RegionEntityData[] Items { get; } =
	[
		new RegionEntityData("First"),
		new RegionEntityData("Second"),
		new RegionEntityData("Third"),
		new RegionEntityData("Fourth")
	];
}
