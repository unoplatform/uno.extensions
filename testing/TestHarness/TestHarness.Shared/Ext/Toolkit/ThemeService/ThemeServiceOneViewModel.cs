using Uno.Extensions.Toolkit;

namespace TestHarness.Ext.Navigation.ThemeService;

[ReactiveBindable(false)]
public partial class ThemeServiceOneViewModel : ObservableObject
{
	private readonly IThemeService _ts;

	[ObservableProperty]
	private string isDarkTheme;

	public ThemeServiceOneViewModel(IThemeService themeService, IDispatcher dispatcher)
	{
		_ts = themeService;
		_ts.ThemeChanged += ts_DesiredThemeChanged;
		_ = dispatcher.ExecuteAsync(() =>
		{
			IsDarkTheme = themeService.IsDark.ToString();
		});
	}

	private void ts_DesiredThemeChanged(object? sender, AppTheme e)
	{
		IsDarkTheme = _ts.IsDark.ToString();
		Console.WriteLine($"Theme was changed to:{e.ToString()}");
		Console.WriteLine($"Desired Theme is:{_ts.Theme}");
	}

	public async Task ChangeToSystem()
	{
		await Task.Run(async () =>
		{
			await _ts.SetThemeAsync(AppTheme.System);
		});
	}

	public async Task ChangeToLight()
	{
		await Task.Run(async () =>
		{
			await _ts.SetThemeAsync(AppTheme.Light);
		});
	}

	public async Task ChangeToDark()
	{
		await Task.Run(async () =>
		{
			await _ts.SetThemeAsync(AppTheme.Dark);
		});
	}
}

