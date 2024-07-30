
using Uno.Extensions.Toolkit;

namespace TestHarness.Ext.Navigation.ThemeService;

public sealed partial class ThemeServiceOnePage : Page
{
	public ThemeServiceOneViewModel? ViewModel => DataContext as ThemeServiceOneViewModel;
	public ThemeServiceOnePage()
	{
		this.InitializeComponent();
	}

	private async void ElementChangeToLightClick(object sender, RoutedEventArgs e)
	{
		var ts = (sender as UIElement)!.GetThemeService();
		await ts.SetThemeAsync(AppTheme.Light);
	}
	private async void ElementChangeToDarkClick(object sender, RoutedEventArgs e)
	{
		var ts = (sender as UIElement)!.GetThemeService();
		await ts.SetThemeAsync(AppTheme.Dark);
	}
	private async void ElementChangeToSystemClick(object sender, RoutedEventArgs e)
	{
		var ts = (sender as UIElement)!.GetThemeService();
		await ts.SetThemeAsync(AppTheme.System);

	}

}
