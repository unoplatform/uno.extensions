
namespace TestHarness.Ext.Navigation.Dialogs;

public sealed partial class DialogsBasicFlyout : Page
{
	public DialogsBasicViewModel? ViewModel { get; set; }
	public DialogsBasicFlyout()
	{
		this.InitializeComponent();

		DataContextChanged += BasicFlyout_DataContextChanged;
	}

	private void BasicFlyout_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		this.ViewModel = args.NewValue as DialogsBasicViewModel;
	}
}
