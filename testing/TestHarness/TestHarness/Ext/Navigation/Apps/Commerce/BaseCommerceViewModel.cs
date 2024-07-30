namespace TestHarness.Ext.Navigation.Apps.Commerce;

// This is just so that a breakpoint can be set in the record constructor
public record BaseCommerceViewModel
{
	public string ViewModelId { get; } = Guid.NewGuid().ToString();

	public BaseCommerceViewModel()
	{

	}
}
