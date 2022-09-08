
namespace TestHarness.Ext.Navigation.Storage;

public sealed partial class StorageOnePage : Page
{
	public StorageOneViewModel? ViewModel => DataContext as StorageOneViewModel;
	public StorageOnePage()
	{
		this.InitializeComponent();
	}

}
