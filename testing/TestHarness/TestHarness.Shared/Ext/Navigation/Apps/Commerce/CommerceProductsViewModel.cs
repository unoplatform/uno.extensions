namespace TestHarness.Ext.Navigation.Apps.Commerce;

public record CommerceProductsViewModel(INavigator Navigator)
{
	public CommerceProduct[] Products { get; } = new[]
				{
					new CommerceProduct("Shoes"),
					new CommerceProduct("Hat"),
					new CommerceProduct("Sun glasses"),
					new CommerceProduct("Watch")
				};

}
