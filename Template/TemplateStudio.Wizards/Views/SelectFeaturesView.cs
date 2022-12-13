using Microsoft.UI.Xaml.Controls;
using TemplateStudio.Wizards.ViewModels;

namespace TemplateStudio.Wizards.Views;

internal class SelectFeaturesView : StackPanel
{
	public SelectFeaturesView() {
		this.DataContext<FeaturesViewModel>((panel, vm) => panel
			.Children(

				new TextBlock().Text("Features").FontSize(16),

				new CheckBox()
					.Content("PWA Manifest for WASM")
					.IsChecked(() => vm.Wpa),
				

				new TextBlock().Text("Test").FontSize(13),
				new CheckBox()
					.Content("Unit test project")
					.IsChecked(() => vm.UnitTest),
				new CheckBox()
					.Content("UI test project")
					.IsChecked(() => vm.UITest)
				


			)
		);
	}
}
