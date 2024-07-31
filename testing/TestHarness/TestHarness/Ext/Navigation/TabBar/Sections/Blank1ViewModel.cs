namespace TestHarness.Ext.Navigation.TabBar;
public class Blank1ViewModel
{
	public Blank1ViewModel()
	{
		Data = Enumerable.Range(1, 10).Select(x => $"item {x}");
	}

	public IEnumerable<string> Data { get; }
}

