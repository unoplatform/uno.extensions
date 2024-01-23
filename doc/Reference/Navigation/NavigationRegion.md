---
uid: Reference.Navigation.Regions
---
# What is a Navigation Region

## Region Name

Regions can be named by specifying the Region.Name="XXX" property.

For selection-based regions, the selectable items (NavigationViewItem, TabBarItem, …) are identified using the Region.Name property

```csharp
<muxc:NavigationView uen:Region.Attached="true">
	<muxc:NavigationView.MenuItems>
		<muxc:NavigationViewItem Content="Products" uen:Region.Name="Products" />
		<muxc:NavigationViewItem Content="Deals" uen:Region.Name="Deals" />
		<muxc:NavigationViewItem Content="Profile" uen:Region.Name="Profile" />
	</muxc:NavigationView.MenuItems>
</muxc:NavigationView>
```

Switching selected item:
	`naviator.NavigateRouteAsync(this,"Deals");`

- Define what a navigation region is and how the hierarchy of regions is created with the Region.Attached property
