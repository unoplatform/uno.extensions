namespace MauiEmbedding.Presentation;

public partial class MainViewModel : ObservableObject
{
	private INavigator _navigator;

	[ObservableProperty]
	private string? name;

	public MainViewModel(
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


	public async Task GoToMauiControls()
	{
		await _navigator.NavigateViewModelAsync<MauiControlsViewModel>(this);
	}


	public async Task GoToCommunityToolkitMauiControls()
	{
		await _navigator.NavigateViewModelAsync<MCTControlsViewModel>(this);
	}


	public async Task GoToMauiEssentialsApi()
	{
		await _navigator.NavigateViewModelAsync<MauiEssentialsViewModel>(this);
	}

	public async Task GoToTelerikPage()
	{
		await _navigator.NavigateViewModelAsync<TelerikControlsViewModel>(this);
	}
	public async Task GoToColorsPage()
	{
		await _navigator.NavigateViewModelAsync<MauiColorsViewModel>(this);
	}
}
