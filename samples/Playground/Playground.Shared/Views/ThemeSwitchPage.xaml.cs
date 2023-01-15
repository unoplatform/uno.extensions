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
	}
}
