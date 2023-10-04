using Microsoft.Maui.Devices;
using MauiEmbedding.Business;

namespace MauiEmbedding.Presentation;

public partial class MainViewModel : ObservableObject
{
	private INavigator _navigator;

	[ObservableProperty]
	private string? name;

	public MainViewModel(
		IStringLocalizer localizer,
		IOptions<AppConfig> appInfo,
		INavigator navigator,
		IVibration vibrate,
		IVibrationService vibrateService)
	{
		_navigator = navigator;
		Title = "Main";
		Title += $" - {localizer["ApplicationName"]}";
		Title += $" - {appInfo?.Value?.Environment}";
		vibrate.Vibrate(3000);
	}

	public string? Title { get; }
}
