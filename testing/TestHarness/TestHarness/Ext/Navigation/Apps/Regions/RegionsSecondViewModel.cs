namespace TestHarness.Ext.Navigation.Apps.Regions;

public record RegionsSecondViewModel
{
	public RegionEntityData[] Items { get; } =
	[
		new RegionEntityData() { Name="First"},
		new RegionEntityData() { Name="Second"},
		new RegionEntityData() { Name="Third"},
		new RegionEntityData() { Name="Fourth"}
	];
}
