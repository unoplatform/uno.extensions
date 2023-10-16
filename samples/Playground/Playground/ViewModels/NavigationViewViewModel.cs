namespace Playground.ViewModels;

public class NavigationViewViewModel
{
	public NavWidget[] NavigationItems { get; } = new[]
	{
		new NavWidget{Name="Fred", Weight=100.0},
		new NavWidget{Name="Jane", Weight=20.0},
		new NavWidget{Name="Bob", Weight=35.0},
		new NavWidget{Name="Sarah", Weight=33.0}
	};
}
