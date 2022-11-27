using Uno.Extensions.Navigation.Toolkit.UI;
using Uno.Extensions.Navigation.UI;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Playground.Views
{
	public sealed partial class ThemeSwitchPage : Page, IInjectable<IServiceProvider>
	{

		// NR: Move this functionality into a ViewModel and use data binding.
		// The IThemeService can be injected into the constructor of the ViewModel


		private IThemeService ts;
		public ThemeSwitchPage()
		{
			this.InitializeComponent();
		}

		private void ts_DesiredThemeChanged(object? sender, DesiredTheme e)
		{
			Console.WriteLine($"Theme was changed to:{e.ToString()}");
			Console.WriteLine($"Desired Theme is:{ts.Theme}");
		}

		private async void ButtonSystem_Click(object sender, RoutedEventArgs e)
		{
			await ts.SetThemeAsync(DesiredTheme.System);
		}

		private async void ButtonDark_Click(object sender, RoutedEventArgs e)
		{
			await ts.SetThemeAsync(DesiredTheme.Dark);
		}

		private async void ButtonLight_Click(object sender, RoutedEventArgs e)
		{
			await ts.SetThemeAsync(DesiredTheme.Light);
		}

		public void Inject(IServiceProvider entity)
		{
			ts = entity.GetRequiredService<IThemeService>();
			ts.DesiredThemeChanged += ts_DesiredThemeChanged;
		}
	}
}
