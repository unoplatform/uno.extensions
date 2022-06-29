
namespace Playground.Views;

public sealed partial class BasicFlyout : Page
{
	public BasicViewModel? ViewModel { get; set; }
	public BasicFlyout()
	{
		this.InitializeComponent();

		DataContextChanged += BasicFlyout_DataContextChanged;
	}

	private void BasicFlyout_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		this.ViewModel = args.NewValue as BasicViewModel;
	}
}
