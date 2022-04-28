namespace Playground.ViewModels;

public class NavigationViewViewModel
{
	public Widget[] NavigationItems { get; } = new[]
	{
		new Widget{Name="Fred", Weight=100.0},
		new Widget{Name="Jane", Weight=20.0},
		new Widget{Name="Bob", Weight=35.0},
		new Widget{Name="Sarah", Weight=33.0}
	};
}
