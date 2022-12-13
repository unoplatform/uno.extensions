using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using TemplateStudio.Wizards.ViewModels;

namespace TemplateStudio.Wizards.Views
{
	internal class SelectExtensionsView : StackPanel
	{
		public SelectExtensionsView() {
			this.DataContext<ExtensionsViewModel>((panel, vm) => panel
				.Children(

					new TextBlock().Text("Extensions").FontSize(16),

					//new CheckBox()
					//	.Content("PWA Manifest for WASM")
					//	.IsChecked(() => vm.iOS),
					new CheckBox()
						.Content("Configuration (appsettings.json)")
						.IsChecked(() => vm.Configuration),

					new CheckBox()
						.Content("Localization")
						.IsChecked(() => vm.Localization),

					new TextBlock().Text("Logging").FontSize(13),
					new RadioButton()
						.Content("No Logging Extensions (None)")
						.GroupName("Logging")
						.IsChecked(() => vm.Logging),
					new RadioButton()
						.Content("Logging (Enabled)")
						.GroupName("Logging")
						.IsChecked(() => vm.Logging),
					new RadioButton()
						.Content("Logging - Serilog (Enabled)")
						.GroupName("Logging")
						.IsChecked(() => vm.Logging)


				)
			);
		}
	}
}
