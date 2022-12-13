using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TemplateStudio.Wizards.ViewModel;
using TemplateStudio.Wizards.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;
using TemplateStudio.Wizards.Views;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TemplateStudio.Wizards;

public partial class MainUnoPage : Page
{
	public MainUnoPage()
	{
		//this.InitializeComponent();
		//ProjectPlatformsViewModel vm = (ProjectPlatformsViewModel)DataContext;
		this.DataContext<ProjectPlatformsViewModel>((page, vm) => page
		.Content(
				new StackPanel()
				.HorizontalAlignment(HorizontalAlignment.Center)
				.VerticalAlignment(VerticalAlignment.Top)
				.Children(
					new SelectPlatformView().DataContext(x => x.Bind(() => this.DataContext).Convert(dc => {
						if (dc is MainViewModel mvm)
						{
							return new ProjectPlatformsViewModel(mvm.Replacements);
						}
						return null;
					})),
					new SelectFeaturesView().DataContext(x => x.Bind(() => this.DataContext).Convert(dc => {
						if (dc is MainViewModel mvm)
						{
							return new FeaturesViewModel(mvm.Replacements);
						}
						return null;
					})),
					new SelectExtensionsView().DataContext(x => x.Bind(() => this.DataContext).Convert(dc => {
						if (dc is MainViewModel mvm)
						{
							return new ExtensionsViewModel(mvm.Replacements);
						}
						return null;
					})),
					
					new SelectAppConfigurationView().DataContext(x => x.Bind(() => this.DataContext).Convert(dc => {
						if (dc is MainViewModel mvm)
						{
							return new AppConfigurationViewModel(mvm.Replacements);
						}
						return null;
					}))
				)
			)
		);
	}

	private CheckBox CheckBox(string ContentName, bool value )
	   => new CheckBox()
	   .Content(ContentName)
	  // .IsChecked(x => x.Bind(() => value).Mode(BindingMode.TwoWay))
	  .IsChecked(value)
		;

}
