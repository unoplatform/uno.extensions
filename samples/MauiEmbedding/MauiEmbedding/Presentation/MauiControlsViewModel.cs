namespace MauiEmbedding.Presentation;
partial class MauiControlsViewModel : ObservableObject
{
	[ObservableProperty]
	private string? name = "This is from the ViewModel!";

	public MauiControlsViewModel(
		IStringLocalizer localizer,
		IOptions<AppConfig> appInfo)
	{
		Title = "Main";
		Title += $" - {localizer["ApplicationName"]}";
		Title += $" - {appInfo?.Value?.Environment}";
	}


	public MauiControlsViewModel()
	{
		Title = "Main";
	}

	public string? Title { get; }
}
