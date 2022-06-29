namespace Playground.ViewModels;

public partial class SecondViewModel
{
	public SecondViewModel(NavigationRequest request)
	{
		var originatingNavigator = request.Source;
	}

	public string Title => "Second page with View Model";
}
