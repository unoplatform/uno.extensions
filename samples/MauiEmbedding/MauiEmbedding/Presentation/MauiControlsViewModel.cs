namespace MauiEmbedding.Presentation;
partial class MauiControlsViewModel : ObservableObject
{
	private INavigator _navigator;

	[ObservableProperty]
	private string? name;

	public MauiControlsViewModel(
		IStringLocalizer localizer,
		IOptions<AppConfig> appInfo,
		INavigator navigator)
	{
		_navigator = navigator;
		Title = "Main";
		Title += $" - {localizer["ApplicationName"]}";
		Title += $" - {appInfo?.Value?.Environment}";
	}
	public string? Title { get; }
}
