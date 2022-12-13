using System.Windows;
using Uno.UI.XamlHost.Skia.Wpf;

namespace TemplateStudio.Wizards.Host;

public partial class WizardHost : Window
{

	public WizardHost()
	{
		InitializeComponent();

		// TODO: Work out how to new this up in XAML - fails unable to find assembly
		this.Content = new UnoXamlHost() {
			InitialTypeName = "TemplateStudio.Wizards.MainUnoPage",
			Height = 1024,
			Width = 786};
	}
	
}
