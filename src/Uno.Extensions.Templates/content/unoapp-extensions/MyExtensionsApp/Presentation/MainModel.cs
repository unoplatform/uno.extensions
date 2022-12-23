//-:cnd:noEmit
using Uno.Extensions.Reactive;

namespace MyExtensionsApp.Presentation;

public partial record MainModel
{
	public string? Title { get; }

	public IState<string> Name { get; }

	public MainModel(
		INavigator navigator,
		IOptions<AppConfig> appInfo)
	{

		_navigator = navigator;
		Title = $"Main - {appInfo?.Value?.Title}";

		Name = State<string>.Value(this, ()=>"");
	}

	public async Task GoToSecond()
	{
		var name = await Name;
		await _navigator.NavigateViewModelAsync<SecondModel>(this, data: new Entity(name!));
	}

	private INavigator _navigator;
}
