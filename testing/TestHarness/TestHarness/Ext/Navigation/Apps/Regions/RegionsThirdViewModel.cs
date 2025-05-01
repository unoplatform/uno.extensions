namespace TestHarness.Ext.Navigation.Apps.Regions;

public record RegionsThirdViewModel
{
	public string? MyText { get; set; }

	public RegionsThirdViewModel(string myText)
	{
		MyText = myText;
	}
}
