
namespace TestHarness.Ext.Navigation.ListToDetails;

[ReactiveBindable(false)]
public partial class ListToDetailsListViewModel:ObservableObject
{
	private INavigator _navigator;

	public ICommand RawNavigateWithDataCommand { get; }

	public ListToDetailsListViewModel(INavigator navigator)
	{
		_navigator = navigator;
		RawNavigateWithDataCommand = new AsyncRelayCommand(RawNavigateWithData);
	}
	
	public Widget[] Widgets { get; } = new[]
	{
		new Widget{Name="Bob", Weight=34.5},
		new Widget{Name="Jane", Weight=88.23},
		new Widget{Name="Fred", Weight=12.4},
		new Widget{Name="Sarah", Weight=25.7},
	};

	public async Task RawNavigateWithData()
	{
		await _navigator.NavigateAsync(new NavigationRequest(this, new Route(Qualifiers.None,default,default,new Dictionary<string, object> { { "", Widgets[2] } })));
	}
}
