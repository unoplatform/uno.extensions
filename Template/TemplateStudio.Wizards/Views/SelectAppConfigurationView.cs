using Microsoft.UI.Xaml.Controls;
using TemplateStudio.Wizards.ViewModels;

namespace TemplateStudio.Wizards.Views;

internal class SelectAppConfigurationView : StackPanel
{
	public SelectAppConfigurationView() {
		this.DataContext<AppConfigurationViewModel>((panel, vm) => panel
			.Children(

				new TextBlock().Text("Coding Style").FontSize(16),
				new RadioButton()
					.Content("C# Markup")
					.GroupName("Markup")
					.IsChecked(() => vm.Markup),

				new RadioButton()
					.Content("XAML")
					.GroupName("Markup")
					.IsChecked(() => vm.Markup)


				//vm.MarkupChoices.ForEach(x => {
				//	new RadioButton()
				//	.Content(x.DisplayName)
				//	.GroupName("Markup")
				//	.IsChecked(() =>  vm.Markup.Choice == x.Choice);
				//})
				//,

				//new TextBlock().Text("Framework").FontSize(16),

				//vm.TargetFrameworkChoices.ForEach(x => {
				//	new RadioButton()
				//	.Content(x.DisplayName)
				//	.GroupName(x.Choice)
				//	.IsChecked(() => vm.Markup);
				//})



			)
		);
	}


}
