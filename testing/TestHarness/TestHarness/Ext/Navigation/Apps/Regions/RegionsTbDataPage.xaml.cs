namespace TestHarness.Ext.Navigation.Apps.Regions;

public sealed partial class RegionsTbDataPage : Page
{
	public RegionsTbDataPageViewModel ViewModel => (RegionsTbDataPageViewModel)DataContext;

	public RegionsTbDataPage()
	{
		this.InitializeComponent();
	}
}
