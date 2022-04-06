
namespace Playground.Views;

public sealed partial class SimpleDialog : ContentDialog
{
	public SimpleDialog()
	{
		this.InitializeComponent();

		DataContextChanged += SimpleDialog_DataContextChanged;
	}

	private void SimpleDialog_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		var vm = args.NewValue as SimpleViewModel;
	}

	private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
	{
	}

	private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
	{
	}
}
