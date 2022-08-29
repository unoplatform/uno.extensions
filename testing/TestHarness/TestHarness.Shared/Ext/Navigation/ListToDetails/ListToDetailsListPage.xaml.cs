
namespace TestHarness.Ext.Navigation.ListToDetails;

public sealed partial class ListToDetailsListPage : Page
{
	public ListToDetailsListPage()
	{
		this.InitializeComponent();

		
	}

	private void SelectSecondItemClick(object sender, RoutedEventArgs e)
	{
		WidgetList.SelectedIndex = 1;
	}
}
