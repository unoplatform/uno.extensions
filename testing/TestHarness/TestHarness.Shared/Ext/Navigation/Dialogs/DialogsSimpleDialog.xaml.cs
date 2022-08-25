
namespace TestHarness.Ext.Navigation.Dialogs;

public sealed partial class DialogsSimpleDialog : ContentDialog
{
	public DialogsSimpleDialog()
	{
		this.InitializeComponent();

		DataContextChanged += DialogsSimpleDialog_DataContextChanged;
	}

	private void DialogsSimpleDialog_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		var vm = args.NewValue as DialogsSimpleViewModel;
	}

	private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
	{
	}

	private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
	{
	}
}
