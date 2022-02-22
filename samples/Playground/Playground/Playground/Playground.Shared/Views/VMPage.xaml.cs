// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Playground.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
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
