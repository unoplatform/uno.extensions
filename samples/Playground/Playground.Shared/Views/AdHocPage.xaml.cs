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
		if (args.NewValue != null)
		{
			var bvm = args.NewValue as BindableAdHocViewModel;
			ViewModel = bvm?.Model as AdHocViewModel;
		}
	}
}
