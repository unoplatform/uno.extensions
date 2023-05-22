
namespace TestHarness.Ext.AdHoc;

// Uncomment this and the NavigationBar Title won't display
//[ForceUpdate(false)]
[ReactiveBindable(false)]
public sealed partial class AdHocOnePage : Page
{
	public AdHocOneViewModel? ViewModel => DataContext as AdHocOneViewModel;
	public AdHocOnePage()
	{
		this.InitializeComponent();
	}

}
