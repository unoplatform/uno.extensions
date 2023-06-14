namespace MauiEmbedding.Presentation;
partial class MauiControlsViewModel : ObservableObject
{
	[ObservableProperty]
	private string? name;

	public MauiControlsViewModel(
		IStringLocalizer localizer,
		IOptions<AppConfig> appInfo)
	{
		Title = "Main";
		Title += $" - {localizer["ApplicationName"]}";
		Title += $" - {appInfo?.Value?.Environment}";
	}
	public string? Title { get; }
}
