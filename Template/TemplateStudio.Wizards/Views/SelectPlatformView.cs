using Microsoft.UI.Xaml.Controls;
using TemplateStudio.Wizards.ViewModels;

namespace TemplateStudio.Wizards.Views;

internal class SelectPlatformView : StackPanel
{
	public SelectPlatformView() {
		this.DataContext<ProjectPlatformsViewModel>((panel, vm) => panel
			.Children(

				new TextBlock().Text("Platform").FontSize(16),

				new TextBlock().Text("Mobile").FontSize(13),
				new CheckBox()
					.Content("iOS")
					.IsChecked(() => vm.iOS),
				new CheckBox()
					.Content("Android")
					.IsChecked(() => vm.Android),


				new TextBlock().Text("Desktop").FontSize(13),

				new CheckBox()
					.Content("Windows (Windows App SDK)")
					.IsChecked(() => vm.WinUI),
				new CheckBox()
					.Content("Windows (Wpf)")
					.IsChecked(() => vm.Wpf),


				new CheckBox()
					.Content("Linux (GTK)")
					.IsChecked(() => vm.Gtk),

				new CheckBox()
					.Content("Linux (Frame Buffer)")
					.IsChecked(() => vm.LinuxFrameBuffer),
				new CheckBox()

					.Content("Mac (MacOS)")
					.IsChecked(() => vm.MacCatalyst),
				new CheckBox()
					.Content("Mac (Catalyst)")
					.IsChecked(() => vm.MacCatalyst),

				new TextBlock().Text("Web").FontSize(13),
				new CheckBox()
					.Content("Web")
					.IsChecked(() => vm.WebAssembly)


			)
		);
	}


}
