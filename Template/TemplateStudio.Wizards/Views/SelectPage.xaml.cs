using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using TemplateStudio.Wizards.ViewModels;
using TemplateStudio.Wizards.Views;


namespace TemplateStudio.Wizards;

public sealed partial class SelectPage : Page
{

	//private TextBlock TextBlockFileUnoCheck()
	//	  => new TextBlock()
	//	.Width(1024)
	//	.Height(800)
	//	.HorizontalAlignment(HorizontalAlignment.Center)
	//	.Text(Helpers.ProcessCommand.getFileContent());


	public SelectPage()
	{
		//this.InitializeComponent();
		this.DataContext<MainViewModel>((page, vm) => page
		.Content(
				new StackPanel()
				.HorizontalAlignment(HorizontalAlignment.Center)
				.VerticalAlignment(VerticalAlignment.Top)
				.Children(
					//TextBlockFileUnoCheck(),
					new TextBlock()
						.Text(Helpers.ProcessCommand.getFileContent()),

					new SelectPlatformView().DataContext(x => x.Bind(() => this.DataContext).Convert(dc =>
					{
						if (dc is MainViewModel mvm)
						{
							return new ProjectPlatformsViewModel(mvm.Replacements);
						}
						return null;
					}).Source(this)),
					new SelectFeaturesView().DataContext(x => x.Bind(() => this.DataContext).Convert(dc =>
					{
						if (dc is MainViewModel mvm)
						{
							return new FeaturesViewModel(mvm.Replacements);
						}
						return null;
					}).Source(this)),
					new SelectExtensionsView().DataContext(x => x.Bind(() => this.DataContext).Convert(dc =>
					{
						if (dc is MainViewModel mvm)
						{
							return new ExtensionsViewModel(mvm.Replacements);
						}
						return null;
					}).Source(this)),

					new SelectAppConfigurationView().DataContext(x => x.Bind(() => this.DataContext).Convert(dc =>
					{
						if (dc is MainViewModel mvm)
						{
							return new AppConfigurationViewModel(mvm.Replacements);
						}
						return null;
					}).Source(this))
				)
			)
		);
	}

	private CheckBox CheckBox(string ContentName, bool value)
	   => new CheckBox()
	   .Content(ContentName)
	  // .IsChecked(x => x.Bind(() => value).Mode(BindingMode.TwoWay))
	  .IsChecked(value)
		;

}
