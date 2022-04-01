
namespace Playground.ViewModels;

public class HomeViewModel
{
	public string Platform { get; }

	public HomeViewModel(IOptions<AppInfo> appInfo)
	{
		Platform = appInfo.Value.Platform;
	}
}
