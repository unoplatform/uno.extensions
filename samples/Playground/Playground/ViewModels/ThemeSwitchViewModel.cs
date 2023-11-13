using Uno.Extensions.Reactive;
using Uno.Extensions.Toolkit;

namespace Playground.ViewModels;

[ReactiveBindable(false)]
public partial class ThemeSwitchViewModel:ObservableObject
{
	private readonly IThemeService _ts;

	[ObservableProperty]
	private string isDarkTheme = string.Empty;

	public ThemeSwitchViewModel(IThemeService themeService,IDispatcher dispatcher)
	{
		_ts = themeService;
		_ts.ThemeChanged += ts_ThemeChanged;
		_ = dispatcher.ExecuteAsync(() =>
		{
			IsDarkTheme = themeService.IsDark.ToString();
		});
	}

	private void ts_ThemeChanged(object? sender, AppTheme e)
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

