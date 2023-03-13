
namespace Playground.Views;

public sealed partial class AdHocPage : Page
{
	public AdHocViewModel? ViewModel { get; private set; }

	public AdHocPage()
	{
		this.InitializeComponent();

		DataContextChanged += XamlPage_DataContextChanged;
	}

	private void XamlPage_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		ViewModel = args.NewValue as AdHocViewModel;
	}
}
