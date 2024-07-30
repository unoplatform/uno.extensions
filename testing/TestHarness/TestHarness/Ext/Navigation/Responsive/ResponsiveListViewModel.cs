
namespace TestHarness.Ext.Navigation.Responsive;

public record ResponsiveListViewModel(INavigator Navigator)
{
	public Widget[] Widgets { get; } = new[]
	{
		new Widget{Name="Bob", Weight=34.5},
		new Widget{Name="Jane", Weight=88.23},
		new Widget{Name="Fred", Weight=12.4},
		new Widget{Name="Sarah", Weight=25.7},
	};
}
