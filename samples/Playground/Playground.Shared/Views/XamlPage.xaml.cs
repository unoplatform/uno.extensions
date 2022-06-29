namespace Playground.Views;

public sealed partial class XamlPage : Page
{
	public XamlViewModel? ViewModel { get; private set; }

	public XamlPage()
	{
		this.InitializeComponent();

		DataContextChanged += XamlPage_DataContextChanged;
	}

	private void XamlPage_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		ViewModel = args.NewValue as XamlViewModel;
	}
}
