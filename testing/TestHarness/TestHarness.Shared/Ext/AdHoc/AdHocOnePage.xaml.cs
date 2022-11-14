
namespace TestHarness.Ext.AdHoc;

public sealed partial class AdHocOnePage : Page
{
	public AdHocOneViewModel? ViewModel => DataContext as AdHocOneViewModel;
	public AdHocOnePage()
	{
		this.InitializeComponent();
	}

}
