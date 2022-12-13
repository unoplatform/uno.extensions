using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace TemplateStudio.Wizards;

public partial class MainUnoPage : Page
{
	public MainUnoPage()
	{
		this.InitializeComponent();

		//Test: Change on MainUnoPage
		if (this.DataContext is ViewModels.MainViewModel mvm)
		{

			mvm.Replacements.Add("passthrough:test-MainUno", true.ToString());

		}

		// TODO: Check if this is necessary in order to set MainViewModel on the SelectPage
		RootFrame.Navigated += (s, e) => {
			(RootFrame.Content as FrameworkElement).DataContext = this.DataContext;
		};
	}
}
