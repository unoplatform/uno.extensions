namespace Playground.ViewModels;

public class NavContentViewModel
{
	public string? Name { get; }
	public double Weight { get; }
	public NavContentViewModel(NavWidget widget)
	{
		Name = widget?.Name;
		Weight = widget?.Weight??0.00;
	}
}
