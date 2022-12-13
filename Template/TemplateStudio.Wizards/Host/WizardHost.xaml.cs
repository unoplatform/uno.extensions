using System.Collections.Generic;
using System.Windows;
using TemplateStudio.Wizards.ViewModels;
using Uno.UI.XamlHost.Skia.Wpf;

namespace TemplateStudio.Wizards.Host;

public partial class WizardHost : Window
{

	public WizardHost(Dictionary<string, string> replacementsDictionary)
	{
		DataContext= new MainViewModel() { Replacements = replacementsDictionary };
		InitializeComponent();


		//Test: Change on MainUnoPage
		if (this.DataContext is ViewModels.MainViewModel mvm)
		{
			mvm.Replacements.Add("passthrough:WizardHostBefore", true.ToString());

		}

		// TODO: Work out how to new this up in XAML - fails unable to find assembly
		this.Content = new UnoXamlHost() {
			InitialTypeName = "TemplateStudio.Wizards.MainUnoPage",
			Height = 1024,
			Width = 786,
			DataContext = DataContext
		};

		//Test: Change on MainUnoPage
		if (this.DataContext is ViewModels.MainViewModel mvm2)
		{
			mvm2.Replacements.Add("passthrough:WizardHostAfter", true.ToString());
		}
	}
	
}
