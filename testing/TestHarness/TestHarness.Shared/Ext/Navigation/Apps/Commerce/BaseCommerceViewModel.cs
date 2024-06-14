[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(TestHarness.Ext.Navigation.Apps.Commerce.CommerceUpdateHandler))]
//[assembly: ElementMetadataUpdateHandler(typeof(object), typeof(TestHarness.Ext.Navigation.Apps.Commerce.CommerceUpdateHandler))]


namespace TestHarness.Ext.Navigation.Apps.Commerce;



// This is just so that a breakpoint can be set in the record constructor
public record BaseCommerceViewModel
{
	public string ViewModelId { get; } = Guid.NewGuid().ToString();

	public BaseCommerceViewModel()
	{

	}
}


public static class CommerceUpdateHandler
{
	internal static void UpdateApplication(Type[]? types)
	{
	}
}
