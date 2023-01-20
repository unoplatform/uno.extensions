using Playground.ViewModels;
namespace Playground.Views
{
	public sealed partial class ThemeSwitchPage : Page
	{
		public ThemeSwitchViewModel? ViewModel { get; private set; }
		public ThemeSwitchPage()
		{
			this.InitializeComponent();
			DataContextChanged += XamlPage_DataContextChanged;
		}

		private void XamlPage_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
		{
			ViewModel = args.NewValue as ThemeSwitchViewModel;
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
}
