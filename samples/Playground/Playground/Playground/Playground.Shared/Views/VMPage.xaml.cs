namespace Playground.Views;

public sealed partial class VMPage : Page
{
	public VMViewModel? ViewModel { get; private set; }


	public VMPage()
	{
		this.InitializeComponent();

		DataContextChanged += VMPage_DataContextChanged;
	}

	private void VMPage_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		ViewModel = args.NewValue as VMViewModel;
	}
}
