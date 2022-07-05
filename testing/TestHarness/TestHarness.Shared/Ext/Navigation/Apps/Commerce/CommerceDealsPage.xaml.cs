
using System.Diagnostics;

namespace TestHarness.Ext.Navigation.Apps.Commerce;

public sealed partial class CommerceDealsPage : Page
{
	public CommerceDealsViewModel? ViewModel => DataContext as CommerceDealsViewModel;
	public CommerceDealsPage()
	{
		this.InitializeComponent();
	}

	private void ResponsiveStateChanged(object sender, VisualStateChangedEventArgs e)
	{
		Debug.WriteLine("State Changed");
	}

}
